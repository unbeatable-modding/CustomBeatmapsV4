using System;
using System.Collections.Generic;
using System.IO;
using CustomBeatmaps.Util;
using UnityEngine.SceneManagement;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.CustomPackages
{
    public class OSUSongManager
    {
        private readonly List<Song> _beatmaps = new List<Song>();
        private string _folderOverride;
        private int _category;
        private string _config { get; set; }

        public string WarningMessages;

        private FileSystemWatcher _watcher;

        public Song[] OsuSongs
        {
            get
            {
                lock (_beatmaps)
                {
                    return _beatmaps.ToArray();
                }
            }
        }

        public string Error { get; private set; }

        public void SetOverride(ref string folderOverride, int category)
        {
            if (!string.Equals(_folderOverride, folderOverride, StringComparison.Ordinal))
            {
                _folderOverride = folderOverride;
            }

            _config = folderOverride;
            _category = category;

            // Clear previous watcher
            if (_watcher != null)
            {
                _watcher.Dispose();
            }

            string folder = _folderOverride;
            try
            {
                // Watch for changes
                _watcher = FileWatchHelper.WatchFolder(folder, true, evt => Reload());
            }
            catch (Exception e)
            {
                EventBus.ExceptionThrown?.Invoke(new DirectoryNotFoundException($"Can't find folder: {folder}", e));
            }

            // Reload now
            Reload();
        }

        private void Reload()
        {
            if (_folderOverride == null)
                return;
            lock (_beatmaps)
            {
                string folder = _folderOverride;
                var bmaps = OSUHelper.LoadOsuBeatmaps(folder, _category, out WarningMessages);
                if (bmaps != null)
                {
                    _beatmaps.Clear();
                    _beatmaps.AddRange(bmaps);
                    if (SceneManager.GetActiveScene().name == "ArcadeModeMenu")
                    {
                        ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                    }
                    Error = null;
                }
                else
                {
                    Error = $"Can't find OSU songs path at {folder}";
                    EventBus.ExceptionThrown?.Invoke(new DirectoryNotFoundException(Error));
                }
            }
        }
    }
}
