using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.Highscore;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using HarmonyLib;
using Pri.LongPath;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UI
{
    public class PackageListUIOnline : AbstractPackageList<LocalPackageManager, CustomServerPackage, CustomServerBeatmap>
    {

        private CustomServerPackageList _list;
        private bool _reloading;
        private bool _loaded;
        private string _failure;
        private KeyValuePair<string,CustomServerBeatmap>[]  _selectedServerBeatmapKVPairs;

        protected int _selectedHeaderIndex = 0;

        public PackageListUIOnline(LocalPackageManager manager) : base(manager)
        {  
            LeftRender = () =>
            {
                GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                RenderReloadHeader($"Got {_list.Packages.Length} Packages", () =>
                {
                    GUILayout.FlexibleSpace();
                    DifficultyPickerUI.Render(_difficulty, SetDifficulty);
                    GUILayout.FlexibleSpace();
                    SortModePickerUI.Render(SortMode, SetSortMode);
                });
                RenderSearchbar();
                PackageListUI.Render($"Server Packages", _pkgHeaders, SelectedPackageIndex, SetSelectedPackageIndex);
                AssistAreaUI.Render();
                GUILayout.EndVertical();
            };
            
            RightRenders = [
                () =>
                    {
                        PackageInfoTopUI.Render(_selectedBeatmaps, _selectedBeatmapIndex);
                    },
                    () =>
                    {
                        if (_selectedPackage.Beatmaps.Count != 0)
                        {
                            // LOCAL high score
                            var downloadStatus = CustomBeatmaps.Downloader.GetDownloadStatus(_selectedPackage.ServerURL);
                            var selectedBeatmapKeyPath = _selectedServerBeatmapKVPairs[_selectedBeatmapIndex].Key;
                            if (downloadStatus == BeatmapDownloadStatus.Downloaded)
                            {
                                var localPackages = CustomBeatmaps.LocalServerPackages;
                                try
                                {
                                    var (_, selectedBeatmap) =
                                        localPackages.FindCustomBeatmapInfoFromServer(_selectedPackage.ServerURL,
                                            selectedBeatmapKeyPath);
                                    string highScoreKey = selectedBeatmap.Info.SongPath;
                                    PersonalHighScoreUI.Render(highScoreKey);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning("Invalid package found: (ignoring)");
                                    Debug.LogException(e);
                                }
                            }
                            // SERVER high scores
                            HighScoreListUI.Render(UserServerHelper.GetHighScoreBeatmapKeyFromServerBeatmap(_selectedPackage.ServerURL, selectedBeatmapKeyPath));
                        }
                    },
                    () =>
                    {
                        if (_selectedPackage.Beatmaps.Count == 0)
                        {
                            GUILayout.Label("No beatmaps found...");
                        }
                        else
                        {
                            var downloadStatus = CustomBeatmaps.Downloader.GetDownloadStatus(_selectedPackage.ServerURL);

                            _selectedBeatmap = _selectedServerBeatmapKVPairs[_selectedBeatmapIndex].Value;
                            var selectedBeatmapKeyPath = _selectedServerBeatmapKVPairs[_selectedBeatmapIndex].Key;

                            string buttonText = "??";
                            string buttonSub = "";
                            switch (downloadStatus)
                            {
                                case BeatmapDownloadStatus.Downloaded:
                                    buttonText = "PLAY";
                                    buttonSub = $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}";
                                    break;
                                case BeatmapDownloadStatus.CurrentlyDownloading:
                                    buttonText = "Downloading...";
                                    break;
                                case BeatmapDownloadStatus.Queued:
                                    buttonText = "Queued for download...";
                                    break;
                                case BeatmapDownloadStatus.NotDownloaded:
                                    buttonText = "DOWNLOAD";
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            PackageBeatmapPickerUI.Render(_selectedBeatmaps, _selectedBeatmapIndex, newVal => _selectedBeatmapIndex = newVal);

                            if (ArcadeHelper.UsingHighScoreProhibitedAssists())
                            {
                                GUILayout.Label("<size=24><b>USING ASSISTS</b></size> (no high score)");
                            }
                            else if (!CustomBeatmaps.UserSession.LoggedIn)
                            {
                                GUILayout.Label("<b>Register above to post your own high scores!<b>");
                            }

                            bool buttonPressed = PlayButtonUI.Render(buttonText, buttonSub);
                            switch (downloadStatus)
                            {
                                case BeatmapDownloadStatus.Downloaded:
                                    try
                                    {
                                        var localPackages = CustomBeatmaps.LocalServerPackages;
                                        // Play a local beatmap
                                        var (localPackage, customBeatmapInfo) =
                                            localPackages.FindCustomBeatmapInfoFromServer(_selectedPackage.ServerURL,
                                                selectedBeatmapKeyPath);
                                        // Preview, cause we can!
                                        if (customBeatmapInfo != null)
                                        {
                                            //ForceSelectSong(customBeatmapInfo);
                                            BGM.PlaySongPreview(SongDatabase.GetBeatmapItemByPath(customBeatmapInfo.Info.SongPath));
                                        }
                                        if (buttonPressed)
                                        {
                                            CustomBeatmaps.PlayedPackageManager.RegisterPlay(localPackage.FolderName);
                                            PlaySong(customBeatmapInfo);

                                        }
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        if (PlayButtonUI.Render("INVALID PACKAGE: Redownload"))
                                        {
                                            // Delete + redownload
                                            Directory.Delete(CustomPackageHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir, _selectedPackage.ServerURL), true);
                                            CustomBeatmaps.Downloader.QueueDownloadPackage(_selectedPackage.ServerURL);
                                        }
                                    }

                                    break;
                                case BeatmapDownloadStatus.NotDownloaded:
                                    BGM.StopSongPreview();
                                    if (buttonPressed)
                                    {
                                        CustomBeatmaps.Downloader.QueueDownloadPackage(_selectedPackage.ServerURL);
                                    }
                                    break;
                            }

                        }
                    }
                ];
            
        }

        protected override void Init(LocalPackageManager manager)
        {
            try
            {
                
                Manager.PackageUpdated += package =>
                {
                    Reload(true, true);
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

                ReloadPackageList();
                //Reload(false);
            } catch (Exception e)
            {
                CustomBeatmaps.Log.LogError(e);
            }
            
        }

        public override void Reload(bool retain)
        {
            Reload(retain, false);
        }

        public void Reload(bool retain, bool delayed)
        {
            if (_reloading)
                return;
            _reloading = true;
            Task.Run(async () => {
                if (delayed)
                    await Task.Delay(300);

                var pkg = _selectedPackage;
                //_localPackages = _pkgHeaders;
                SortPackages();
                RegenerateHeaders();

                // Try to keep the same package selected when retain is true

                if (retain)
                {
                    var packages = _pkgHeaders.Select(h => h.Package).ToList();
                    if (packages.Contains(pkg))
                    {
                        SetSelectedPackageIndex(packages.IndexOf(pkg));
                        _reloading = false;
                        return;
                    }
                }

                if (_selectedPackageIndex > _pkgHeaders.Count)
                {
                    SetSelectedPackageIndex(_pkgHeaders.Count - 1);
                    _reloading = false;
                    return;
                }


                MapPackages();

                _reloading = false;
            });
        }

        protected override void MapPackages()
        {
            try
            {
                if (_selectedPackageIndex > _pkgHeaders.Count)
                    SetSelectedPackageIndex(_pkgHeaders.Count - 1);
                _selectedPackage = (CustomServerPackage)_pkgHeaders[_selectedPackageIndex].Package;
                _selectedServerBeatmapKVPairs = _selectedPackage.Beatmaps.ToArray();
                // Jank overflow fix between packages with varying beatmap sizes
                if (_selectedBeatmapIndex > _selectedPackage.Beatmaps.Count)
                    SetSelectedBeatmapIndex(_selectedPackage.Beatmaps.Count - 1);
                _selectedBeatmaps = new List<BeatmapHeader>(_selectedPackage.Beatmaps.Count);
                foreach (var bmapKVPair in _selectedServerBeatmapKVPairs)
                {
                    var bmap = bmapKVPair.Value;
                    _selectedBeatmaps.Add(new BeatmapHeader(
                        bmap.SongName,
                        bmap.Artist,
                        bmap.Creator,
                        bmap.Difficulty,
                        null,
                        bmap.Level,
                        bmap.FlavorText,
                        new()
                    ));
                }
            } 
            catch (Exception e)
            {
                CustomBeatmaps.Log.LogError(e);
            }
            
        }


        private static bool _headerRegenerationQueued;

        protected override void RegenerateHeaders()
        {
            _pkgHeaders = new List<PackageHeader>(_list.Packages.Length);
            foreach (var serverPackage in _list.Packages)
            {
                // Get unique song count
                HashSet<string> songs = new HashSet<string>();
                HashSet<string> names = new HashSet<string>();
                HashSet<string> creators = new HashSet<string>();
                foreach (var bmap in serverPackage.Beatmaps.Values)
                {
                    songs.Add(bmap.AudioFileName);
                    names.Add(bmap.SongName);
                    creators.Add(bmap.Creator);
                }

                if (!UIConversionHelper.PackageMatchesFilter(serverPackage, _searchQuery))
                    continue;

                if (!UIConversionHelper.PackageHasDifficulty(serverPackage, _difficulty))
                    continue;

                string creator = creators.Join(x => x, " | ");
                string name = names.Join(x => x, ", ");

                string serverUrl = serverPackage.ServerURL;
                var downloadStatus = CustomBeatmaps.Downloader.GetDownloadStatus(serverUrl);
                bool isNew = !CustomBeatmaps.PlayedPackageManager.HasPlayed(
                    CustomPackageHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir, serverUrl));
                _pkgHeaders.Add(new PackageHeader(name, songs.Count, serverPackage.Beatmaps.Count, creator, isNew, downloadStatus, serverPackage));

                if (_selectedHeaderIndex > _pkgHeaders.Count)
                {
                    _selectedHeaderIndex = 0;
                }
            }

        }

        private void RenderReloadHeader(string label, Action renderHeaderSortPicker = null) // ok this part is jank but that's all I need
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", GUILayout.ExpandWidth(false)))
            {
                ReloadPackageList();
                Reload(false);
            }
            GUILayout.Label(label, GUILayout.ExpandWidth(false));

            renderHeaderSortPicker?.Invoke();
            GUILayout.EndHorizontal();
            //GUILayout.Space(20);
        }

        private void ReloadPackageList()
        {
            ScheduleHelper.SafeLog("RELOADING Packages from Server...");
            // Also reload high scores because... yeah
            CustomBeatmaps.ServerHighScoreManager.Reload();
            _failure = "loading...";
            CustomPackageHelper.FetchServerPackageList(CustomBeatmaps.BackendConfig.ServerPackageList).ContinueWith(result =>
            {
                if (result.Exception != null)
                {
                    foreach (var ex in result.Exception.InnerExceptions)
                        _failure += ex.Message + " ";
                    EventBus.ExceptionThrown?.Invoke(result.Exception);
                    return;
                }
                _failure = null;
                _loaded = false; // Impromptu mutex :P
                _list = result.Result;
                Reload(true, true);
                _loaded = true;
            });
        }

        protected override void SortPackages()
        {
            if (_list.Packages == null) // forgor guard
                return;

            var l = _list.Packages.ToList();
            UIConversionHelper.SortServerPackages(l, SortMode);
            _list.Packages = l.ToArray();
        }

        protected override void RunSong()
        {
            throw new NotImplementedException();
        }

        public override void Render(Action onRenderAboveList)
        {
  
            if (_reloading)
            {
                onRenderAboveList();
                GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                RenderReloadHeader($"Got {_list.Packages.Length} Packages", () =>
                {
                    GUILayout.FlexibleSpace();
                    DifficultyPickerUI.Render(_difficulty, SetDifficulty);
                    GUILayout.FlexibleSpace();
                    SortModePickerUI.Render(SortMode, SetSortMode);
                });
                RenderSearchbar();
                PackageListUI.Render($"Server Packages", new(), 0, SetSelectedPackageIndex);
                AssistAreaUI.Render();
                GUILayout.EndVertical();
                PackageInfoUI.Render(RightRenders[0], RightRenders[1], RightRenders[2]);

                GUILayout.EndHorizontal();
                return;
            }


            if (_failure != null)
            {
                onRenderAboveList();
                RenderReloadHeader($"Failed to grab packages from server: {_failure}");
                return;
            }

            //var loadState = CustomBeatmaps.LocalServerPackages.InitialLoadState;
            if (LoadState.Loading)
            {
                onRenderAboveList();
                float p = (float)LoadState.Loaded / LoadState.Total;
                ProgressBarUI.Render(p, $"Loaded {LoadState.Loaded} / {LoadState.Total}", GUILayout.ExpandWidth(true), GUILayout.Height(32));
                return;
            }

            if (!_loaded)
            {
                onRenderAboveList();
                RenderReloadHeader("Loading...");
                return;
            }

            if (_list.Packages.Length == 0 || _pkgHeaders == null)
            {
                onRenderAboveList();
                RenderReloadHeader("No packages found!");
                return;
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
