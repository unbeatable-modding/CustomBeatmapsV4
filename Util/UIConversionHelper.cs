using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static BeatmapHeader CustomBeatmapInfoToBeatmapHeader(BeatmapData bmap)
        {
            return new BeatmapHeader(
                bmap.SongName,
                bmap.Artist,
                bmap.Creator,
                bmap.Difficulty,
                null,
                bmap.Level,
                bmap.FlavorText,
                bmap.Attributes
            );
        }

        public static List<BeatmapHeader> CustomBeatmapInfosToBeatmapHeaders(List<SongData> customBeatmaps)
        {
            List<BeatmapHeader> headers = new List<BeatmapHeader>(customBeatmaps.Count);
            foreach (var song in customBeatmaps)
            {
                foreach (var bmap in song.BeatmapDatas)
                {
                    headers.Add(CustomBeatmapInfoToBeatmapHeader(bmap));
                }
            }

            return headers;
        }

        private static string GetServerPackageName(CustomServerPackage package)
        {
            return package.Beatmaps.Join(beatmap => beatmap.Value.SongName, " | ");
        }
        private static string GetLocalPackageName(CustomPackage package)
        {
            return package.PkgSongs.Join(beatmap => beatmap.Name, " | ");
        }

        public static void SortServerPackages(List<CustomServerPackage> headers, SortMode sortMode)
        {
            headers.Sort((left, right) =>
            {
                switch (sortMode)
                {
                    case SortMode.New:
                        return DateTime.Compare(right.UploadTime, left.UploadTime);
                    case SortMode.Title:
                        string nameL = GetServerPackageName(left),
                            nameR = GetServerPackageName(right);
                        return String.CompareOrdinal(nameL, nameR);
                    case SortMode.Artist:
                        string artistLeft = left.Beatmaps.Values.Select(map => map.Artist).OrderBy(x => x).Join();
                        string artistRight = right.Beatmaps.Values.Select(map => map.Artist).OrderBy(x => x).Join();
                        return String.CompareOrdinal(artistLeft, artistRight);
                    case SortMode.Creator:
                        string creatorLeft = left.Beatmaps.Values.Select(map => map.Creator).OrderBy(x => x).Join();
                        string creatorRight = right.Beatmaps.Values.Select(map => map.Creator).OrderBy(x => x).Join();
                        return String.CompareOrdinal(creatorLeft, creatorRight);
                    case SortMode.Downloaded:
                        bool downloadedRight = CustomBeatmaps.LocalServerPackages.PackageExists(
                            zzzCustomPackageHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir,
                                left.ServerURL));
                        bool downloadedLeft = CustomBeatmaps.LocalServerPackages.PackageExists(
                            zzzCustomPackageHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir,
                                right.ServerURL));
                        return (downloadedLeft ? 1 : 0).CompareTo(downloadedRight ? 1 : 0);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, null);
                }
                ;
            });
        }
        public static void SortLocalPackages(List<CustomPackage> packages, SortMode sortMode)
        {
            packages.Sort((left, right) =>
            {
                switch (sortMode)
                {
                    case SortMode.New:
                        return DateTime.Compare(Directory.GetLastWriteTime(right.FolderName), Directory.GetLastWriteTime(left.FolderName));
                    case SortMode.Title:
                        string nameL = GetLocalPackageName(left),
                            nameR = GetLocalPackageName(right);
                        return String.CompareOrdinal(nameL, nameR);
                    case SortMode.Artist:
                        string artistLeft = left.PkgSongs.Select(map => map.Artist).OrderBy(x => x).Join();
                        string artistRight = right.PkgSongs.Select(map => map.Artist).OrderBy(x => x).Join();
                        return String.CompareOrdinal(artistLeft, artistRight);
                    case SortMode.Creator:
                        string creatorLeft = left.PkgSongs.Select(map => map.Creator).OrderBy(x => x).Join();
                        string creatorRight = right.PkgSongs.Select(map => map.Creator).OrderBy(x => x).Join();
                        return String.CompareOrdinal(creatorLeft, creatorRight);
                    case SortMode.Downloaded:
                        nameL = GetLocalPackageName(left);
                        nameR = GetLocalPackageName(right);
                        return String.CompareOrdinal(nameL, nameR); ; // um
                    default:
                        throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, null);
                }
                ;
            });
        }

        public static bool PackageHasDifficulty(CustomPackage package, Difficulty diff)
        {
            if (diff == Difficulty.All)
                return true;
            Dictionary<Difficulty, string> EdifficultyIndex = new Dictionary<Difficulty, string>
            {
                {Difficulty.Beginner, "Beginner"},
                {Difficulty.Normal, "Easy"},
                {Difficulty.Hard, "Normal"},
                {Difficulty.Expert, "Hard"},
                {Difficulty.Unbeatable, "UNBEATABLE"},
                {Difficulty.Star, "Star"},
            };
            return package.Difficulties.Contains(EdifficultyIndex[diff]);
        }
        
        public static readonly Dictionary<Difficulty, string[]> DifficultyIndex = 
            new Dictionary<Difficulty, string[]>
            {
                {Difficulty.Beginner, ["beginner"]},
                {Difficulty.Normal, ["easy", "normal"]},
                {Difficulty.Hard, ["hard"]},
                {Difficulty.Expert, ["expert", "beatable"]},
                {Difficulty.Unbeatable, ["unbeatable"]},
            };
        
        public static bool PackageHasDifficulty(CustomServerPackage package, Difficulty diff)
        {
            if (diff == Difficulty.All)
                return true;
            try
            {

                foreach (var tryDiff in package.Difficulties)
                {
                    if (diff == Difficulty.Star)
                    {
                        
                        foreach (string i in DifficultyIndex.Values.SelectMany(s => s.ToList()))
                        {
                            if (tryDiff.ToLower().StartsWith(i))
                                return false;
                        }
                        
                        return true;
                    }
                    else
                    {
                        foreach (string i in DifficultyIndex[diff].ToList())
                        {
                            if (tryDiff.ToLower().StartsWith(i))
                                return true;
                        }
                    }
                        
                }
            }
            catch (Exception e)
            {
                CustomBeatmaps.Log.LogError(e);
            }
            


            return false;
        }

        public static bool PackageMatchesFilter(CustomServerPackage serverPackage, string filterQuery)
        {
            if (string.IsNullOrEmpty(filterQuery))
            {
                return true;
            }

            bool caseSensitive = filterQuery.ToLower() != filterQuery;

            foreach (var (bmapName, bmap) in serverPackage.Beatmaps)
            {
                string[] possibleMatches = new[]
                {
                    bmapName,
                    bmap.SongName,
                    bmap.Artist,
                    bmap.Creator,
                    bmap.Difficulty
                };
                foreach (var possibleMatch in possibleMatches)
                {
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

        public static bool PackageMatchesFilter(CustomPackage serverPackage, string filterQuery)
        {
            if (string.IsNullOrEmpty(filterQuery))
            {
                return true;
            }

            bool caseSensitive = filterQuery.ToLower() != filterQuery;
            foreach (var bmap in serverPackage.PkgSongs.SelectMany(s => s.BeatmapDatas).ToList())
            {
                string[] possibleMatches = new[]
                {
                    bmap.SongName,
                    bmap.Artist,
                    bmap.Creator,
                    bmap.InternalDifficulty
                };
                foreach (var possibleMatch in possibleMatches)
                {
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