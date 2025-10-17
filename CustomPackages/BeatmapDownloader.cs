using System;
using System.Collections.Generic;
using System.Linq;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;

namespace CustomBeatmaps.CustomPackages
{
    public class BeatmapDownloader
    {
        private readonly Queue<CustomPackageServer> _queuedIdsToDownload = new Queue<CustomPackageServer>();
        private string _currentlyDownloading;
        /*
        public BeatmapDownloadStatus GetDownloadStatus(string serverPackageURL)
        {
            if (!FetchHelper.GetAvailable(Config.Backend.ServerPackageList).Result)
            {

            }

            // Check the local package folder, if it exists then we've downloaded it
            string packageFolder = PackageServerHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir, serverPackageURL);

            if (CustomBeatmaps.LocalServerPackages.PackageExists(packageFolder))
                return BeatmapDownloadStatus.Downloaded;

            // Check if we're downloading/are queued to download this file...
            lock (_queuedIdsToDownload)
            {
                if (_currentlyDownloading == serverPackageURL)
                    return BeatmapDownloadStatus.CurrentlyDownloading;
                if (_queuedIdsToDownload.Contains(serverPackageURL))
                    return BeatmapDownloadStatus.Queued;
            }
            // It's not in the queue
            return BeatmapDownloadStatus.NotDownloaded;
        }
        */
        public BeatmapDownloadStatus GetDownloadStatus(CustomPackageServer package)
        {

            BeatmapDownloadStatus status = BeatmapDownloadStatus.NotDownloaded;
            FetchHelper.GetAvailable(Config.Backend.ServerPackageList).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    lock (_queuedIdsToDownload)
                    {
                        if (_currentlyDownloading == package.ServerURL)
                            status = BeatmapDownloadStatus.CurrentlyDownloading;
                        if (_queuedIdsToDownload.Where(p => p.ServerURL == package.ServerURL).Any())
                            status = BeatmapDownloadStatus.Queued;
                    }
                }
            });
            if (CustomBeatmaps.LocalServerPackages.PackageExists(package.BaseDirectory))
                return BeatmapDownloadStatus.Downloaded;
            return status;
            // Check the local package folder, if it exists then we've downloaded it
            //string packageFolder = PackageServerHelper.GetLocalFolderFromServerPackageURL(Config.Mod.ServerPackagesDir, package.ServerURL);




            // It's not in the queue
            return BeatmapDownloadStatus.NotDownloaded;
        }

        private async void DownloadPackageInner(CustomPackageServer package)
        {
            _currentlyDownloading = package.ServerURL;

            try
            {
                await PackageServerHelper.DownloadPackage(package,
                    Config.Backend.ServerStorageURL,
                    Config.Mod.ServerPackagesDir);
            }
            catch (Exception e)
            {
                ScheduleHelper.SafeLog($"FAILED DOWNLOADING PACKAGE (skipping): {e}");
                _currentlyDownloading = null;
            }

            // We downloaded one, grab the next one.
            lock (_queuedIdsToDownload)
            {
                if (_queuedIdsToDownload.TryDequeue(out var upNext))
                {
                    DownloadPackageInner(upNext);
                }
                else
                {
                    // No more left, we're done downloading.
                    _currentlyDownloading = null;
                }
            }
        }

        public void QueueDownloadPackage(CustomPackageServer package)
        {
            lock (_queuedIdsToDownload)
            {
                bool notDownloading = _currentlyDownloading == null;
                if (notDownloading)
                {
                    DownloadPackageInner(package);
                }
                else
                {
                    _queuedIdsToDownload.Enqueue(package);
                }

            }
        }
    }
}