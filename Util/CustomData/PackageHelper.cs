using System;
using System.Collections.Generic;
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
using System.Threading.Tasks;
using System.Threading;

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

        /// <summary>
        /// Returns true or false depending on if the out package should be loaded
        /// </summary>
        /// <param name="packageFolder">Folder we are trying to find packages in (ex: ./USER_PACKAGES/ExamplePackage)</param>
        /// <param name="outerFolderPath">Folder associated with the package manager (ex: ./USER_PACKAGES)</param>
        /// <param name="package">The returned package</param>
        /// <param name="category">Category to put the package in</param>
        /// <param name="recursive">Bool to dictate searching for .bmap files in subdirectories</param>
        /// <param name="onBeatmapFail"></param>
        /// <param name="GUIDs">Func to return a set of GUID's currently being used by other packages</param>
        /// <returns></returns>
        public static bool TryLoadLocalPackage(string packageFolder, string outerFolderPath, out CustomPackageLocal package, CCategory category, bool recursive = false,
            Action<BeatmapException> onBeatmapFail = null, Func<HashSet<Guid>> GUIDs = null)
        {
            // Get full paths
            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start 
            // We also only want the stub (lowest directory)
            string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));

            package = new CustomPackageLocal();
            package.BaseDirectory = rootSubFolder;
            package.Time = Directory.GetLastWriteTime(packageFolder);
            ScheduleHelper.SafeLog($"{relative}\\");

            var songs = new Dictionary<string, SongData>();

            var subFiles = recursive ?
                Directory.EnumerateFiles(packageFolder, "*.*", SearchOption.AllDirectories) :
                Directory.EnumerateFiles(packageFolder, "*.*");

            if (subFiles.Where(s => s.ToLower().EndsWith(".bmap")).Any())
            {                
                foreach (string packageCoreFile in subFiles.Where(s => s.ToLower().EndsWith(".bmap")))
                {
                    ScheduleHelper.SafeLog($"    {packageCoreFile.Substring(packageFolder.Length)}");
                    try
                    {
                        var pkgCore = SerializeHelper.LoadJSON<PackageCore>(packageCoreFile);
                        
                        // Duplicate Package Guid returns false
                        if (GUIDs.Invoke().Contains(pkgCore.GUID))
                        {
                            package = new CustomPackageLocal();
                            onBeatmapFail.Invoke(new BeatmapException("Package Guid already exists...", packageCoreFile));
                            return false;
                        }

                        package.GUID = pkgCore.GUID;

                        // Add BeatmapDatas to SongDatas
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
            package = new CustomPackageLocal();
            return false;
        }

        public static CustomPackageLocal[] LoadLocalPackages(string folderPath, CCategory category, 
            Action<CustomPackage> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
        {
            folderPath = Path.GetFullPath(folderPath);

            var result = new List<CustomPackageLocal>();
            var songNames = new HashSet<Guid>();
            Func<HashSet<Guid>> getNames = () => { return songNames; };

            //ScheduleHelper.SafeLog("step A");

            // Folders = packages
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (TryLoadLocalPackage(subDir, folderPath, out var potentialNewPackage, category, false, onBeatmapFail, getNames))
                    {
                        onLoadPackage?.Invoke(potentialNewPackage);
                        // forcing SafeInvoke so things can see eachother properly
                        // no i will not elaborate
                        ScheduleHelper.SafeInvoke(() => { });
                        songNames.Add(potentialNewPackage.GUID);
                        result.Add(potentialNewPackage);
                    }
                }
                catch (Exception e)
                {
                    CustomBeatmaps.Log.LogError(e);
                }
                
            }

            ScheduleHelper.SafeLog("step B");

            ScheduleHelper.SafeLog($"LOADED {result.Count} PACKAGES");
            ScheduleHelper.SafeLog($"####### FULL PACKAGES LIST: #######\n{result.Join(delimiter: "\n")}");

            return result.ToArray();
        }

        public static CustomPackageLocal[] LoadLocalPackagesMulti(string[] folderPaths, CCategory category,
            Action<CustomPackage> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
        {
            var result = new List<CustomPackageLocal>();
            var songNames = new HashSet<Guid>();
            Func<HashSet<Guid>> getNames = () => { return songNames; };

            //ScheduleHelper.SafeLog("step A");

            foreach (string f in folderPaths)
            {
                string folderPath = Path.GetFullPath(f);

                // Folders = packages
                foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (TryLoadLocalPackage(subDir, folderPath, out var potentialNewPackage, category, false, onBeatmapFail, getNames))
                        {
                            onLoadPackage?.Invoke(potentialNewPackage);
                            // forcing SafeInvoke so things can see eachother properly
                            // no i will not elaborate
                            ScheduleHelper.SafeInvoke(() => { });
                            songNames.Add(potentialNewPackage.GUID);
                            result.Add(potentialNewPackage);
                        }
                    }
                    catch (Exception e)
                    {
                        CustomBeatmaps.Log.LogError(e);
                    }

                }
            }

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

        public static HashSet<Guid> GetAllGUIDs
        {
            get
            {
                var songl = new List<Guid>();
                //songl.AddRange(CustomBeatmaps.LocalUserPackages.SelectMany(p => p.Songs));
                songl.AddRange(CustomBeatmaps.LocalUserPackages.Packages.Select(s => s.GUID));
                //songl.AddRange(CustomBeatmaps.SubmissionPackageManager.Songs);
                songl.AddRange(CustomBeatmaps.LocalServerPackages.Packages.Select(s => s.GUID));
                songl.AddRange(CustomBeatmaps.OSUSongManager.Packages.Select(s => s.GUID));
                return songl.ToHashSet();
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
                songl.AddRange(CustomBeatmaps.LocalUserPackages.Songs.Where(s => s.Local).Select(s => s.Song));
                //songl.AddRange(CustomBeatmaps.SubmissionPackageManager.Songs);
                songl.AddRange(CustomBeatmaps.LocalServerPackages.Songs.Where(s => s.Local).Select(s => s.Song));
                songl.AddRange(CustomBeatmaps.OSUSongManager.Songs.Where(s => s.Local).Select(s => s.Song));
                return songl;
            }
        }

        public static int EstimatePackageCount(string folderPath)
        {
            return Directory.GetDirectories(folderPath).Length + Directory.GetFiles(folderPath).Length;
        }

        /// <summary>
        /// Auto generate a new valid PackageCore from a folder
        /// </summary>
        /// <param name="folderPath">Directory to generate package from</param>
        /// <param name="recursive">Bool to check recursively for beatmaps</param>
        /// <returns></returns>
        public static PackageCore GeneratePackageCore(string folderPath, bool recursive = true)
        {
            PackageCore pkgCore = new();
            pkgCore.GUID = Guid.NewGuid();
            pkgCore.Songs = new();

            var subFiles = recursive ?
                Directory.EnumerateFiles(folderPath, "*.osu", SearchOption.AllDirectories) :
                Directory.EnumerateFiles(folderPath, "*.osu", SearchOption.TopDirectoryOnly);

            var songs = new List<string>();
            
            foreach (var file in subFiles)
            {
                var bmap = new BeatmapData(file, new CCategory(0));
                var diffIndex = new Dictionary<string, InternalDifficulty>
                {
                    {"Beginner", InternalDifficulty.Beginner},
                    {"Easy", InternalDifficulty.Normal},
                    {"Normal", InternalDifficulty.Hard},
                    {"Hard", InternalDifficulty.Expert},
                    {"UNBEATABLE", InternalDifficulty.UNBEATABLE},
                    {"Star", InternalDifficulty.Star},
                };

                var offset = 0;
                while (true)
                {
                    if (!songs.Contains($"{bmap.SongName}{offset}"))
                    {
                        songs.Add($"{bmap.SongName}{offset}");
                        pkgCore.Songs.Add(new());
                    }

                    try
                    {
                        var relative = Path.GetFullPath(file).Substring(Path.GetFullPath(folderPath).Length + 1);
                        pkgCore.Songs[songs.IndexOf($"{bmap.SongName}{offset}")].Add(diffIndex[bmap.InternalDifficulty], relative);
                        break;
                    }
                    catch
                    {
                        offset++;
                        continue;
                    }
                }
                

            }

            return pkgCore;
        }

        /// <summary>
        /// Auto generate a new valid PackageCore from an existing CustomPackage
        /// </summary>
        /// <param name="pkg">A loaded CustomPackage</param>
        /// <returns></returns>
        public static PackageCore GeneratePackageCore(CustomPackage pkg)
        {
            var pkgCore = new PackageCore();
            pkgCore.GUID = Guid.NewGuid();
            pkgCore.Songs = new();
            //var offset = 0;


            List<string> diffIndextmp = ["Beginner", "Easy", "Normal", "Hard", "UNBEATABLE", "Star"];
            foreach (var s in pkg.SongDatas)
            {
                pkgCore.Songs.Add(new());
                foreach (var b in s.BeatmapDatas)
                {
                    var relative = Path.GetFullPath(b.BeatmapPath).Substring(pkg.BaseDirectory.Length + 1);
                    InternalDifficulty diff = (InternalDifficulty)diffIndextmp.IndexOf(b.InternalDifficulty);
                    pkgCore.Songs.Last().Add(diff, relative);
                }
            }
            /*
            var diffIndex = new Dictionary<string, InternalDifficulty>
                {
                    {"Beginner", InternalDifficulty.Beginner},
                    {"Easy", InternalDifficulty.Normal},
                    {"Normal", InternalDifficulty.Hard},
                    {"Hard", InternalDifficulty.Expert},
                    {"UNBEATABLE", InternalDifficulty.UNBEATABLE},
                    {"Star", InternalDifficulty.Star},
                };

            foreach (var s in pkg.SongDatas)
            {
                pkgCore.Songs.Add(new());
                foreach (var b in s.BeatmapDatas)
                {
                    var relative = Path.GetFullPath(b.BeatmapPath).Substring(pkg.BaseDirectory.Length + 1);
                    pkgCore.Songs.Last().Add(diffIndex[b.InternalDifficulty], relative);
                }
            }
            */
            return pkgCore;
        }

        public static void PopulatePackageCores(string folderPath, Action onLoadPackage = null)
        {
            folderPath = Path.GetFullPath(folderPath);

            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                if (TryPopulatePackageCore(subDir, folderPath))
                    continue;
                    //onLoadPackage.Invoke();
            }
        }

        public static async Task PopulatePackageCoresNew(string folderPath)
        {
            await Task.Delay(0);
            folderPath = Path.GetFullPath(folderPath);
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                TryPopulatePackageCore(subDir, folderPath);
            }
        }

        public static bool TryPopulatePackageCore(string packageFolder, string outerFolderPath, bool recursive = false)
        {
            packageFolder = Path.GetFullPath(packageFolder);
            if (!Directory.EnumerateFiles(packageFolder, "*.osu", SearchOption.TopDirectoryOnly).Any() || Directory.EnumerateFiles(packageFolder, "*.bmap", SearchOption.TopDirectoryOnly).Any())
                return false;

            try
            {
                //var relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1);
                var pkgCore = GeneratePackageCore(packageFolder, recursive);
                SerializeHelper.SaveJSON($"{packageFolder}\\package.bmap", pkgCore);
                //return false;
            }
            catch (Exception e)
            {
                ScheduleHelper.SafeLog(e);
                return false;
            }
            return true;
        }
    }
}
