using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FMOD.Studio;
using HarmonyLib;
using Rhythm;
using CustomBeatmaps;

using CustomBeatmaps.CustomPackages;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.Patches
{
    public static class AudioPatch
    {
        // Make the game play local files
        [HarmonyPatch(typeof(RhythmTracker), "PrepareInstance", new Type[] { typeof(EventInstance), typeof(PlaySource), typeof(string) })]
        [HarmonyPrefix]
        public static bool RhythmTrackerPreparePatch(EventInstance instance, ref PlaySource source, ref string key)
        {
            if (key.StartsWith("CUSTOM__"))
            {
                BeatmapIndex.defaultIndex.TryGetSong(key, out Song songTest);
                key = ((CustomSongInfo)songTest).AudioPath;

                if (File.Exists(key))
                {
                    CustomBeatmaps.Log.LogDebug("Loading custom audio: " + key);
                    source = PlaySource.FromFile;
                }
                else
                {
                    CustomBeatmaps.Log.LogDebug("Custom audio not found: " + key);
                }
            }
            return true;
        }
    }
}
