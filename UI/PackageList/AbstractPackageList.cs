using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
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
    public abstract class AbstractPackageList<TManager, TPackage, TBeatmap> 
        where TManager : IPackageInterface<CustomLocalPackage>
        where TPackage : ICustomPackage<TBeatmap>
        where TBeatmap : ICustomBeatmap
    {
        protected TManager Manager;

        protected List<CustomLocalPackage> _localPackages;
        protected List<CustomLocalPackage> LocalPackages => Manager.Packages;
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

        protected List<BeatmapHeader> _selectedBeatmaps;
        protected TBeatmap _selectedBeatmap;
        protected TPackage _selectedPackage;

        protected List<PackageHeader> _pkgHeaders = new();
        //protected Dictionary<PackageHeader, TPackage> _pkgHeadersMap;

        protected Action LeftRender;
        protected Action[] RightRenders;
        protected string _searchQuery;

        public AbstractPackageList(TManager manager)
        {
            Manager = manager;
            Init(manager);
        }

        protected virtual void Init(TManager manager)
        {
            Manager.PackageUpdated += package =>
            {
                Reload(true);
            };

            Fallbacks();

            // Action Setup
            SetSelectedPackageIndex = (val) => {
                _selectedPackageIndex = val;
                MapPackages();
            };

            SetSelectedBeatmapIndex = (val) => {
                _selectedBeatmapIndex = val;
                MapPackages();
            };

            SetSortMode = (val) => {
                _sortMode = val;
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
                var packages = _pkgHeaders.Select(h => h.Package).ToList();
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
            var headers = new List<PackageHeader>(_localPackages.Count);
            //var headersMap = new Dictionary<PackageHeader, TPackage>(_localPackages.Count);
            foreach (CustomLocalPackage p in _localPackages)
            {

                if (!UIConversionHelper.PackageMatchesFilter(p, _searchQuery))
                    continue;

                var toAdd = new PackageHeader(package: p);
                headers.Add(toAdd);
                //headersMap.Add(toAdd, p);
            }

            _pkgHeaders = headers;
            //_pkgHeadersMap = headersMap;
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

                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                    {
                        // Play a local beatmap
                        CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.FolderName);
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
                float p = (float)loadState.Loaded / loadState.Total;
                ProgressBarUI.Render(p, $"Loaded {loadState.Loaded} / {loadState.Total}", GUILayout.ExpandWidth(true), GUILayout.Height(32));
                return;
            }

            // No packages?

            if (_pkgHeaders.Count == 0)
            {
                onRenderAboveList();
                RenderSearchbar();
                GUILayout.Label($"No Packages Found in {Folder}");
                return;
            }

            // Clamp packages to fit in the event of package list changing while the UI is open
            if (SelectedPackageIndex > _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);

            // Preview audio
            if (_selectedBeatmap.SongPath != null)
            {
                var previewsong = SongDatabase.GetBeatmapItemByPath(_selectedBeatmap.SongPath);
                BGM.PlaySongPreview(previewsong);
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
