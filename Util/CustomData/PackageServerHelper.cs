using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.Util.CustomData
{
    public static class PackageServerHelper
    {

        public static CustomPackageServer[] LoadServerPackages(string folderPath, CCategory category, List<OnlinePackage> opkgs,
            Action<CustomPackage> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
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

            var pkgs = new Dictionary<Guid, CustomPackageServer>();
            Func<Dictionary<Guid, CustomPackageServer>> getPkgs = () => { return pkgs; };


            // Get online Packages first (if we can)
            ScheduleHelper.SafeLog("step A (Loading Online)");
            foreach (var opkg in opkgs)
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
            Action<BeatmapException> onBeatmapFail = null, Func<Dictionary<Guid, CustomPackageServer>> getPkgs = null)
        {
            package = new CustomPackageServer();

            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start slash
            // We also only want the stub (lowest directory)
            string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));


            package.BaseDirectory = rootSubFolder;
            package.Time = Directory.GetLastWriteTime(packageFolder);
            package.DownloadStatus = BeatmapDownloadStatus.Downloaded;

            ScheduleHelper.SafeLog($"{relative}\\");

            var songs = new Dictionary<string, SongData>();

            var subFiles = recursive ?
                Directory.EnumerateFiles(packageFolder, "*", SearchOption.AllDirectories) :
                Directory.EnumerateFiles(packageFolder, "*");

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
                                var bmapInfo = new BeatmapData(pkgCore.GUID, i, song.Key, $"{packageFolder}\\{song.Value}", category);
                                
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

                        // We want to attach to an existing package (if we can)
                        if (getPkgs != null)
                        {
                            if (getPkgs.Invoke().TryGetValue(pkgCore.GUID, out CustomPackageServer pkgFetch) && songs.Any())
                            {
                                pkgFetch.SongDatas = songs.Values.ToList();
                                pkgFetch.DownloadStatus = BeatmapDownloadStatus.Downloaded;
                                pkgFetch.BaseDirectory = package.BaseDirectory;
                                package = new CustomPackageServer();
                                return false;
                            }
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
            Action<BeatmapException> onBeatmapFail = null, Func<Dictionary<Guid, CustomPackageServer>> GUIDs = null)
        {
            package = new CustomPackageServer();

            // ???
            if (GUIDs != null && GUIDs.Invoke().ContainsKey(oPkg.GUID))
            {
                package = new CustomPackageServer();
                onBeatmapFail.Invoke(new BeatmapException("Online Package is Duplicate???", oPkg.ServerURL));
                return false;
            }

            var songs = new Dictionary<string, SongData>();

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

            if (oPkg.Name != null)
                package.Name = oPkg.Name;
            if (oPkg.Mappers != null)
                package.Mappers = oPkg.Mappers;
            if (oPkg.Artists != null)
                package.Artists = oPkg.Artists;

            // This folder has some beatmaps!
            if (songs.Any())
            {
                //ScheduleHelper.SafeLog("Loading ONLINE");
                package.SongDatas = songs.Values.ToList();
                return true;
            }

            // Package already exists locally
            package = new CustomPackageServer();
            return false;
        }

        public static async Task<OnlinePackage[]> FetchOnlinePackageList(string url)
        {
            try
            {
                var pkgList = await FetchHelper.GetJSON<OnlinePackageList>(url);
                return pkgList.Packages;
            }
            catch
            {
                return [];
            }
        }

        private struct OnlinePackageList
        {
            [JsonProperty("packages")]
            public OnlinePackage[] Packages;
        }

        /// <summary>
        /// Downloads a package from a server URL locally
        /// </summary>
        /// <param name="packageDownloadURL"> Hosted Directory above the package location ex. http://64.225.60.116:8080  </param>
        /// <param name="serverPackageRoot"> Hosted Directory within above directory ex. packages, creating http://64.225.60.116:8080/packages) </param>
        /// <param name="localServerPackageDirectory"> Local directory to save packages ex. SERVER_PACKAGES </param>
        /// <param name="serverPackageURL">The url from the server (https or "packages/{something}.zip"</param>
        /// <param name="callback"> Returns the local path of the downloaded file </param>
        public static async Task DownloadPackage(CustomPackageServer pkg, string packageDownloadURL, string localServerPackageDirectory)
        {
            string serverDownloadURL = (packageDownloadURL + "/" + pkg.ServerURL);
            string localDownloadExtractPath = Path.Combine(localServerPackageDirectory, pkg.GUID.ToString());

            await DownloadPackageInner(serverDownloadURL, localDownloadExtractPath);
        }

        private static bool _dealingWithTempFile;

        private static async Task DownloadPackageInner(string downloadURL, string targetFolder)
        {
            ScheduleHelper.SafeLog($"Downloading package from {downloadURL} to {targetFolder}");

            string tempDownloadFilePath = ".TEMP.zip";

            // Impromptu mutex, as per usual.
            // Only let one download handle the temporary file at a time.
            while (_dealingWithTempFile)
            {
                Thread.Sleep(200);
            }

            _dealingWithTempFile = true;
            try
            {
                await FetchHelper.DownloadFile(downloadURL, tempDownloadFilePath);

                // Extract
                ZipHelper.ExtractToDirectory(tempDownloadFilePath, targetFolder);
                // Delete old
                File.Delete(tempDownloadFilePath);
            }
            catch (Exception)
            {
                _dealingWithTempFile = false;
                throw;
            }
            _dealingWithTempFile = false;
        }

        [Obsolete]
        public static void FindPackageFromServer(string serverPackageURL, string beatmapRelativeKeyPath)
        {
            beatmapRelativeKeyPath = beatmapRelativeKeyPath.Replace('/', '\\');
        }

        // TODO: change server fetching
        [Obsolete]
        private static string GetPackageFolderIdFromServerPackageURL(string serverPackageURL)
        {
            string endingName = Path.GetFileName(serverPackageURL);
            return endingName;
        }

        [Obsolete]
        public static string GetLocalFolderFromServerPackageURL(string localServerPackageDirectory, string serverPackageURL)
        {
            string packageFolderId = GetPackageFolderIdFromServerPackageURL(serverPackageURL);
            return Path.Combine(localServerPackageDirectory, packageFolderId);
        }

    }
}
