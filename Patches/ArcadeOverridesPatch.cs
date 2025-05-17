using System.Collections.Generic;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using static Rhythm.BeatmapIndex;
using CustomBeatmaps.Util;



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

        [HarmonyPatch(typeof(HighScoreList), nameof(HighScoreList.ReplaceHighScore))]
        [HarmonyPrefix]
        public static bool HighScoreSaveCheck(ref bool __result)
        {
            if (!ArcadeHelper.UsingHighScoreProhibitedAssists())
                return true;
            __result = false;
            return false;
        }

    }
}
