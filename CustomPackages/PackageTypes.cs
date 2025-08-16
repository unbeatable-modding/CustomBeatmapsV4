using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util;
using Rhythm;

namespace CustomBeatmaps.CustomPackages
{
    public enum PackageType
    {
        Local,
        Server,
        Temp

    }
    public abstract class CustomPacakage
    {
        public string FolderName { get; set; }
        public virtual BeatmapData[] BeatmapDatas { get; protected set; }

        public List<SongData> PkgSongs;
        public abstract PackageType PkgType { get; }

        public BeatmapDownloadStatus DownloadStatus; // Kinda jank since this should only be for servers, but whatever.
        public bool New { get; set; }
        public virtual List<string> Difficulties
        {
            get
            {
                return BeatmapDatas.Select(b => b.Difficulty).ToList();
            }
        }
    }

}
