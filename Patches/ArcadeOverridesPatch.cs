using System.Collections.Generic;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using static Rhythm.BeatmapIndex;
using CustomBeatmaps.Util;
using UnityEngine.SocialPlatforms;



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
            if (!CustomBeatmaps.ModConfig.ShowHiddenStuff)
                return true;
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

        [HarmonyPatch(typeof(LevelManager), "LoadArcadeLevel")]
        [HarmonyPrefix]
        public static bool LoadArcadeLevel(string beatmapName, string beatmapDifficulty, int spawn = 0, bool transition = true)
        {
            ArcadeProgression arcadeProgression = new ArcadeProgression(beatmapName + "/" + beatmapDifficulty, RhythmGameType.ArcadeMode);
            JeffBezosController.rhythmGameType = RhythmGameType.ArcadeMode;
            JeffBezosController.rhythmProgression = arcadeProgression;
            arcadeProgression.stageScene = ArcadeHelper.GetSceneNameByIndex(CustomBeatmaps.Memory.SelectedRoom);
            LevelManager.LoadLevel(arcadeProgression.stageScene, spawn, transition);
            return false;
        }

        // fixes half/double time
        [HarmonyPatch(typeof(RhythmController), "LoadLocalBeatmap")]
        [HarmonyPostfix]
        public static void InitializeController()
        {
            JeffBezosController.SetTimeScale(Mathf.Abs(FileStorage.beatmapOptions.songSpeed), 10f);
            //return true;
        }
    }
}
