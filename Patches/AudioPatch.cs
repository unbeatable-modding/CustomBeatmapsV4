using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FMOD.Studio;
using HarmonyLib;
using Rhythm;
using CustomBeatmaps;

using Encoder = CustomBeatmaps.Util.Encoder;

namespace CustomBeatmaps.Patches
{
    public static class AudioPatch
    {
        // Make the game play local files
        [HarmonyPatch(typeof(RhythmTracker), "PrepareInstance", new Type[] { typeof(EventInstance), typeof(PlaySource), typeof(string) })]
        [HarmonyPrefix]
        public static bool RhythmTrackerPreparePatch(EventInstance instance, ref PlaySource source, ref string key)
        {
            if (key.StartsWith("CUSTOM__") && key.Contains("."))
            {

                //key = "C:/Program Files (x86)/Steam/steamapps/common/UNBEATABLE Demo/CustomSongs/Rick Astley - Never Gonna Give You Up (Official Music Video)/audio.mp3";
                key = Encoder.DecodeAudioName(key);


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
