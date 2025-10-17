using System;
using HarmonyLib;

namespace CustomBeatmaps.Patches
{
    public static class OneLifeModePatch
    {
        [HarmonyPatch(typeof(Rhythm.RhythmController), "Miss", new Type[0])]
        [HarmonyPostfix, HarmonyPriority(Priority.HigherThanNormal)] // This will override beatmap testing, as we may want to test that.
        private static void MissFails1(Rhythm.RhythmController __instance)
        {
            MissOneLifeInject(__instance);
        }

        [HarmonyPatch(typeof(Rhythm.RhythmController), "Miss", typeof(float), typeof(bool))]
        [HarmonyPostfix, HarmonyPriority(Priority.HigherThanNormal)] // This will override beatmap testing, as we may want to test that.
        private static void MissFails2(Rhythm.RhythmController __instance)
        {
            MissOneLifeInject(__instance);
        }

        private static void MissOneLifeInject(Rhythm.RhythmController __instance)
        {
            if (CustomBeatmaps.Memory.OneLifeMode)
            {
                // One life only
                __instance.song.health = -1;
            }
        }

    }
}
