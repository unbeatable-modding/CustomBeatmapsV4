using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util.CustomData;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class MetadataUI
    {
        private static string Level;
        private static string FlavorText;
        private static bool BlindTurn;
        private static bool MotionWarning;
        private static bool FourKey;

        public static void Render(BeatmapData bmap)
        {
            Reacc.UseEffect(() => SetBeatmap(bmap), new object[] { bmap });

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            
            GUILayout.Label("Level: ", GUILayout.ExpandWidth(false));
            var level = GUILayout.TextArea(Level, GUILayout.ExpandWidth(false));
            if (level != Level)
                Level = level;

            GUILayout.Label("Flavor Text: ", GUILayout.ExpandWidth(false));
            var flavorText = GUILayout.TextArea(FlavorText);
            if (flavorText != FlavorText)
                FlavorText = flavorText;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var bt = GUILayout.Toggle(BlindTurn, "Blind Turn", GUILayout.ExpandWidth(false));
            if (bt != BlindTurn)
                BlindTurn = bt;

            var mw = GUILayout.Toggle(MotionWarning, "Motion Warning", GUILayout.ExpandWidth(false));
            if (mw != MotionWarning)
                MotionWarning = mw;

            var fk = GUILayout.Toggle(FourKey, "4k", GUILayout.ExpandWidth(false));
            if (fk != FourKey)
                FourKey = fk;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (GUILayout.Button($"UPDATE METADATA"))
            {
                Int32.TryParse(Level, out var lvlInt);
                bmap.Tags.Level = lvlInt;
                bmap.Tags.FlavorText = FlavorText;
                if (BlindTurn)
                    bmap.Tags.Attributes.Add("BT");
                if (MotionWarning)
                    bmap.Tags.Attributes.Add("MW");
                if (FourKey)
                    bmap.Tags.Attributes.Add("4K");
                BeatmapHelper.SetBeatmapJson(bmap.BeatmapPointer.text, bmap.Tags, bmap.BeatmapPath);
            }
        }

        private static void SetBeatmap(BeatmapData bmap)
        {
            Level = bmap.Level.ToString();
            FlavorText = bmap.FlavorText;
            BlindTurn = bmap.Attributes.Contains("BT");
            MotionWarning = bmap.Attributes.Contains("MW");
            FourKey = bmap.Attributes.Contains("4K");
        }
    }
}
