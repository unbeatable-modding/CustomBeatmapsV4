using Arcade.UI;
using CustomBeatmaps.CustomData;
using FMOD;
using FMOD.Studio;
using HarmonyLib;
using Rhythm;
using System;
using static Rhythm.BeatmapIndex;
using File = Pri.LongPath.File;

namespace CustomBeatmaps.Patches
{
    public static class AudioPatch
    {

        // Make the game play local files
        [HarmonyPatch(typeof(RhythmTracker), "PrepareInstance", new Type[] { typeof(EventInstance), typeof(PlaySource), typeof(string) })]
        [HarmonyPrefix]
        public static bool RhythmTrackerPreparePatch(ref PlaySource source, ref string key)
        {
            if (key.StartsWith("CUSTOM__"))
            {
                BeatmapIndex.defaultIndex.TryGetSong(key, out Song songTest);
                if (songTest is CustomSong)
                    key = ((CustomSong)songTest).Data.AudioPath;

                if (File.Exists(key))
                {
                    CustomBeatmaps.Log.LogDebug("Loading custom audio: " + key);
                    source = PlaySource.FromFile;
                }
                else
                {
                    CustomBeatmaps.Log.LogDebug("Custom audio not found: " + key);
                    return false;
                }
            }
            return true;
        }

        // Make the miniplayer work with custom songs which magicially fixes other issues
        [HarmonyPatch(typeof(ArcadeBGMManager), "WaitForSoundReady")]
        [HarmonyPrefix]
        public static bool MadeSound(ref PROGRAMMER_SOUND_PROPERTIES properties)
        {
            var sound = new Sound(properties.sound);
            var getLength = sound.getLength(out var length, TIMEUNIT.MS);
            while (getLength == RESULT.ERR_NOTREADY)
            {
                getLength = sound.getLength(out length, TIMEUNIT.MS);
            }
            if (sound.getLength(out _, TIMEUNIT.MS) == RESULT.OK && length > 0)
            {
                AccessTools.PropertySetter(typeof(ArcadeBGMManager), nameof(ArcadeBGMManager.SongDuration))
                    .Invoke(null, new object[] { ((float)length / 1000f) + 0.5f }); // extra half second so looping is not weird
            }
            return true;
        }

    }
}
