using System;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using CustomBeatmaps.Patches;
using HarmonyLib;

namespace CustomBeatmaps
{
    [BepInPlugin(modGUID, modName, modVer)]
    public class CustomBeatmaps : BaseUnityPlugin
    {
        private const string modGUID = "gold-me.unbeatable.custombeatmaps";
        private const string modName = "Custom Beatmaps V4";
        private const string modVer = "0.0.1";

        internal static new ManualLogSource Log;

        //private readonly Harmony harmony = new Harmony(modGUID);


        void Awake()
        {
            Logger.LogInfo("CustomBeatmapsV4: Awake?");
            Log = base.Logger;

            // At a regular interval, reload changed configs.
            //_checkConfigReload.Elapsed += (obj, evt) => ScheduleHelper.SafeInvoke(ConfigHelper.ReloadChangedConfigs);
            //_checkConfigReload.Start();

            // User session
            //Task.Run(UserSession.AttemptLogin);

            // Harmony Patching
            Type[] classesToPatch = {
                //typeof(DebugLogPatch),
                //typeof(WhiteLabelMainMenuPatch),
                //typeof(CustomBeatmapLoadingOverridePatch),
                //typeof(OsuEditorPatch),
                //typeof(HighScoreScreenPatch),
                //typeof(PauseMenuPatch),
                //typeof(DisablePracticeRoomOpenerPatch),
                //typeof(CursorUnhidePatch),
                //typeof(OneLifeModePatch),
                //typeof(FlipModePatch),
                //typeof(SimpleJankHighScoreSongReplacementPatch),
                typeof(DisableRewiredMouseInputPatch),
                typeof(ArcadeOverridesPatch),
                typeof(AudioPatch)
            };
            foreach (var toPatch in classesToPatch)
            {
                try
                {
                    Logger.LogDebug($"Patching {toPatch}");
                    Harmony.CreateAndPatchAll(toPatch);
                }
                catch (Exception e)
                {
                    Logger.LogError("EXCEPTION CAUGHT while PATCHING:");
                    Logger.LogError(e.ToString());
                }
            }


        }

    }
}
