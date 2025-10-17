﻿using System;
using System.Diagnostics;
using System.IO.Compression;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.Util
{
    public static class ZipHelper
    {
        const string Path7Zip = "BepInEx/plugins/CustomBeatmapsV4/7z/7z.exe";

        private static bool Has7Zip()
        {
            return File.Exists(Path7Zip);
        }

        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            destinationDirectoryName = destinationDirectoryName.Replace("\\", "/");
            if (Has7Zip())
            {
                try
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = Path.GetFullPath(Path7Zip),
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        // yes because we may replace and that would make us stuck :(
                        Arguments = $"x \"{sourceArchiveFileName}\" -o\"{destinationDirectoryName}\" -y"
                    };
                    Console.Write($"RUNNING {processStartInfo.Arguments}");
                    Process process = Process.Start(processStartInfo);
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string line = process.StandardOutput.ReadLine();
                        Console.WriteLine("proc: " + line);
                    }
                    process.WaitForExit();
                    Console.WriteLine("done");
                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine("Error extracting {0}.", destinationDirectoryName);
                    }
                    else
                    {
                        // We're done.
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error extracting {0}: {1}", destinationDirectoryName, e.Message);
                }
            }
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, true);
        }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            destinationArchiveFileName = destinationArchiveFileName.Replace("\\", "/");
            if (File.Exists(destinationArchiveFileName))
                File.Delete(destinationArchiveFileName);
            if (Has7Zip())
            {
                try
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = Path.GetFullPath(Path7Zip),
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        // yes because we may replace and that would make us stuck :(
                        Arguments = $"a -tzip \"{destinationArchiveFileName}\" \"{sourceDirectoryName}\" -y"
                    };
                    Console.Write($"RUNNING {processStartInfo.Arguments}");
                    Process process = Process.Start(processStartInfo);
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string line = process.StandardOutput.ReadLine();
                        Console.WriteLine("proc: " + line);
                    }
                    process.WaitForExit();
                    Console.WriteLine("done");
                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine("Error extracting {0}.", destinationArchiveFileName);
                    }
                    else
                    {
                        // We're done.
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error extracting {0}: {1}", destinationArchiveFileName, e.Message);
                }
            }
            // bad fallback
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
        }
    }
}
