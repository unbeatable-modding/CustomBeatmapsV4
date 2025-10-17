using CustomBeatmaps.Util;
using Newtonsoft.Json;
using Rhythm;
using System;
using System.Collections.Generic;
using UnityEngine;
using static CustomBeatmaps.Util.CustomData.BeatmapHelper;
using static Rhythm.BeatmapIndex;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.CustomData
{
    /// <summary>
    /// Stores data for Custom Beatmaps and is not a real Beatmap. Exists to make server packages which don't have maps work, and have everything go to one place.
    /// </summary>
    public class BeatmapData
    {
        // Below are things that should be set on EVERY Beatmap

        /// <summary>
        /// Name of the Song. Self Explanatory.
        /// </summary>
        public string SongName { get; private set; }

        /// <summary>
        /// Dev name for the Song of this Beatmap
        /// </summary>
        public string InternalName
        {
            get
            {
                return $"CUSTOM__{Category.InternalCategory}__{GUID}-{Offset}";
            }
        }

        /// <summary>
        /// Name of the person who made the song.
        /// </summary>
        public string Artist { get; private set; }

        /// <summary>
        /// Mapper name goes here
        /// </summary>
        public string Creator { get; private set; }

        /// <summary>
        /// Difficulty the user sees (Expert, Unbeatable, etc.)
        /// </summary>
        public string Difficulty { get; private set; }

        /// <summary>
        /// Difficulty the game needs to properly sort maps. (and is not the same as the real difficulty names for some reason)<br/>
        /// Must be set even on server packages.
        /// </summary>
        public string InternalDifficulty { get; private set; }

        /// <summary>
        /// Holds paramaters for levels + extra ones added through this mod (Level, Flavor Text, (BT, MW, and 4K) Indicators, etc.)
        /// </summary>
        public TagData Tags;

        /// <summary>
        /// The level of this Beatmap
        /// </summary>
        public int Level => Tags.Level;

        /// <summary>
        /// This is the stuff that's shown beside Beatmaps on the vanilla select screen
        /// </summary>
        public string FlavorText => Tags.FlavorText;

        public HashSet<string> Attributes
        {
            get
            {
                if (Tags.Attributes == null)
                    Tags.Attributes = new();
                return Tags.Attributes;
            }
        }
        /// <summary>
        /// The Song's internal name + Difficulty (This is how the game checks for songs)
        /// </summary>
        public string SongPath
        {
            get
            {
                return $"{InternalName}/{InternalDifficulty}";
            }
        }

        public CCategory Category { get; private set; }

        private int Offset = 0;

        // Stuff that currently only works locally but should work online later

        /// <summary>
        /// Image associated with this beatmap
        /// </summary>
        public string CoverPath { get; private set; }

        // Below are things that should only be getting set on downloaded Beatmaps
        // (even if they are server beatmaps)

        /// <summary>
        /// IS the beatmap local???
        /// </summary>
        public bool IsLocal { get; private set; }
        
        /// <summary>
        /// File location of this Beatmap
        /// </summary>
        public string BeatmapPath { get; private set; }
        
        /// <summary>
        /// The Directory this Beatmap is in
        /// </summary>
        public string DirectoryPath { get; private set; }

        /// <summary>
        /// The actual Beatmap the game uses
        /// </summary>
        public CustomBeatmap BeatmapPointer { get; private set; }

        /// <summary>
        /// Points to the audio file associated with the beatmap
        /// </summary>
        public string AudioPath { get; private set; }

        public Guid GUID = Guid.Empty;


        public Category BeatmapCategory { get; private set; }

        /// <summary>
        /// BeatmapData from Local *.bmap files
        /// </summary>
        public BeatmapData(Guid guid, int offset, InternalDifficulty internalDifficulty, string bmapPath, CCategory category)
        {
            BeatmapPath = bmapPath;
            Category = category;
            DirectoryPath = Path.GetDirectoryName(bmapPath);
            GUID = guid;
            Offset = offset;
            //InternalName = $"CUSTOM__{Category.InternalCategory}__{internalName}";

            string[] difficultyIndex = ["Beginner", "Easy", "Normal", "Hard", "UNBEATABLE", "Star"];
            InternalDifficulty = difficultyIndex[(int)internalDifficulty];

            IsLocal = CreateLocalPackagedBeatmap();
        }

        /// <summary>
        /// BeatmapData from Online Beatmaps
        /// </summary>
        public BeatmapData(OnlineBeatmap oBmap, Guid guid, int offset, CCategory category)
        {
            Category = category;
            Offset = offset;

            SongName = oBmap.SongName;
            //InternalName = $"CUSTOM__{Category.InternalCategory}__{guid}-{Offset}";
            GUID = guid;
            Artist = oBmap.Artist;
            Creator = oBmap.Creator;
            Difficulty = oBmap.Difficulty;
            InternalDifficulty = oBmap.InternalDifficulty;

            var tagTest = oBmap.Tags;
            if (tagTest.StartsWith("{") && tagTest.EndsWith("}"))
            {
                try
                {
                    Tags = JsonConvert.DeserializeObject<TagData>(tagTest);
                }
                catch (Exception)
                {
                    ScheduleHelper.SafeLog("INVALID TAG JSON");
                }
            }

            IsLocal = false;
        }

        private bool CreateLocalPackagedBeatmap()
        {
            try
            {
                var text = File.ReadAllText(BeatmapPath);

                SongName = GetBeatmapProp(text, "TitleUnicode", BeatmapPath);
                
                Artist = GetBeatmapProp(text, "Artist", BeatmapPath);
                Creator = GetBeatmapProp(text, "Creator", BeatmapPath);

                CoverPath = GetBeatmapImage(text, BeatmapPath);

                var bmapVer = GetBeatmapProp(text, "Version", BeatmapPath);
                Difficulty = bmapVer;

                var audio = GetBeatmapProp(text, "AudioFilename", BeatmapPath);
                // realPath fixes some issues with old beatmaps, don't remove
                var realPath = audio.Contains("/") ? audio.Substring(audio.LastIndexOf("/") + 1, audio.Length - (audio.LastIndexOf("/") + 1)) : audio;
                AudioPath = $"{DirectoryPath}\\{realPath}";

                var tagTest = GetBeatmapProp(text, "Tags", BeatmapPath);
                if (tagTest.StartsWith("{") && tagTest.EndsWith("}"))
                {
                    try
                    {
                        Tags = JsonConvert.DeserializeObject<TagData>(tagTest);
                    }
                    catch (Exception)
                    {
                        ScheduleHelper.SafeLog("INVALID TAG JSON");
                    }
                }

                BeatmapPointer = new CustomBeatmap(this, new TextAsset(text), InternalDifficulty);
            }
            catch (Exception e)
            {
                //throw new BeatmapException("Failed to make local beatmap", SongPath);
                throw e;
                //return false;
            }
            return true;
        }

        public override string ToString()
        {
            return $"{{{SongName} by {Artist} ({Difficulty}) mapped {Creator} ({SongPath})}}";
        }
    }

    /// <summary>
    /// Basicially a vanilla BeatmapInfo, but using a different class so it's easier to seperate
    /// </summary>
    public class CustomBeatmap : BeatmapInfo
    {
        public BeatmapData Data { get; private set; }

        public CustomBeatmap(BeatmapData bmap, TextAsset textAsset, string difficulty) : base(textAsset, difficulty)
        {
            Data = bmap;
        }
    }

}
