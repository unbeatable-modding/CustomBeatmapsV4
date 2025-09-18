using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;

namespace CustomBeatmaps.Util
{
    public class CustomPackageServer : CustomPackage
    {
        public CustomPackageServer() : base() { }
        public CustomPackageServer(Guid guid) : base(guid) { }

        public override PackageType PkgType => PackageType.Server;
        public override string ToString()
        {
            return $"{{{Path.GetFileName(BaseDirectory)}: [\n  {SongDatas.ToArray().Select(song =>
            new
            {
                Song = song.Name,
                Difficulties = song.InternalDifficulties.Join()
            }).Join(delimiter: ",\n  ")}\n]}}";
        }

        public string ServerURL;

    }

    /// <summary>
    /// Package formatted from server
    /// </summary>
    public struct OnlinePackage
    {
        [JsonProperty("filePath")]
        public string ServerURL;
        [JsonProperty("time")]
        public DateTime UploadTime;
        [JsonProperty("beatmaps")]
        public Dictionary<string, OnlineBeatmap> Beatmaps;
    }

    public struct OnlineBeatmap
    {
        [JsonProperty("name")]
        public string SongName;

        [JsonProperty("artist")]
        public string Artist;

        [JsonProperty("creator")]
        public string Creator;

        [JsonProperty("difficulty")]
        public string Difficulty;

        [JsonProperty("audioFileName")]
        public string AudioFileName;

        [JsonProperty("level")]
        public int Level;

        [JsonProperty("flavorText")]
        public string FlavorText;
    }

    [Obsolete]
    public struct CustomServerPackageList
    {
        [JsonProperty("packages")]
        public CustomServerPackage[] Packages;
        /*
        public override string ToString()
        {
            return string.Join(",\n", Packages);
        }
        */
    }

    [Obsolete]
    public class CustomServerPackage : ICustomLocalPackage<CustomServerBeatmap>
    {
        [JsonProperty("filePath")]
        public string ServerURL;
        [JsonProperty("time")]
        public DateTime UploadTime;
        [JsonProperty("beatmaps")]
        public Dictionary<string, CustomServerBeatmap> Beatmaps;

        public string FolderName { get; set; }
        public CustomServerBeatmap[] CustomBeatmaps => Beatmaps.Values.ToArray();

        public List<string> Difficulties
        {
            get
            {
                return CustomBeatmaps.Select(b => b.Difficulty).ToList();
            }
        }

        public override string ToString()
        {
            return $"{{[{string.Join(", ", Beatmaps)}] at {ServerURL} on {UploadTime}}}";
        }

        public string GetServerPackageURL()
        {
            return ServerURL;
        }
    }

    [Obsolete]
    public class CustomServerBeatmap : ICustomBeatmap
    {
        [JsonProperty("name")]
        public string SongName { get; set; }
        [JsonProperty("artist")]
        public string Artist { get; set; }
        [JsonProperty("creator")]
        public string Creator { get; set; }
        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }
        [JsonProperty("audioFileName")]
        public string AudioFileName { get; set; }
        [JsonProperty("level")]
        public int Level { get; set; }
        [JsonProperty("flavorText")]
        public string FlavorText { get; set; }

        public string SongPath => null;

        public override string ToString()
        {
            return $"{{{SongName} ({Difficulty}) by {Artist}: mapped by {Creator}}}";
        }
    }

    public struct ServerSubmissionPackage
    {
        [JsonProperty("username")]
        public string Username;
        [JsonProperty("avatarURL")]
        public string AvatarURL;
        [JsonProperty("downloadURL")]
        public string DownloadURL;
    }
    public struct UserInfo
    {
        [JsonProperty("name")]
        public string Name;
    }

    public struct NewUserInfo
    {
        [JsonProperty("id")]
        public string UniqueId;
    }

    public struct BeatmapHighScoreEntry
    {
        [JsonProperty("score")]
        public int Score;
        [JsonProperty("accuracy")]
        public float Accuracy;
        // TODO: just use an enum for gods sake
        // 0: None
        // 1: No misses
        // 2: Full clear
        [JsonProperty("fc")]
        public int FullComboMode;
    }
    public struct UserHighScores
    {
        // scores["beatmap key"]["Username"] = score
        public Dictionary<string, Dictionary<string, BeatmapHighScoreEntry>> Scores;
    }
}
