using System;
using System.Collections.Generic;
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
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;

namespace CustomBeatmaps.Patches
{
    public static class ArcadeOverridesPatch
    {
        private static bool didFirstLoad = false;

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
        //[HarmonyPatch(typeof(ArcadeSongDatabase), "LoadDatabase")]
        //[HarmonyPrefix]
        public static void LoadDatabasePatch(ArcadeSongDatabase __instance)
        {
            if (didFirstLoad) { return; }
            var test = BeatmapIndex.defaultIndex;

            // Load in songs
            CustomBeatmaps.Log.LogMessage("Loading DB...");
            CustomBeatmaps.Log.LogDebug($"Currently {test.SongNames.Count()} songs exist!");
            Util.ArcadeHelper.LoadCustomSongs();
            CustomBeatmaps.Log.LogDebug($"Now {test.SongNames.Count()} songs exist!");
            didFirstLoad = true;
            
        }

        //[HarmonyPatch(typeof(BeatmapIndex), "GetVisibleCategories")]
        //[HarmonyPostfix]
        public static void GetVisibleCategoriesPatch(ref List<Category> __result)
        {
            if (didFirstLoad) { return; }
            // Actually put the categories in the game
            //CustomPackageHelper.TryAddCustomCategory();
            // Make all the default categories visible because we can
            var loadHiddenCategory = BeatmapIndex.defaultIndex.Categories[3];
            if (!__result.Contains(loadHiddenCategory))
            {
                __result.Add(loadHiddenCategory);
            }

            // Add the custom categories to the list of visible categories
            foreach (var category in CustomPackageHelper.customCategories)
            {
                if (!__result.Contains(category))
                {
                    __result.Add(category);
                }
            }
        }

    }
}
