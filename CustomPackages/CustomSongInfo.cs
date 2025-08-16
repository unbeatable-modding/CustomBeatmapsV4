using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using CustomBeatmaps.Util;
using static Rhythm.BeatmapIndex;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using Newtonsoft.Json;
using CustomBeatmaps.UI;

namespace CustomBeatmaps.CustomPackages
{
    [Obsolete]
    public class CustomSongInfo : Song
    {
        public string AudioPath { get; private set; }
        public string FilePath { get; private set; }
        public string DirectoryPath { get; private set; }
        public string TrueName { get; private set; }
        public string Creator { get; private set; }
        public string Artist { get; private set; }


        public CustomSongInfo(string bmapPath, int category) : base(null)
        {
            var text = File.ReadAllText(bmapPath);
            FilePath = bmapPath;
            DirectoryPath = Path.GetDirectoryName(bmapPath);
            name = $"CUSTOM__{BeatmapIndex.defaultIndex.Categories[category]}__{CustomPackageHelper.GetBeatmapProp(text, "Title", bmapPath)}";
            var audio = CustomPackageHelper.GetBeatmapProp(text, "AudioFilename", bmapPath);
            var realPath = audio.Contains("/") ? audio.Substring((audio.LastIndexOf("/") + 1), audio.Length - (audio.LastIndexOf("/")+1)) : audio;
            AudioPath = $"{DirectoryPath}\\{realPath}";

            TrueName = CustomPackageHelper.GetBeatmapProp(text, "Title", bmapPath);
            Artist = CustomPackageHelper.GetBeatmapProp(text, "Artist", bmapPath);
            Creator = CustomPackageHelper.GetBeatmapProp(text, "Creator", bmapPath);
            
            // Difficulty Logic
            var difficulty = "Star";
            var bmapVer = CustomPackageHelper.GetBeatmapProp(text, "Version", bmapPath);
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
            foreach (var i in difficultyIndex.Keys.ToArray())
            {
                // Check if the start of the version field matches a difficulty and then set accordingly
                // This is so songs that have (UNBEATABLE + 4k) get put in the UNBEATABLE difficulty
                if (bmapVer.ToLower().StartsWith(i))
                {
                    difficultyIndex.TryGetValue(i, out difficulty);
                    break;
                }
            }
            var traverse = Traverse.Create(this);
            
            traverse.Field("visibleInArcade").SetValue(true);
            traverse.Field("_category").SetValue(BeatmapIndex.defaultIndex.Categories[category]);
            traverse.Field("category").SetValue(category);
            var _difficulties = new List<string>();
            var _beatmapinfo = new Dictionary<string, BeatmapInfo>();


            var map = new CustomBeatmapInfo(new TextAsset(text), difficulty,
                Artist, Creator, name, TrueName, bmapVer, FilePath, BeatmapIndex.defaultIndex.Categories[category], this);
            var beatmapinfo = traverse.Field("beatmaps").GetValue<List<BeatmapInfo>>();
            beatmapinfo.Add(map);

            _beatmapinfo.Add(difficulty, map);
            traverse.Field("_beatmaps").SetValue(_beatmapinfo);

            _difficulties.Add(difficulty);
            traverse.Field("_difficulties").SetValue(_difficulties);
            stageScene = "TrainStationRhythm";

            return;
            
            
            
            
        }

        // Custom Images Stuff
        // Note: this code is very bad and slow
        public void GetTexture()
        {
            var traverse = Traverse.Create(this);
            var text = File.ReadAllText(FilePath);

            if (CustomPackageHelper.GetBeatmapImage(text, FilePath) != null && File.Exists($"{DirectoryPath}\\{CustomPackageHelper.GetBeatmapImage(text, FilePath)}"))
            {
                var texture = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes($"{DirectoryPath}\\{CustomPackageHelper.GetBeatmapImage(text, FilePath)}");
                ImageConversion.LoadImage(texture, bytes);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                traverse.Field("coverArt").SetValue(sprite);
            }
        }

        public List<CustomBeatmapInfo> CustomBeatmaps
        {
            get
            {
                var beatmaps = new List<CustomBeatmapInfo>();
                foreach (CustomBeatmapInfo b in Beatmaps.Values)
                {
                    beatmaps.Add(b);
                }
                return beatmaps;
            }
        }

        public List<CustomLocalBeatmap> CustomBeatmaps2
        {
            get
            {
                var beatmaps = new List<CustomLocalBeatmap>();
                foreach (CustomBeatmapInfo b in Beatmaps.Values)
                {
                    beatmaps.Add(b.Info);
                }
                return beatmaps;
            }
        }
    }
}
