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
            string dataDir = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            // Get the directory of the custom songs
            string songDir = dataDir + "/CustomSongs";
            return songDir;
        }

        public static string GetWhiteLabelBeatmapDirectory()
        {
            // Path of the game exe
            string test = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            string dataDir = test.Substring(0, test.LastIndexOf('/'));
            // Get the directory of the custom songs
            string songDir = dataDir + "/UNBEATABLE [white label]/";
            return songDir;
        }
    }
}
