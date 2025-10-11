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
using CustomBeatmaps.Util.CustomData;

namespace CustomBeatmaps.Util
{
    public static class OSUHelper
    {
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
            return BeatmapHelper.GetBeatmapProp(text, "Title", osuPath);
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

        public static string CreateExportZipFile(CustomPackage pkg, string temporaryFolderLocation)
        {
            if (!Directory.Exists(temporaryFolderLocation))
                Directory.CreateDirectory(temporaryFolderLocation);

            // Zip
            string packageName = pkg.Name;
            string osuFullPath = Path.GetFullPath(pkg.BaseDirectory);
            int lastSlash = osuFullPath.LastIndexOf("\\", StringComparison.Ordinal);
            string osuParentDir = lastSlash != -1 ? osuFullPath.Substring(0, lastSlash) : "";

            string zipTarget = $"{temporaryFolderLocation}/{packageName}.zip";

            ZipHelper.CreateFromDirectory(osuParentDir, zipTarget);

            return zipTarget;
        }
    }
}
