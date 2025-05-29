using System;
using System.Collections.Generic;
using System.Text;
using CustomBeatmaps.Util;
using static CustomBeatmaps.CustomPackages.LocalPackageManager;

namespace CustomBeatmaps.CustomPackages
{
    public interface IPackageInterface<T>
    {
        List<T> Packages { get; }
        string Folder { get; }
        InitialLoadStateData InitialLoadState { get; }

        Action<T> PackageUpdated { get; set; }

    }
}
