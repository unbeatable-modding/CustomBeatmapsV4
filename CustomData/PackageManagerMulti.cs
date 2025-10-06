using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerMulti : PackageManagerGeneric<CustomPackageLocal>
    {
        public PackageManagerMulti(Action<BeatmapException> onLoadException) : base(onLoadException)
        {
        }

        private List<string> _folders = new();

        private Dictionary<string, FileSystemWatcher> _watchers = new();

        public override void ReloadAll()
        {
            if (!_folders.Any())
                return;
            
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                lock (_packages)
                {
                    InitialLoadState.Loading = true;
                    InitialLoadState.Loaded = 0;
                    InitialLoadState.Total = 0;
                    _folders.ForEach(f => InitialLoadState.Total += PackageHelper.EstimatePackageCount(f));
                    //InitialLoadState.Total += PackageHelper.EstimatePackageCount(_folders[0]);
                    ScheduleHelper.SafeLog($"RELOADING ALL PACKAGES FROM {_folders[0]}");

                    _packages.Clear();
                    var packages = PackageHelper.LoadLocalPackagesMulti(_folders, _category, loadedPackage =>
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

                ScheduleHelper.SafeInvoke(() =>
                {
                    Songs.ForEach(s => s.Song.GetTexture());
                    ArcadeHelper.LoadCustomSongs();
                });
            }).Start();
        }

        public void GenerateCorePackages()
        {
            if (!_folders.Any())
                return;
            ScheduleHelper.SafeLog($"LOADING CORES");
            lock (_packages)
            {
                Task.Run(async () =>
                {
                    foreach (var folder in _folders)
                        await PackageHelper.PopulatePackageCoresNew(folder);
                }).Wait();


            }
        }

        protected override void UpdatePackage(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            // Remove old package if there was one and update
            lock (_packages)
            {
                int toRemove = _packages.FindIndex(check => check.BaseDirectory == folderPath);
                if (toRemove != -1)
                    _packages.RemoveAt(toRemove);
            }

            // ???
            if (!_folders.Where(f => folderPath.Contains(f)).Any())
                return;
            var folder = _folders.Where(f => folderPath.Contains(f)).First();

            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
            {
                if (PackageHelper.TryLoadLocalPackage(subDir, folder, out CustomPackageLocal package, _category, false,
                    _onLoadException, () => { return Packages.Select(s => s.GUID).ToHashSet(); }))
                {
                    ScheduleHelper.SafeInvoke(() => package.SongDatas.ForEach(s => s.Song.GetTexture()));
                    ScheduleHelper.SafeLog($"UPDATING PACKAGE: {subDir}");
                    lock (_packages)
                    {
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
                    ScheduleHelper.SafeLog($"CANNOT find package: {subDir}");
                }
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

        public void SetFolders(string[] folders, CCategory category)
        {
            _category = category;

            foreach (var f in folders)
            {
                if (f == null)
                    continue;
                string folder = Path.GetFullPath(f);
                if (_folders.Contains(folder))
                    continue;

                _folders.Add(folder);


                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                // Clear previous watcher
                if (_watchers.TryGetValue(folder, out var watcher))
                {
                    watcher.Dispose();
                    _watchers.Remove(folder);
                }

            }

            GenerateCorePackages();

            // Watch for changes
            foreach (var f in _folders)
            {
                if (!_watchers.TryAdd(f, FileWatchHelper.WatchFolder(f, true, OnFileChange)))
                    ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning($"FAILED TO MAKE {f} A WATCHER"));
            }

            ReloadAll();
        }

        public override void SetFolder(string folder, CCategory category)
        {
            if (folder == null)
                return;
            folder = Path.GetFullPath(folder);
            if (_folders.Contains(folder))
                return;
            
            _folders.Add(folder);
            _category = category;
            

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            
            // Clear previous watcher
            if (_watchers.TryGetValue(folder, out var watcher))
            {
                watcher.Dispose();
                _watchers.Remove(folder);
            }

            //GenerateCorePackages();

            // Watch for changes
            _watchers.Add(folder, FileWatchHelper.WatchFolder(folder, true, OnFileChange));
            //_watcher = FileWatchHelper.WatchFolder(folder, true, OnFileChange);
            // Reload now
            //ReloadAll();
        }

        protected override void OnFileChange(FileSystemEventArgs evt)
        {
            string changedFilePath = Path.GetFullPath(evt.FullPath);

            // ???
            if (!_folders.Where(f => changedFilePath.Contains(f)).Any())
                return;
            var folder = _folders.Where(f => changedFilePath.Contains(f)).First();

            // The root folder within the packages folder we consider to be a "package"
            string basePackageFolder = Path.GetFullPath(Path.Combine(folder, StupidMissingTypesHelper.GetPathRoot(changedFilePath.Substring(folder.Length + 1))));

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

                    })
                        .ContinueWith(task => {
                            // VERY hacky
                            task.RunSynchronously();
                            ArcadeHelper.ReloadArcadeList();
                        });

                }
            }

        }

        public override List<CustomPackageLocal> Packages
        {
            get
            {
                if (InitialLoadState.Loading)
                {
                    return new List<CustomPackageLocal>();
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
