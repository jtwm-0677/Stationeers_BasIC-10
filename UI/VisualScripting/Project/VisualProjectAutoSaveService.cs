using System;
using System.IO;
using System.Windows.Threading;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Auto-save service specifically for visual script projects
    /// Saves to .autosave subfolder and provides recovery
    /// </summary>
    public class VisualProjectAutoSaveService
    {
        private readonly DispatcherTimer _timer;
        private readonly ProjectManager _projectManager;
        private Func<(List<NodeBase> nodes, List<Wire> wires)>? _getGraphDataFunc;
        private bool _enabled = true;
        private int _intervalMinutes = 5;
        private bool _hasChanges = false;

        /// <summary>
        /// Auto-save interval in minutes
        /// </summary>
        public int IntervalMinutes
        {
            get => _intervalMinutes;
            set
            {
                _intervalMinutes = Math.Max(1, value);
                _timer.Interval = TimeSpan.FromMinutes(_intervalMinutes);
            }
        }

        /// <summary>
        /// Enable or disable auto-save
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled)
                    _timer.Start();
                else
                    _timer.Stop();
            }
        }

        /// <summary>
        /// Event raised when auto-save completes
        /// </summary>
        public event EventHandler? AutoSaveCompleted;

        /// <summary>
        /// Event raised when auto-save fails
        /// </summary>
        public event EventHandler<string>? AutoSaveFailed;

        public VisualProjectAutoSaveService(ProjectManager projectManager)
        {
            _projectManager = projectManager;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(_intervalMinutes)
            };
            _timer.Tick += Timer_Tick;

            // Subscribe to project manager events
            _projectManager.DirtyStateChanged += (s, e) =>
            {
                _hasChanges = _projectManager.IsDirty;
            };
        }

        /// <summary>
        /// Set the function to retrieve current graph data
        /// </summary>
        public void SetGraphDataProvider(Func<(List<NodeBase> nodes, List<Wire> wires)> getGraphDataFunc)
        {
            _getGraphDataFunc = getGraphDataFunc;
        }

        /// <summary>
        /// Start the auto-save timer
        /// </summary>
        public void Start()
        {
            if (_enabled)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Stop the auto-save timer
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            PerformAutoSave();
        }

        /// <summary>
        /// Perform an auto-save operation
        /// </summary>
        public void PerformAutoSave()
        {
            // Don't save if no changes or no project
            if (!_hasChanges || _projectManager.CurrentProject == null || _getGraphDataFunc == null)
                return;

            try
            {
                var projectDir = _projectManager.CurrentProjectDirectory;
                if (string.IsNullOrEmpty(projectDir))
                {
                    // Project hasn't been saved yet, can't auto-save
                    return;
                }

                // Create .autosave subfolder
                var autoSaveDir = Path.Combine(projectDir, ".autosave");
                Directory.CreateDirectory(autoSaveDir);

                // Get current graph data
                var (nodes, wires) = _getGraphDataFunc();

                // Create a copy of the project for auto-save
                var autoSaveProject = CloneProject(_projectManager.CurrentProject);
                autoSaveProject.FilePath = Path.Combine(autoSaveDir, "visual.json");

                // Save to autosave directory
                ProjectSerializer.SaveProject(
                    autoSaveProject,
                    autoSaveDir,
                    nodes,
                    wires,
                    null,
                    null);

                // Create timestamp file
                var timestampPath = Path.Combine(autoSaveDir, "timestamp.txt");
                File.WriteAllText(timestampPath, DateTime.Now.ToString("O"));

                AutoSaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                AutoSaveFailed?.Invoke(this, $"Auto-save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if an auto-save exists for a project
        /// </summary>
        public static bool HasAutoSave(string projectDir)
        {
            var autoSaveDir = Path.Combine(projectDir, ".autosave");
            var autoSavePath = Path.Combine(autoSaveDir, "visual.json");
            return File.Exists(autoSavePath);
        }

        /// <summary>
        /// Get auto-save timestamp
        /// </summary>
        public static DateTime? GetAutoSaveTimestamp(string projectDir)
        {
            try
            {
                var autoSaveDir = Path.Combine(projectDir, ".autosave");
                var timestampPath = Path.Combine(autoSaveDir, "timestamp.txt");

                if (File.Exists(timestampPath))
                {
                    var timestampStr = File.ReadAllText(timestampPath);
                    return DateTime.Parse(timestampStr);
                }

                // Fallback to file modification time
                var autoSavePath = Path.Combine(autoSaveDir, "visual.json");
                if (File.Exists(autoSavePath))
                {
                    return File.GetLastWriteTime(autoSavePath);
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }

        /// <summary>
        /// Recover from auto-save
        /// </summary>
        public static VisualScriptProject RecoverFromAutoSave(string projectDir)
        {
            var autoSaveDir = Path.Combine(projectDir, ".autosave");
            return ProjectSerializer.LoadProject(autoSaveDir);
        }

        /// <summary>
        /// Delete auto-save data
        /// </summary>
        public static void DeleteAutoSave(string projectDir)
        {
            try
            {
                var autoSaveDir = Path.Combine(projectDir, ".autosave");
                if (Directory.Exists(autoSaveDir))
                {
                    Directory.Delete(autoSaveDir, recursive: true);
                }
            }
            catch
            {
                // Ignore deletion errors
            }
        }

        /// <summary>
        /// Cleanup old auto-saves (keep only the most recent)
        /// </summary>
        public static void CleanupOldAutoSaves(string projectDir)
        {
            // Currently we only keep one auto-save per project
            // This method is here for future expansion
        }

        /// <summary>
        /// Clone a project for auto-save
        /// </summary>
        private VisualScriptProject CloneProject(VisualScriptProject original)
        {
            return new VisualScriptProject
            {
                Version = original.Version,
                Name = original.Name,
                Created = original.Created,
                Modified = DateTime.Now,
                Canvas = new CanvasState
                {
                    Zoom = original.Canvas.Zoom,
                    OffsetX = original.Canvas.OffsetX,
                    OffsetY = original.Canvas.OffsetY
                },
                ExperienceMode = original.ExperienceMode,
                Metadata = new Dictionary<string, object>(original.Metadata)
            };
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _timer.Stop();
        }
    }
}
