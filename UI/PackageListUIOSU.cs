using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;
/*
namespace CustomBeatmaps.UI
{
    public class PackageListUIOSU : AbstractPackageList<LocalPackageManager, CustomLocalPackage, CustomLocalBeatmap>
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
                        MetadataUI.Render(_selectedBeatmap.Beatmap);
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
                            RunSong();
                        }
                    }
            ];

        }

        protected override void RegenerateHeaders()
        {
            var headers = new List<PackageHeader>(_localPackages.Count);
            var headersMap = new Dictionary<PackageHeader, CustomLocalPackage>(_localPackages.Count);
            foreach (var p in _localPackages)
            {

                if (!UIConversionHelper.PackageHasDifficulty(p, _difficulty))
                    continue;

                if (!UIConversionHelper.PackageMatchesFilter(p, _searchQuery))
                    continue;

                var toAdd = new PackageHeader(p, false);
                headers.Add(toAdd);
                headersMap.Add(toAdd, p);
            }

            _pkgHeaders = headers;
            //_pkgHeadersMap = headersMap;
        }

        protected override void SortPackages()
        {
            UIConversionHelper.SortLocalPackages(_localPackages, SortMode);
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
            ArcadeHelper.PlaySongEdit(_selectedBeatmap.Beatmap, _overrideCountdown);
        }

    }
}
*/