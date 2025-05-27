using System;
using System.Collections.Generic;
using System.Text;

namespace CustomBeatmaps.CustomPackages
{
    internal class CustomServerPackageHandler : CustomPackageHandler
    {
        protected new List<LocalPackageManager> Managers;
        public CustomServerPackageHandler(LocalPackageManager manager) : base(managers: null)
        {
            Managers = [manager];
            
        }

       
    }
}
