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
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.Util.CustomData
{
    public static class PackageServerHelper
    {

        public static CustomPackage[] LoadServerPackages(string folderPath, int category, Action<CustomPackage> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
        {
            folderPath = Path.GetFullPath(folderPath);

            var result = new List<CustomPackage>();
            var songNames = new HashSet<string>();
            Func<HashSet<string>> getNames = () => { return songNames; };

            ScheduleHelper.SafeLog("step A (Loading Locally)");

            // Folders = packages
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                CustomPackageServer potentialNewPackage;
                if (TryLoadLocalServerPackage(subDir, folderPath, out potentialNewPackage, category, false, onBeatmapFail, getNames))
                {
                    onLoadPackage?.Invoke(potentialNewPackage);
                    // forcing SafeInvoke so things can see eachother properly
                    // no i will not elaborate
                    ScheduleHelper.SafeInvoke(() => {
                        //potentialNewPackage.PkgSongs.ForEach(s => songs.Add(s.InternalName, null));

                    });
                    foreach (var s in potentialNewPackage.SongDatas)
                    {
                        songNames.Add(s.InternalName);
                    }
                    result.Add(potentialNewPackage);
                    //potentialNewPackage.PkgSongs.ForEach(s => songs.Add(s.InternalName, null));
                }
            }

            ScheduleHelper.SafeLog("step B (Loading Online)");
            
            FetchOnlinePackageList(CustomBeatmaps.BackendConfig.ServerPackageList).ContinueWith(r =>
            {
                if (r.Exception != null)
                {
                    EventBus.ExceptionThrown?.Invoke(r.Exception);
                    return;
                }
                OnlinePackage[] oPkgList = r.Result;
                foreach (var opkg in oPkgList)
                {
                    if (TryLoadOnlineServerPackage(opkg, out var potentialNewPackage, category, onBeatmapFail, getNames))
                    {
                        onLoadPackage?.Invoke(potentialNewPackage);
                        foreach (var s in potentialNewPackage.SongDatas)
                        {
                            songNames.Add(s.InternalName);
                        }
                        result.Add(potentialNewPackage);
                    }
                }
            });
            
            ScheduleHelper.SafeLog($"LOADED {result.Count} PACKAGES");
            ScheduleHelper.SafeLog($"####### FULL PACKAGES LIST: #######\n{result.Join(delimiter: "\n")}");

            return result.ToArray();
        }

        public static bool TryLoadLocalServerPackage(string packageFolder, string outerFolderPath, out CustomPackageServer package, int category, bool recursive = false,
            Action<BeatmapException> onBeatmapFail = null, Func<HashSet<string>> songNames = null)
        {
            //ScheduleHelper.SafeLog($"{tmpPkg.Count}");
            package = new CustomPackageServer();
            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start slash
            // We also only want the stub (lowest directory)
            string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));
            package.BaseDirectory = rootSubFolder;
            ScheduleHelper.SafeLog($"{packageFolder.Substring(AppDomain.CurrentDomain.BaseDirectory.Length)}");

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
                        var offset = 0;
                        foreach (var songsSubFile in pkgCore.Songs)
                        {
                            foreach (var song in songsSubFile)
                            {
                                var bmapInfo = new BeatmapData($"{pkgCore.GUID}-{offset}", song.Key, $"{packageFolder}\\{song.Value}", category);
                                bmapInfo.TryAttachSong(ref songs, songNames);
                            }
                            offset++;
                        }

                        // Set using core data if it exists
                        if (pkgCore.Name != null)
                            package.Name = pkgCore.Name;
                        if (pkgCore.Mappers != null)
                            package.Mappers = pkgCore.Mappers;
                        if (pkgCore.Artists != null)
                            package.Artists = pkgCore.Artists;
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

            // Empty
            package = new CustomPackageServer();
            return false;
        }

        public static bool TryLoadOnlineServerPackage(OnlinePackage oPkg, out CustomPackageServer package, int category,
            Action<BeatmapException> onBeatmapFail = null, Func<HashSet<string>> songNames = null)
        {
            package = new CustomPackageServer();
            var songs = new Dictionary<string, SongData>();

            foreach (var b in oPkg.Beatmaps.Values)
            {
                var bmapInfo = new BeatmapData(b, category);
                bmapInfo.TryAttachSong(ref songs, songNames);
            }


            // This folder has some beatmaps!
            if (songs.Any())
            {
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
