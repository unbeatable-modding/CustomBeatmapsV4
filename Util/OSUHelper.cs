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
using CustomBeatmaps.CustomData;
/*
namespace CustomBeatmaps.Util
{
    public static class OSUHelper
    {
        public static CustomSong[] LoadOsuBeatmaps(string path, int category, out string failMessage)
        {
            failMessage = "";
            path = GetOsuPath(path);
            if (Directory.Exists(path))
            {
                List<CustomSong> songs = new List<CustomSong>();
                foreach (string osuProjectDir in Directory.EnumerateDirectories(path))
                {
                    foreach (string file in Directory.EnumerateFiles(osuProjectDir, "*.*", SearchOption.AllDirectories))
                    {
                        if (file.EndsWith(".osu"))
                        {
                            try
                            {
                                var toLoad = new CustomSong(file, category);
                                //CustomPackageHelper.AddSongToList(toLoad, ref songs);
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
            return zzzCustomPackageHelper.GetBeatmapProp(text, "Title", osuPath);
        }

        public static string CreateExportZipFile(string osuPath, string temporaryFolderLocation)
        {
            if (!Directory.Exists(temporaryFolderLocation))
                Directory.CreateDirectory(temporaryFolderLocation);

            // Zip
            string packageName = LoadPackageNameFromOsu(osuPath);
            string osuFullPath = Path.GetFullPath(osuPath);
            int lastSlash = osuFullPath.LastIndexOf("\\", StringComparison.Ordinal);
            string osuParentDir = lastSlash != -1 ? osuFullPath.Substring(0, lastSlash) : "";

            string zipTarget = $"{temporaryFolderLocation}/{packageName}.zip";
            
            ZipHelper.CreateFromDirectory(osuParentDir, zipTarget);

            return zipTarget;
        }
    }
}
*/