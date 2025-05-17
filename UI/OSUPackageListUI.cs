using System;
using System.IO;
using System.Linq;
using BepInEx;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Patches;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UI
{
    public static class OSUPackageListUI
    {
        private static bool _overrideCountdown = true;

        public static void Render(Action onRenderAboveList)
        {
            var (selectedBeatmapIndex, setSelectedBeatmapIndex) = Reacc.UseState(0);
            var (scroll, setScroll) = Reacc.UseState(Vector2.zero);

            var osuBeatmaps = CustomBeatmaps.OSUSongManager.Songs.SelectMany(s => ((CustomSongInfo)s).Beatmaps.Values).ToArray();

            onRenderAboveList();

            //if (CustomBeatmaps.OSUSongManager.Error != null)
            //{
            //    GUILayout.Label(CustomBeatmaps.OSUSongManager.Error);
            //    BGM.StopSongPreview();
            //    return;
            //}

            //GUILayout.Label(CustomBeatmaps.OSUSongManager.WarningMessages);

            if (osuBeatmaps.Length == 0)
            {
                GUILayout.Label("No OSU beatmaps found!");
                BGM.StopSongPreview();
                return;
            }

            // Out of bounds net
            if (selectedBeatmapIndex >= osuBeatmaps.Length)
            {
                selectedBeatmapIndex = osuBeatmaps.Length - 1;
                setSelectedBeatmapIndex(selectedBeatmapIndex);
            }

            GUILayout.BeginHorizontal();

            setScroll(GUILayout.BeginScrollView(scroll));
            for (int i = 0; i < osuBeatmaps.Length; ++i)
            {
                var osuBmap = (CustomBeatmapInfo)osuBeatmaps[i];
                string name = Path.GetFileName(osuBmap.OsuPath);
                if (GUILayout.Button(name))
                {
                    setSelectedBeatmapIndex(i);
                }
            }
            GUILayout.EndScrollView();

            var selectedBeatmap = (CustomBeatmapInfo)osuBeatmaps[selectedBeatmapIndex];
            BGM.PlaySongPreview(SongDatabase.GetBeatmapItemByPath(selectedBeatmap.Path));

            PackageInfoUI.Render(
                () =>
                {
                    BeatmapInfoCardUI.Render(UIConversionHelper.CustomBeatmapInfoToBeatmapHeader(selectedBeatmap));
                },
                () =>
                {
                    GUILayout.TextArea("GUIDE:\n" +
                                       "1) Create a beatmap in OSU following this tutorial: https://github.com/Ratismal/CustomBeats/blob/master/creation.md\n" +
                                       "2) It should appear in this screen at the top. Open to test it." +
                                       "3) While testing, the beatmap should automatically reload when you make changes and save in OSU");
                },
                () =>
                {
                    _overrideCountdown = GUILayout.Toggle(_overrideCountdown, "Do Countdown?");
                    if (GUILayout.Button($"EXPORT"))
                    {
                        string exportFolder = Config.Mod.OsuExportDirectory;
                        string exportName = selectedBeatmap.SongName;
                        OSUHelper.CreateExportZipFile(selectedBeatmap.OsuPath, Path.Combine(exportFolder, exportName));
                    }
                    if (PlayButtonUI.Render($"EDIT: {selectedBeatmap.SongName}"))
                    {
                        PlaySongEdit(selectedBeatmap, _overrideCountdown);
                        //PlaySong(selectedBeatmap);
                    }
                }
            );

            GUILayout.EndHorizontal();
        }
    }
}