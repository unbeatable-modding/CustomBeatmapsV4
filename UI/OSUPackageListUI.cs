using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcade.UI;
using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Patches;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using HarmonyLib;
using Newtonsoft.Json;
using Rhythm;
using UnityEngine;
using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UI
{
    public static class OSUPackageListUI
    {

        // To preserve across play sessions
        private static int _selectedPackageIndex;
        private static bool _overrideCountdown = true;

        public static void Render(Action onRenderAboveList)
        {
            (int selectedPackageIndex, Action<int>setSelectedPackageIndex) =
                (_selectedPackageIndex, val => _selectedPackageIndex = val);
            var (selectedBeatmapIndex, setSelectedBeatmapIndex) = Reacc.UseState(0);
            var (sortMode, setSortMode) = Reacc.UseState(SortMode.New);

            var localPackages = CustomBeatmaps.OSUSongManager.Packages;

            var loadState = CustomBeatmaps.OSUSongManager.InitialLoadState;
            if (loadState.Loading)
            {
                float p = (float) loadState.Loaded / loadState.Total;
                ProgressBarUI.Render(p, $"Loaded {loadState.Loaded} / {loadState.Total}", GUILayout.ExpandWidth(true), GUILayout.Height(32));
                return;
            }

            // This is... kinda highly inefficient but whatever?
            UIConversionHelper.SortLocalPackages(localPackages, sortMode);

            // No packages?

            if (localPackages.Count == 0)
            {
                onRenderAboveList();
                GUILayout.Label($"No Osu Packages Found in {Config.Mod.OsuSongsOverrideDirectory}");
                return;
            }

            // Clamp packages to fit in the event of package list changing while the UI is open
            if (selectedPackageIndex > localPackages.Count)
            {
                selectedPackageIndex = localPackages.Count - 1;
            }

            // Map local packages -> package header

            var selectedPackage = localPackages[selectedPackageIndex];

            List<PackageHeader> headers = new List<PackageHeader>(localPackages.Count);
            int packageIndex = -1;
            foreach (var p in localPackages)
            {
                ++packageIndex;
                // Get unique song count
                HashSet<string> songs = new HashSet<string>();
                HashSet<string> names = new HashSet<string>();
                HashSet<string> creators = new HashSet<string>();
                foreach (CustomBeatmapInfo bmap in p.PkgSongs.SelectMany(s => s.CustomBeatmaps))
                {
                    songs.Add(bmap.InternalName);
                    names.Add(bmap.SongName);
                    creators.Add(bmap.BeatmapCreator);
                }

                string creator = creators.Join(x => x," | ");
                string name = names.Join(x => x,", ");

                bool isNew = false;
                headers.Add(new PackageHeader(name, songs.Count, p.PkgSongs.SelectMany(s => s.Beatmaps).Count(), creator, isNew, BeatmapDownloadStatus.Downloaded, packageIndex));
            }

            // Beatmaps of selected package
            List<BeatmapHeader> selectedBeatmaps =
                UIConversionHelper.CustomBeatmapInfosToBeatmapHeaders(selectedPackage.PkgSongs);
            if (selectedBeatmapIndex >= selectedBeatmaps.Count)
            {
                selectedBeatmapIndex = selectedBeatmaps.Count - 1;
                setSelectedBeatmapIndex?.Invoke(selectedBeatmaps.Count - 1);
            }

            var selectedBeatmap = selectedPackage.PkgSongs.SelectMany(s => s.CustomBeatmaps).ToArray()[selectedBeatmapIndex];

            // Preview audio
            
            var previewsong = SongDatabase.GetBeatmapItemByPath(selectedBeatmap.Path);
            BGM.PlaySongPreview(previewsong);
            
            

            // Render
            onRenderAboveList();

            GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    SortModePickerUI.Render(sortMode, setSortMode);
                    PackageListUI.Render($"Osu Packages in {Config.Mod.OsuSongsOverrideDirectory}", headers, selectedPackageIndex, setSelectedPackageIndex);
                    AssistAreaUI.Render();
                GUILayout.EndVertical();

                // Render Right Info
                PackageInfoUI.Render(
                    () =>
                    {
                        PackageInfoTopUI.Render(selectedBeatmaps, selectedBeatmapIndex);
                    },
                    () =>
                    {
                        GUILayout.TextArea("GUIDE:\n" +
                                       "1) Create a beatmap in OSU following this tutorial: https://github.com/Ratismal/CustomBeats/blob/master/creation.md\n" +
                                       "2) It should appear in this screen at the top. Open to test it." +
                                       "3) While testing, the beatmap should automatically reload when you make changes and save in OSU");
                        //Searchbar.Render(selectedBeatmap.FlavorText, searchTextInput =>
                        //{
                        //    selectedBeatmap.Tags.FlavorText = searchTextInput;
                        //});
                        MetadataUI.Render(selectedBeatmap);
                    },
                    () =>
                    {
                        _overrideCountdown = GUILayout.Toggle(_overrideCountdown, "Do Countdown?");
                        if (GUILayout.Button($"EXPORT"))
                        {
                            string exportFolder = Config.Mod.OsuExportDirectory;
                            string exportName = selectedBeatmap.SongName;
                            OSUHelper.CreateExportZipFile(selectedBeatmap.OsuPath, exportFolder);
                        }
                        PackageBeatmapPickerUI.Render(selectedBeatmaps, selectedBeatmapIndex, setSelectedBeatmapIndex);
                        if (PlayButtonUI.Render("EDIT", $"{selectedBeatmap.SongName}: {selectedBeatmap.RealDifficulty}"))
                        {
                            // Play a local beatmap
                            var package = localPackages[selectedPackageIndex];
                            //CustomBeatmaps.PlayedPackageManager.RegisterPlay(package.FolderName);
                            //PlaySong(selectedBeatmap);
                            PlaySongEdit(selectedBeatmap, _overrideCountdown);
                        }
                    }
                );
            GUILayout.EndHorizontal();
        }
    }
}
