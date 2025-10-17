using CustomBeatmaps.CustomPackages;
using System.Text.RegularExpressions;
using Arcade.UI;

using File = Pri.LongPath.File;

namespace CustomBeatmaps.Util.CustomData
{
    public static class BeatmapHelper
    {
        public static string GetBeatmapProp(string beatmapText, string prop, string beatmapPath)
        {
            var match = Regex.Match(beatmapText, $"{prop}: *(.+?)\r?\n");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            throw new BeatmapException($"{prop} property not found.", beatmapPath);
        }

        public static string GetBeatmapImage(string beatmapText, string beatmapPath)
        {
            var match = Regex.Match(beatmapText, $"Background and Video events\r?\n.*\"(.+?)\"");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        public static bool IsBeatmapFile(string beatmapPath)
        {
            return beatmapPath.ToLower().EndsWith(".osu");
        }

        public static void SetBeatmapJson(string beatmapText, TagData data, string beatmapPath)
        {
            data.SongLength = ArcadeBGMManager.SongDuration;
            var beatmapSave = SerializeHelper.SerializeJSON(data);
            var match = Regex.Replace(beatmapText, $"(?<=Tags:)(.+?)\r?\n", beatmapSave + "\r\n");
            File.WriteAllText(beatmapPath, match);
        }
    }
}
