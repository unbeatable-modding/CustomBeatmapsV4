using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using RJBuildScripts;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageListUIOSU : AbstractPackageList<CustomPackageLocal>
    {
        private static bool _overrideCountdown = true;
        public PackageListUIOSU(PackageManagerLocal pkgManager) : base(pkgManager)
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
                        PackageBeatmapPickerUI.Render(_selectedBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                        if (PlayButtonUI.Render("EDIT", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                        {
                            // Play a local beatmap
                            var package = _localPackages[SelectedPackageIndex];
                            RunSong();
                        }
                    }
            ];
        }
        /*
        protected override void RegenerateHeaders()
        {
            var headers = new List<CustomPackage>(_localPackages.Count);
            //var headersMap = new Dictionary<PackageHeader, CustomLocalPackage>(_localPackages.Count);
            foreach (var p in _localPackages)
            {

                if (!UIConversionHelper.PackageHasDifficulty(p, _difficulty))
                    continue;

                if (!UIConversionHelper.PackageMatchesFilter(p, _searchQuery))
                    continue;

                //var toAdd = new PackageHeader(p, false);
                headers.Add(p);
                //headersMap.Add(toAdd, p);
            }

            _pkgHeaders = headers;
            //_pkgHeadersMap = headersMap;
        }
        */
        protected override void SortPackages()
        {
            UIConversionHelper.SortPackages(_localPackages, SortMode);
        }
        
        protected override void MapPackages()
        {
            if (_pkgHeaders.Count < 1)
                return;

            if (SelectedPackageIndex >= _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);
            _selectedPackage = _pkgHeaders[SelectedPackageIndex];

            //_selectedBeatmaps =
            //    UIConversionHelper.CustomBeatmapInfosToBeatmapHeaders(_selectedPackage.PkgSongs);
            _selectedBeatmaps = _selectedPackage.BeatmapDatas.ToList();

            if (SelectedBeatmapIndex >= _selectedBeatmaps.Count)
            {
                SetSelectedBeatmapIndex?.Invoke(_selectedBeatmaps.Count - 1);
            }

            _selectedBeatmap = _selectedPackage.BeatmapDatas[SelectedBeatmapIndex];
        }

        protected override void RunSong()
        {
            ArcadeHelper.PlaySongEdit(_selectedBeatmap, _overrideCountdown);
        }

    }
}
