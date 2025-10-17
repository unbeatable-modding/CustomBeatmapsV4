using System.Linq;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageTabUIOSU : AbstractPackageTab<CustomPackageLocal>
    {
        private static bool _overrideCountdown = true;
        public PackageTabUIOSU(PackageManagerLocal pkgManager) : base(pkgManager)
        {
            RightRenders = [
                () =>
                    {
                        PackageInfoTopUI.Render(_selectableBeatmaps, SelectedBeatmapIndex);
                    },
                    () =>
                    {
                        GUILayout.TextArea("GUIDE:\n" +
                                       "1) Create a beatmap in OSU following this tutorial: https://github.com/Ratismal/CustomBeats/blob/master/creation.md\n" +
                                       "2) It should appear in this screen at the top. Open to test it.\n" +
                                       "3) While testing, the beatmap should automatically reload when you make changes and save in OSU"
                            );
                        MetadataUI.Render(_selectedBeatmap);
                        if (GUILayout.Button($"Init OSU Packages"))
                        {
                            CustomBeatmaps.OSUSongManager.GenerateCorePackages();
                            //CustomBeatmaps.LocalServerPackages.GenerateCorePackages();
                        }
                        if (GUILayout.Button($"Init ALL Packages"))
                        {
                            CustomBeatmaps.OSUSongManager.GenerateCorePackages();
                            CustomBeatmaps.LocalServerPackages.GenerateCorePackages();
                            CustomBeatmaps.LocalUserPackages.GenerateCorePackages();
                        }
                    },
                    () =>
                    {
                        _overrideCountdown = GUILayout.Toggle(_overrideCountdown, "Do Countdown?");
                        if (GUILayout.Button($"EXPORT"))
                        {
                            string exportFolder = Config.Mod.OsuExportDirectory;
                            string exportName = _selectedBeatmap.SongName;
                            OSUHelper.CreateExportZipFile(_selectedPackage, exportFolder);
                        }
                        PackageBeatmapPickerUI.Render(_selectableBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                        if (PlayButtonUI.Render("EDIT", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                        {
                            // Play a local beatmap
                            var package = _localPackages[SelectedPackageIndex];
                            RunSong();
                        }
                    }
            ];
        }

        protected override void SortPackages()
        {
            UIConversionHelper.SortPackages(_localPackages, SortMode);
        }

        protected override void RunSong()
        {
            ArcadeHelper.PlaySongEdit(_selectedBeatmap, _overrideCountdown);
        }

    }
}
