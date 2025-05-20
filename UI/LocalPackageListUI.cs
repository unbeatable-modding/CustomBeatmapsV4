using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Patches;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using HarmonyLib;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UI
{
    public static class LocalPackageListUI
    {

        // To preserve across play sessions
        private static int _selectedPackageIndex;

        public static void Render(Action onRenderAboveList)
        {
            (int selectedPackageIndex, Action<int>setSelectedPackageIndex) =
                (_selectedPackageIndex, val => _selectedPackageIndex = val);
            var (selectedBeatmapIndex, setSelectedBeatmapIndex) = Reacc.UseState(0);
            var (sortMode, setSortMode) = Reacc.UseState(SortMode.New);

            var localPackages = CustomBeatmaps.LocalUserPackages.SelectMany(p => p.Packages).ToList();

            var loadState = CustomBeatmaps.LocalUserPackages.Select(p => p.InitialLoadState).First();
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
                GUILayout.Label($"No Local Packages Found in {Config.Mod.UserPackagesDir}");
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

                bool isNew = !CustomBeatmaps.PlayedPackageManager.HasPlayed(p.FolderName);
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
            BGM.PlaySongPreview(SongDatabase.GetBeatmapItemByPath(selectedBeatmap.Path));

            // Render
            onRenderAboveList();

            GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    SortModePickerUI.Render(sortMode, setSortMode);
                    PackageListUI.Render($"Local Packages in {Config.Mod.UserPackagesDir.Join()}", headers, selectedPackageIndex, setSelectedPackageIndex);
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
                        string highScoreKey = selectedBeatmap.Path;
                        PersonalHighScoreUI.Render(highScoreKey);
                    },
                    () =>
                    {
                        PackageBeatmapPickerUI.Render(selectedBeatmaps, selectedBeatmapIndex, setSelectedBeatmapIndex);
                        if (PlayButtonUI.Render("PLAY", $"{selectedBeatmap.SongName}: {selectedBeatmap.RealDifficulty}"))
                        {
                            // Play a local beatmap
                            var package = localPackages[selectedPackageIndex];
                            CustomBeatmaps.PlayedPackageManager.RegisterPlay(package.FolderName);
                            PlaySong(selectedBeatmap);
                        }
                    }
                );
            GUILayout.EndHorizontal();
        }
    }
}
