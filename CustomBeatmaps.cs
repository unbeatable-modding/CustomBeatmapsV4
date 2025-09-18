using BepInEx;
using BepInEx.Logging;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Patches;
using CustomBeatmaps.UI;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;


namespace CustomBeatmaps
{
    [BepInPlugin(modGUID, modName, modVer)]
    public class CustomBeatmaps : BaseUnityPlugin
    {
        private const string modGUID = PluginInfo.PLUGIN_GUID;
        private const string modName = PluginInfo.PLUGIN_NAME;
        private const string modVer = PluginInfo.PLUGIN_VERSION;

        internal static new ManualLogSource Log;

        public static ModConfig ModConfig { get; private set; }
        public static BackendConfig BackendConfig { get; private set; }

        public static UserSession UserSession { get; private set; }

        //public static List<LocalPackageManager> LocalUserPackages { get; private set; }
        public static PackageManagerLocal LocalUserPackages { get; private set; }
        public static PackageManagerServer LocalServerPackages { get; private set; }
        public static SubmissionPackageManager SubmissionPackageManager { get; private set; }
        public static PackageManagerLocal OSUSongManager { get; private set; }
        public static PlayedPackageManager PlayedPackageManager { get; private set; }
        public static ServerHighScoreManager ServerHighScoreManager { get; private set; }
        public static BeatmapDownloader Downloader { get; private set; }
        public static Rhythm.BeatmapIndex DefaultBeatmapIndex { get; private set; }
        public static GameMemory Memory { get; private set; }

        private static readonly string MEMORY_LOCATION = "CustomBeatmapsV4-Data/.memory";

        // Check for config reload every 2 seconds
        private readonly Timer _checkConfigReload = new Timer(2000);

        private static bool arcadePatched = false;

        private static readonly Harmony Harmony = new Harmony(modGUID);


        static CustomBeatmaps()
        {
            // Log inner exceptions by default
            EventBus.ExceptionThrown += ex => ScheduleHelper.SafeInvoke(() => Debug.LogException(ex));

            PackageHelper.TryAddCustomCategory();

            // Anything with Static access should be ALWAYS present.
            LocalUserPackages = new PackageManagerLocal(OnError);
            LocalServerPackages = new PackageManagerServer(OnError);
            SubmissionPackageManager = new SubmissionPackageManager(OnError);
            OSUSongManager = new PackageManagerLocal(OnError);
            ServerHighScoreManager = new ServerHighScoreManager();

            if (!Directory.Exists("CustomBeatmapsV4-Data"))
                Directory.CreateDirectory("CustomBeatmapsV4-Data");

            // Load game memory from disk
            Memory = GameMemory.Load(MEMORY_LOCATION);

            ConfigHelper.LoadConfig("custombeatmaps_config.json", () => new ModConfig(), config =>
            {
                ModConfig = config;
                // Local package folders
                /*
                for (int i = 0; i < config.UserPackagesDir.Count(); i++)
                {
                    LocalUserPackages.Add(new LocalPackageManager(OnError));
                    LocalUserPackages.Last().SetFolder(config.UserPackagesDir[i], 7);
                }
                */
                LocalUserPackages.SetFolder(config.UserPackagesDir[0], 7);
                LocalServerPackages.SetFolder(config.ServerPackagesDir, 10);
                OSUSongManager.SetFolder(config.OsuSongsOverrideDirectory, 9);
                PlayedPackageManager = new PlayedPackageManager(config.PlayedBeatmapList);
            });
            ConfigHelper.LoadConfig("CustomBeatmapsV4-Data/custombeatmaps_backend.json", () => new BackendConfig(), config => BackendConfig = config);

            UserSession = new UserSession();
            Downloader = new BeatmapDownloader();
        }

        private static void OnError(Exception ex)
        {
            ScheduleHelper.SafeInvoke(() => Debug.LogException(ex));
            try
            {
                EventBus.ExceptionThrown?.Invoke(ex);
            }
            catch (Exception e)
            {
                // ???
                ScheduleHelper.SafeInvoke(() => Debug.LogException(e));
            }
        }

        void Awake()
        {
            Logger.LogInfo("CustomBeatmapsV4: Awake?");
            UnityEngine.Object.DontDestroyOnLoad(this);
            Log = base.Logger;

            // At a regular interval, reload changed configs.
            _checkConfigReload.Elapsed += (obj, evt) => ScheduleHelper.SafeInvoke(ConfigHelper.ReloadChangedConfigs);
            _checkConfigReload.Start();

            // User session
            Task.Run(UserSession.AttemptLogin);

            // Harmony Patching
            Type[] classesToPatch = {
                typeof(DebugLogPatch),
                typeof(OsuEditorPatch),
                //typeof(PauseMenuPatch),
                //typeof(DisablePracticeRoomOpenerPatch),
                //typeof(CursorUnhidePatch),
                typeof(OneLifeModePatch),
                typeof(FlipModePatch),
                typeof(DisableRewiredMouseInputPatch),
                typeof(ArcadeOverridesPatch),
                typeof(AudioPatch),
                typeof(ChaboButtonPatch),
            };
            foreach (var toPatch in classesToPatch)
            {
                try
                {
                    Logger.LogDebug($"Patching {toPatch}");
                    Harmony.CreateAndPatchAll(toPatch, modGUID);
                }
                catch (Exception e)
                {
                    Logger.LogError($"EXCEPTION CAUGHT while PATCHING:");
                    Logger.LogError(e.ToString());
                }
            }

            PackageHelper.TryAddCustomCategory();
        }

        public void Start()
        {
            // Disclaimer screen
            Logger.LogDebug($"Opening Disclaimer Disabled: {Memory.OpeningDisclaimerDisabled}");
            if (!Memory.OpeningDisclaimerDisabled)
            {
                // Make the game freeze
                Time.timeScale = 0;

                var disclaimer = new GameObject().AddComponent<OpeningDisclaimerUIBehaviour>();
                disclaimer.OnSelect += () =>
                {
                    // Reload
                    Time.timeScale = 1;
                    Memory.OpeningDisclaimerDisabled = true;
                    GameMemory.Save(MEMORY_LOCATION, Memory);
                    SceneManager.LoadScene(0);
                };
            }

            // Add images to songs at later timing because the game will crash if loaded any earlier
            PackageHelper.GetAllCustomSongInfos.ForEach(s => s.GetTexture());
            ArcadeHelper.LoadCustomSongs();

        }

        private static bool _quitted;
        private void OnDestroy()
        {
            // Save our memory
            if (!_quitted)
                GameMemory.Save(MEMORY_LOCATION, Memory);
            _quitted = true;
        }

        private void OnApplicationQuit()
        {
            // Save our memory
            if (!_quitted)
                GameMemory.Save(MEMORY_LOCATION, Memory);
            _quitted = true;
        }

    }
}
