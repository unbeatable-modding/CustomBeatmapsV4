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

namespace CustomBeatmaps.Util
{
    public class ArcadeHelper
    {
        public static readonly Category[] customCategories = {
            new Category("LOCAL", "Local songs", 7),
            new Category("[white label]", "from that other game", 8)
            };

        private static Traverse traverse = Traverse.Create(BeatmapIndex.defaultIndex);
        private static List<string> songNames = traverse.Field("_songNames").GetValue<List<string>>();
        private static List<Song> songs = traverse.Field("songs").GetValue<List<Song>>();
        private static Dictionary<string, Song> _songs = traverse.Field("_songs").GetValue<Dictionary<string, Song>>();
        private static List<Song> songList = new();

        public static Category GetCustomCategory(int index)
        {
            return customCategories[index];
        }

        public static void ReloadArcadeList()
        {
            LoadCustomSongs();
            var currentArcade = ArcadeSongDatabase.Instance;
            var arcade = Traverse.Create(currentArcade);
            var _songDatabase = arcade.Field("_songDatabase").GetValue<Dictionary<string, BeatmapItem>>();
            _songDatabase.Clear();
            arcade.Method("LoadDatabase").GetValue();
            arcade.Method("RefreshSongList").GetValue();
        }

        public static void TryAddCustomCategory()
        {
            foreach (var customCategory in customCategories)
            {
                
                var categories = traverse.Field("categories").GetValue<List<Category>>();
                var categorySongs = traverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();

                // Check if the custom category already exists
                if (!categories.Contains(customCategory))
                {
                    // If not, add it to the list
                    categories.Add(customCategory);
                    categorySongs.TryAdd(customCategory, new List<Song>());

                    CustomBeatmaps.Log.LogDebug($"Added category {customCategory.Name}");
                    
                }
            }
        }

        public static void LoadCustomSongs()
        {
            CleanSongs();
            songList.Clear();
            TryAddCustomSongs(PackageHelper.GetLocalBeatmapDirectory(), 7);
            TryAddCustomSongs($"{PackageHelper.GetWhiteLabelBeatmapDirectory()}CustomBeatmapsV3-Data/SERVER_PACKAGES", 8);
            TryAddCustomSongs($"{PackageHelper.GetWhiteLabelBeatmapDirectory()}USER_PACKAGES", 8);
            
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
        }

        public static string GetBeatmapProp(string beatmapText, string prop, string beatmapPath)
        {
            var match = Regex.Match(beatmapText, $"{prop}: *(.+?)\r?\n");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            throw new BeatmapException($"{prop} property not found.", beatmapPath);
        }
    }
}

    

