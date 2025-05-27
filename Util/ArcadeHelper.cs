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

        public static void LoadCustomSongs()
        {
            CleanSongs();

            foreach (Song s in CustomPackageHelper.GetAllCustomSongs)
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

        }

        // Remove all modded songs for when we want to reload the database
        private static void CleanSongs()
        {
            var killList = new List<string>();

            songs.DoIf((Song s) => s is CustomSongInfo, s => killList.Add(s.name));

            //CustomBeatmaps.Log.LogDebug("Trying to kill songs");
            killList.ForEach((string k) => songs.Remove(_songs[k]));
            killList.ForEach((string k) => _visibleSongs.Remove(_songs[k]));
            killList.ForEach((string k) => _songs.Remove(k));
            killList.ForEach((string k) => songNames.Remove(k));
            return;
            foreach (Category c in CustomPackageHelper.customCategories)
            {
                _categorySongs.Keys.Where(k => k.Name == c.Name).ToList().ForEach(k => _categorySongs[k].Clear());
                //_categorySongs[c].Clear();
            }
        }

        public static ArcadeSongDatabase SongDatabase => ArcadeSongDatabase.Instance;
        public static ArcadeSongList SongList => ArcadeSongList.Instance;
        public static ArcadeBGMManager BGM => ArcadeBGMManager.Instance;
        private static BeatmapIndex BeatmapIndex => BeatmapIndex.defaultIndex;

        public static void SetArcadeBGM()
        {
            
        }

        public static void ForceSelectSong(CustomBeatmapInfo customBeatmapInfo)
        {
            SongDatabase.SetCategory(customBeatmapInfo.Category);
            SongDatabase.SetDifficulty(customBeatmapInfo.difficulty);
            SongList.SetSelectedSongIndex(SongDatabase.SongList.FindIndex(b => b.Path == customBeatmapInfo.Path));
        }

        public static void PlaySong(CustomBeatmapInfo customBeatmapInfo)
        {
            PlaySong(customBeatmapInfo, GetSceneNameByIndex(CustomBeatmaps.Memory.SelectedRoom));
        }
        public static void PlaySong(CustomBeatmapInfo customBeatmapInfo, string scene)
        {
            ForceSelectSong(customBeatmapInfo);
            var onSongPlaySound = Traverse.Create(SongDatabase).Field("onSongPlaySound").GetValue<EventReference>();

            if (customBeatmapInfo != null)
            {
                if (!onSongPlaySound.IsNull)
                {
                    RuntimeManager.PlayOneShot(onSongPlaySound);
                }

                JeffBezosController.instance.DisableUIInputs();
                JeffBezosController.returnFromArcade = true;
                LevelManager.LoadArcadeLevel(customBeatmapInfo.InternalName, customBeatmapInfo.difficulty);
            }
        }

        public static void PlaySongEdit(CustomBeatmapInfo beatmap, bool enableCountdown = false)
        {
            OsuEditorPatch.SetEditMode(true, enableCountdown, beatmap.OsuPath, beatmap.Path);
            PlaySong(beatmap, DefaultBeatmapScene);
        }

        // CUSTOMBEATMAPS V3 STUFF TO CHANGE LATER BELOW


        public static HighScoreList LoadArcadeHighscores()
        {
            return HighScoreScreen.LoadHighScores(RhythmGameType.ArcadeMode);
        }

        /// <returns> whether <code>potentialSongPath</code> is in the format "[UNBEATABLE Song]/[DIFFICULTY] </returns>
        public static bool IsValidUnbeatableSongPath(string potentialSongPath)
        {
            var whiteLabelSongs = BeatmapIndex.SongNames;

            int lastDashIndex = potentialSongPath.LastIndexOf("/", StringComparison.Ordinal);
            if (lastDashIndex != -1)
            {
                // Also check to make sure it's a valid UNBEATABLE song
                string songName = potentialSongPath.Substring(0, lastDashIndex);
                return whiteLabelSongs.Contains(songName);
            }

            return false;
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

    

