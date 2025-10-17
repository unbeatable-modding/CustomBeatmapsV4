using System.Linq;
using CustomBeatmaps.CustomData;
using UnityEngine;

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
                GUILayout.Label($"Mapper: {beatmapHeader.Creator}");
                if (beatmapHeader.Attributes.Any())
            {
                /*
                var attrs = new List<string>();
                foreach (var i in beatmapHeader.Attributes)
                {
                    if (i.Value)
                        attrs.Add(i.Key);
                }
                */
                GUILayout.Label($"Attributes: [{string.Join(",", beatmapHeader.Attributes)}]");
            }
                    

                GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            

        }

    }
}