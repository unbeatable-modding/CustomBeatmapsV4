using System;
using System.Collections.Generic;
using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using UnityEngine;
using static Arcade.UI.SongSelect.ArcadeSongDatabase;

namespace CustomBeatmaps.UI.PackageList
{
    public static class PackageInfoTopUI
    {
        public static void Render(List<BeatmapData> packageBeatmaps, int selectedBeatmapIndex)
        {
            // If No beatmaps...
            if (packageBeatmaps == null)
                return;
            if (packageBeatmaps.Count == 0)
            {
                GUILayout.Label("No beatmaps provided!");
                return;
            }

            // Construct list of unique map names and our currently selected map's difficulties

            if (selectedBeatmapIndex >= packageBeatmaps.Count)
            {
                selectedBeatmapIndex = packageBeatmaps.Count - 1;
            }

            var selected = packageBeatmaps[selectedBeatmapIndex];

            // Beatmap Info Card
            BeatmapInfoCardUI.Render(selected);

            // Leaderboards and the "PLAY/DOWNLOAD" button are rendered separately
        }
    }
}