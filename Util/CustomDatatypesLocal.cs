using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI;
using HarmonyLib;
using Newtonsoft.Json;
using Rhythm;
using UnityEngine;
using static Rhythm.BeatmapIndex;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.CustomData;

namespace CustomBeatmaps.Util
{
    public class CustomPackageLocal : CustomPackage
    {
        public CustomPackageLocal() : base()
        {
        }
        public CustomPackageLocal(Guid guid) : base(guid) { }
        
        //public CustomBeatmap[] CustomBeatmaps => SongDatas.SelectMany(p => p.BeatmapInfos).ToArray();

        /*
        public override List<string> Difficulties
        {
            get
            {
                return BeatmapDatas.Select(b => b.Difficulty).ToList();
            }
        }
        */
        public override PackageType PkgType => PackageType.Local;

        public override string ToString()
        {
            //return $"{{{Path.GetFileName(FolderName)}: [{Beatmaps.Join()}]}}";
            return $"{{{Path.GetFileName(BaseDirectory)}: [\n  {SongDatas.ToArray().Select(song => 
            new 
            { 
                Song = song.Name,
                Difficulties = song.InternalDifficulties.Join()
            }).Join(delimiter: ",\n  ")}\n]}}";
        }
    }

    [Obsolete]
    public class CustomLocalPackage : ICustomLocalPackage<CustomLocalBeatmap>
    {
        public string FolderName { get; set; }
        public List<CustomSongInfo> PkgSongs;
        public CustomBeatmapInfo[] Beatmaps => PkgSongs.SelectMany(p => p.CustomBeatmaps).ToArray();


        public List<string> Difficulties
        {
            get
            {
                return Beatmaps.Select(b => b.difficulty).ToList();
            }
        }

        public CustomLocalBeatmap[] CustomBeatmaps => PkgSongs.SelectMany(p => p.CustomBeatmaps2).ToArray();

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

    [Obsolete]
    public interface IPackageInterface<T>
    {
        List<T> Packages { get; }
        string Folder { get; }
        InitialLoadStateData InitialLoadState { get; }

        Action<T> PackageUpdated { get; set; }

    }

    [Obsolete]
    public interface ICustomLocalPackage<T> where T : ICustomBeatmap
    {
        string FolderName { get; }
        T[] CustomBeatmaps { get; } 

    }
    [Obsolete]
    public class CustomLocalBeatmap : ICustomBeatmap
    {
        public string SongName { get; }
        public string Artist { get; }
        public string Creator { get; }
        public string Difficulty { get; }
        public string AudioFileName { get; }
        public int Level => Tags.Level;
        public string FlavorText => Tags.FlavorText;

        public string InternalName { get; set; }
        public string RealDifficulty { get; }
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

        public CustomLocalBeatmap(string internalName, string songName, string difficulty, string realDifficulty, 
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
    [Obsolete]
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