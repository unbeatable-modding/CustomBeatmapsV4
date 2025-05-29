using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Newtonsoft.Json;
using Rhythm;
using UnityEngine;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.Util
{
    public class CustomLocalPackage : ICustomPackage<LocalCustomBeatmap>
    {
        public string FolderName { get; set; }
        public List<CustomSongInfo> PkgSongs;
        public CustomBeatmapInfo[] Beatmaps => PkgSongs.SelectMany(p => p.CustomBeatmaps).ToArray();

        public LocalCustomBeatmap[] CustomBeatmaps => PkgSongs.SelectMany(p => p.CustomBeatmaps2).ToArray();

        public override string ToString()
        {
            //return $"{{{Path.GetFileName(FolderName)}: [{Beatmaps.Join()}]}}";
            return $"{{{Path.GetFileName(FolderName)}: [\n  {PkgSongs.ToArray().Select(Song => 
            new 
            { 
                Song = Song.name,
                Difficulties = Song.Difficulties.Join()
            }).Join(delimiter: ",\n  ")}\n]}}";
        }
    }

    public class InitialLoadStateData
    {
        public bool Loading;
        public int Loaded;
        public int Total;
    }

    public struct TagData
    {
        public int Level;

        public string FlavorText;

        public float SongLength;

        public Dictionary<string, bool> Attributes;
    }

    public interface ICustomPackage<T> where T : ICustomBeatmap
    {
        string FolderName { get; }
        T[] CustomBeatmaps { get; } 

    }

    public class LocalCustomBeatmap : ICustomBeatmap
    {
        public string SongName { get; }
        public string Artist { get; }
        public string Creator { get; }
        public string Difficulty { get; }
        public string AudioFileName { get; }
        public int Level => Tags.Level;
        public string FlavorText => Tags.FlavorText;

        public string InternalName { get; set; }
        public readonly string RealDifficulty;
        public CustomSongInfo Song;
        public readonly Category Category;
        public TagData Tags;
        public string OsuPath;
        public CustomBeatmapInfo Beatmap;
        public string SongPath
        {
            get
            {
                return $"{InternalName}/{Difficulty}";
            }
        }

        public LocalCustomBeatmap(string internalName, string songName, string difficulty, string realDifficulty, 
            string artist, string beatmapCreator, string osuPath, 
            Category category, CustomSongInfo song, TagData tags, CustomBeatmapInfo beatmap)
        {
            InternalName = internalName;
            SongName = songName;
            OsuPath = osuPath;
            Artist = artist;
            Difficulty = difficulty;
            RealDifficulty = realDifficulty;
            Creator = beatmapCreator;
            Category = category;
            Song = song;
            Tags = tags;
            AudioFileName = song.AudioPath;
            Beatmap = beatmap;
        }


    }

    public interface ICustomBeatmap
    {
        string SongName { get; }
        string Artist { get; }
        string Creator { get; }
        string Difficulty { get;  }
        string AudioFileName { get; }
        int Level { get; }
        string FlavorText { get; }
        string SongPath { get; }

    }

    
}