using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomBeatmaps.UI;
using CustomBeatmaps.Util;
using Unity.Loading;
using static CustomBeatmaps.CustomPackages.LocalPackageManager;

namespace CustomBeatmaps.CustomPackages
{
    public class CustomPackageHandler
    {
        protected List<LocalPackageManager> Managers;

        public Action<CustomLocalPackage> PackageUpdated;

        public CustomPackageHandler(List<LocalPackageManager> managers)
        {
            Managers = managers;
            foreach (LocalPackageManager m in managers)
            {
                m.PackageUpdated += package =>
                {
                    PackageUpdated.Invoke(package);
                };
            }     
        }

        public List<CustomLocalPackage> Packages
        {
            get
            {
                return Managers.SelectMany(m => m.Packages).ToList();
            }
        }

        public string Folder
        {
            get
            {
                return string.Join(", ", Managers.Select(m => m.Folder));
            }
        }


        public InitialLoadStateData InitialLoadState
        {
            get
            {
                var loadstate = new InitialLoadStateData();
                loadstate.Loading = Managers.Where(m => m.InitialLoadState.Loading).Any();
                loadstate.Loaded = Managers.Select(m => m.InitialLoadState.Loaded).Sum();
                loadstate.Total = Managers.Select(m => m.InitialLoadState.Total).Sum();
                return loadstate;
            }
        }
        
    }
}
