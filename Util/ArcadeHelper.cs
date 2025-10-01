using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using Utilities;
using CustomBeatmaps.CustomPackages;
using Arcade.UI.SongSelect;
using UnityEngine.SceneManagement;
using Arcade.UI;
using FMODUnity;
using CustomBeatmaps.Patches;

using static Rhythm.BeatmapIndex;
using File = Pri.LongPath.File;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util.CustomData;
using System.Threading.Tasks;

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
        private static List<Song> songList = new();

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

        /// <summary>
        /// Forcefully reload the arcade
        /// </summary>
        public static void ReloadArcadeList()
        {
            LoadCustomSongs();
            if (SceneManager.GetActiveScene().name != "ArcadeModeMenu")
                return;
            var currentArcade = ArcadeSongDatabase.Instance;
            var arcade = Traverse.Create(currentArcade);
            var _songDatabase = arcade.Field("_songDatabase").GetValue<Dictionary<string, ArcadeSongDatabase.BeatmapItem>>();
            //var allCategory = arcade.Field("allCategory").GetValue<BeatmapIndex.Category>();
            //var SelectableCategories = arcade.Field("allCategory").GetValue<List<BeatmapIndex.Category>>();
            //SelectableCategories = BeatmapIndex.defaultIndex.GetVisibleCategories().Prepend(allCategory).ToList();
            _songDatabase.Clear();
            arcade.Method("LoadDatabase").GetValue();
            arcade.Method("RefreshSongList").GetValue();
        }

        private static bool _loadingSongs = false;
        public static void LoadCustomSongs()
        {
            while (_loadingSongs) { }
            _loadingSongs = true;

            CleanSongs();
            var fetch = PackageHelper.GetAllCustomSongInfos.ToList();
            lock (fetch)
            {
                foreach (Song s in fetch)
                {
                    //CustomBeatmaps.Log.LogDebug($"{s.name}");

                    if (!_songs.ContainsKey(s.name))
                    {
                        songs.Add(s);
                        _songs.Add(s.name, s);
                        _visibleSongs.Add(s);
                        songNames.Add(s.name);
                        songList.Add(s);
                        //_categorySongs[s.Category].Add(s);
                    }
                }
                _loadingSongs = false;
            }
            
        }

        // Remove all modded songs for when we want to reload the database
        private static void CleanSongs()
        {

            var killList = songs.Where(s => s is CustomSong).Select(s => s.name).ToList();

            //CustomBeatmaps.Log.LogDebug("Trying to kill songs");
            foreach (string k in killList)
            {
                songs.Remove(_songs[k]);
                _visibleSongs.Remove(_songs[k]);
                _songs.Remove(k);
                songNames.Remove(k);
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

    

