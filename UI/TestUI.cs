using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using CustomBeatmaps.Util;

namespace CustomBeatmaps.UI
{
    public class TestUI
    {

        public void DrawUI()
        {
            // Just a small button to reload the arcade
            if (GUI.Button(new Rect(10, 10, 32, 32), "O"))
            {
                ArcadeHelper.ReloadArcadeList();
            }



        }
    }
}
