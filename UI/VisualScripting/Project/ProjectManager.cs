using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Manages visual script project lifecycle
    /// </summary>
    public class ProjectManager
    {
        private VisualScriptProject? _currentProject;
        private bool _isDirty = false;
        private DateTime _lastSaveTime = DateTime.Now;

        /// <summary>
        /// Currently loaded project
        /// </summary>
        public VisualScriptProject? CurrentProject
        {
            get => _currentProject;
            private set
            {
                _currentProject = value;
                ProjectChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Whether the current project has unsaved changes
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    DirtyStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Whether a project is currently open
        /// </summary>
        public bool HasProject => _currentProject != null;

        /// <summary>
        /// Get current project file path
        /// </summary>
        public string? CurrentProjectPath => _currentProject?.FilePath;

        /// <summary>
        /// Get current project name
        /// </summary>
        public string? CurrentProjectName => _currentProject?.Name;

        /// <summary>
        /// Get current project directory
        /// </summary>
        public string? CurrentProjectDirectory => _currentProject?.GetProjectDirectory();

        // Events
        public event EventHandler? ProjectOpened;
        public event EventHandler? ProjectSaved;
        public event EventHandler? ProjectClosed;
        public event EventHandler? ProjectChanged;
        public event EventHandler? DirtyStateChanged;

        /// <summary>
        /// Create a new blank project
        /// </summary>
        public VisualScriptProject NewProject(string name = "Untitled", ExperienceLevel experienceMode = ExperienceLevel.Beginner)
        {
            var project = new VisualScriptProject
            {
                Name = name,
                Created = DateTime.Now,
                Modified = DateTime.Now,
                ExperienceMode = experienceMode,
                Canvas = new CanvasState
                {
                    Zoom = 1.0,
                    OffsetX = 0,
                    OffsetY = 0
                }
            };

            CurrentProject = project;
            IsDirty = false;
            _lastSaveTime = DateTime.Now;

            ProjectOpened?.Invoke(this, EventArgs.Empty);
            return project;
        }

        /// <summary>
        /// Open an existing project from disk
        /// </summary>
        public VisualScriptProject OpenProject(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Project folder not found: {folderPath}");
            }

            var project = ProjectSerializer.LoadProject(folderPath);
            CurrentProject = project;
            IsDirty = false;
            _lastSaveTime = DateTime.Now;

            ProjectOpened?.Invoke(this, EventArgs.Empty);
            return project;
        }

        /// <summary>
        /// Save the current project to its existing location
        /// </summary>
        public void SaveProject(List<NodeBase> nodes, List<Wire> wires,
            string? author = null, string? description = null)
        {
            if (CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently open");
            }

            string folderPath;

            if (string.IsNullOrEmpty(CurrentProject.FilePath))
            {
                throw new InvalidOperationException("Project has no file path. Use SaveProjectAs instead.");
            }

            folderPath = CurrentProject.GetProjectDirectory()!;

            SaveProjectInternal(folderPath, nodes, wires, author, description);
        }

        /// <summary>
        /// Save the current project to a new location
        /// </summary>
        public void SaveProjectAs(string folderPath, List<NodeBase> nodes, List<Wire> wires,
            string? author = null, string? description = null)
        {
            if (CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently open");
            }

            SaveProjectInternal(folderPath, nodes, wires, author, description);
        }

        /// <summary>
        /// Internal save logic
        /// </summary>
        private void SaveProjectInternal(string folderPath, List<NodeBase> nodes, List<Wire> wires,
            string? author, string? description)
        {
            if (CurrentProject == null)
                return;

            // Update canvas state if provided
            if (CurrentProject.Canvas == null)
            {
                CurrentProject.Canvas = new CanvasState();
            }

            // Save the project
            ProjectSerializer.SaveProject(CurrentProject, folderPath, nodes, wires, author, description);

            IsDirty = false;
            _lastSaveTime = DateTime.Now;

            ProjectSaved?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Close the current project
        /// </summary>
        /// <param name="checkForUnsavedChanges">If true, will throw exception if there are unsaved changes</param>
        public void CloseProject(bool checkForUnsavedChanges = true)
        {
            if (CurrentProject == null)
                return;

            if (checkForUnsavedChanges && IsDirty)
            {
                throw new InvalidOperationException("Cannot close project with unsaved changes");
            }

            var oldProject = CurrentProject;
            CurrentProject = null;
            IsDirty = false;

            ProjectClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Mark the current project as modified
        /// </summary>
        public void MarkDirty()
        {
            if (CurrentProject != null)
            {
                CurrentProject.MarkModified();
                IsDirty = true;
            }
        }

        /// <summary>
        /// Get time since last save
        /// </summary>
        public TimeSpan TimeSinceLastSave => DateTime.Now - _lastSaveTime;

        /// <summary>
        /// Update canvas state
        /// </summary>
        public void UpdateCanvasState(double zoom, double offsetX, double offsetY)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Canvas.Zoom = zoom;
                CurrentProject.Canvas.OffsetX = offsetX;
                CurrentProject.Canvas.OffsetY = offsetY;
                MarkDirty();
            }
        }

        /// <summary>
        /// Set project metadata
        /// </summary>
        public void SetMetadata(string key, object value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.SetMetadata(key, value);
                MarkDirty();
            }
        }

        /// <summary>
        /// Get project metadata
        /// </summary>
        public T GetMetadata<T>(string key, T defaultValue = default!)
        {
            if (CurrentProject == null)
                return defaultValue;
            return CurrentProject.GetMetadata(key, defaultValue);
        }

        /// <summary>
        /// Get display title for the project (includes dirty indicator)
        /// </summary>
        public string GetDisplayTitle()
        {
            if (CurrentProject == null)
                return "Visual Scripting";

            var title = CurrentProject.Name ?? "Untitled";
            if (IsDirty)
                title += " *";

            return title;
        }

        /// <summary>
        /// Check if a path is a valid project directory
        /// </summary>
        public static bool IsValidProjectDirectory(string path)
        {
            return ProjectSerializer.IsValidProjectFolder(path);
        }

        /// <summary>
        /// Get quick project info without full load
        /// </summary>
        public static (string name, DateTime modified, int nodeCount)? GetProjectInfo(string folderPath)
        {
            return ProjectSerializer.GetProjectInfo(folderPath);
        }
    }
}
