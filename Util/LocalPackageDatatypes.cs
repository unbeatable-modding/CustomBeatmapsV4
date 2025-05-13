using System.Collections.Generic;
using System.IO;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Rhythm;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.Util
{
    public struct CustomLocalPackage
    {
        public string FolderName;
        public List<Song> PkgSongs;
        //public CustomBeatmapInfo[] Beatmaps;

        public override string ToString()
        {
            //return $"{{{Path.GetFileName(FolderName)}: [{Beatmaps.Join()}]}}";
            return $"{{{Path.GetFileName(FolderName)}}}";
        }

    }
}