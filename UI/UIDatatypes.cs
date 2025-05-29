
using System.Collections.Generic;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using HarmonyLib;

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

    public enum Difficulty
    {
        All,
        Beginner,
        Normal,
        Hard,
        Expert,
        Unbeatable,
        Star
    }

    public struct PackageHeader
    {
        public string Name;
        public int SongCount;
        public int MapCount;
        public string Creator;
        public bool New { get; set; }
        public BeatmapDownloadStatus DownloadStatus; // Kinda jank since this should only be for servers, but whatever.
        public int Level;
        public object Package;

        public PackageHeader(string name, int songCount, int mapCount, string creator, bool @new, BeatmapDownloadStatus downloadStatus, object package = null)
        {
            Name = name;
            SongCount = songCount;
            MapCount = mapCount;
            Creator = creator;
            New = @new;
            DownloadStatus = downloadStatus;
            Package = package;
        }

        public PackageHeader(CustomLocalPackage package)
        {
            ConvertLocal(package);
        }

        public PackageHeader(CustomLocalPackage package, bool @new)
        {
            ConvertLocal(package);
            New = @new;
        }

        private void ConvertLocal(CustomLocalPackage package)
        {
            var songs = new HashSet<string>();
            var names = new HashSet<string>();
            var creators = new HashSet<string>();
            var beatmaps = 0;
            foreach (CustomBeatmapInfo bmap in package.PkgSongs.SelectMany(s => s.CustomBeatmaps))
            {
                songs.Add(bmap.Info.InternalName);
                names.Add(bmap.Info.SongName);
                creators.Add(bmap.Info.Creator);
                beatmaps++;
            }

            var creator = creators.Join(x => x, " | ");
            var name = names.Join(x => x, ", ");
            var isNew = !CustomBeatmaps.PlayedPackageManager.HasPlayed(package.FolderName);

            Name = name;
            SongCount = songs.Count;
            MapCount = beatmaps;
            Creator = creator;
            New = isNew;
            DownloadStatus = BeatmapDownloadStatus.Downloaded;
            Package = package;
        }
    }

    public struct NewPackageHeader
    {
        public string Name;
        public int SongCount;
        public int MapCount;
        public string Creator;
        public bool New;
        public BeatmapDownloadStatus DownloadStatus; // Kinda jank since this should only be for servers, but whatever.
        public int Level;
        public object Package;

        public NewPackageHeader(string name, int songCount, int mapCount, string creator, bool @new, BeatmapDownloadStatus downloadStatus, int packageIndex, object package = null)
        {
            Name = name;
            SongCount = songCount;
            MapCount = mapCount;
            Creator = creator;
            New = @new;
            DownloadStatus = downloadStatus;
            Package = package;
        }

        public NewPackageHeader(CustomLocalPackage package)
        {
            var songs = new HashSet<string>();
            var names = new HashSet<string>();
            var creators = new HashSet<string>();
            var beatmaps = 0;
            foreach (CustomBeatmapInfo bmap in package.PkgSongs.SelectMany(s => s.CustomBeatmaps))
            {
                songs.Add(bmap.Info.InternalName);
                names.Add(bmap.Info.SongName);
                creators.Add(bmap.Info.Creator);
                beatmaps++;
            }

            var creator = creators.Join(x => x, " | ");
            var name = names.Join(x => x, ", ");
            var isNew = !CustomBeatmaps.PlayedPackageManager.HasPlayed(package.FolderName);

            
            Name = name;
            SongCount = songs.Count;
            MapCount = beatmaps;
            Creator = creator;
            New = isNew;
            DownloadStatus = BeatmapDownloadStatus.Downloaded;
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