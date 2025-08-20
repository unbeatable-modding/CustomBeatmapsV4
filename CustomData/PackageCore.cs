using System;
using System.Collections.Generic;
using System.Text;

namespace CustomBeatmaps.CustomData
{
    public struct PackageCore
    {
        public string Name;

        public string Mappers;

        public string Artists;
        
        public Guid GUID;

        public List<Dictionary<InternalDifficulty, string>> Songs;
    }
    
    public enum InternalDifficulty
    {
        Beginner = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3,
        UNBEATABLE = 4,
        Star = 5
    }

    public static class PackageCoreTest
    {
        public static PackageCore Package
        {
            get
            {
                PackageCore pkg = new PackageCore();
                pkg.GUID = Guid.NewGuid();
                pkg.Songs = new();
                var testSong = new Dictionary<InternalDifficulty, string> { { InternalDifficulty.Beginner, "/example"} };
                pkg.Songs.Add(testSong);
                pkg.Name = "Example Package";
                return pkg;
            }
        }
    }
}
