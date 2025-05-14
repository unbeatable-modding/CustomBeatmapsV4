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
using static Rhythm.BeatmapIndex;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.CustomPackages;
using Arcade.UI.SongSelect;
using static Arcade.UI.SongSelect.ArcadeSongDatabase;
using UnityEngine.SceneManagement;

namespace CustomBeatmaps.Util
{
    public class ArcadeHelper
    {

        private static Traverse traverse = Traverse.Create(BeatmapIndex.defaultIndex);
        private static List<string> songNames = traverse.Field("_songNames").GetValue<List<string>>();
        private static List<Song> songs = traverse.Field("songs").GetValue<List<Song>>();
        private static Dictionary<string, Song> _songs = traverse.Field("_songs").GetValue<Dictionary<string, Song>>();
        private static Dictionary<Category, List<Song>> _categorySongs = traverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();
        private static List<Song> songList = new();

        public static void ReloadArcadeList()
        {
            LoadCustomSongs();
            if (SceneManager.GetActiveScene().name != "ArcadeModeMenu")
                return;
            var currentArcade = ArcadeSongDatabase.Instance;
            var arcade = Traverse.Create(currentArcade);
            var _songDatabase = arcade.Field("_songDatabase").GetValue<Dictionary<string, BeatmapItem>>();
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
                    songNames.Add(s.name);
                    songList.Add(s);
                    _categorySongs[s.Category].Add(s);
                }
            }

        }

        private static void TryAddCustomSongs(string directory, int category)
        {
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.osu", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    SongSmuggle(file, category);
                }
            }
        }

        // Put a song into the BeatmapIndex
        private static void SongSmuggle(string beatmapPath, int category)
        {
            CustomBeatmaps.Log.LogDebug("Loading a song...");

            var toLoad = new CustomSongInfo(beatmapPath, category);
            var dupeInt = 0;
            var startingName = toLoad.name;
            while (songList.Where((Song s) => 
                s.name == toLoad.name && ( ((CustomSongInfo)s).directoryPath != toLoad.directoryPath || s.Difficulties.Contains(toLoad.Difficulties.Single()) ) ).Any())
            {
                toLoad.name = startingName + dupeInt;
                dupeInt++;
            }
            

            if (!_songs.ContainsKey(toLoad.name))
            {
                // Song we just created has never existed
                CustomBeatmaps.Log.LogDebug($"Loading in Song: {toLoad.name}");
                songs.Add(toLoad);
                _songs.Add(toLoad.name, toLoad);
                songNames.Add(toLoad.name);

                songList.Add(toLoad);
            }
            else if (songList.Where((Song s) => s.name == toLoad.name).Any())
            {
                // Song we just created has multiple difficulties
                var mergeSong = songList.Where((Song s) => s.name == toLoad.name).Single();
                CustomBeatmaps.Log.LogDebug($"Adding to Song: {toLoad.name}");

                var traverseSong = Traverse.Create(mergeSong);
                var _difficulties = traverseSong.Field("_difficulties").GetValue<List<string>>();
                var beatmaps = traverseSong.Field("beatmaps").GetValue<List<BeatmapInfo>>();
                var _beatmaps = traverseSong.Field("_beatmaps").GetValue<Dictionary<string, BeatmapInfo>>();

                beatmaps.Add(toLoad.Beatmaps.Values.ToArray()[0]);
                _beatmaps.Add(toLoad.Difficulties[0], toLoad.Beatmaps.Values.ToArray()[0]);
                _difficulties.Add(toLoad.Difficulties[0]);
                _songs[toLoad.name] = mergeSong;

            }
            else
            {
                // This should never happen
                CustomBeatmaps.Log.LogDebug($"Song {toLoad.name} never got cleaned???");

                //CustomBeatmaps.Log.LogDebug("Song " + toLoad.name + " IS loaded");
                //songs.DoIf((Song self) => self.name == toLoad.name, (Song self) => self = toLoad);
                //_songs[toLoad.name] = toLoad;
                //songList.Add(toLoad);
            }


        }

        // Remove all modded songs for when we want to reload the database
        private static void CleanSongs()
        {
            var killList = new List<string>();

            songs.DoIf((Song s) => s is CustomSongInfo, s => killList.Add(s.name));

            //CustomBeatmaps.Log.LogDebug("Trying to kill songs");
            killList.ForEach((string k) => songs.Remove(_songs[k]));
            killList.ForEach((string k) => _songs.Remove(k));
            killList.ForEach((string k) => songNames.Remove(k));
            return;
            foreach (Category c in CustomPackageHelper.customCategories)
            {
                _categorySongs.Keys.Where(k => k.Name == c.Name).ToList().ForEach(k => _categorySongs[k].Clear());
                //_categorySongs[c].Clear();
            }
        }

    }
}

    

