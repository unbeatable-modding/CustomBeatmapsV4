using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using Rhythm;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageListUILocal : AbstractPackageList<CustomPackageHandler, CustomLocalPackage, LocalCustomBeatmap>
    {

        //protected override List<CustomLocalPackage> _packages;
        public PackageListUILocal(CustomPackageHandler pkgManager) : base(pkgManager)
        {
            //Manager2 = new PkgTransformer<object>(pkgManager, pkgManager.Folder, pkgManager.Packages);

            RightRenders = [
                () =>
                {
                    PackageInfoTopUI.Render(_selectedBeatmaps, SelectedBeatmapIndex);
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
                    PackageBeatmapPickerUI.Render(_selectedBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.RealDifficulty}"))
                    {
                        // Play a local beatmap
                        CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.FolderName);
                        RunSong();
                    }
                }
            ];
            
        }

        protected override void MapPackages()
        {
            if (SelectedPackageIndex >= _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);
            _selectedPackage = (CustomLocalPackage)_pkgHeaders[SelectedPackageIndex].Package;

            _selectedBeatmaps =
                UIConversionHelper.CustomBeatmapInfosToBeatmapHeaders(_selectedPackage.PkgSongs);
            if (SelectedBeatmapIndex >= _selectedBeatmaps.Count)
            {
                SetSelectedBeatmapIndex?.Invoke(_selectedBeatmaps.Count - 1);
            }

            _selectedBeatmap = _selectedPackage.CustomBeatmaps[SelectedBeatmapIndex];
        }

        protected override void RunSong()
        {
            ArcadeHelper.PlaySong(_selectedBeatmap.Beatmap);
        }

        protected override void SortPackages()
        {
            UIConversionHelper.SortLocalPackages(_localPackages, SortMode);
        }
    }
}
