using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Tracks and manages recently opened visual script projects
    /// </summary>
    public class RecentProjectsService
    {
        private readonly List<RecentProjectEntry> _recentProjects = new();
        private const int MaxRecentProjects = 10;

        /// <summary>
        /// Event raised when recent projects list changes
        /// </summary>
        public event EventHandler? RecentsChanged;

        /// <summary>
        /// Add a project to the recent projects list
        /// </summary>
        public void AddRecent(string path, string name)
        {
            // Remove if already exists (to move to top)
            _recentProjects.RemoveAll(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

            // Add to top of list
            _recentProjects.Insert(0, new RecentProjectEntry
            {
                Path = path,
                Name = name,
                LastOpened = DateTime.Now
            });

            // Limit to max entries
            while (_recentProjects.Count > MaxRecentProjects)
            {
                _recentProjects.RemoveAt(_recentProjects.Count - 1);
            }

            RecentsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get all recent projects
        /// </summary>
        public List<RecentProjectEntry> GetRecentProjects()
        {
            // Filter out projects that no longer exist
            var existing = _recentProjects
                .Where(p => Directory.Exists(p.Path))
                .ToList();

            // If we filtered any out, update the list
            if (existing.Count != _recentProjects.Count)
            {
                _recentProjects.Clear();
                _recentProjects.AddRange(existing);
                RecentsChanged?.Invoke(this, EventArgs.Empty);
            }

            return new List<RecentProjectEntry>(_recentProjects);
        }

        /// <summary>
        /// Remove a project from recents
        /// </summary>
        public void RemoveRecent(string path)
        {
            var removed = _recentProjects.RemoveAll(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                RecentsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Clear all recent projects
        /// </summary>
        public void ClearRecents()
        {
            if (_recentProjects.Count > 0)
            {
                _recentProjects.Clear();
                RecentsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Load recent projects from serialized data
        /// </summary>
        public void LoadFromList(List<RecentProjectData> data)
        {
            _recentProjects.Clear();

            foreach (var item in data.Take(MaxRecentProjects))
            {
                _recentProjects.Add(new RecentProjectEntry
                {
                    Path = item.Path,
                    Name = item.Name,
                    LastOpened = item.LastOpened
                });
            }

            RecentsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Save recent projects to serializable format
        /// </summary>
        public List<RecentProjectData> SaveToList()
        {
            return _recentProjects.Select(p => new RecentProjectData
            {
                Path = p.Path,
                Name = p.Name,
                LastOpened = p.LastOpened
            }).ToList();
        }

        /// <summary>
        /// Check if a project is in recents
        /// </summary>
        public bool Contains(string path)
        {
            return _recentProjects.Any(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get the most recent project
        /// </summary>
        public RecentProjectEntry? GetMostRecent()
        {
            return _recentProjects.FirstOrDefault();
        }
    }

    /// <summary>
    /// Recent project entry with metadata
    /// </summary>
    public class RecentProjectEntry
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime LastOpened { get; set; }

        /// <summary>
        /// Get additional project info (lazy loaded)
        /// </summary>
        public (DateTime modified, int nodeCount)? GetProjectInfo()
        {
            var info = ProjectSerializer.GetProjectInfo(Path);
            if (info.HasValue)
            {
                return (info.Value.modified, info.Value.nodeCount);
            }
            return null;
        }

        /// <summary>
        /// Get friendly last opened text
        /// </summary>
        public string GetLastOpenedText()
        {
            var span = DateTime.Now - LastOpened;

            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} days ago";

            return LastOpened.ToShortDateString();
        }
    }

    /// <summary>
    /// Serializable recent project data for settings storage
    /// </summary>
    public class RecentProjectData
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime LastOpened { get; set; }
    }
}
