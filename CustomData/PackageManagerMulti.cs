using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using HarmonyLib;
using System;
using System.Collections;
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
        public PackageManagerMulti(Action<BeatmapException> onLoadException) : base(onLoadException) { }

        private string[] _folders = [];

        private FileSystemWatcher[] _watchers = [];

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
                    foreach (var f in _folders)
                        InitialLoadState.Total += PackageHelper.EstimatePackageCount(f);
                    //_folders.ForEach(f => InitialLoadState.Total += PackageHelper.EstimatePackageCount(f));
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
                        await PackageHelper.PopulatePackageCores(folder);
                }).Wait();


            }
        }

        protected override void UpdatePackage(string folderPath)
        {
            // Remove old package if there was one and update
            lock (_packages)
            {
                if (_packages.Exists(p => p.BaseDirectory == folderPath))
                    RemovePackage(folderPath);
            }

            if (!Directory.Exists(folderPath))
                return;

            // ???
            if (!_folders.ToList().Exists(f => folderPath.Contains(f)))
            {
                ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning("folderPath not real???"));
                return;
            }
                
            var folder = _folders.ToList().First(f => folderPath.Contains(f));

            // Weird fix for also getting the top folder
            List<string> dirs = Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories).ToList();
            dirs.Add(folderPath);

            foreach (string subDir in dirs)
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
                    ArcadeHelper.ReloadArcadeList();
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
                //string fullPath = Path.GetFullPath(folderPath);
                int toRemove = _packages.FindIndex(check => check.BaseDirectory == folderPath);
                if (toRemove != -1)
                {
                    var p = _packages[toRemove];
                    _packages.RemoveAt(toRemove);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Remove(folderPath);
                    }

                    ScheduleHelper.SafeLog($"REMOVED PACKAGE: {folderPath}");
                    ArcadeHelper.ReloadArcadeList();
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
            if (folders == null || folders.Length < 1)
                return;

            _category = category;

            // Clear previous watchers
            foreach (var w in _watchers)
                w.Dispose();
            _watchers = [];

            folders.First(f => true);

            for (var i = 0; i < folders.Length; i++)
            {
                folders[i] = Path.GetFullPath(folders[i]);

                if (!Directory.Exists(folders[i]))
                    Directory.CreateDirectory(folders[i]);
            }

            _folders = folders;

            GenerateCorePackages();

            // Watch for changes
            _watchers = new FileSystemWatcher[_folders.Length];
            for (var i = 0; i < _folders.Length; i++)
            {
                _watchers[i] = FileWatchHelper.WatchFolder(_folders[i], true, OnFileChange);
            }

            ReloadAll();
        }

        public override void SetFolder(string folder, CCategory category)
        {
            SetFolders([folder], category);
        }

        protected override void OnFileChange(FileSystemEventArgs evt)
        {
            string changedFilePath = Path.GetFullPath(evt.FullPath);

            // ???
            if (!_folders.First(f => changedFilePath.Contains(f)).Any())
                return;
            var folder = _folders.First(f => changedFilePath.Contains(f));

            // The root folder within the packages folder we consider to be a "package"
            string basePackageFolder = Path.GetFullPath(Path.Combine(folder, StupidMissingTypesHelper.GetPathRoot(changedFilePath.Substring(folder.Length + 1))));

            ScheduleHelper.SafeLog($"Base Package Folder IN LOCAL: {basePackageFolder}");

            // Special case: Root package folder is deleted, we delete a package.
            if (evt.ChangeType == WatcherChangeTypes.Deleted && basePackageFolder == changedFilePath)
            {
                ScheduleHelper.SafeLog($"Local Package DELETE: {basePackageFolder}");
                RemovePackage(basePackageFolder);
                _dontLoad.Add(basePackageFolder);
                return;
            }

            ScheduleHelper.SafeLog($"Local Package Change: {evt.ChangeType}: {basePackageFolder} ");

            lock (_loadQueue)
            {
                // We should refresh queued packages in bulk.
                bool isFirst = _loadQueue.Count == 0;
                if (!_loadQueue.Contains(basePackageFolder) && !_dontLoad.Contains(basePackageFolder))
                {
                    _loadQueue.Enqueue(basePackageFolder);
                }

                if (isFirst)
                {
                    // Wait for potential other loads to come in
                    Task.Run(async () =>
                    {
                        await Task.Delay(400);
                        RefreshQueuedPackages();

                    });

                }
            }

        }

    }
}
