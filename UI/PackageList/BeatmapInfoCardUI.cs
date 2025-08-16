using System.Linq;
using Arcade.UI;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using Rhythm;
using UnityEngine;
using static Arcade.UI.ArcadeBeatmapProvider;

namespace CustomBeatmaps.UI.PackageList
{
    public static class BeatmapInfoCardUI
    {
        public static void Render(BeatmapData beatmapHeader)
        {

            var cardStyle = new GUIStyle(GUI.skin.box);
            var m = cardStyle.margin;
            var padH = 16;
            cardStyle.margin = new RectOffset(m.left + padH, m.right + padH, m.top, m.bottom);
            
            GUILayout.BeginHorizontal(cardStyle);
            // TODO: Icon if provided! For fun!
                GUILayout.BeginVertical();
                    GUILayout.Label($"<b><size=20>{beatmapHeader.SongName}</size></b>");
                    GUILayout.Label($"by <b>{beatmapHeader.Artist}</b>");
                    if (beatmapHeader.FlavorText != null && beatmapHeader.FlavorText.Any())
                        GUILayout.Label($"{beatmapHeader.FlavorText}");
                GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            /*
                GUILayout.BeginVertical();
                    GUILayout.Label($"{beatmapHeader.FlavorText}");
                GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            */
                GUILayout.BeginVertical();
                    if (beatmapHeader.Level != 0)
                        GUILayout.Label($"{beatmapHeader.Difficulty} ({beatmapHeader.Level})");
                    else
                        GUILayout.Label($"{beatmapHeader.Difficulty}");
                GUILayout.Label($"mapper: {beatmapHeader.Creator}");
                if (beatmapHeader.Attributes.Any())
                GUILayout.Label($"{string.Join(",", beatmapHeader.Attributes)}");

                GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            

        }

    }
}