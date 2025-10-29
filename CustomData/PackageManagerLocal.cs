using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Directory = Pri.LongPath.Directory;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerLocal : PackageManagerGeneric<CustomPackageLocal>
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

                ArcadeHelper.LoadCustomSongs();
                ScheduleHelper.SafeInvoke(() => Songs.ForEach(s => s.Song.GetTexture()));

            }).Start();
        }

        protected override void UpdatePackage(string folderPath)
        {
            // Remove old package if there was one
            lock (_packages)
            {
                int toRemove = _packages.FindIndex(p => p.BaseDirectory == folderPath);
                if (toRemove != -1)
                {
                    _packages.RemoveAt(toRemove);
                    lock (_downloadedFolders)
                        _downloadedFolders.Remove(folderPath);
                }
            }

            if (!Directory.Exists(folderPath))
            {
                // Reload here as a failsafe
                PackageUpdated?.Invoke();
                ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                return;
            }
                

            // Weird fix for also getting the top folder
            List<string> dirs = Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories).ToList();
            dirs.Add(folderPath);

            foreach (string subDir in dirs)
            {
                if (PackageHelper.TryLoadLocalPackage(subDir, _folder, out CustomPackageLocal package, _category, false,
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
                    PackageUpdated?.Invoke();
                    ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
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
                    PackageUpdated?.Invoke();
                    ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
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
                _dontLoad.Add(basePackageFolder);
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
                    });

                }
            }

        }

    }
}
