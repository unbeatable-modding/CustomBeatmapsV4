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

    [Obsolete]
    public class CustomBeatmapInfo : BeatmapInfo
    {
        public CustomLocalBeatmap Info;

        public TagData Tags;
        public int Level => Tags.Level;
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

        public CustomBeatmapInfo(TextAsset textAsset, string difficulty, string artist,
            string beatmapCreator, string name, string songName, string realDifficulty, string osuPath, Category category, CustomSongInfo song) : base(textAsset, difficulty)
        {

            var tagTest = zzzCustomPackageHelper.GetBeatmapProp(text, "Tags", osuPath);
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

            Info = new CustomLocalBeatmap(name, songName, difficulty, realDifficulty,
                artist, beatmapCreator, osuPath,
                category, song, Tags, this);
        }
        public override string ToString()
        {
            return $"{{{Info.SongName} by {Info.Artist} ({Info.RealDifficulty}) mapped {Info.Creator} ({Info.SongPath})}}";
        }
    }
}