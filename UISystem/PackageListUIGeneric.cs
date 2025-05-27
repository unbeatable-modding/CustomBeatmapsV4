using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.Util;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UISystem
{
    public class PackageListUIGeneric
    {
        /// <summary>
        /// Package manager
        /// </summary>
        protected object Manager;
        protected string _folder;
        
        protected CustomPackageHandler PkgManager => (CustomPackageHandler)Manager;
        protected List<CustomLocalPackage> LocalPackages;


        protected List<PackageHeader> Packages;
        protected Action ReloadPackages;

        protected int _selectedPackageIndex = 0;
        protected int SelectedPackageIndex => _selectedPackageIndex;
        protected Action<int> SetSelectedPackageIndex;

        protected int _selectedBeatmapIndex = 0;
        protected int SelectedBeatmapIndex => _selectedBeatmapIndex;
        protected Action<int> SetSelectedBeatmapIndex;

        protected SortMode _sortMode = SortMode.New;
        protected SortMode SortMode => _sortMode;
        protected Action<SortMode> SetSortMode;

        protected List<BeatmapHeader> _selectedBeatmaps;
        protected CustomBeatmapInfo _selectedBeatmap;
        protected CustomLocalPackage _selectedPackage;

        protected List<PackageHeader> _pkgHeaders;
        protected Action<object[]> SetPkgHeaders;

        public PackageListUIGeneric(object pkgManager)
        {
            Manager = (CustomPackageHandler)pkgManager;
            LocalPackages = PkgManager.Packages;

            SetSelectedPackageIndex = (val) => {
                _selectedPackageIndex = val;
                ReloadPackages?.Invoke();
            };
            SetSelectedBeatmapIndex = (val) => _selectedBeatmapIndex = val;

            ReloadPackages = () => {
                LocalPackages = PkgManager.Packages;
                Load();
            };

            PkgManager.PackageUpdated += package =>
            {
                ReloadPackages?.Invoke();
            };

            SetSortMode = (val) => {
                _sortMode = val;
                ReloadPackages?.Invoke();
            };

            ReloadPackages?.Invoke();
        }

        public void Render(Action onRenderAboveList)
        {

            //var localPackages = PkgManager.Packages;

            var loadState = PkgManager.InitialLoadState;
            if (loadState.Loading)
            {
                float p = (float) loadState.Loaded / loadState.Total;
                ProgressBarUI.Render(p, $"Loaded {loadState.Loaded} / {loadState.Total}", GUILayout.ExpandWidth(true), GUILayout.Height(32));
                return;
            }

            // No packages?

            if (LocalPackages.Count == 0)
            {
                onRenderAboveList();
                GUILayout.Label($"No Packages Found in {PkgManager.Folder}");
                return;
            }

            // Clamp packages to fit in the event of package list changing while the UI is open
            if (SelectedPackageIndex > LocalPackages.Count)
                SetSelectedPackageIndex(LocalPackages.Count - 1);

            // Preview audio

            var previewsong = SongDatabase.GetBeatmapItemByPath(_selectedBeatmap.Path);
            BGM.PlaySongPreview(previewsong);

            // Render
            onRenderAboveList();
            GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    SortModePickerUI.Render(SortMode, SetSortMode);
                    PackageListUI.Render($"Packages in {PkgManager.Folder}", _pkgHeaders, SelectedPackageIndex, SetSelectedPackageIndex);
                    AssistAreaUI.Render();
                GUILayout.EndVertical();

                // Render Right Info
                PackageInfoUI.Render(
                    () =>
                    {
                        PackageInfoTopUI.Render(_selectedBeatmaps, SelectedBeatmapIndex);
                    },
                    () =>
                    {
                    },
                    () =>
                    {
                        
                        // This Render specificially should probably never happen, it's just a fallback
                        PackageBeatmapPickerUI.Render(_selectedBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                        
                        if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.RealDifficulty}"))
                        {
                            // Play a local beatmap
                            CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.FolderName);
                            PlaySong(_selectedBeatmap);
                        }
                        
                    }
                );
            GUILayout.EndHorizontal();
        }

        protected void Load()
        {
            
            UIConversionHelper.SortLocalPackages(LocalPackages, SortMode);

            _selectedPackage = LocalPackages[SelectedPackageIndex];

            // Map local packages -> package header

            List<PackageHeader> headers = new List<PackageHeader>(LocalPackages.Count);
            int packageIndex = -1;
            foreach (var p in LocalPackages)
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

                string creator = creators.Join(x => x, " | ");
                string name = names.Join(x => x, ", ");

                bool isNew = false;
                headers.Add(new PackageHeader(name, songs.Count, p.PkgSongs.SelectMany(s => s.Beatmaps).Count(), creator, isNew, BeatmapDownloadStatus.Downloaded, packageIndex));
            }

            _pkgHeaders =  headers;

            _selectedBeatmaps =
                UIConversionHelper.CustomBeatmapInfosToBeatmapHeaders(_selectedPackage.PkgSongs);
            if (SelectedBeatmapIndex >= _selectedBeatmaps.Count)
            {
                SetSelectedBeatmapIndex?.Invoke(_selectedBeatmaps.Count - 1);
            }

            _selectedBeatmap = _selectedPackage.PkgSongs.SelectMany(s => s.CustomBeatmaps).ToArray()[SelectedBeatmapIndex];
        }


    }
}
