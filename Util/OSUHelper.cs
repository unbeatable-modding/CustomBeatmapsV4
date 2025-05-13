using System;
using System.Collections.Generic;
using System.IO;
using CustomBeatmaps.CustomPackages;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using HarmonyLib;
using Rhythm;
using static Rhythm.BeatmapIndex;
using System.Linq;

namespace CustomBeatmaps.Util
{
    public static class OSUHelper
    {
        public static CustomSongInfo[] LoadOsuBeatmaps(string path, int category, out string failMessage)
        {
            failMessage = "";
            path = GetOsuPath(path);
            if (Directory.Exists(path))
            {
                List<CustomSongInfo> songs = new List<CustomSongInfo>();
                foreach (string osuProjectDir in Directory.EnumerateDirectories(path))
                {
                    foreach (string file in Directory.EnumerateFiles(osuProjectDir, "*.*", SearchOption.AllDirectories))
                    {
                        if (file.EndsWith(".osu"))
                        {
                            try
                            {
                                var toLoad = new CustomSongInfo(file, category);

                                if (songs.Any())
                                {
                                    var dupeInt = 0;
                                    var startingName = toLoad.name;
                                    while (songs.Where((Song s) =>
                            s.name == toLoad.name && (((CustomSongInfo)s).directoryPath != toLoad.directoryPath || s.Difficulties.Contains(toLoad.Difficulties.Single()))).Any())
                                    {
                                        toLoad.name = startingName + dupeInt;
                                        dupeInt++;
                                    }
                                }



                                if (!songs.Any())
                                {
                                    //ScheduleHelper.SafeLog($"not null");
                                    songs.Add(toLoad);
                                    //song = new Song("test");
                                }
                                else if (songs.Where((Song s) => s.name == toLoad.name).Any())
                                {
                                    // Song we just created has multiple difficulties

                                    //CustomBeatmaps.Log.LogDebug($"Adding to Song: {toLoad.name}");
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

                                //var b = CustomPackageHelper.LoadLocalBeatmap(file);
                                //beatmaps.Add(b);
                            }
                            catch (Exception e)
                            {
                                failMessage += e.Message + "\n";
                            }
                        }
                    }
                }

                double TimeSinceLastWrite(string filename)
                {
                    return (DateTime.Now - File.GetLastWriteTime(filename)).TotalSeconds;
                }

                // Sort by newest access
                //beatmaps.Sort((left, right) => Math.Sign(TimeSinceLastWrite(left.OsuPath) - TimeSinceLastWrite(right.OsuPath)));
                ScheduleHelper.SafeInvoke(() => songs.ForEach((CustomSongInfo s) => s.GetTexture()) );
                return songs.ToArray();
            }
            return null;
        }

        public static string GetOsuPath(string overridePath)
        {
            if (string.IsNullOrEmpty(overridePath))
            {
                return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData).Replace('\\', '/'), "../Local/osu!/Songs"));
            }
            return overridePath;
        }

        private static string LoadPackageNameFromOsu(string osuPath)
        {
            string text = File.ReadAllText(osuPath);
            return CustomPackageHelper.GetBeatmapProp(text, "Title", osuPath);
        }

        public static string CreateExportZipFile(string osuPath, string temporaryFolderLocation)
        {
            if (Directory.Exists(temporaryFolderLocation))
            {
                Directory.Delete(temporaryFolderLocation, true);
            }
            Directory.CreateDirectory(temporaryFolderLocation);
            string packageName = LoadPackageNameFromOsu(osuPath);
            string filesLocation = temporaryFolderLocation;
            Directory.CreateDirectory(filesLocation);

            // Copy over the files
            string osuFullPath = Path.GetFullPath(osuPath);
            int lastSlash = osuFullPath.LastIndexOf("\\", StringComparison.Ordinal);
            string osuParentDir = lastSlash != -1 ? osuFullPath.Substring(0, lastSlash) : "";
            foreach (string fpath in Directory.EnumerateFiles(osuParentDir))
            {
                string fname = Path.GetFileName(fpath);
                File.Copy(fpath, $"{filesLocation}/{fname}");
            }

            // Zip

            string zipTarget = $"{packageName}.zip";
            // Remove LOCAL_ just... to make it a bit more neat.
            if (zipTarget.StartsWith("LOCAL_")) zipTarget = zipTarget.Substring("LOCAL_".Length);

            ZipHelper.CreateFromDirectory(temporaryFolderLocation, zipTarget);

            // Delete temporary directory afterwards
            Directory.Delete(temporaryFolderLocation, true);

            return zipTarget;
        }
    }
}