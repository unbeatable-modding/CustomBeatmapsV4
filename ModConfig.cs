using System.Collections.Generic;
using Pri.LongPath;
using UnityEngine;

namespace CustomBeatmaps
{
    /// <summary>
    /// Base config file for entire mod
    /// </summary>
    public class ModConfig
    {
        // :sunglasses:
        public bool DarkMode = true;
        // Directory for user (local) packages
        public string[] UserPackagesDir = ["USER_PACKAGES"];
        /// Directory for server/downloaded packages
        public string ServerPackagesDir = "CustomBeatmapsV4-Data/SERVER_PACKAGES";
        // Directory for [white label] user (local) packages
        public string WhiteLabelPackagesDir = GetWhiteLabelBeatmapDirectory();
        /// Songs directory for your OSU install for the mod to access & test
        public string OsuSongsOverrideDirectory = null;
        /// Directory (relative to UNBEATABLE) where your OSU file packages will export
        public string OsuExportDirectory = ".";
        /// Temporary folder used to load + play a user submission
        public string TemporarySubmissionPackageFolder = "CustomBeatmapsV4-Data/.SUBMISSION_PACKAGE.temp";
        /// The local user "key" for high score submissions
        public string UserUniqueIdFile = "CustomBeatmapsV4-Data/.USER_ID";
        /// A line separated list of all beatmaps we've tried playing
        public string PlayedBeatmapList = "CustomBeatmapsV4-Data/.played_beatmaps";

        private static string GetWhiteLabelBeatmapDirectory()
        {
            // Path of the game exe
            var test = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            var dataDir = test.Substring(0, test.LastIndexOf('/'));
            // Get the directory of the custom songs
            var songDir = $"{dataDir}/UNBEATABLE [white label]/CustomBeatmapsV3-Data/SERVER_PACKAGES";
            if (Directory.Exists(songDir))
            {
                return songDir;
            }
            return null;
        }
    }
}
