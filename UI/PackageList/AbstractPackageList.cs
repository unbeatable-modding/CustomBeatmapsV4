using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Arcade.UI;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.Util;
using HarmonyLib;
using JetBrains.Annotations;
using Rhythm;
using UnityEngine;
using static CustomBeatmaps.CustomPackages.LocalPackageManager;
using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UISystem
{
    public abstract class AbstractPackageList
    {
        
        protected object Manager;
        protected List<CustomLocalPackage> _localPackages;
        protected List<CustomLocalPackage> LocalPackages => (List<CustomLocalPackage>)Manager.GetType().GetProperty("Packages").GetValue(Manager, null);
        protected string Folder => (string)Manager.GetType().GetProperty("Folder").GetValue(Manager, null);
        protected InitialLoadStateData LoadState => (InitialLoadStateData)Manager.GetType().GetProperty("InitialLoadState").GetValue(Manager, null);


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

        protected Action LeftRender;
        protected Action[] RightRenders;
        private static string _searchQuery;

        private void Init()
        {
            Fallbacks();

            // Action Setup
            SetSelectedPackageIndex = (val) => {
                _selectedPackageIndex = val;
                ReloadPackages?.Invoke();
            };
            SetSelectedBeatmapIndex = (val) => _selectedBeatmapIndex = val;

            ReloadPackages = () => {
                _localPackages = LocalPackages;
                Load();
            };

            SetSortMode = (val) => {
                _sortMode = val;
                var pkg = _selectedPackage;
                _localPackages = LocalPackages;
                UIConversionHelper.SortLocalPackages(_localPackages, _sortMode);
                RegenerateHeaders();
                var packages = _pkgHeaders.Select(h => h.Package).ToList();
                if (packages.Contains(pkg))
                {
                    SetSelectedPackageIndex(packages.IndexOf(pkg));
                    return;
                }
                ReloadPackages?.Invoke();
            };
            
            ReloadPackages?.Invoke();
            
        }

        public AbstractPackageList(object manager)
        {
            Manager = manager;
            _localPackages = LocalPackages;

            if (manager is LocalPackageManager pkgManager)
            {
                pkgManager.PackageUpdated += package =>
                {
                    var pkg = _selectedPackage;
                    _localPackages = LocalPackages;
                    UIConversionHelper.SortLocalPackages(_localPackages, _sortMode);
                    if (_localPackages.IndexOf(pkg) != -1)
                        SetSelectedPackageIndex(_localPackages.IndexOf(pkg));
                    else
                        ReloadPackages?.Invoke();
                };
            }
            else if (manager is CustomPackageHandler pkgHandler)
            {
                pkgHandler.PackageUpdated += package =>
                {
                    var pkg = _selectedPackage;
                    _localPackages = LocalPackages;
                    UIConversionHelper.SortLocalPackages(_localPackages, _sortMode);
                    if (_localPackages.IndexOf(pkg) != -1)
                        SetSelectedPackageIndex(_localPackages.IndexOf(pkg));
                    else
                        ReloadPackages?.Invoke();
                };
            }

            Init();
            
        }

        public void Render(Action onRenderAboveList)
        {

            var loadState = LoadState;
            if (loadState.Loading)
            {
                float p = (float)loadState.Loaded / loadState.Total;
                ProgressBarUI.Render(p, $"Loaded {loadState.Loaded} / {loadState.Total}", GUILayout.ExpandWidth(true), GUILayout.Height(32));
                return;
            }

            // No packages?

            if (_localPackages.Count == 0)
            {
                onRenderAboveList();
                RenderSearchbar();
                GUILayout.Label($"No Packages Found in {Folder}");
                return;
            }

            // Clamp packages to fit in the event of package list changing while the UI is open
            if (SelectedPackageIndex > _localPackages.Count)
                SetSelectedPackageIndex(_localPackages.Count - 1);

            // Preview audio
            var previewsong = SongDatabase.GetBeatmapItemByPath(_selectedBeatmap.Path);
            BGM.PlaySongPreview(previewsong);
                

            // Render
            onRenderAboveList();
            LeftRender();

            // Render Right Info
            PackageInfoUI.Render(RightRenders[0], RightRenders[1], RightRenders[2]);

            GUILayout.EndHorizontal();
        }

        protected abstract void Load();

        protected void LoadDefault()
        {

            UIConversionHelper.SortLocalPackages(_localPackages, SortMode);

            //_selectedPackage = _localPackages[SelectedPackageIndex];

            // Map local packages -> package header

            RegenerateHeaders();

            //_selectedPackage = _localPackages[SelectedPackageIndex];
            if (SelectedPackageIndex >= _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);
            _selectedPackage = (CustomLocalPackage)_pkgHeaders.ToArray()[SelectedPackageIndex].Package;

            _selectedBeatmaps =
                UIConversionHelper.CustomBeatmapInfosToBeatmapHeaders(_selectedPackage.PkgSongs);
            if (SelectedBeatmapIndex >= _selectedBeatmaps.Count)
            {
                SetSelectedBeatmapIndex?.Invoke(_selectedBeatmaps.Count - 1);
            }

            _selectedBeatmap = _selectedPackage.PkgSongs.SelectMany(s => s.CustomBeatmaps).ToArray()[SelectedBeatmapIndex];
        }

        private void RenderSearchbar()
        {
            Searchbar.Render(_searchQuery, searchTextInput =>
            {
                _searchQuery = searchTextInput;
                SetSortMode(SortMode);

            });
        }
        private void Fallbacks()
        {
            LeftRender = () =>
            {
                GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                RenderSearchbar();
                SortModePickerUI.Render(SortMode, SetSortMode);
                PackageListUI.Render($"Packages in {Folder}", _pkgHeaders, SelectedPackageIndex, SetSelectedPackageIndex);
                AssistAreaUI.Render();
                GUILayout.EndVertical();
            };

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
                    PackageBeatmapPickerUI.Render(_selectedBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);

                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.RealDifficulty}"))
                    {
                        // Play a local beatmap
                        CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.FolderName);
                        PlaySong(_selectedBeatmap);
                    }
                }
            ];
        }

        protected void RegenerateHeaders()
        {
            List<PackageHeader> headers = new List<PackageHeader>(_localPackages.Count);
            int packageIndex = -1;
            foreach (var p in _localPackages)
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

                if (!UIConversionHelper.PackageMatchesFilter(p, _searchQuery))
                    continue;

                string creator = creators.Join(x => x, " | ");
                string name = names.Join(x => x, ", ");

                bool isNew = false;
                headers.Add(new PackageHeader(name, songs.Count, p.PkgSongs.SelectMany(s => s.Beatmaps).Count(), creator, isNew, BeatmapDownloadStatus.Downloaded, packageIndex, p));
            }

            _pkgHeaders = headers;
        }
    }
}
