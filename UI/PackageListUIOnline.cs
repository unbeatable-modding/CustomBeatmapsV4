using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.Highscore;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using HarmonyLib;
using Pri.LongPath;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;

namespace CustomBeatmaps.UI
{
    public class PackageListUIOnline : AbstractPackageList<CustomPackageServer>
    {

        //private CustomServerPackageList _list = new();
        //private bool _reloading;
        //private bool _loaded;
        //private string _failure;
        //private KeyValuePair<string,CustomServerBeatmap>[]  _selectedServerBeatmapKVPairs = new KeyValuePair<string, CustomServerBeatmap>[1];

        private BeatmapDownloadStatus DLStatus;

        private string ServerURL
        {
            get
            {
                if (_selectedPackage is CustomPackageServer)
                {
                    return ((CustomPackageServer)_selectedPackage).ServerURL;
                }
                return null;
            }
        }

        protected int _selectedHeaderIndex = 0;

        public PackageListUIOnline(PackageManagerServer manager) : base(manager)
        {  
            LeftRender = () =>
            {
                GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                RenderReloadHeader($"Got {_localPackages.Count} Packages", () =>
                {
                    GUILayout.FlexibleSpace();
                    DifficultyPickerUI.Render(_difficulty, SetDifficulty);
                    GUILayout.FlexibleSpace();
                    SortModePickerUI.Render(SortMode, SetSortMode);
                });
                RenderSearchbar();
                if (_pkgHeaders.Count != 0)
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
                        if (_selectedPackage.BeatmapDatas.Length != 0)
                        {
                            // LOCAL high score

                            if (DLStatus == BeatmapDownloadStatus.Downloaded)
                            {
                                try
                                {
                                    PersonalHighScoreUI.Render(_selectedBeatmap.SongPath);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning("Invalid package found: (ignoring)");
                                    Debug.LogException(e);
                                }
                            }
                            // SERVER high scores
                            //HighScoreListUI.Render(UserServerHelper.GetHighScoreBeatmapKeyFromServerBeatmap(ServerURL, _selectedBeatmap.DirectoryPath));
                        }
                    },
                    () =>
                    {
                        if (_selectedPackage.BeatmapDatas.Length == 0)
                        {
                            GUILayout.Label("No beatmaps found...");
                        }
                        else
                        {
                            //var downloadStatus = CustomBeatmaps.Downloader.GetDownloadStatus((CustomPackageServer)_selectedPackage);
                            //var downloadStatus = BeatmapDownloadStatus.Downloaded;

                            //_selectedBeatmap = _selectedServerBeatmapKVPairs[_selectedBeatmapIndex].Value;
                            //var selectedBeatmapKeyPath = _selectedServerBeatmapKVPairs[_selectedBeatmapIndex].Key;

                            string buttonText = "??";
                            string buttonSub = "";
                            switch (DLStatus)
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

                            PackageBeatmapPickerUI.Render(_selectedBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);

                            if (ArcadeHelper.UsingHighScoreProhibitedAssists())
                            {
                                GUILayout.Label("<size=24><b>USING ASSISTS</b></size> (no high score)");
                            }
                            else if (!CustomBeatmaps.UserSession.LoggedIn != (CustomBeatmaps.UserSession.LocalSessionExists() != CustomBeatmaps.UserSession.LoginFailed))
                            {
                                GUILayout.Label("<b>Register above to post your own high scores!<b>");
                            }

                            bool buttonPressed = PlayButtonUI.Render(buttonText, buttonSub);
                            switch (DLStatus)
                            {
                                case BeatmapDownloadStatus.Downloaded:
                                    try
                                    {
                                        if (buttonPressed)
                                        {
                                            CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.BaseDirectory);
                                            RunSong();

                                        }
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        if (PlayButtonUI.Render("INVALID PACKAGE: Redownload"))
                                        {
                                            // Delete + redownload
                                            Directory.Delete(_selectedPackage.BaseDirectory);
                                            CustomBeatmaps.Downloader.QueueDownloadPackage(_selectedPackage);
                                        }
                                    }

                                    break;
                                case BeatmapDownloadStatus.NotDownloaded:
                                    BGM.StopSongPreview();
                                    if (buttonPressed)
                                    {
                                        CustomBeatmaps.Downloader.QueueDownloadPackage(_selectedPackage);
                                    }
                                    break;
                            }

                        }
                    }
                ];
        }

        private void RenderReloadHeader(string label, Action renderHeaderSortPicker = null) // ok this part is jank but that's all I need
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", GUILayout.ExpandWidth(false)))
            {
                //ReloadPackageList();
                //Reload(false);
                Manager.ReloadAll();
            }
            GUILayout.Label(label, GUILayout.ExpandWidth(false));

            renderHeaderSortPicker?.Invoke();
            GUILayout.EndHorizontal();
            //GUILayout.Space(20);
        }

        protected override void RunSong()
        {
            ArcadeHelper.PlaySong(_selectedBeatmap);
        }

        protected override void SortPackages()
        {
            UIConversionHelper.SortPackages(_localPackages, SortMode);
        }

        /*
        protected override void Init(PackageManagerGeneric manager)
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
        */

        protected override void MapPackages()
        {
            if (_pkgHeaders.Count < 1)
                return;

            if (SelectedPackageIndex >= _pkgHeaders.Count)
                SetSelectedPackageIndex(_pkgHeaders.Count - 1);
            _selectedPackage = _pkgHeaders[SelectedPackageIndex];

            _selectedBeatmaps = _selectedPackage.BeatmapDatas.ToList();

            if (SelectedBeatmapIndex >= _selectedBeatmaps.Count)
                SetSelectedBeatmapIndex?.Invoke(_selectedBeatmaps.Count - 1);

            _selectedBeatmap = _selectedPackage.BeatmapDatas[SelectedBeatmapIndex];

            DLStatus = _selectedPackage.DownloadStatus;
            //DLStatus = CustomBeatmaps.Downloader.GetDownloadStatus((CustomPackageServer)_selectedPackage);
            //DLStatus = _selectedPackage.DownloadStatus = CustomBeatmaps.Downloader.GetDownloadStatus((CustomPackageServer)_selectedPackage);
            //PreviewAudio();
        }

        /*
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
                    zzzCustomPackageHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir, serverUrl));
                _pkgHeaders.Add(new PackageHeader(name, songs.Count, serverPackage.Beatmaps.Count, creator, isNew, downloadStatus, serverPackage));

                if (_selectedHeaderIndex > _pkgHeaders.Count)
                {
                    _selectedHeaderIndex = 0;
                }
            }

        }

        private void ReloadPackageList()
        {
            ScheduleHelper.SafeLog("RELOADING Packages from Server...");
            // Also reload high scores because... yeah
            CustomBeatmaps.ServerHighScoreManager.Reload();
            _failure = "loading...";
            zzzCustomPackageHelper.FetchServerPackageList(CustomBeatmaps.BackendConfig.ServerPackageList).ContinueWith(result =>
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
    */
    }
    
}
