using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arcade.UI;
using System.Text.RegularExpressions;
using CustomBeatmaps.Util;
using Rhythm;
using CustomBeatmaps.CustomData;

namespace CustomBeatmaps.CustomPackages
{
    public enum PackageType
    {
        Local,
        Server,
        Temp
    }

    public abstract class CustomPackage
    {

        private Func<string> _name;
        /// <summary>
        /// Display Name of package
        /// </summary>
        public string Name
        {
            get => _name.Invoke();
            set => _name = (() => value);
        }

        private Func<string> _mappers;
        /// <summary>
        /// Mappers/Creators of Package
        /// </summary>
        public string Mappers
        {
            get => _mappers.Invoke();
            set => _mappers = (() => value);
        }

        private Func<string> _artists;
        /// <summary>
        /// Artists of Package
        /// </summary>
        public string Artists
        {
            get => _artists.Invoke();
            set => _artists = (() => value);
        }

        public CustomPackage() : this(Guid.Empty)
        {
            //GUID = Guid.Empty;
        }
        public CustomPackage(Guid guid)
        {
            GUID = guid;
            _name = (() => string.Join(", ", SongDatas.Select(s => s.Name).ToHashSet()));
            _mappers = (() => string.Join(", ", SongDatas.Select(s => s.Creator).ToHashSet()));
            _artists = (() => string.Join(", ", SongDatas.Select(s => s.Artist).ToHashSet()));
        }
        public Guid GUID { get; set; }
        
        /// <summary>
        /// Top level folder of Package
        /// </summary>
        public string BaseDirectory { get; set; }
        
        /// <summary>
        /// Array of All BeatmapDatas this package contains
        /// </summary>
        public virtual BeatmapData[] BeatmapDatas => SongDatas.SelectMany(p => p.BeatmapDatas).ToArray();

        /// <summary>
        /// Here for redundancy, use BeatmapDatas unless this is 100% required
        /// </summary>
        public virtual CustomBeatmap[] CustomBeatmaps => SongDatas.SelectMany(p => p.BeatmapInfos).ToArray();

        public List<SongData> SongDatas;
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

        public virtual List<string> InternalDifficulties
        {
            get
            {
                return BeatmapDatas.Select(b => b.InternalDifficulty).ToList();
            }
        }
    }
    
}
