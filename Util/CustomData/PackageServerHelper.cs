using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Rhythm.BeatmapIndex;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.Util.CustomData
{
    public static class PackageServerHelper
    {

        public static CustomPackage[] LoadServerPackages(string folderPath, CCategory category, Action<CustomPackage> onLoadPackage = null, 
            Action<BeatmapException> onBeatmapFail = null)
        {
            /*  Intended order for reference:
             *  1. Fetch all server packages and make them into a list
             *  2. If we fail to fetch for any reason use steps 'b', otherwise proceed with steps 'a'
             *  
             *  a1. Turn server packages into local ones without any real Songs or Beatmaps
             *  a2.
             * 
             */
            folderPath = Path.GetFullPath(folderPath);

            var pkgs = new Dictionary<Guid, CustomPackage>();
            Func<Dictionary<Guid, CustomPackage>> getPkgs = () => { return pkgs; };


            // Get online Packages first (if we can)
            ScheduleHelper.SafeLog("step A (Loading Online)");
            var r = FetchOnlinePackageList(CustomBeatmaps.BackendConfig.ServerPackageList);
            foreach (var opkg in r.Result)
            {
                if (TryLoadOnlineServerPackage(opkg, out var potentialNewPackage, category, onBeatmapFail, getPkgs))
                {
                    pkgs.TryAdd(potentialNewPackage.GUID, potentialNewPackage);
                    onLoadPackage?.Invoke(potentialNewPackage);
                }
            }


            // Packages = .bmap files
            ScheduleHelper.SafeLog("step B (Loading Locally)");
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                if (TryLoadLocalServerPackage(subDir, folderPath, out CustomPackageServer potentialNewPackage, category, false, onBeatmapFail, getPkgs))
                {
                    pkgs.TryAdd(potentialNewPackage.GUID, potentialNewPackage);
                    onLoadPackage?.Invoke(potentialNewPackage);
                }
            }

            ScheduleHelper.SafeLog($"LOADED {pkgs.Count} PACKAGES");
            ScheduleHelper.SafeLog($"####### FULL PACKAGES LIST: #######\n{pkgs.Values.Join(delimiter: "\n")}");

            return pkgs.Values.ToArray();
        }

        public static bool TryLoadLocalServerPackage(string packageFolder, string outerFolderPath, out CustomPackageServer package, CCategory category, bool recursive = false,
            Action<BeatmapException> onBeatmapFail = null, Func<Dictionary<Guid, CustomPackage>> pkgs = null)
        {
            //ScheduleHelper.SafeLog($"{tmpPkg.Count}");
            package = new CustomPackageServer();
            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start slash
            // We also only want the stub (lowest directory)
            //string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));
            package.BaseDirectory = relative;
            package.Time = Directory.GetLastWriteTime(relative);
            package.DownloadStatus = BeatmapDownloadStatus.Downloaded;
            ScheduleHelper.SafeLog($"{relative}\\");
            //ScheduleHelper.SafeLog($"{packageFolder.Substring(AppDomain.CurrentDomain.BaseDirectory.Length)}");

            var songs = new Dictionary<string, SongData>();

            var subFiles = recursive ?
                Directory.EnumerateFiles(packageFolder, "*.*", SearchOption.AllDirectories) :
                Directory.EnumerateFiles(packageFolder);

            if (subFiles.Where(s => s.ToLower().EndsWith(".bmap")).Any())
            {
                foreach (string packageCoreFile in subFiles.Where(s => s.ToLower().EndsWith(".bmap")))
                {
                    ScheduleHelper.SafeLog($"    {packageCoreFile.Substring(packageFolder.Length)}");
                    try
                    {
                        var pkgCore = SerializeHelper.LoadJSON<PackageCore>(packageCoreFile);
                        package.GUID = pkgCore.GUID;

                        for (var i = 0; i < pkgCore.Songs.Count; i++)
                        {
                            foreach (var song in pkgCore.Songs[i])
                            {
                                var bmapInfo = new BeatmapData($"{pkgCore.GUID}-{i}", song.Key, $"{packageFolder}\\{song.Value}", category);
                                
                                if (songs.TryGetValue(bmapInfo.InternalName, out _))
                                {
                                    if (!songs[bmapInfo.InternalName].TryAddToThisSong(bmapInfo))
                                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning($"FAILED TO ADD BEATMAP \"{bmapInfo.BeatmapPath}\" TO IT'S SONG"));
                                }
                                else
                                {
                                    songs.Add(bmapInfo.InternalName, new SongData(bmapInfo));
                                }
                            }
                        }

                        // Duplicate Package returns false
                        if (pkgs.Invoke().TryGetValue(pkgCore.GUID, out CustomPackage pkgFetch) && songs.Any())
                        {
                            pkgFetch.SongDatas = songs.Values.ToList();
                            pkgFetch.DownloadStatus = BeatmapDownloadStatus.Downloaded;
                            pkgFetch.BaseDirectory = package.BaseDirectory;
                            package = new CustomPackageServer();
                            return false;
                        }

                        // Set using core data if it exists
                        if (pkgCore.Name != null)
                            package.Name = pkgCore.Name;
                        if (pkgCore.Mappers != null)
                            package.Mappers = pkgCore.Mappers;
                        if (pkgCore.Artists != null)
                            package.Artists = pkgCore.Artists;
                    }
                    catch (BeatmapException f)
                    {
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    BEATMAP FAIL: {f.Message}"));
                        onBeatmapFail?.Invoke(f);
                    }
                    catch (Exception f)
                    {
                        BeatmapException e = new BeatmapException("Invalid Package formatting", packageCoreFile);
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    BEATMAP FAIL: {e.Message}"));
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    Exception: {f}"));
                        onBeatmapFail?.Invoke(e);
                    }
                    
                }

            }

            // This folder has some beatmaps!
            if (songs.Any())
            {
                package.SongDatas = songs.Values.ToList();
                return true;
            }

            // Package already exists
            package = new CustomPackageServer();
            return false;
        }

        public static bool TryLoadOnlineServerPackage(OnlinePackage oPkg, out CustomPackageServer package, CCategory category,
            Action<BeatmapException> onBeatmapFail = null, Func<Dictionary<Guid, CustomPackage>> GUIDs = null)
        {
            package = new CustomPackageServer();

            // ???
            if (GUIDs.Invoke().ContainsKey(oPkg.GUID))
            {
                package = new CustomPackageServer();
                onBeatmapFail.Invoke(new BeatmapException("Duplicate Package Guid", oPkg.ServerURL));
            }

            var songs = new Dictionary<string, SongData>();

            package.Name = oPkg.Name;
            package.GUID = oPkg.GUID;
            package.ServerURL = oPkg.ServerURL;
            //package.BaseDirectory = oPkg.ServerURL;
            package.Time = oPkg.UploadTime;
            package.DownloadStatus = BeatmapDownloadStatus.NotDownloaded;

            for (var i = 0; i < oPkg.Songs.Length; i++)
            {
                foreach (var s in oPkg.Songs[i])
                {
                    var bmapInfo = new BeatmapData(s, oPkg.GUID, i, category);
                    if (songs.TryGetValue(bmapInfo.InternalName, out _))
                    {
                        if (!songs[bmapInfo.InternalName].TryAddToThisSong(bmapInfo))
                            ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning($"FAILED TO ADD BEATMAP \"{bmapInfo.BeatmapPath}\" TO IT'S SONG"));
                    }
                    else
                    {
                        songs.Add(bmapInfo.InternalName, new SongData(bmapInfo));
                    }
                }
            }

            // This folder has some beatmaps!
            if (songs.Any())
            {
                ScheduleHelper.SafeLog("Loading ONLINE");
                package.SongDatas = songs.Values.ToList();
                return true;
            }

            // Package already exists locally
            package = new CustomPackageServer();
            return false;
        }

        public static async Task<OnlinePackage[]> FetchOnlinePackageList(string url)
        {
            var pkgList = await FetchHelper.GetJSON<OnlinePackageList>(url);
            return pkgList.Packages;
        }

        private struct OnlinePackageList
        {
            [JsonProperty("packages")]
            public OnlinePackage[] Packages;
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

    }
}
