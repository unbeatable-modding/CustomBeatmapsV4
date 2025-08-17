using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using Rhythm;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageListUILocal : AbstractPackageList
    {
        public PackageListUILocal(PackageManagerLocal pkgManager) : base(pkgManager)
        {

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
                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
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
            _selectedPackage = (CustomPackage)_pkgHeaders[SelectedPackageIndex];

            _selectedBeatmaps = _selectedPackage.BeatmapDatas.ToList();

            if (SelectedBeatmapIndex >= _selectedBeatmaps.Count)
                SetSelectedBeatmapIndex?.Invoke(_selectedBeatmaps.Count - 1);

            _selectedBeatmap = _selectedPackage.BeatmapDatas[SelectedBeatmapIndex];
        }

        protected override void RunSong()
        {
            ArcadeHelper.PlaySong(_selectedBeatmap.BeatmapPointer);
        }

        protected override void SortPackages()
        {
            UIConversionHelper.SortLocalPackages(_localPackages, SortMode);
        }
    }
}
