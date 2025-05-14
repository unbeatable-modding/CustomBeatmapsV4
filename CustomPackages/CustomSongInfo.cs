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

namespace CustomBeatmaps.CustomPackages
{
    public class CustomSongInfo : Song
    {
        public string audioPath;
        public string filePath;
        public string directoryPath;
        public string trueName;

        public CustomSongInfo(string bmapPath, int category) : base(null)
        {
            var text = File.ReadAllText(bmapPath);
            filePath = bmapPath;
            directoryPath = Path.GetDirectoryName(bmapPath);
            name = $"CUSTOM__{BeatmapIndex.defaultIndex.Categories[category]}__{CustomPackageHelper.GetBeatmapProp(text, "Title", bmapPath)}";
            trueName = CustomPackageHelper.GetBeatmapProp(text, "Title", bmapPath);
            audioPath = $"{directoryPath}\\{CustomPackageHelper.GetBeatmapProp(text, "AudioFilename", bmapPath)}";
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



            var map = new BeatmapInfo(new TextAsset(text), difficulty);
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
            var text = File.ReadAllText(filePath);

            if (CustomPackageHelper.GetBeatmapImage(text, filePath) != null && File.Exists($"{directoryPath}\\{CustomPackageHelper.GetBeatmapImage(text, filePath)}"))
            {
                var texture = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes($"{directoryPath}\\{CustomPackageHelper.GetBeatmapImage(text, filePath)}");
                ImageConversion.LoadImage(texture, bytes);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                traverse.Field("coverArt").SetValue(sprite);
            }
        }

    }
}
