using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace CustomBeatmaps.Patches
{
    /// <summary>
    /// JeffBezosController hides the cursor, but we have our UI's open sometimes so don't do that lel
    /// </summary>
    public static class CursorUnhidePatch
    {
        [HarmonyPatch(typeof(JeffBezosController), "Update")]
        [HarmonyPostfix]
        public static void JeffBezosPostUpdate()
        {
            //if (CustomBeatmapsUIBehaviour.Opened || HighScoreUIBehaviour.Opened)
            //{
            //CustomBeatmaps.Log.LogDebug("HELLO JEFF");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            //}
        }
    }
}