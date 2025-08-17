using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CustomBeatmaps.Util;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util.CustomData;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerServer : PackageManagerGeneric
    {
        //protected readonly Action<BeatmapException> _onLoadException;

        public PackageManagerServer(Action<BeatmapException> onLoadException) : base(onLoadException)
        {
        }

        protected override void ReloadAll()
        {
            if (_folder == null)
                return;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                lock (_packages)
                {
                    InitialLoadState.Loading = true;
                    InitialLoadState.Loaded = 0;
                    InitialLoadState.Total = PackageHelper.EstimatePackageCount(_folder);
                    ScheduleHelper.SafeLog($"RELOADING ALL PACKAGES FROM {_folder}");

                    _packages.Clear();
                    var packages = PackageHelper.LoadLocalPackages(_folder, _category, loadedPackage =>
                    {
                        InitialLoadState.Loaded++;
                    }, _onLoadException);
                    ScheduleHelper.SafeLog($"(step 2)");
                    _packages.AddRange(packages);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Clear();
                        foreach (var package in _packages)
                        {
                            _downloadedFolders.Add(Path.GetFullPath(package.FolderName));
                        }
                    }
                    InitialLoadState.Loading = false;
                }
            }).Start();
        }

        public override bool PackageExists(string folder)
        {
            throw new NotImplementedException();
        }

        public override void SetFolder(string folder, int category)
        {
            throw new NotImplementedException();
        }

        protected override void OnFileChange(FileSystemEventArgs evt)
        {
            throw new NotImplementedException();
        }

        protected override void RemovePackage(string folderPath)
        {
            throw new NotImplementedException();
        }

        protected override void UpdatePackage(string folderPath = null)
        {
            throw new NotImplementedException();
        }
    }

}
