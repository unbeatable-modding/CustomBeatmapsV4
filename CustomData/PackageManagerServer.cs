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
                    // Don't fetch for beta update
                    _onlinePackages = new();
                    //_onlinePackages = PackageServerHelper.FetchOnlinePackageList(CustomBeatmaps.BackendConfig.ServerPackageList).Result.ToList();
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

                ArcadeHelper.ReloadArcadeList();
                ScheduleHelper.SafeInvoke(() =>
                    {
                        Songs.ForEach(s =>
                        {
                            if (s.Local)
                                s.Song.GetTexture();
                        });
                    });
                
            }).Start();
        }

        protected override void UpdatePackage(string folderPath)
        {
            try
            {
                // Remove old package if there was one
                lock (_packages)
                {
                    var toRemove = _packages.FirstOrDefault(p => p.BaseDirectory == folderPath);
                    if (toRemove != null)
                    {
                        RemovePackage(toRemove);
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
                List<string> dirs = Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories).ToList();
                dirs.Add(folderPath);

                foreach (string subDir in dirs)
                {
                    if (PackageServerHelper.TryLoadLocalServerPackage(subDir, _folder, out CustomPackageServer package, _category, false,
                    _onLoadException, () => { return Packages.Where(p => p.DownloadStatus == BeatmapDownloadStatus.Downloaded).ToDictionary(p => p.GUID); } ))
                    {
                        ScheduleHelper.SafeInvoke(() => package.SongDatas.ForEach(s => s.Song.GetTexture()));
                        ScheduleHelper.SafeLog($"UPDATING PACKAGE: {subDir}");
                        lock (_packages)
                        {
                            
                            // Use online data if we can find it
                            if (OnlinePackages.Any(o => o.GUID == package.GUID))
                            {
                                var opkg = OnlinePackages.First(o => o.GUID == package.GUID);
                                package.ServerURL = opkg.ServerURL;
                                package.Time = opkg.UploadTime;

                                var toReplace = _packages.FindIndex(o => o.GUID == package.GUID);
                                _packages[toReplace] = package;
                            }
                            // Add like normal otherwise
                            else
                            {
                                _packages.Add(package);
                            }
                            
                            //_packages.Add(package);

                            lock (_downloadedFolders)
                            {
                                if (package.DownloadStatus == BeatmapDownloadStatus.Downloaded)
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
                
            } catch (Exception e)
            {
                ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError(e));
            }
            
        }

        protected override void RemovePackage(string folderPath)
        {
            try
            {
                lock (_packages)
                {
                    int toRemove = _packages.FindIndex(check => check.BaseDirectory == folderPath);
                    if (toRemove != -1)
                    {
                        var p = _packages[toRemove];
                        _packages.RemoveAt(toRemove);
                        lock (_downloadedFolders)
                            _downloadedFolders.Remove(folderPath);

                        if (OnlinePackages.Exists(o => o.GUID == p.GUID) &&
                            PackageServerHelper.TryLoadOnlineServerPackage(OnlinePackages.First(o => o.GUID == p.GUID), out var package, _category, _onLoadException))
                        {
                            _packages.Add(package);
                        }

                        ScheduleHelper.SafeLog($"REMOVED PACKAGE: {folderPath}");
                        PackageUpdated?.Invoke();
                        ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                    }
                    else
                    {
                        ScheduleHelper.SafeLog($"CANNOT find package to remove: {folderPath}");
                    }
                }
            }
            catch (Exception e)
            {
                CustomBeatmaps.Log.LogError(e);
            } 
        }

        protected void RemovePackage(CustomPackageServer pkg)
        {
            lock (_packages)
            {
                if (_packages.Exists(check => check.BaseDirectory == pkg.BaseDirectory))
                {
                    _packages.Remove(pkg);
                    lock (_downloadedFolders)
                        _downloadedFolders.Remove(pkg.BaseDirectory);

                    if (OnlinePackages.Exists(o => o.GUID == pkg.GUID) &&
                        PackageServerHelper.TryLoadOnlineServerPackage(OnlinePackages.First(o => o.GUID == pkg.GUID), out var package, _category, _onLoadException))
                    {
                        _packages.Add(package);
                    }

                    PackageUpdated?.Invoke();
                    ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                }
                else
                {
                    // ???
                    ScheduleHelper.SafeLog($"Package doesn't exist???: {pkg.BaseDirectory}");
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

            ScheduleHelper.SafeLog($"Base Package Folder IN SERVER: {basePackageFolder}");

            // Special case: Root package folder is deleted, we delete a package.
            if (evt.ChangeType == WatcherChangeTypes.Deleted && basePackageFolder == changedFilePath)
            {
                ScheduleHelper.SafeLog($"Server Package DELETE: {basePackageFolder}");
                RemovePackage(basePackageFolder);
                _dontLoad.Add(basePackageFolder);
                return;
            }

            ScheduleHelper.SafeLog($"Server Package Change: {evt.ChangeType}: {basePackageFolder} ");

            lock (_loadQueue)
            {
                // We should refresh queued packages in bulk.
                bool isFirst = _loadQueue.Count == 0;
                if (!_loadQueue.Contains(basePackageFolder))
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


    }
}
