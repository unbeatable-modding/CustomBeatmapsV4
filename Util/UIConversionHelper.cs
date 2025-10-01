using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI;
using HarmonyLib;
using static Rhythm.BeatmapIndex;

using Directory = Pri.LongPath.Directory;

namespace CustomBeatmaps.Util
{
    public static class UIConversionHelper
    {
        public static void SortPackages<T>(List<T> packages, SortMode sortMode)
            where T : CustomPackage
        {
            packages.Sort((left, right) =>
            {
                switch (sortMode)
                {
                    case SortMode.New:
                        return DateTime.Compare(right.Time, left.Time);
                    case SortMode.Title:
                        string nameL = left.Name,
                            nameR = right.Name;
                        return String.CompareOrdinal(nameL, nameR);
                    case SortMode.Artist:
                        //string artistLeft = left.SongDatas.Select(map => map.Artist).OrderBy(x => x).Join();
                        //string artistRight = right.SongDatas.Select(map => map.Artist).OrderBy(x => x).Join();
                        return String.CompareOrdinal(left.Artists, right.Artists);
                    case SortMode.Creator:
                        //string creatorLeft = left.SongDatas.Select(map => map.Creator).OrderBy(x => x).Join();
                        //string creatorRight = right.SongDatas.Select(map => map.Creator).OrderBy(x => x).Join();
                        return String.CompareOrdinal(left.Mappers, right.Mappers);
                    case SortMode.Downloaded:
                        //bool downloadedLeft = CustomBeatmaps.LocalServerPackages.PackageExists(left.BaseDirectory);
                        //bool downloadedRight = CustomBeatmaps.LocalServerPackages.PackageExists(right.BaseDirectory);
                        bool downloadedLeft = left.DownloadStatus == BeatmapDownloadStatus.Downloaded;
                        bool downloadedRight = right.DownloadStatus == BeatmapDownloadStatus.Downloaded;
                        //nameL = GetLocalPackageName(left);
                        //nameR = GetLocalPackageName(right);
                        //return String.CompareOrdinal(nameL, nameR); ; // um
                        return (downloadedRight ? 1 : 0).CompareTo(downloadedLeft ? 1 : 0);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, null);
                }
                ;
            });
        }

        public static bool PackageHasDifficulty(CustomPackage package, Difficulty diff)
        {
            switch (diff)
            {
                case Difficulty.All:
                    return true;
                case Difficulty.Beginner:
                    return package.InternalDifficulties.Contains("Beginner");
                case Difficulty.Normal:
                    return package.InternalDifficulties.Contains("Easy");
                case Difficulty.Hard:
                    return package.InternalDifficulties.Contains("Normal");
                case Difficulty.Expert:
                    return package.InternalDifficulties.Contains("Hard");
                case Difficulty.Unbeatable:
                    return package.InternalDifficulties.Contains("UNBEATABLE");
                case Difficulty.Star:
                    return package.InternalDifficulties.Contains("Star");
                default:
                    throw new ArgumentOutOfRangeException(nameof(diff), diff, null);
            } 
        }

        public static bool PackageMatchesFilter(CustomPackage pkg, string filterQuery)
        {
            if (string.IsNullOrEmpty(filterQuery))
            {
                return true;
            }

            bool caseSensitive = filterQuery.ToLower() != filterQuery;

            // Check Package
            string[] possibleMatches = new[]
                {
                    pkg.Name,
                    pkg.Mappers,
                    pkg.Artists
                };
            foreach (var possibleMatch in possibleMatches)
            {
                if (string.IsNullOrEmpty(possibleMatch))
                    continue;

                string toCheck = caseSensitive
                    ? possibleMatch
                    : possibleMatch.ToLower();
                if (toCheck.Contains(filterQuery))
                {
                    return true;
                }
            }

            // Check Beatmaps
            foreach (var bmap in pkg.BeatmapDatas.ToList())
            {
                possibleMatches = new[]
                {
                    bmap.SongName,
                    bmap.Artist,
                    bmap.Creator,
                    bmap.Difficulty,
                    bmap.InternalDifficulty,
                    bmap.FlavorText
                };
                foreach (var possibleMatch in possibleMatches)
                {
                    if (string.IsNullOrEmpty(possibleMatch))
                        continue;

                    string toCheck = caseSensitive
                        ? possibleMatch
                        : possibleMatch.ToLower();
                    if (toCheck.Contains(filterQuery))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}