using Rhythm;
using UnityEngine;
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
        public string Path
        {
            get
            {
                return $"{InternalName}/{Difficulty}";
            }
        }
        public readonly string OsuPath;
        public readonly Category Category;

        public CustomBeatmapInfo(TextAsset textAsset, string difficulty, string artist,
            string beatmapCreator, string name, string songName, string realDifficulty, string osuPath, Category category) : base(textAsset, difficulty)
        {
            
            OsuPath = osuPath;
            Artist = artist;
            InternalName = name;
            SongName = songName;
            Difficulty = difficulty;
            RealDifficulty = realDifficulty;
            BeatmapCreator = beatmapCreator;
            Category = category;
        }

        public override string ToString()
        {
            return $"{{{SongName} by {Artist} ({RealDifficulty}) mapped {BeatmapCreator} ({Path})}}";
        }

    }
}