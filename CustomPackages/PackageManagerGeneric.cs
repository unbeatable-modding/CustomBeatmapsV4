using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;

namespace CustomBeatmaps.CustomPackages
{
    /// <summary>
    /// To stop myself from losing my mind, most things for packages should be defined here
    /// </summary>
    public abstract class PackageManagerGeneric
    {
        /// <summary>
        /// Action that is invoked after a package is updated
        /// </summary>
        public Action<CustomPacakage> PackageUpdated;
        public string Folder { get; protected set; }

        protected readonly List<CustomPacakage> _packages = new List<CustomPacakage>();
        protected readonly HashSet<string> _downloadedFolders = new HashSet<string>();

        protected readonly Action<BeatmapException> _onLoadException;

        protected readonly Queue<string> _loadQueue = new Queue<string>();

        protected string _folder;
        protected int _category;

        protected FileSystemWatcher _watcher;

        public InitialLoadStateData InitialLoadState { get; protected set; } = new InitialLoadStateData();
        
        /*
        public class InitialLoadStateData
        {
            public bool Loading;
            public int Loaded;
            public int Total;
        }
        */

        public PackageManagerGeneric(Action<BeatmapException> onLoadException)
        {
            _onLoadException = onLoadException;
        }

        protected abstract void ReloadAll();
        protected abstract void UpdatePackage(string folderPath = null);
        protected abstract void RemovePackage(string folderPath);
        
        /// <summary>
        /// List of all Packages this manager can see
        /// </summary>
        public virtual List<CustomPacakage> Packages { get; private set; }
        /// <summary>
        /// List of all Songs inside all Packages this manager can see
        /// (Songs contain beatmaps)
        /// </summary>
        public virtual List<SongData> Songs { get; private set; }

        public abstract bool PackageExists(string folder);

        public abstract void SetFolder(string folder, int category);

        protected abstract void OnFileChange(FileSystemEventArgs evt);

        protected void RefreshQueuedPackages()
        {
            while (true)
            {
                lock (_loadQueue)
                {
                    if (_loadQueue.Count <= 0)
                        break;
                    UpdatePackage(_loadQueue.Dequeue());
                }
            }
        }
    }
}
