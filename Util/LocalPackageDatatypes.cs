using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Rhythm;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.Util
{
    public struct CustomLocalPackage
    {
        public string FolderName;
        public List<CustomSongInfo> PkgSongs;
        //public CustomBeatmapInfo[] Beatmaps;

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
}