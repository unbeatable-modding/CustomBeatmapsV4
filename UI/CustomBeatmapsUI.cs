
using System;
using CustomBeatmaps.Util;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UI
{
    public static class CustomBeatmapsUI
    {
        private static PackageListUIOnline OnlinePackageList = new PackageListUIOnline(CustomBeatmaps.LocalServerPackages);
        private static PackageListUILocal LocalPackageList = new PackageListUILocal(CustomBeatmaps.LocalUserPackages);
        private static PackageListUIOSU OsuPackageList = new PackageListUIOSU(CustomBeatmaps.OSUSongManager);

        public static void Render()
        {
            /*
             *  State:
             *      - Which tab we're on
             *  
             *  UI:
             *      List:
             *      - Tab Picker
             *      - User/Online Info
             *      - Depending on current tab, local/server/submission/OSU!
             *      - Assist info on the bottom
             */

            GUIHelper.SetDefaultStyles();

            // Remember our tab state statically for convenience (ShaiUI might have been right here, maybe I didn't even need react lmfao)
            (Tab tab, Action<Tab> setTab) = (CustomBeatmaps.Memory.SelectedTab, val => {
                CustomBeatmaps.Memory.SelectedTab = val;
            });

            try
            {
                switch (tab)
                {
                    case Tab.Online:
                        try
                        {
                            OnlinePackageList.Render(() => RenderListTop(tab, setTab));
                        }
                        catch
                        {
                            LocalPackageList.Render(() => RenderListTop(tab, setTab));
                        }
                        
                        break;
                    case Tab.Local:
                        LocalPackageList.Render(() => RenderListTop(tab, setTab));
                        break;
                    /*
                case Tab.Submissions:
                    SubmissionPackageListUI.Render(() => RenderListTop(tab, setTab));
                    break;
                    */
                    case Tab.Osu:
                        OsuPackageList.Render(() => RenderListTop(tab, setTab));
                        //OSUPackageList.Render(() => RenderListTop(tab, setTab));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                // NOTE: there are a fuck ton of errors, but somehow it still works
                
                CustomBeatmaps.Log.LogError(e);
                //setTab.Invoke(Tab.Local);
            }



            // Keyboard Shortcut: Cycle tabs
            if (GUIHelper.CanDoInput() && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                int count = Enum.GetValues(typeof(Tab)).Length;
                int ind = (int)tab;
                if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    ind -= 1;
                    if (ind < 0)
                        ind = count - 1;
                    // Net for Bugs
                    BGM.StopSongPreview();
                    setTab((Tab)ind);
                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    ind += 1;
                    ind %= count;
                    // Net for Bugs
                    BGM.StopSongPreview();
                    setTab((Tab)ind);
                }
            }
        }

        private static void RenderListTop(Tab tab, Action<Tab> onSetTab)
        {
            GUILayout.BeginHorizontal();
            EnumTooltipPickerUI.Render(tab, onSetTab, tabName =>
            {
                switch (tabName)
                {
                    case Tab.Online:
                        return "Online";
                    case Tab.Local:
                        return "Local";
                        /*
                    case Tab.Submissions:
                        var p = CustomBeatmaps.SubmissionPackageManager;
                        if (p.ListLoaded && p.SubmissionPackages.Count > 0)
                            return $"Submissions <b>x {p.SubmissionPackages.Count}!</b>";
                        return "Submissions";
                        */
                    case Tab.Osu:
                        return "OSU!";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tabName), tabName, null);
                }
            });
            GUILayout.EndHorizontal();
            UserOnlineInfoBarUI.Render();
        }
    }
}