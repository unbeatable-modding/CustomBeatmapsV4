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
            Song toLoad = new CustomSongInfo(beatmapPath, category);
            
            while (songList.Where((Song s) => s.name == toLoad.name && s.Difficulties.Contains(toLoad.Difficulties.Single())).Any())
            {
                toLoad.name = toLoad.name + "1";
            }
            
            if (!_songs.ContainsKey(toLoad.name))
            {
                CustomBeatmaps.Log.LogDebug("Song " + toLoad.name + " IS NOT loaded");
                songs.Add(toLoad);
                _songs.Add(toLoad.name, toLoad);
                songNames.Add(toLoad.name);

                songList.Add(toLoad);
            }
            else if (songList.Where((Song s) => s.name == toLoad.name).Any())
            {
                Song mergeSong = songList.Where((Song s) => s.name == toLoad.name).Single();
                CustomBeatmaps.Log.LogDebug("Song " + toLoad.name + " IS PARTIALLY loaded");
                traverse = Traverse.Create(mergeSong);
                List<string> _difficulties; _difficulties = traverse.Field("_difficulties").GetValue<List<string>>();
                List<BeatmapInfo> beatmaps = traverse.Field("beatmaps").GetValue<List<BeatmapInfo>>();
                Dictionary<string, BeatmapInfo> _beatmaps = traverse.Field("_beatmaps").GetValue<Dictionary<string, BeatmapInfo>>();

                beatmaps.Add(toLoad.Beatmaps.Values.ToArray()[0]);
                _beatmaps.Add(toLoad.Difficulties[0], toLoad.Beatmaps.Values.ToArray()[0]);
                _difficulties.Add(toLoad.Difficulties[0]);
                _songs[toLoad.name] = mergeSong;

            }
            else
            {
                // Replace the song with itself incase of any updates to any stored values
                CustomBeatmaps.Log.LogDebug("Song " + toLoad.name + " IS loaded");
                songs.Where((Song self) => self.name == toLoad.name);
                _songs[toLoad.name] = toLoad;
                songList.Add(toLoad);
            }

            
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

    

