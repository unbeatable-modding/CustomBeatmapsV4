using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.Util;
using UnityEngine;
using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UISystem
{
    // the horror
    public abstract class AbstractPackageList
    {
        protected PackageManagerGeneric Manager;

        protected List<CustomPackage> _localPackages = new();
        protected List<CustomPackage> LocalPackages => Manager.Packages;
        protected string Folder => Manager.Folder;        
        protected InitialLoadStateData LoadState => Manager.InitialLoadState;

        protected int _selectedPackageIndex = 0;
        protected int SelectedPackageIndex => _selectedPackageIndex;
        protected Action<int> SetSelectedPackageIndex;

        protected int _selectedBeatmapIndex = 0;
        protected int SelectedBeatmapIndex => _selectedBeatmapIndex;
        protected Action<int> SetSelectedBeatmapIndex;

        protected SortMode _sortMode = SortMode.New;
        protected SortMode SortMode => _sortMode;
        protected Action<SortMode> SetSortMode;

        protected Difficulty _difficulty = Difficulty.All;
        protected Action<Difficulty> SetDifficulty;

        protected List<BeatmapData> _selectedBeatmaps;
        protected BeatmapData _selectedBeatmap;
        protected CustomPackage _selectedPackage;

        protected List<CustomPackage> _pkgHeaders = new();

        protected Action LeftRender;
        protected Action[] RightRenders;
        protected string _searchQuery;

        public AbstractPackageList(PackageManagerGeneric manager)
        {
            Manager = manager;
            Init(manager);
        }

        protected virtual void Init(PackageManagerGeneric manager)
        {
            Manager.PackageUpdated += package =>
            {
                Reload(true);
            };

            Fallbacks();

            // Action Setup
            SetSelectedPackageIndex = (val) => {
                _selectedPackageIndex = val;
                if (val < 0)
                    _selectedPackageIndex = 0;
                MapPackages();
            };

            SetSelectedBeatmapIndex = (val) => {
                _selectedBeatmapIndex = val;
                if (val < 0)
                    _selectedBeatmapIndex = 0;
                MapPackages();
            };

            SetSortMode = (val) => {
                _sortMode = val;
                Reload(true);
            };

            SetDifficulty = (val) => {
                _difficulty = val;
                Reload(true);
            };

            Reload(false);
        }

        // Load stuff
        public virtual void Reload(bool retain)
        {
            var pkg = _selectedPackage;
            _localPackages = LocalPackages;
            SortPackages();
            RegenerateHeaders();

            // Try to keep the same package selected when retain is true
            if (retain)
            {
                var packages = _pkgHeaders.ToList();
                if (packages.Contains(pkg))
                {
                    SetSelectedPackageIndex(packages.IndexOf(pkg));
                    return;
                }
            }

            if (SelectedPackageIndex > _pkgHeaders.Count)
            {
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);
                return;
            }

            MapPackages();
        }
        protected abstract void SortPackages();
        protected virtual void RegenerateHeaders()
        {
            var headers = new List<CustomPackage>(_localPackages.Count);
            foreach (CustomPackage p in _localPackages)
            {
                if (!UIConversionHelper.PackageHasDifficulty(p, _difficulty))
                    continue;

                if (!UIConversionHelper.PackageMatchesFilter(p, _searchQuery))
                    continue;

                //var toAdd = new CustomPackage(package: p);
                headers.Add(p);
            }

            _pkgHeaders = headers;
        }

        protected abstract void MapPackages();
        protected virtual void RenderSearchbar()
        {
            Searchbar.Render(_searchQuery, searchTextInput =>
            {
                _searchQuery = searchTextInput;
                Reload(true);

            });
        }
        protected void Fallbacks()
        {
            LeftRender = () =>
            {
                GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal();
                DifficultyPickerUI.Render(_difficulty, SetDifficulty);
                GUILayout.FlexibleSpace();
                SortModePickerUI.Render(SortMode, SetSortMode);
                GUILayout.EndHorizontal();
                RenderSearchbar();
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

                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                    {
                        // Play a local beatmap
                        CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.BaseDirectory);
                        RunSong();
                    }
                }
            ];
        }
        protected abstract void RunSong();
        public virtual void Render(Action onRenderAboveList)
        {
            var loadState = LoadState;
            if (loadState.Loading)
            {
                onRenderAboveList();
                float p = (float)loadState.Loaded / loadState.Total;
                ProgressBarUI.Render(p, $"Loaded {loadState.Loaded} / {loadState.Total}", GUILayout.ExpandWidth(true), GUILayout.Height(32));
                return;
            }

            // No packages?

            if (_pkgHeaders.Count == 0)
            {
                onRenderAboveList();
                //RenderSearchbar();
                LeftRender();
                GUILayout.Label($"No Packages Found in {Folder}");
                GUILayout.EndHorizontal();
                
                return;
            }

            // Clamp packages to fit in the event of package list changing while the UI is open
            if (SelectedPackageIndex > _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);

            // Preview audio
            if (_selectedBeatmap != null)
            {
                if (_selectedBeatmap.SongPath != null)
                {
                    var previewsong = SongDatabase.GetBeatmapItemByPath(_selectedBeatmap.SongPath);
                    BGM.PlaySongPreview(previewsong);
                }
            }


            // Render
            onRenderAboveList();
            LeftRender();

            // Render Right Info
            PackageInfoUI.Render(RightRenders[0], RightRenders[1], RightRenders[2]);

            GUILayout.EndHorizontal();
        }
    }
}
