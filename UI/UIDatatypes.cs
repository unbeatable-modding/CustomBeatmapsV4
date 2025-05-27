﻿
using System.Collections.Generic;
using System.Linq;
using CustomBeatmaps.CustomPackages;

namespace CustomBeatmaps.UI
{

    public enum Tab
    {
        //Online, Local, Submissions, Osu
        Online, Local, Osu
    }

    public enum SortMode
    {
        New, Title, Artist, Creator, Downloaded
    }

    public struct PackageHeader
    {
        public string Name;
        public int SongCount;
        public int MapCount;
        public string Creator;
        public bool New;
        public BeatmapDownloadStatus DownloadStatus; // Kinda jank since this should only be for servers, but whatever.
        public int PackageIndex; // Also kind of jank but whatever it's book-keeping
        public int Level;
        public object Package;

        public PackageHeader(string name, int songCount, int mapCount, string creator, bool @new, BeatmapDownloadStatus downloadStatus, int packageIndex, object package = null)
        {
            Name = name;
            SongCount = songCount;
            MapCount = mapCount;
            Creator = creator;
            New = @new;
            DownloadStatus = downloadStatus;
            PackageIndex = packageIndex;
            Package = package;
        }
    }

    public struct BeatmapHeader
    {
        public string Name;
        public string Artist;
        public string Creator;
        public string Difficulty;
        public string IconURL;
        public int Level;
        public string FlavorText;
        public string[] Attributes;

        public BeatmapHeader(string name, string artist, string creator, string difficulty, string iconURL, int level, string flavorText, Dictionary<string,bool> attributes)
        {
            Name = name;
            Artist = artist;
            Creator = creator;
            Difficulty = difficulty;
            IconURL = iconURL;
            Level = level;
            FlavorText = flavorText;
            if (attributes != null)
            {
                var trueValues = new List<string>();
                foreach (string k in attributes.Keys)
                {
                    if (attributes[k])
                        trueValues.Add(k);
                }
                Attributes = trueValues.ToArray();
            }
            
        }
    }
}