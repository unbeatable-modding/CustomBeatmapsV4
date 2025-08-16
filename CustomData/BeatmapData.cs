using System;
using System.Collections.Generic;
using System.Text;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using Rhythm;
using Steamworks;
using UnityEngine;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using System.Linq;
using static Rhythm.BeatmapIndex;

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
        public string InternalName { get; private set; }

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

        public Dictionary<string, bool> Attributes
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

        public int Category { get; private set; }
        private int Offset = 1;



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

        public Category BeatmapCategory { get; private set; }

        // The groundwork for local beatmaps
        public BeatmapData(string bmapPath, int category)
        {
            BeatmapPath = bmapPath;
            Category = category;
            BeatmapCategory = BeatmapIndex.defaultIndex.Categories[category];
            DirectoryPath = Path.GetDirectoryName(bmapPath);

            IsLocal = CreateLocalBeatmap();
        }

        private bool CreateLocalBeatmap()
        {
            try
            {
                var text = File.ReadAllText(BeatmapPath);

                SongName = CustomPackageHelper.GetBeatmapProp(text, "Title", BeatmapPath);
                InternalName = $"CUSTOM__{BeatmapIndex.defaultIndex.Categories[Category]}__{SongName}";
                Artist = CustomPackageHelper.GetBeatmapProp(text, "Artist", BeatmapPath);
                Creator = CustomPackageHelper.GetBeatmapProp(text, "Creator", BeatmapPath);

                CoverPath = CustomPackageHelper.GetBeatmapImage(text, BeatmapPath);

                // Difficulty Logic
                var difficulty = "Star";
                var bmapVer = CustomPackageHelper.GetBeatmapProp(text, "Version", BeatmapPath);
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
                Difficulty = bmapVer;
                InternalDifficulty = difficulty;

                var audio = CustomPackageHelper.GetBeatmapProp(text, "AudioFilename", BeatmapPath);
                // realPath fixes some issues with old beatmaps, don't remove
                var realPath = audio.Contains("/") ? audio.Substring(audio.LastIndexOf("/") + 1, audio.Length - (audio.LastIndexOf("/") + 1)) : audio;
                AudioPath = $"{DirectoryPath}\\{realPath}";

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

        /// <summary>
        /// Smart way of adding a Beatmap to a Song, ensuring duplicates are only made when needed
        /// </summary>
        /// <param name="songDatas">do not try to comprehend these inner workings</param>
        public void TryAttachSong(ref Dictionary<string,SongData> songDatas)
        {
            try
            {
                songDatas.Add(InternalName, new SongData(this));
                return;
            }
            catch (Exception)
            {
                if (songDatas[InternalName].TryAddToThisSong(this))
                    return;
                while (true)
                {
                    try
                    {
                        InternalName = $"CUSTOM__{BeatmapIndex.defaultIndex.Categories[Category]}__{SongName}{Offset}";
                        Offset++;
                        songDatas.Add(InternalName, new SongData(this));
                        return;
                    }
                    catch (Exception)
                    {
                        if (songDatas[InternalName].TryAddToThisSong(this))
                            return;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"{{{SongName} by {Artist} ({Difficulty}) mapped {Creator} ({SongPath})}}";
        }
    }

    public class CustomBeatmap : BeatmapInfo
    {
        public BeatmapData Data { get; private set; }

        public CustomBeatmap(BeatmapData bmap, TextAsset textAsset, string difficulty) : base(textAsset, difficulty)
        {
            Data = bmap;
        }
    }

}
