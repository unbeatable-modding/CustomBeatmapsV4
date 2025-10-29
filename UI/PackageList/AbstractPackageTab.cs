using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UISystem
{
    // the horror
    public abstract class AbstractPackageTab<P>
        where P : CustomPackage
    {
        protected PackageManagerGeneric<P> Manager;

        /// <summary>
        /// List of Packages rendered by the UI
        /// </summary>
        protected List<P> _pkgHeaders = new();
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

        protected List<BeatmapData> _selectableBeatmaps;
        protected BeatmapData _selectedBeatmap;
        protected P _selectedPackage;

        protected Action LeftRender;
        protected Action[] RightRenders;
        protected string _searchQuery;

        public AbstractPackageTab(PackageManagerGeneric<P> manager)
        {
            Manager = manager;
            Init(manager);
        }

        protected virtual void Init(PackageManagerGeneric<P> manager)
        {
            Manager.PackageUpdated += () =>
            {
                _pkgHeaders = Manager.Packages;
                SortPackages();
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
                SortPackages();
                Reload(true);
            };

            SetDifficulty = (val) => {
                _difficulty = val;
                Reload(true);
            };

            _pkgHeaders = Manager.Packages;
            SortPackages();
            Reload(false);
        }

        // Load stuff
        /// <summary>
        /// Reload the Package List (for UI)
        /// </summary>
        /// <param name="retain">If true, this tries to remember the package currently selected by the user</param>
        public virtual void Reload(bool retain)
        {
            // Abort if no packages
            if (Manager.Packages.Count < 1)
                return;

            var pkg = _selectedPackage;
            _pkgHeaders = Manager.Packages;
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
            var headers = new List<P>(_pkgHeaders.Count);
            foreach (P p in _pkgHeaders)
            {
                if (!UIConversionHelper.PackageHasDifficulty(p, _difficulty))
                    continue;

                if (!UIConversionHelper.PackageMatchesFilter(p, _searchQuery))
                    continue;

                headers.Add(p);
            }


            _pkgHeaders = headers;
        }

        protected virtual bool MapPackages()
        {
            // No packages
            if (_pkgHeaders.Count < 1)
                return false;

            // Fix Package Index being out of bounds
            if (SelectedPackageIndex >= _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);

            _selectedPackage = _pkgHeaders[SelectedPackageIndex];

            _selectableBeatmaps = _selectedPackage.BeatmapDatas.ToList();
            // Fix Beatmap Index being out of bounds
            if (SelectedBeatmapIndex >= _selectableBeatmaps.Count)
                SetSelectedBeatmapIndex?.Invoke(_selectableBeatmaps.Count - 1);

            _selectedBeatmap = _selectedPackage.BeatmapDatas[SelectedBeatmapIndex];

            return true;
        }

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
                    PackageInfoTopUI.Render(_selectableBeatmaps, SelectedBeatmapIndex);
                },
                () =>
                {
                },
                () =>
                {
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
        protected virtual void RunSong()
        {
            ArcadeHelper.PlaySong(_selectedBeatmap);
        }
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
            if (Manager.Packages.Count == 0)
            {
                onRenderAboveList();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"No Packages Found in {Folder}");
                GUILayout.EndHorizontal();
                return;
            }

            // Clamp packages to fit in the event of package list changing while the UI is open
            if (SelectedPackageIndex > _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);

            PreviewAudio();

            // Render
            onRenderAboveList();
            LeftRender();

            // Render Right Info
            PackageInfoUI.Render(RightRenders[0], RightRenders[1], RightRenders[2]);

            GUILayout.EndHorizontal();
        }

        protected void PreviewAudio()
        {
            if (LoadingArcade)
                return;
            if (_selectedBeatmap.BeatmapPointer != null)
            {
                if (_selectedBeatmap.SongPath != null)
                {
                    var previewsong = SongDatabase.GetBeatmapItemByPath(_selectedBeatmap.SongPath);
                    BGM.PlaySongPreview(previewsong);
                }
            }
        }
    }
}
