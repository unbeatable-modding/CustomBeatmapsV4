using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Rhythm;
using Arcade.UI.SongSelect;
using UnityEngine.SceneManagement;
using Arcade.UI;
using FMODUnity;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util.CustomData;
using System.Threading.Tasks;
using System.Threading;

using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.Util
{
    public class ArcadeHelper
    {

        private static Traverse traverse = Traverse.Create(BeatmapIndex.defaultIndex);
        private static List<string> songNames = traverse.Field("_songNames").GetValue<List<string>>();
        private static List<Song> songs = traverse.Field("songs").GetValue<List<Song>>();
        private static Dictionary<string, Song> _songs = traverse.Field("_songs").GetValue<Dictionary<string, Song>>();
        private static List<Song> _visibleSongs = traverse.Field("_visibleSongs").GetValue<List<Song>>();
        private static Dictionary<Category, List<Song>> _categorySongs = traverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();

        //static OsuBeatmapHotLoader HotLoader = new OsuBeatmapHotLoader();
        public static CustomBeatmapRoom[] Rooms
        {
            get
            {
                var rooms = new List<CustomBeatmapRoom>();
                rooms.AddRange(BaseRooms);
                if (CustomBeatmaps.ModConfig.ShowHiddenStuff)
                    rooms.AddRange(ExtraRooms);
                return rooms.ToArray();
            }
        }

        private static readonly CustomBeatmapRoom[] BaseRooms = {
            new CustomBeatmapRoom("Default", "TrainStationRhythm"),
            new CustomBeatmapRoom("NSR", "NSR_Stage"),
            new CustomBeatmapRoom("Green Screen", "GreenscreenRhythm"),
            // I am not re-implementing these just yet
            //new CustomBeatmapRoom("Practice Room", "PracticeRoomRhythm"),
            //new CustomBeatmapRoom("Tutorial", "Tutorial"),
            // This one would be interesting but we already have the tutorial screen
            //new CustomBeatmapRoom("Offset Wizard", "OffsetWizard")
        };
        private static readonly CustomBeatmapRoom[] ExtraRooms = {
            new CustomBeatmapRoom("Stage", "Stage")
        };

        private static readonly string DefaultBeatmapScene = "TrainStationRhythm";

        public static string GetSceneNameByIndex(int index)
        {
            if (index < 0 || index >= Rooms.Length)
            {
                return DefaultBeatmapScene;
            }

            return Rooms[index].SceneName;
        }

        public static bool LoadingArcade = false;

        /// <summary>
        /// Forcefully reload the arcade
        /// </summary>
        public static void ReloadArcadeList()
        {
            LoadingArcade = true;
            LoadCustomSongs();
            if (SceneManager.GetActiveScene().name != "ArcadeModeMenu")
                return;
            var currentArcade = ArcadeSongDatabase.Instance;
            var arcade = Traverse.Create(currentArcade);
            var _songDatabase = arcade.Field("_songDatabase").GetValue<Dictionary<string, ArcadeSongDatabase.BeatmapItem>>();
            _songDatabase.Clear();
            arcade.Method("LoadDatabase").GetValue();
            arcade.Method("RefreshSongList").GetValue();
            LoadingArcade = false;
        }

        private static bool _loadingSongs = false;
        public static void LoadCustomSongs()
        {
            // Weird logic to not freak out when called multiple times in quick succession
            while (_loadingSongs) { Thread.Sleep(200); }
            
            _loadingSongs = true;

            var killList = songs.Where(s => s is CustomSong).Select(s => s.name);

            // Make clones
            var songsTmp = songs.ToList();
            var _visibleSongsTmp = _visibleSongs.ToList();
            var _songsTmp = new Dictionary<string, Song>(_songs.AsEnumerable());
            var songNamesTmp = songNames.ToList();

            foreach (string k in killList)
            {
                songsTmp.Remove(_songsTmp[k]);
                _visibleSongsTmp.Remove(_songsTmp[k]);
                _songsTmp.Remove(k);
                songNamesTmp.Remove(k);
            }

            
            var fetch = PackageHelper.GetAllCustomSongInfos.ToList();
            lock (fetch)
            {
                foreach (Song s in fetch)
                {
                    //CustomBeatmaps.Log.LogDebug($"{s.name}");

                    if (!_songsTmp.ContainsKey(s.name))
                    {
                        songsTmp.Add(s);
                        _songsTmp.Add(s.name, s);
                        _visibleSongsTmp.Add(s);
                        songNamesTmp.Add(s.name);
                        //_categorySongs[s.Category].Add(s);
                    }
                }
                Task.Run(() =>
                {
                    traverse.Field("songs").SetValue(songsTmp);
                    traverse.Field("_songs").SetValue(_songsTmp);
                    traverse.Field("_visibleSongs").SetValue(_visibleSongsTmp);
                    traverse.Field("songNames").SetValue(songNamesTmp);

                    _loadingSongs = false;
                });
                
            }
            
        }

        public static ArcadeSongDatabase SongDatabase => ArcadeSongDatabase.Instance;
        public static ArcadeSongList SongList => ArcadeSongList.Instance;
        public static ArcadeBGMManager BGM => ArcadeBGMManager.Instance;

        public static void PlaySong(BeatmapData bmap)
        {
            PlaySong(bmap, GetSceneNameByIndex(CustomBeatmaps.Memory.SelectedRoom));
        }
        public static void PlaySong(BeatmapData bmap, string scene)
        {
            ForceSelectSong(bmap);
            var onSongPlaySound = Traverse.Create(SongDatabase).Field("onSongPlaySound").GetValue<EventReference>();

            if (bmap.BeatmapPointer != null)
            {
                if (!onSongPlaySound.IsNull)
                {
                    RuntimeManager.PlayOneShot(onSongPlaySound);
                }

                JeffBezosController.instance.DisableUIInputs();
                JeffBezosController.returnFromArcade = true;
                LevelManager.LoadArcadeLevel(bmap.InternalName, bmap.InternalDifficulty);
            }
        }
        public static void ForceSelectSong(BeatmapData bmap)
        {
            SongDatabase.SetCategory(bmap.Category.InternalCategory);
            SongDatabase.SetDifficulty(bmap.InternalDifficulty);
            SongList.SetSelectedSongIndex(SongDatabase.SongList.FindIndex(b => b.Path == bmap.SongPath));
        }
        public static void PlaySongEdit(BeatmapData bmap, bool enableCountdown = false)
        {
            //OsuEditorPatch.SetEditMode(true, enableCountdown, beatmap.Info.OsuPath, beatmap.Info.SongPath);
            PlaySong(bmap, DefaultBeatmapScene);
        }


        // CUSTOMBEATMAPS V3 STUFF TO CHANGE LATER BELOW


        public static HighScoreList LoadArcadeHighscores()
        {
            return HighScoreScreen.LoadHighScores(RhythmGameType.ArcadeMode);
        }

        public static float GetSongSpeed(int songSpeedIndex)
        {
            switch (songSpeedIndex)
            {
                case 0:
                    return 1f;
                case 1:
                    return 0.5f;
                case 2:
                    return 2f;
                default:
                    throw new InvalidOperationException($"Invalid song speed index: {songSpeedIndex}");
            }
        }

        public static bool UsingHighScoreProhibitedAssists()
        {
            // We include flip mode because _potentially_ it might be used to make high notes easier to hit?
            return (JeffBezosController.GetAssistMode() == 1) || GetSongSpeed(JeffBezosController.GetSongSpeed()) < 1 || (JeffBezosController.GetNoFail() == 1) || CustomBeatmaps.Memory.FlipMode;
        }

        public struct CustomBeatmapRoom
        {
            public string Name;
            public string SceneName;

            public CustomBeatmapRoom(string name, string sceneName)
            {
                Name = name;
                SceneName = sceneName;
            }
        }
    }
}

    

