using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
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

        public static void Render(CustomBeatmapInfo bmap)
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
                bmap.Tags.Attributes["BT"] = BlindTurn;
                bmap.Tags.Attributes["MW"] = MotionWarning;
                bmap.Tags.Attributes["4K"] = FourKey;
                zzzCustomPackageHelper.SetBeatmapJson(bmap.text, bmap.Tags, bmap.Info.OsuPath);
            }
        }

        private static void SetBeatmap(CustomBeatmapInfo bmap)
        {
            Level = bmap.Level.ToString();
            FlavorText = bmap.FlavorText;
            bmap.Attributes.TryGetValue("BT", out BlindTurn);
            bmap.Attributes.TryGetValue("MW", out MotionWarning);
            bmap.Attributes.TryGetValue("4K", out FourKey);
        }
    }
}
