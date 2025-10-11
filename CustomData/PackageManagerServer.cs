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
using UnityEngine;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerServer : PackageManagerGeneric<CustomPackageServer>
    {
        public PackageManagerServer(Action<BeatmapException> onLoadException) : base(onLoadException)
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
                    _onlinePackages = PackageServerHelper.FetchOnlinePackageList(CustomBeatmaps.BackendConfig.ServerPackageList).Result.ToList();
                    var packages = PackageServerHelper.LoadServerPackages(_folder, _category, OnlinePackages, loadedPackage =>
                    {
                        InitialLoadState.Loaded++;
                    }, _onLoadException).ToList();
                    ScheduleHelper.SafeLog($"(step 2)");
                    _packages.AddRange(packages);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Clear();
                        foreach (var package in _packages)
                        {
                            if (package.DownloadStatus != BeatmapDownloadStatus.Downloaded)
                                continue;
                            _downloadedFolders.Add(Path.GetFullPath(package.BaseDirectory));
                        }
                    }
                    InitialLoadState.Loading = false;
                }

                ScheduleHelper.SafeInvoke(() =>
                {
                    Songs.ForEach(s =>
                    {
                        if (s.Local)
                            s.Song.GetTexture();
                    });
                    ArcadeHelper.LoadCustomSongs();
                });
            }).Start();
        }

        protected override void UpdatePackage(string folderPath)
        {
            ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning($"USING {folderPath} to check"));
            try
            {
                // Remove old package if there was one and update
                lock (_packages)
                {
                    int toRemove = _packages.FindIndex(p => p.BaseDirectory == folderPath);
                    if (toRemove != -1)
                        _packages.RemoveAt(toRemove);
                }

                if (!Directory.Exists(folderPath))
                        return;

                foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
                {
                    if (PackageServerHelper.TryLoadLocalServerPackage(subDir, _folder, out CustomPackageServer package, _category, false,
                     _onLoadException, null))
                    //_onLoadException, () => _packages.ToDictionary(p => p.GUID)))
                    {
                        

                        ScheduleHelper.SafeInvoke(() => package.SongDatas.ForEach(s => s.Song.GetTexture()));
                        ScheduleHelper.SafeLog($"UPDATING PACKAGE: {subDir}");
                        lock (_packages)
                        {
                            /*
                            if (OnlinePackages.Any(o => o.GUID == package.GUID))
                            {
                                var opkg = OnlinePackages.First(o => o.GUID == package.GUID);
                                package.ServerURL = opkg.ServerURL;
                                package.Time = opkg.UploadTime;

                                var toReplace = _packages.FindIndex(o => o.GUID == package.GUID);
                                _packages.RemoveAt(toReplace);
                                _packages.Add(package);
                                //_packages[toReplace] = package;
                            }
                            else
                            {
                                _packages.Add(package);
                            }
                            */

                            _packages.Add(package);

                            lock (_downloadedFolders)
                            {
                                if (package.DownloadStatus == BeatmapDownloadStatus.Downloaded)
                                    _downloadedFolders.Add(Path.GetFullPath(package.BaseDirectory));
                            }
                        }
                        PackageUpdated?.Invoke(package);
                        Task.Run(() => ArcadeHelper.ReloadArcadeList());
                    }
                    else
                    {
                        ScheduleHelper.SafeLog($"CANNOT find package: {subDir}");
                    }
                }
                
            } catch (Exception e)
            {
                ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError(e));
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
                    //_packages.RemoveAt(toRemove);
                    //string.IsNullOrEmpty(p.ServerURL);

                    if (PackageServerHelper.TryLoadOnlineServerPackage(OnlinePackages.First(o => o.GUID == p.GUID), out var package, _category, _onLoadException))
                    {
                        _packages[toRemove] = package;
                    }
                    else
                    {
                        _packages.RemoveAt(toRemove);
                    }

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

                    })
                        .ContinueWith(task => {
                            // VERY hacky
                            //task.RunSynchronously();
                            //ArcadeHelper.ReloadArcadeList();
                        });

                }
            }

        }

        public static void FindPackageFromServer(string serverPackageURL, string beatmapRelativeKeyPath)
        {
            beatmapRelativeKeyPath = beatmapRelativeKeyPath.Replace('/', '\\');
        }

        // TODO: change server fetching
        private static string GetPackageFolderIdFromServerPackageURL(string serverPackageURL)
        {
            string endingName = Path.GetFileName(serverPackageURL);
            return endingName;
        }

        public static string GetLocalFolderFromServerPackageURL(string localServerPackageDirectory, string serverPackageURL)
        {
            string packageFolderId = GetPackageFolderIdFromServerPackageURL(serverPackageURL);
            return Path.Combine(localServerPackageDirectory, packageFolderId);
        }

        private List<OnlinePackage> _onlinePackages;
        public List<OnlinePackage> OnlinePackages => _onlinePackages;

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
    }
}
