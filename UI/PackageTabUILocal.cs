using System.Linq;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageTabUILocal : AbstractPackageTab<CustomPackageLocal>
    {
        public PackageTabUILocal(PackageManagerMulti pkgManager) : base(pkgManager)
        {

            RightRenders = [
                () =>
                {
                    PackageInfoTopUI.Render(_selectableBeatmaps, SelectedBeatmapIndex);
                },
                () =>
                {
                },
                () =>
                {
                    if (ArcadeHelper.UsingHighScoreProhibitedAssists())
                    {
                        GUILayout.Label("<size=24><b>USING ASSISTS</b></size> (no high score)");
                    }
                    PackageBeatmapPickerUI.Render(_selectableBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                    {
                        // Play a local beatmap
                        CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.BaseDirectory);
                        RunSong();
                    }
                }
            ];
            
        }

        protected override void SortPackages()
        {
            UIConversionHelper.SortPackages(_pkgHeaders, SortMode);
        }
    }
}
