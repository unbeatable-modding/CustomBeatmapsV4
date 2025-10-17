using System.Collections.Generic;
using HarmonyLib;
using Rhythm;

namespace CustomBeatmaps.Patches
{
    public static class FlipModePatch
    {
        [HarmonyPatch(typeof(Rhythm.RhythmController), "Start")]
        [HarmonyPostfix]
        private static void FlipLoadedBeatmapsAfterLoad(ref Queue<NoteInfo> ___notes)
        {
            if (CustomBeatmaps.Memory.FlipMode)
            {
                var og = ___notes.ToArray();
                ___notes.Clear();
                foreach (var note in og)
                {
                    switch (note.height)
                    {
                        case Height.Low:
                            note.height = Height.Top;
                            break;
                        case Height.Top:
                            note.height = Height.Low;
                            break;
                    }
                    ___notes.Enqueue(note);
                }
            }
        }

    }
}
