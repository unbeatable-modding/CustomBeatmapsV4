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

}
