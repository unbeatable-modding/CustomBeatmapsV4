using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CustomBeatmaps.CustomPackages;
using UnityEngine;
using HarmonyLib;
using Rhythm;
using System.Linq;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;

using static Rhythm.BeatmapIndex;
using Arcade.UI;
using CustomBeatmaps.CustomData;


namespace CustomBeatmaps.Util
{
    [Obsolete]
    public static class zzzCustomPackageHelper
    {

        // TODO: make this not hardcoded
        public static readonly Category[] customCategories = {
            new Category("LOCAL", "Local songs", 7),
            new Category("submissions", "temp songs", 8),
            new Category("osu", "click the circle", 9),
            new Category("server", "online", 10)
            };

        public static string GetBeatmapProp(string beatmapText, string prop, string beatmapPath)
        {
            var match = Regex.Match(beatmapText, $"{prop}: *(.+?)\r?\n");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            throw new BeatmapException($"{prop} property not found.", beatmapPath);
        }

        public static string GetBeatmapImage(string beatmapText, string beatmapPath)
        {
            var match = Regex.Match(beatmapText, $"Background and Video events\r?\n.*\"(.+?)\"");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return null;
            //throw new BeatmapException($"Image property not found.", beatmapPath);
        }

        public static void SetBeatmapJson(string beatmapText, TagData data, string beatmapPath)
        {
            data.SongLength = ArcadeBGMManager.SongDuration;
            var beatmapSave = SerializeHelper.SerializeJSON(data);
            var match = Regex.Replace(beatmapText, $"(?<=Tags:)(.+?)\r?\n", beatmapSave + "\r\n");
            File.WriteAllText(beatmapPath, match);
        }

        private static bool IsBeatmapFile(string beatmapPath)
        {
            return beatmapPath.ToLower().EndsWith(".osu");
        }

        public static bool TryLoadLocalPackage(string packageFolder, string outerFolderPath, out CustomPackageLocal package, int category, bool recursive = false,
            Action<BeatmapException> onBeatmapFail = null, List<CustomPackageLocal> tmpPkg = null)
        {
            package = new CustomPackageLocal();
            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start slash
            // We also only want the stub (lowest directory)
            string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));
            package.BaseDirectory = rootSubFolder;
            ScheduleHelper.SafeLog($"{packageFolder.Substring(AppDomain.CurrentDomain.BaseDirectory.Length)}");

            var songs = new Dictionary<string, SongData>();

            foreach (string packageSubFile in recursive ? Directory.EnumerateFiles(packageFolder, "*.*", SearchOption.AllDirectories) : Directory.EnumerateFiles(packageFolder))
            {
                ScheduleHelper.SafeLog($"    {packageSubFile.Substring(packageFolder.Length)}");
                if (IsBeatmapFile(packageSubFile))
                {
                    try
                    {
                        //var toLoad = new CustomSongInfo(packageSubFile, category);
                        //AddSongToList(toLoad, ref songs, tmpPkg);
                        var bmapInfo = new BeatmapData(packageSubFile, category);
                        bmapInfo.TryAttachSong(ref songs);
                    }
                    catch (BeatmapException e)
                    {
                        ScheduleHelper.SafeLog($"    BEATMAP FAIL: {e.Message}");
                        onBeatmapFail?.Invoke(e);
                    }
                    catch (Exception e)
                    {
                        ScheduleHelper.SafeInvoke(() => Debug.LogException(e));
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
            package = new CustomPackageLocal();
            return false;
        }

        public static int EstimatePackageCount(string folderPath)
        {
            return Directory.GetDirectories(folderPath).Length + Directory.GetFiles(folderPath).Length;
        }

        public static CustomPackageLocal[] LoadLocalPackages(string folderPath, int category, Action<CustomPackageLocal> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
        {
            folderPath = Path.GetFullPath(folderPath);

            var result = new List<CustomPackageLocal>();

            ScheduleHelper.SafeLog("step A");

            // Folders = packages
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                CustomPackageLocal potentialNewPackage;
                if (TryLoadLocalPackage(subDir, folderPath, out potentialNewPackage, category, false, onBeatmapFail, result))
                {
                    onLoadPackage?.Invoke(potentialNewPackage);
                    result.Add(potentialNewPackage);
                }
            }

            ScheduleHelper.SafeLog("step B");

            // Files = packages too! For compatibility with V1 (cause why not)
            /*
            foreach (string subFile in Directory.GetFiles(folderPath))
            {
                //if (IsBeatmapFile(subFile))
                if (false)
                {
                    try
                    {
                        //var customBmap = LoadLocalBeatmap(subFile);
                        var newPackage = new CustomLocalPackage();
                        //newPackage.Beatmaps = new[] { customBmap };
                        onLoadPackage?.Invoke(newPackage);
                        result.Add(newPackage);
                    }
                    catch (BeatmapException e)
                    {
                        onBeatmapFail?.Invoke(e);
                    }
                }
            }
            */

            ScheduleHelper.SafeLog($"LOADED {result.Count} PACKAGES");
            ScheduleHelper.SafeLog($"####### FULL PACKAGES LIST: #######\n{result.Join(delimiter: "\n")}");

            return result.ToArray();
        }

        // Package IDs are just the ending zip file name.
        // If we want the server to hold on to external packages, we probably want a custom key system or something to prevent duplicate names... 
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

        public static async Task<CustomServerPackageList> FetchServerPackageList(string url)
        {
            return await FetchHelper.GetJSON<CustomServerPackageList>(url);
        }

        public static async Task<Dictionary<string, ServerSubmissionPackage>> FetchServerSubmissions(string url)
        {
            return await FetchHelper.GetJSON<Dictionary<string, ServerSubmissionPackage>>(url);
        }

        private static string GetURLFromServerPackageURL(string serverDirectory, string serverPackageRoot, string serverPackageURL)
        {
            // In the form "packages/<something>/zip"
            if (serverPackageURL.StartsWith(serverPackageRoot))
            {
                return serverDirectory + "/" + serverPackageURL;
            }
            // Probably an absolute URL
            return serverPackageURL;
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

        /// <summary>
        /// Downloads a package from a server URL locally
        /// </summary>
        /// <param name="packageDownloadURL"> Hosted Directory above the package location ex. http://64.225.60.116:8080  </param>
        /// <param name="serverPackageRoot"> Hosted Directory within above directory ex. packages, creating http://64.225.60.116:8080/packages) </param>
        /// <param name="localServerPackageDirectory"> Local directory to save packages ex. SERVER_PACKAGES </param>
        /// <param name="serverPackageURL">The url from the server (https or "packages/{something}.zip"</param>
        /// <param name="callback"> Returns the local path of the downloaded file </param>
        public static async Task DownloadPackage(string packageDownloadURL, string serverPackageRoot, string localServerPackageDirectory, string serverPackageURL)
        {
            string serverDownloadURL = GetURLFromServerPackageURL(packageDownloadURL, serverPackageRoot, serverPackageURL);
            string localDownloadExtractPath =
                GetLocalFolderFromServerPackageURL(localServerPackageDirectory, serverPackageURL);

            await DownloadPackageInner(serverDownloadURL, localDownloadExtractPath);
        }

        public static async Task DownloadTemporarySubmissionPackage(string downloadURL, string tempSubmissionFolder)
        {
            try
            {
                if (Directory.Exists(tempSubmissionFolder))
                    Directory.Delete(tempSubmissionFolder, true);
            }
            catch (Exception e)
            {
                EventBus.ExceptionThrown?.Invoke(e);
            }
            await DownloadPackageInner(downloadURL, tempSubmissionFolder);
        }

        /// <summary>
        /// Adds Custom Categories into the game
        /// </summary>
        public static void TryAddCustomCategory()
        {
            foreach (var customCategory in customCategories)
            {
                var traverse = Traverse.Create(BeatmapIndex.defaultIndex);
                var categories = traverse.Field("categories").GetValue<List<Category>>();
                var categorySongs = traverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();
                // hidden category is submissions for now
                traverse.Field("hiddenCategory").SetValue(8);

                // Check if the custom category already exists
                if (!categories.Contains(customCategory))
                {
                    // If not, add it to the list
                    categories.Add(customCategory);
                    categorySongs.TryAdd(customCategory, new List<Song>([new Song("LoadBearingSongDoNotDeleteThisSeriously")]));
                    //categorySongs.TryAdd(customCategory, new List<Song>());

                    ScheduleHelper.SafeLog($"Added category {customCategory.Name}");

                }
            }
        }

        /// <summary>
        /// Add a Custom Song to a list with logic
        /// </summary>
        /// <param name="toLoad"> Song to be added to List  </param>
        /// <param name="songs"> List the Song will be added to </param>
        /*
        public static void AddSongToList(CustomSongInfo toLoad, ref List<CustomSongInfo> songs, List<CustomPackageLocal> tmpPkg = null)
        {
            //List<Song>[] toCheck = [songs, GetAllCustomSongs, tmpPkg.SelectMany(p => p.PkgSongs).ToList()];
            List<List<CustomSongInfo>> toCheck = [songs, GetAllCustomSongs];
            // DO NOT TRY TO PARSE TMPPKG IF NULL STOP BREAKING THINGS
            if (tmpPkg != null)
                toCheck.Add(tmpPkg.SelectMany(p => p.PkgSongs).ToList());

            foreach (List<CustomSongInfo> list in toCheck)
            {
                DupeSongChecker(ref toLoad, list);
                //break;
            }

            if (!songs.Any())
            {
                //ScheduleHelper.SafeLog($"not null");
                songs.Add(toLoad);
            }
            else if (songs.Where((Song s) => s.name == toLoad.name).Any())
            {
                // Song we just created has multiple difficulties
                var currentSong = songs.Where((Song s) => s.name == toLoad.name).Single();
                var traverseSong = Traverse.Create(currentSong);
                var _difficulties = traverseSong.Field("_difficulties").GetValue<List<string>>();
                var beatmaps = traverseSong.Field("beatmaps").GetValue<List<BeatmapInfo>>();
                var _beatmaps = traverseSong.Field("_beatmaps").GetValue<Dictionary<string, BeatmapInfo>>();

                beatmaps.Add(toLoad.Beatmaps.Values.ToArray()[0]);
                _beatmaps.Add(toLoad.Difficulties[0], toLoad.Beatmaps.Values.ToArray()[0]);
                _difficulties.Add(toLoad.Difficulties[0]);
            }
            else
            {
                songs.Add(toLoad);
            }
        }
        */

        /// <summary>
        /// Return a list of all Custom Songs
        /// </summary>
        public static List<SongData> GetAllCustomSongs
        {
            get
            {
                var songl = new List<SongData>();
                //songl.AddRange(CustomBeatmaps.LocalUserPackages.SelectMany(p => p.Songs));
                songl.AddRange(CustomBeatmaps.LocalUserPackages.Songs);
                //songl.AddRange(CustomBeatmaps.SubmissionPackageManager.Songs);
                songl.AddRange(CustomBeatmaps.LocalServerPackages.Songs);
                songl.AddRange(CustomBeatmaps.OSUSongManager.Songs);
                return songl;
            }
        }

        /// <summary>
        /// Return a list of all Custom Songs
        /// </summary>
        public static List<CustomSong> GetAllCustomSongInfos
        {
            get
            {
                var songl = new List<CustomSong>();
                //songl.AddRange(CustomBeatmaps.LocalUserPackages.SelectMany(p => p.Songs));
                songl.AddRange(CustomBeatmaps.LocalUserPackages.Songs.Select(s => s.Song));
                //songl.AddRange(CustomBeatmaps.SubmissionPackageManager.Songs);
                songl.AddRange(CustomBeatmaps.LocalServerPackages.Songs.Select(s => s.Song));
                songl.AddRange(CustomBeatmaps.OSUSongManager.Songs.Select(s => s.Song));
                return songl;
            }
        }

    }
}
