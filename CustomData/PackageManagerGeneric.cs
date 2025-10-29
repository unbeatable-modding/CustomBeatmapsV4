using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CustomBeatmaps.CustomData
{
    /// <summary>
    /// To stop myself from losing my mind, most things for packages should be defined here
    /// </summary>
    public abstract class PackageManagerGeneric<P>
        where P : CustomPackage
    {
        /// <summary>
        /// Action that is invoked after a package is updated
        /// </summary>
        public Action PackageUpdated;
        
        protected readonly List<P> _packages = new List<P>();
        protected readonly HashSet<string> _downloadedFolders = new HashSet<string>();

        protected readonly Action<BeatmapException> _onLoadException;

        protected readonly Queue<string> _loadQueue = new Queue<string>();

        protected string _folder;
        public string Folder => _folder;

        protected CCategory _category;

        protected FileSystemWatcher _watcher;

        public InitialLoadStateData InitialLoadState { get; protected set; } = new InitialLoadStateData();

        public PackageManagerGeneric(Action<BeatmapException> onLoadException)
        {
            _onLoadException = onLoadException;
        }

        public abstract void ReloadAll();
        protected abstract void UpdatePackage(string folderPath = null);
        protected abstract void RemovePackage(string folderPath);

        /// <summary>
        /// List of all Packages this manager can see
        /// </summary>
        public virtual List<P> Packages
        {
            get
            {
                if (InitialLoadState.Loading)
                {
                    return new List<P>();
                }
                lock (_packages)
                {
                    return _packages;
                }
            }
        }

        /// <summary>
        /// List of all Songs inside all Packages this manager can see
        /// (Songs contain beatmaps)
        /// </summary>
        public virtual List<SongData> Songs
        {
            get
            {
                if (InitialLoadState.Loading)
                {
                    return new List<SongData>();
                }
                lock (_packages)
                {
                    return _packages.SelectMany(p => p.SongDatas).ToList();
                }
            }
        }

        public abstract bool PackageExists(string folder);

        public virtual void SetFolder(string folder, CCategory category)
        {
            if (folder == null)
                return;
            folder = Path.GetFullPath(folder);
            if (folder == _folder)
                return;

            _folder = folder;
            _category = category;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Clear previous watcher
            if (_watcher != null)
            {
                _watcher.Dispose();
            }

            GenerateCorePackages();

            // Watch for changes
            _watcher = FileWatchHelper.WatchFolder(folder, true, OnFileChange);
            // Reload now
            ReloadAll();
        }

        protected abstract void OnFileChange(FileSystemEventArgs evt);

        protected List<string> _dontLoad = new();
        protected virtual void RefreshQueuedPackages()
        {
            while (true)
            {
                lock (_loadQueue)
                {
                    if (_loadQueue.Count <= 0)
                    {
                        _dontLoad.Clear();
                        break;
                    }
                    if (_dontLoad.Contains(_loadQueue.Peek()))
                    {
                        _loadQueue.Dequeue();
                        continue;
                    }
                    UpdatePackage(_loadQueue.Dequeue());
                }
            }
        }

        public virtual void GenerateCorePackages()
        {
            if (_folder == null)
                return;
            ScheduleHelper.SafeLog($"LOADING CORES");
            lock (_packages)
            {
                Task.Run(async () =>
                {
                    await PackageHelper.PopulatePackageCores(_folder);
                }).Wait();
            }
        }


    }
}
