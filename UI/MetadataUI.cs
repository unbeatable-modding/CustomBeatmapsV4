using System;
using System.Collections.Generic;
using System.Text;
using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class MetadataUI
    {
        public static void Render(CustomBeatmapInfo bmap)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            
            GUILayout.Label("Level: ", GUILayout.ExpandWidth(false));
            var level = GUILayout.TextArea(bmap.Tags.Level.ToString(), GUILayout.ExpandWidth(false));
            if (Int32.TryParse(level, out int lvlInt) && lvlInt != bmap.Tags.Level)
                bmap.Tags.Level = lvlInt;

            GUILayout.Label("Flavor Text: ", GUILayout.ExpandWidth(false));
            var flavorText = GUILayout.TextArea(bmap.Tags.FlavorText);
            if (flavorText != bmap.Tags.FlavorText)
                bmap.Tags.FlavorText = flavorText;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (GUILayout.Button($"UPDATE METADATA"))
            {
                bmap.Tags.FlavorText = flavorText;
                CustomPackageHelper.SetBeatmapJson(bmap.text, bmap.Tags, bmap.OsuPath);
            }
        }
    }
}
