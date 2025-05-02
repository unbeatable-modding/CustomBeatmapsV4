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
        public CustomSongInfo(string bmapPath, int category) : base(null)
        {
            CustomBeatmaps.Log.LogDebug("Generating song...");
            string text = File.ReadAllText(bmapPath);
            name = "CUSTOM__" + BeatmapIndex.defaultIndex.Categories[category] + "__" + Util.ArcadeHelper.GetBeatmapProp(text, "Title", bmapPath);
            //name = "CUSTOM__" + Util.ArcadeHelper.GetBeatmapProp(text, "TitleUnicode", bmapPath);
            audioPath = Path.GetDirectoryName(bmapPath) + "/" + Util.ArcadeHelper.GetBeatmapProp(text, "AudioFilename", bmapPath);

            // Difficulty Logic
            string difficulty = "Star";
            string bmapVer = Util.ArcadeHelper.GetBeatmapProp(text, "Version", bmapPath);
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

            //Song customSong = this;
            //Traverse traverse;
            List<string> _difficulties;
            Dictionary<string, BeatmapInfo> _beatmapinfo;
            Traverse traverse = Traverse.Create(this);
            traverse.Field("visibleInArcade").SetValue(true);
            traverse.Field("_category").SetValue(BeatmapIndex.defaultIndex.Categories[category]);
            traverse.Field("category").SetValue(category);
            _difficulties = new List<string>();
            _beatmapinfo = new Dictionary<string, BeatmapInfo>();



            BeatmapInfo map = new BeatmapInfo(new TextAsset(text), difficulty);
            List<BeatmapInfo> beatmapinfo = traverse.Field("beatmaps").GetValue<List<BeatmapInfo>>();
            beatmapinfo.Add(map);

            _beatmapinfo.Add(difficulty, map);
            traverse.Field("_beatmaps").SetValue(_beatmapinfo);

            _difficulties.Add(difficulty);
            traverse.Field("_difficulties").SetValue(_difficulties);
            stageScene = "TrainStationRhythm";
        }

    }
}
