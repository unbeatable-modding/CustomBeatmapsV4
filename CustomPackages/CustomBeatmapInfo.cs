using System;
using System.Collections.Generic;
using Arcade.UI;
using Arcade.UI.SongSelect;
using CustomBeatmaps.Util;
using Newtonsoft.Json;
using Rhythm;
using UnityEngine;
using static Arcade.UI.ArcadeBeatmapProvider;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.CustomPackages
{
    public class CustomBeatmapInfo : BeatmapInfo
    {
        public readonly string Artist;
        public readonly string BeatmapCreator;
        /// <summary>
        /// Name of the song.
        /// <example>
        /// For example:
        /// Worn Out Tapes
        /// </example>
        /// </summary>
        public readonly string SongName;
        /// <summary>
        /// Name used in BeatmapIndex.
        /// <example>
        /// For example:
        /// CUSTOM__LOCAL__Example
        /// </example>
        /// </summary>
        public string InternalName;
        /// <summary>
        /// Difficulty for BeatmapIndex
        /// </summary>
        public readonly string Difficulty;
        /// <summary>
        /// Parsed Difficulty name
        /// </summary>
        public readonly string RealDifficulty;
        public CustomSongInfo Song;
        public string Path
        {
            get
            {
                return $"{InternalName}/{Difficulty}";
            }
        }
        public readonly string OsuPath;
        public readonly Category Category;
        public TagData Tags;
        public int Level => Tags.Level;
        public string FlavorText => Tags.FlavorText;
        public Dictionary<string, bool> Attributes
        {
            get
            {
                if (Tags.Attributes == null)
                    Tags.Attributes = new();
                return Tags.Attributes;
            }
        }

        public CustomBeatmapInfo(TextAsset textAsset, string difficulty, string artist,
            string beatmapCreator, string name, string songName, string realDifficulty, string osuPath, Category category, CustomSongInfo song) : base(textAsset, difficulty)
        {
            
            OsuPath = osuPath;
            Artist = artist;
            InternalName = name;
            SongName = songName;
            Difficulty = difficulty;
            RealDifficulty = realDifficulty;
            BeatmapCreator = beatmapCreator;
            Category = category;
            Song = song;

            var tagTest = CustomPackageHelper.GetBeatmapProp(text, "Tags", OsuPath);
            if (tagTest.StartsWith("{") && tagTest.EndsWith("}"))
            {
                try
                {
                    Tags = JsonConvert.DeserializeObject<TagData>(tagTest);
                }
                catch (Exception e)
                {
                    ScheduleHelper.SafeLog("INVALID JSON");
                }
            }
                
        }
        public override string ToString()
        {
            return $"{{{SongName} by {Artist} ({RealDifficulty}) mapped {BeatmapCreator} ({Path})}}";
        }

        


        public struct TagData
        {
            public int Level;

            public string FlavorText;

            public float SongLength;

            public Dictionary<string, bool> Attributes;
        }
    }
}