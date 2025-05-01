using System.IO;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;



namespace CustomBeatmaps.Util
{
    class Encoder
    {

        // Using symbols that can't be used in paths to avoid conflicts
        public static string pathSymbol = "|";
        public static string dataSeparator = "%";

        // Custom indicator to identify custom songs in other parts of the code
        public static string customPathIndicator = "CUSTOM__";


        public static string NormalizePath(string path)
        {
            // Normalize the path to use forward slashes
            return path.Replace("\\", "/");
        }

        // Turn Path/To/Map, Path/To/Audio into __CUSTOM.PATH|TO|MAP.PATH|TO|AUDIO
        public static string EncodeSongName(string mapPath, string audioPath)
        {
            string songDir = PackageHelper.GetLocalBeatmapDirectory();

            // Remove the song directory from the path
            mapPath = NormalizePath(mapPath);
            audioPath = NormalizePath(audioPath);

            // Remove the song directory from the path
            if (mapPath.StartsWith(songDir))
            {
                mapPath = mapPath.Substring(songDir.Length);
            }

            if (audioPath.StartsWith(songDir))
            {
                audioPath = audioPath.Substring(songDir.Length);
            }

            // Remove leading slashes
            if (mapPath.StartsWith("/"))
            {
                mapPath = mapPath.Substring(1);
            }

            if (audioPath.StartsWith("/"))
            {
                audioPath = audioPath.Substring(1);
            }


            // Replace slashes with the path symbol
            string encodedMapPath = mapPath.Replace("/", pathSymbol);
            string encodedAudioPath = audioPath.Replace("/", pathSymbol);

            var encodedPath = customPathIndicator + dataSeparator + encodedMapPath + dataSeparator + encodedAudioPath;

            return encodedPath;
        }

        // Turn __CUSTOM.PATH|TO|MAP.PATH|TO|AUDIO into Path/To/Map, Path/To/Audio
        public static string[] GetDataParts(string path)
        {
            if (path.StartsWith(customPathIndicator))
            {
                path = path.Substring(customPathIndicator.Length + dataSeparator.Length);
            }

            if (path.Contains("/"))
            {
                path = path.Split(new char[] { '/' }, 2)[0];
            }


            string[] parts = path.Split(new char[] { char.Parse(dataSeparator) }, 2);

            if (parts.Length == 1)
            {
                return new string[] { parts[0], "" };
            }
            return parts;
        }

        // Turn __CUSTOM.PATH|TO|MAP.PATH|TO|AUDIO into Path/To/Map
        public static string DecodeMapName(string path)
        {

            // Split the path into map and audio parts
            string[] parts = GetDataParts(path);

            if (parts.Length == 1)
            {
                return "";
            }

            path = parts[0].Replace(pathSymbol, "/");

            var songDir = PackageHelper.GetLocalBeatmapDirectory();

            // Add the song directory back to the path
            if (!path.StartsWith(songDir))
            {
                path = Path.Combine(songDir, path);
            }

            return path;
        }

        // Turn __CUSTOM.PATH|TO|MAP.PATH|TO|AUDIO into Path/To/Audio
        public static string DecodeAudioName(string path)
        {

            // Split the path into map and audio parts
            string[] parts = GetDataParts(path);

            if (parts.Length == 1)
            {
                return "";
            }

            path = parts[1].Replace(pathSymbol, "/");

            var songDir = PackageHelper.GetLocalBeatmapDirectory();

            // Add the song directory back to the path
            if (!path.StartsWith(songDir))
            {
                path = Path.Combine(songDir, path);
            }

            return path;
        }




        // Currently not needed

        // Turn __CUSTOM.PATH|TO|MAP.PATH|TO|AUDIO/Version into [Path/To/Map, Version]
        public static string[] DecodeSongPath(string path)
        {
            string[] parts = path.Split(new char[] { '/' }, 2);
            parts[0] = DecodeMapName(parts[0]);

            if (parts.Length == 1)
            {
                return new string[] { parts[0], "" };
            }
            return parts;

        }


    }
}
