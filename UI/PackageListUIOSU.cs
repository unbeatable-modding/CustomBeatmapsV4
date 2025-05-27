using System;
using System.Collections.Generic;
using System.Text;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageListUIOSU : AbstractPackageList
    {
        private static bool _overrideCountdown = true;
        public PackageListUIOSU(LocalPackageManager pkgManager) : base(pkgManager)
        {

            RightRenders = [
                () =>
                    {
                        PackageInfoTopUI.Render(_selectedBeatmaps, SelectedBeatmapIndex);
                    },
                    () =>
                    {
                        GUILayout.TextArea("GUIDE:\n" +
                                       "1) Create a beatmap in OSU following this tutorial: https://github.com/Ratismal/CustomBeats/blob/master/creation.md\n" +
                                       "2) It should appear in this screen at the top. Open to test it.\n" +
                                       "3) While testing, the beatmap should automatically reload when you make changes and save in OSU"
                            );
                        MetadataUI.Render(_selectedBeatmap);
                    },
                    () =>
                    {
                        _overrideCountdown = GUILayout.Toggle(_overrideCountdown, "Do Countdown?");
                        if (GUILayout.Button($"EXPORT"))
                        {
                            string exportFolder = Config.Mod.OsuExportDirectory;
                            string exportName = _selectedBeatmap.SongName;
                            OSUHelper.CreateExportZipFile(_selectedBeatmap.OsuPath, exportFolder);
                        }
                        PackageBeatmapPickerUI.Render(_selectedBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                        if (PlayButtonUI.Render("EDIT", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.RealDifficulty}"))
                        {
                            // Play a local beatmap
                            var package = _localPackages[SelectedPackageIndex];
                            ArcadeHelper.PlaySongEdit(_selectedBeatmap, _overrideCountdown);
                        }
                    }
            ];

        }

        protected override void Load()
        {
            LoadDefault();
            _pkgHeaders.ForEach(p => p.New = false);
        }
    }
}
