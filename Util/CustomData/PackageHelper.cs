using System;
using System.Collections.Generic;
using System.Text;
using Arcade.UI;
using System.Text.RegularExpressions;
using UnityEngine;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using System.IO;
using System.Linq;
using HarmonyLib;
using Rhythm;
using static Rhythm.BeatmapIndex;
using Discord;

namespace CustomBeatmaps.Util.CustomData
{
    public static class PackageHelper
    {

        // TODO: make this not hardcoded
        public static readonly Category[] customCategories = {
            new Category("LOCAL", "Local songs", 7),
            new Category("submissions", "temp songs", 8),
            new Category("osu", "click the circle", 9),
            new Category("server", "online", 10)
            };

        public static string GetLocalBeatmapDirectory()
        {
            // Path of the game exe
            var dataDir = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            // Get the directory of the custom songs
            var songDir = $"{dataDir}/{Config.Mod.UserPackagesDir}";
            return songDir;
        }

        public static string GetWhiteLabelBeatmapDirectory()
        {
            // Path of the game exe
            var test = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            var dataDir = test.Substring(0, test.LastIndexOf('/'));
            // Get the directory of the custom songs
            var songDir = $"{dataDir}/UNBEATABLE [white label]/";
            return songDir;
        }

        public static bool TryLoadLocalPackage(string packageFolder, string outerFolderPath, out CustomPackageLocal package, int category , bool recursive = false,
            Action<BeatmapException> onBeatmapFail = null, Func<HashSet<string>> songNames = null)
        {
            //ScheduleHelper.SafeLog($"{tmpPkg.Count}");
            package = new CustomPackageLocal();
            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start slash
            // We also only want the stub (lowest directory)
            string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));
            package.FolderName = rootSubFolder;
            ScheduleHelper.SafeLog($"{packageFolder.Substring(AppDomain.CurrentDomain.BaseDirectory.Length)}");

            var songs = new Dictionary<string, SongData>();

            foreach (string packageSubFile in recursive ? Directory.EnumerateFiles(packageFolder, "*.*", SearchOption.AllDirectories) : Directory.EnumerateFiles(packageFolder))
            {
                ScheduleHelper.SafeLog($"    {packageSubFile.Substring(packageFolder.Length)}");
                if (BeatmapHelper.IsBeatmapFile(packageSubFile))
                {
                    try
                    {
                        //var toLoad = new CustomSongInfo(packageSubFile, category);
                        //AddSongToList(toLoad, ref songs, tmpPkg);
                        //ScheduleHelper.SafeInvoke(() => { });
                        var bmapInfo = new BeatmapData(packageSubFile, category);
                        bmapInfo.TryAttachSong(ref songs, songNames);
                    }
                    catch (BeatmapException e)
                    {
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    BEATMAP FAIL: {e.Message}"));
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
                package.PkgSongs = songs.Values.ToList();
                return true;
            }

            // Empty
            package = new CustomPackageLocal();
            return false;
        }

        public static CustomPackage[] LoadLocalPackages(string folderPath, int category, Action<CustomPackageLocal> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
        {
            folderPath = Path.GetFullPath(folderPath);

            var result = new List<CustomPackage>();
            var songNames = new HashSet<string>();
            Func<HashSet<string>> getNames = () => { return songNames; };

            ScheduleHelper.SafeLog("step A");

            // Folders = packages
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                CustomPackageLocal potentialNewPackage;
                if (TryLoadLocalPackage(subDir, folderPath, out potentialNewPackage, category, false, onBeatmapFail, getNames))
                {
                    onLoadPackage?.Invoke(potentialNewPackage);
                    // forcing SafeInvoke so things can see eachother properly
                    // no i will not elaborate
                    ScheduleHelper.SafeInvoke(() => {
                        //potentialNewPackage.PkgSongs.ForEach(s => songs.Add(s.InternalName, null));
                        
                    });
                    foreach (var s in potentialNewPackage.PkgSongs)
                    {
                        songNames.Add(s.InternalName);
                    }
                    result.Add(potentialNewPackage);
                    //potentialNewPackage.PkgSongs.ForEach(s => songs.Add(s.InternalName, null));
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

        public static int EstimatePackageCount(string folderPath)
        {
            return Directory.GetDirectories(folderPath).Length + Directory.GetFiles(folderPath).Length;
        }
    }
}
