using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CustomBeatmaps.Util;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util.CustomData;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerLocal : PackageManagerGeneric
    {
        public PackageManagerLocal(Action<BeatmapException> onLoadException) : base(onLoadException)
        {
        }
        public override void ReloadAll()
        {
            if (_folder == null)
                return;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                lock (_packages)
                {
                    InitialLoadState.Loading = true;
                    InitialLoadState.Loaded = 0;
                    InitialLoadState.Total = PackageHelper.EstimatePackageCount(_folder);
                    ScheduleHelper.SafeLog($"RELOADING ALL PACKAGES FROM {_folder}");

                    _packages.Clear();
                    var packages = PackageHelper.LoadLocalPackages(_folder, _category, loadedPackage =>
                    {
                        InitialLoadState.Loaded++;
                    }, _onLoadException);
                    ScheduleHelper.SafeLog($"(step 2)");
                    _packages.AddRange(packages);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Clear();
                        foreach (var package in _packages)
                        {
                            _downloadedFolders.Add(Path.GetFullPath(package.BaseDirectory));
                        }
                    }
                    InitialLoadState.Loading = false;
                }
            }).Start();

            //GenerateCorePackages();
        }

        public void GenerateCorePackages()
        {
            if (_folder == null)
                return;
            ScheduleHelper.SafeLog($"LOADING CORES");
            lock (_packages)
            {
                Task.Run(async () =>
                {
                    await PackageHelper.PopulatePackageCoresNew(_folder);
                }).Wait();
                

            }
        }

        protected override void UpdatePackage(string folderPath)
        {
            // Remove old package if there was one and update
            lock (_packages)
            {
                int toRemove = _packages.FindIndex(check => check.BaseDirectory == folderPath);
                if (toRemove != -1)
                    _packages.RemoveAt(toRemove);
            }

            if (PackageHelper.TryLoadLocalPackage(folderPath, _folder, out CustomPackageLocal package, _category, true,
                    _onLoadException, () => { return CustomBeatmaps.LocalUserPackages.Packages.Select(s => s.GUID).ToHashSet(); } ))
            {
                ScheduleHelper.SafeInvoke(() => package.SongDatas.ForEach(s => s.Song.GetTexture()));
                ScheduleHelper.SafeLog($"UPDATING PACKAGE: {folderPath}");
                lock (_packages)
                {
                    // Remove old package if there was one and update
                    //int toRemove = _packages.FindIndex(check => check.FolderName == package.FolderName);
                    //if (toRemove != -1)
                    //    _packages.RemoveAt(toRemove);
                    _packages.Add(package);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Add(Path.GetFullPath(package.BaseDirectory));
                    }
                }
                PackageUpdated?.Invoke(package);
            }
            else
            {
                ScheduleHelper.SafeLog($"CANNOT find package: {folderPath}");
                PackageUpdated?.Invoke(package);
            }
        }

        protected override void RemovePackage(string folderPath)
        {
            lock (_packages)
            {
                string fullPath = Path.GetFullPath(folderPath);
                int toRemove = _packages.FindIndex(check => check.BaseDirectory == fullPath);
                if (toRemove != -1)
                {
                    var p = _packages[toRemove];
                    _packages.RemoveAt(toRemove);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Remove(fullPath);
                    }

                    ScheduleHelper.SafeLog($"REMOVED PACKAGE: {fullPath}");
                    PackageUpdated?.Invoke(p);
                }
                else
                {
                    ScheduleHelper.SafeLog($"CANNOT find package to remove: {folderPath}");
                }
            }
        }

        public override bool PackageExists(string folder)
        {
            lock (_downloadedFolders)
            {
                string targetFullPath = Path.GetFullPath(folder);
                return _downloadedFolders.Contains(targetFullPath);
            }
        }

        public override void SetFolder(string folder, CCategory category)
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

        protected override void OnFileChange(FileSystemEventArgs evt)
        {
            string changedFilePath = Path.GetFullPath(evt.FullPath);
            // The root folder within the packages folder we consider to be a "package"
            string basePackageFolder = Path.GetFullPath(Path.Combine(_folder, StupidMissingTypesHelper.GetPathRoot(changedFilePath.Substring(_folder.Length + 1))));

            ScheduleHelper.SafeLog($"Base Package Folder IN LOCAL: {basePackageFolder}");

            // Special case: Root package folder is deleted, we delete a package.
            if (evt.ChangeType == WatcherChangeTypes.Deleted && basePackageFolder == changedFilePath)
            {
                ScheduleHelper.SafeLog($"Local Package DELETE: {basePackageFolder}");
                RemovePackage(basePackageFolder);
                return;
            }

            ScheduleHelper.SafeLog($"Local Package Change: {evt.ChangeType}: {basePackageFolder} ");

            lock (_loadQueue)
            {
                // We should refresh queued packages in bulk.
                bool isFirst = _loadQueue.Count == 0;
                if (!_loadQueue.Contains(basePackageFolder))
                {
                    //ScheduleHelper.SafeLog($"adding {basePackageFolder} to queue");
                    _loadQueue.Enqueue(basePackageFolder);
                }

                if (isFirst)
                {
                    // Wait for potential other loads to come in
                    Task.Run(async () =>
                    {
                        await Task.Delay(400);
                        RefreshQueuedPackages();

                    }).ContinueWith(task => ScheduleHelper.SafeInvoke(() => {
                        _loadQueue.Clear();
                        ArcadeHelper.ReloadArcadeList();
                    }));

                }
            }

        }

        public override List<CustomPackage> Packages
        {
            get
            {
                if (InitialLoadState.Loading)
                {
                    return new List<CustomPackage>();
                }
                lock (_packages)
                {
                    return _packages;
                }
            }
        }

        public override List<SongData> Songs
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

    }
}
