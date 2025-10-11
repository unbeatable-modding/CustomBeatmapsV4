using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CustomBeatmaps.Util;
using static CustomBeatmaps.Util.CustomData.PackageHelper;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.CustomPackages;

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
        public Action<P> PackageUpdated;
        public string Folder { get; protected set; }

        protected readonly List<P> _packages = new List<P>();
        protected readonly HashSet<string> _downloadedFolders = new HashSet<string>();

        protected readonly Action<BeatmapException> _onLoadException;

        protected readonly Queue<string> _loadQueue = new Queue<string>();

        protected string _folder;
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

        public abstract void SetFolder(string folder, CCategory category);

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
