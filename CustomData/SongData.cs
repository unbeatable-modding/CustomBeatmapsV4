using System;
using System.Collections.Generic;
using System.Text;
using CustomBeatmaps.Util;
using static Rhythm.BeatmapIndex;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using Rhythm;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using RewiredConsts;
using CustomBeatmaps.UI;
using UnityEngine;

namespace CustomBeatmaps.CustomData
{
    public class SongData
    {
        /// <summary>
        /// The Song the Game uses
        /// </summary>
        public CustomSong Song;

        // FOR ALL SONGS
        /// <summary>
        /// The song name that is displayed to the user
        /// </summary>
        public string Name { get; private set; }
        public string InternalName { get; private set; }
        public string Creator { get; private set; }
        public string Artist { get; private set; }

        public List<BeatmapData> BeatmapDatas = new();

        public CustomBeatmap[] BeatmapInfos
        {
            get
            {
                return BeatmapDatas.Where(b => b.IsLocal).Select(b => b.BeatmapPointer).ToArray();
            }
        }

        public HashSet<string> InternalDifficulties
        {
            get
            {
                try
                {
                    return BeatmapDatas.Select(i => i.InternalDifficulty).ToHashSet();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        // LOCAL FOR NOW

        public string CoverPath { get; private set; }

        // LOCAL ONLY
        public string AudioPath { get; private set; }
        public string DirectoryPath { get; private set; }
        // NOTE: CHANGE ME
        public int TMPCategory { get; private set; }

        private bool isLocal = false;
        public bool Local
        {
            get
            {
                return isLocal;
            }
        }
        private Traverse Traverse;

        /// <summary>
        /// Make a Song using Local Files
        /// </summary>
        /// <param name="bmapPath"></param>
        /// <param name="category"></param>
        public SongData(string bmapPath, int category)
        {
            var text = File.ReadAllText(bmapPath);
            DirectoryPath = Path.GetDirectoryName(bmapPath);

            Name = CustomPackageHelper.GetBeatmapProp(text, "Title", bmapPath);
            Artist = CustomPackageHelper.GetBeatmapProp(text, "Artist", bmapPath);
            Creator = CustomPackageHelper.GetBeatmapProp(text, "Creator", bmapPath);

            InternalName = $"CUSTOM__{defaultIndex.Categories[category]}__{Name}";
            var audio = CustomPackageHelper.GetBeatmapProp(text, "AudioFilename", bmapPath);

            // realPath fixes some issues with old beatmaps, don't remove
            var realPath = audio.Contains("/") ? audio.Substring(audio.LastIndexOf("/") + 1, audio.Length - (audio.LastIndexOf("/") + 1)) : audio;
            AudioPath = $"{DirectoryPath}\\{realPath}";

            InitLocalSong();
            //AddMapToSong(bmapPath);
        }

        public SongData(BeatmapData bmapData)
        {
            // Sync up song stuff
            Name = bmapData.SongName;
            InternalName = bmapData.InternalName;
            TMPCategory = bmapData.Category;

            Artist = bmapData.Artist;
            Creator = bmapData.Creator;

            

            if (bmapData.IsLocal)
            {
                DirectoryPath = bmapData.DirectoryPath;
                AudioPath = bmapData.AudioPath;
                CoverPath = bmapData.CoverPath;
                InitLocalSong(bmapData);
                //TryAddToThisSong(bmapData);
            }
            else
            {
                BeatmapDatas.Add(bmapData);
            }

            
        }

        private void InitLocalSong(BeatmapData bmap = null)
        {
            Song = new CustomSong(InternalName, this);
            Traverse = Traverse.Create(Song);
            Traverse.Field("visibleInArcade").SetValue(true);
            Traverse.Field("_category").SetValue(BeatmapIndex.defaultIndex.Categories[TMPCategory]);
            Traverse.Field("category").SetValue(TMPCategory);

            Traverse.Field("_difficulties").SetValue(new List<string>());
            Traverse.Field("beatmaps").SetValue(new List<BeatmapInfo>());
            Traverse.Field("_beatmaps").SetValue(new Dictionary<string, BeatmapInfo>());

            isLocal = true;
            AddToThisSong(bmap);
        }

        public bool TryAddToThisSong(BeatmapData bmap)
        {
            if (InternalDifficulties.Contains(bmap.InternalDifficulty))
                return false;
            AddToThisSong(bmap);
            return true;
        }

        private void AddToThisSong(BeatmapData bmapData)
        {
            try
            {
                BeatmapDatas.Add(bmapData);
                if (isLocal)
                {
                    var _difficulties = Traverse.Field("_difficulties").GetValue<List<string>>();
                    var beatmaps = Traverse.Field("beatmaps").GetValue<List<BeatmapInfo>>();
                    var _beatmaps = Traverse.Field("_beatmaps").GetValue<Dictionary<string, BeatmapInfo>>();

                    _difficulties.Add(bmapData.InternalDifficulty);
                    beatmaps.Add(bmapData.BeatmapPointer);
                    _beatmaps.Add(bmapData.InternalDifficulty, bmapData.BeatmapPointer);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    public class CustomSong : Song
    {
        public SongData Data { get; }
        public CustomSong(string name, SongData song) : base(name)
        {
            Data = song;
        }

        // Custom Images Stuff
        // Note: This has been copied from before the refactor, make sure it's not breaking things
        public void GetTexture()
        {
            var traverse = Traverse.Create(this);

            if (Data.CoverPath != null && File.Exists($"{Data.DirectoryPath}\\{Data.CoverPath}"))
            {
                var texture = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes($"{Data.DirectoryPath}\\{Data.CoverPath}");
                ImageConversion.LoadImage(texture, bytes);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                traverse.Field("coverArt").SetValue(sprite);
            }
        }
    }
}
