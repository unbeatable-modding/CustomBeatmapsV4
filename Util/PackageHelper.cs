using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CustomBeatmaps.Util
{
    public class PackageHelper
    {
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
    }
}
