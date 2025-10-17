using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomBeatmaps.Util
{
    public class CustomPackageServer : CustomPackage
    {
        public CustomPackageServer() : base() { }
        public CustomPackageServer(Guid guid) : base(guid) { }

        public override PackageType PkgType => PackageType.Server;
        public override string ToString()
        {
            return $"{{{GUID}: [\n  {SongDatas.ToArray().Select(song =>
            new
            {
                Song = song.Name,
                Difficulties = song.InternalDifficulties.Join()
            }).Join(delimiter: ",\n  ")}\n]}}";
        }

        public string ServerURL;

    }

    /// <summary>
    /// Server Formatted Package
    /// </summary>
    public struct OnlinePackage 
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("mappers")]
        public string Mappers;
        [JsonProperty("artists")]
        public string Artists;
        [JsonProperty("guid")]
        public Guid GUID;
        [JsonProperty("filePath")]
        public string ServerURL;
        [JsonProperty("time")]
        public DateTime UploadTime;
        [JsonProperty("songs")]
        public List<OnlineBeatmap>[] Songs;
    }

    /// <summary>
    /// Server Formatted Beatmap
    /// </summary>
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

        [JsonProperty("internalDifficulty")]
        public string InternalDifficulty;

        [JsonProperty("audioFileName")]
        public string AudioFileName;

        [JsonProperty("tags")]
        public string Tags;
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
