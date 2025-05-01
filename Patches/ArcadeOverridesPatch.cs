using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Arcade.UI.SongSelect;
using FMOD.Studio;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using static Arcade.UI.SongSelect.ArcadeSongDatabase;
using static Rhythm.BeatmapIndex;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;

namespace CustomBeatmaps.Patches
{
    public static class ArcadeOverridesPatch
    {
        // Patch the song function to return all (also hidden) songs,
        // so we can access hidden beatmaps
        [HarmonyPatch(typeof(BeatmapIndex), "GetVisibleSongs")]
        [HarmonyPrefix]
        public static bool UnhideSongs(ref BeatmapIndex __instance, ref List<Song> __result)
        {
            CustomBeatmaps.Log.LogDebug("Overriding GetVisibleSongs");
            __result = __instance.GetAllSongs();
            return false;
        }

        // Patch to make the game load custom beatmaps on arcade db load
        [HarmonyPatch(typeof(ArcadeSongDatabase), "LoadDatabase")]
        [HarmonyPrefix]
        public static void LoadDatabasePatch(ArcadeSongDatabase __instance)
        {

            BeatmapIndex test = BeatmapIndex.defaultIndex;

            // Load in songs
            CustomBeatmaps.Log.LogMessage("Loading DB...");
            CustomBeatmaps.Log.LogDebug("Currently " + test.SongNames.Count() + " songs exist!");
            List<Song> songList = new();
            string[] files = Directory.GetFiles(Util.PackageHelper.GetLocalBeatmapDirectory(), "*.osu", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Util.ArcadeHelper.SongSmuggle(file, 7, ref songList);
            }
            files = Directory.GetFiles(Util.PackageHelper.GetWhiteLabelBeatmapDirectory()+ "CustomBeatmapsV3-Data/SERVER_PACKAGES", "*.osu", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Util.ArcadeHelper.SongSmuggle(file, 8, ref songList);
            }
            files = Directory.GetFiles(Util.PackageHelper.GetWhiteLabelBeatmapDirectory() + "USER_PACKAGES", "*.osu", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Util.ArcadeHelper.SongSmuggle(file, 8, ref songList);
            }

            CustomBeatmaps.Log.LogDebug("Now " + test.SongNames.Count() + " songs exist!");
        }

        [HarmonyPatch(typeof(BeatmapIndex), "GetVisibleCategories")]
        [HarmonyPostfix]
        public static void GetVisibleCategoriesPatch(ref List<Category> __result)
        {
            // Actually put the categories in the game
            Util.ArcadeHelper.TryAddCustomCategory();
            // Make all the default categories visible because we can
            foreach (Category category in BeatmapIndex.defaultIndex.Categories)
            {
                if (!__result.Contains(category))
                {
                    __result.Add(category);
                }
            }
            // Add the custom categories to the list of visible categories
            foreach (Category category in Util.ArcadeHelper.customCategories)
            {
                if (!__result.Contains(category))
                {
                    __result.Add(category);
                }
            }
        }

    }
}
