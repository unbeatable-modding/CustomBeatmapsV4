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

namespace CustomBeatmaps.Util
{
    public class ArcadeHelper
    {
        public static Category[] customCategories = {
            new Category("LOCAL", "Local songs", 7),
            new Category("[white label]", "from that other game", 8)
            };

        public static Category GetCustomCategory(int index)
        {
            return customCategories[index];
        }

        public void ReloadArcadeList()
        {

        }

        public static void TryAddCustomCategory()
        {
            foreach (Category customCategory in customCategories)
            {
                BeatmapIndex beatmapIndex = BeatmapIndex.defaultIndex;

                Traverse beatmapIndexTraverse = Traverse.Create(beatmapIndex);

                var beatmapIndexCategories = beatmapIndexTraverse.Field("categories").GetValue<List<Category>>();


                // Check if the custom category already exists
                if (!beatmapIndexCategories.Contains(customCategory))
                {
                    //customCategories.Add(customCategory);
                    // If not, add it to the list

                    beatmapIndexCategories.Add(customCategory);
                    beatmapIndexTraverse.Field("categories").SetValue(beatmapIndexCategories);

                    CustomBeatmaps.Log.LogDebug("Added category " + customCategory.Name);


                    /*var categoriesByName = beatmapIndexTraverse.Field("CategoriesByName").GetValue<Dictionary<string, Category>>();
                    Core.GetLogger().Msg("DEBUG");
                    categoriesByName.TryAdd(customCategory.Name, customCategory);
                    Core.GetLogger().Msg("DEBUG");
                    beatmapIndexTraverse.Field("CategoriesByName").SetValue(categoriesByName);*/


                    var categorySongs = beatmapIndexTraverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();
                    categorySongs.TryAdd(customCategory, new List<Song>());
                    beatmapIndexTraverse.Field("_categorySongs").SetValue(categorySongs);
                }
            }
        }

        // Put a song into the BeatmapIndex
        public static void SongSmuggle(string beatmapPath, int category, ref List<Song> songList)
        {
            CustomBeatmaps.Log.LogDebug("Loading a song...");
            Traverse traverse = Traverse.Create(BeatmapIndex.defaultIndex);
            
            

            List<string> songNames = traverse.Field("_songNames").GetValue<List<string>>();
            List<Song> songs = traverse.Field("songs").GetValue<List<Song>>();
            Dictionary<string, Song> _songs = traverse.Field("_songs").GetValue<Dictionary<string, Song>>();
            Song toLoad = SongLoader(ref songList, beatmapPath, category);

            if (!_songs.ContainsKey(toLoad.name))
            {
                CustomBeatmaps.Log.LogDebug("Song " + toLoad.name + " IS NOT loaded");
                songs.Add(toLoad);
                _songs.Add(toLoad.name, toLoad);
                songNames.Add(toLoad.name);
            }
            else
            {
                // Replace the song with itself incase of any updates to any stored values
                CustomBeatmaps.Log.LogDebug("Song " + toLoad.name + " IS loaded");
                songs.Where((Song self) => self.name == toLoad.name);
                _songs[toLoad.name] = toLoad;
            }

        }

        // Take a beatmap file and set/make song accordingly
        public static Song SongLoader(ref List<Song> songList, string bmapPath, int category)
        {
            CustomBeatmaps.Log.LogDebug("Starting load");
            BeatmapIndex beatmapIndex = BeatmapIndex.defaultIndex;
            string text = File.ReadAllText(bmapPath);
            int mapChannel = 0; // This is to handle duplicate stuff
            string songName = Encoder.EncodeSongName(mapChannel.ToString(), Path.GetDirectoryName(bmapPath)+ "/" + GetBeatmapProp(text, "AudioFilename", bmapPath));

            // Difficulty Logic
            string difficulty = "Star";
            string bmapVer = GetBeatmapProp(text, "Version", bmapPath);
            // Difficulties are not what they seem, welcome to devhell
            Dictionary<string, string> difficultyIndex = new Dictionary<string, string>
            {
                {"beginner", "Beginner"},
                {"easy", "Easy"}, // easy is a lie shove the song into normal
                {"normal", "Easy"},
                {"hard", "Normal"},
                {"expert", "Hard"},
                {"beatable", "Hard"}, // A lot of maps like using this idk
                {"unbeatable", "UNBEATABLE"}
            };
            // Check if the difficulty is in the default list
            // If not, set it to one that can be found in the game
            foreach (string i in difficultyIndex.Keys.ToArray())
            {
                // Check if the start of the version field matches a difficulty and then set accordingly
                // This is so songs that have (UNBEATABLE + 4k) get put in the UNBEATABLE difficulty
                if (bmapVer.ToLower().StartsWith(i))
                {
                    difficultyIndex.TryGetValue(i, out difficulty);
                    break;
                }
            }

            while (songList.Where((Song s) => s.name == songName && s.Difficulties.Contains(difficulty)).Any())
            {
                //CustomBeatmaps.Log.LogDebug("Duplicate found");
                mapChannel++;
                songName = Encoder.EncodeSongName(mapChannel.ToString(), Path.GetDirectoryName(bmapPath) + "/" + GetBeatmapProp(text, "AudioFilename", bmapPath));
            }

            Song customSong;
            Traverse traverse;
            List<string> _difficulties;
            Dictionary<string, BeatmapInfo> _beatmapinfo;
            if (!songList.Where((Song s) => s.name == songName).Any())
            {
                customSong = new Song(songName);
                traverse = Traverse.Create(customSong);
                traverse.Field("visibleInArcade").SetValue(true);
                traverse.Field("_category").SetValue(BeatmapIndex.defaultIndex.Categories[category]);
                traverse.Field("category").SetValue(category);
                _difficulties = new List<string>();
                _beatmapinfo = new Dictionary<string, BeatmapInfo>();
            }
            else
            {
                customSong = songList.First((Song s) => s.name == songName);
                traverse = Traverse.Create(customSong);
                _difficulties = traverse.Field("_difficulties").GetValue<List<string>>();
                _beatmapinfo = traverse.Field("_beatmaps").GetValue<Dictionary<string, BeatmapInfo>>();

            }

            BeatmapInfo map = new BeatmapInfo(new TextAsset(text), difficulty);
            List<BeatmapInfo> beatmapinfo = traverse.Field("beatmaps").GetValue<List<BeatmapInfo>>();
            beatmapinfo.Add(map);

            _beatmapinfo.Add(difficulty, map);
            traverse.Field("_beatmaps").SetValue(_beatmapinfo);

            _difficulties.Add(difficulty);
            traverse.Field("_difficulties").SetValue(_difficulties);

            customSong.stageScene = "TrainStationRhythm";
            songList.Add(customSong);
            return customSong;
        }

        public static Category CategoryLoader()
        {
            return null;
        }

        public static string GetBeatmapProp(string beatmapText, string prop, string beatmapPath)
        {
            var match = Regex.Match(beatmapText, $"{prop}: *(.+?)\r?\n");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            //throw new BeatmapException($"{prop} property not found.", beatmapPath);
            return null;
        }
    }
}

    

