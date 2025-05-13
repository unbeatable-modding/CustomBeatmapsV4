using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using CustomBeatmaps.Patches;
using CustomBeatmaps.UI;
using CustomBeatmaps.Util;
using System.Timers;
using HarmonyLib;
using CustomBeatmaps.CustomPackages;
using Debug = UnityEngine.Debug;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps
{
    [BepInPlugin(modGUID, modName, modVer)]
    public class CustomBeatmaps : BaseUnityPlugin
    {
        private const string modGUID = "gold-me.unbeatable.custombeatmaps";
        private const string modName = "Custom Beatmaps V4";
        private const string modVer = "0.1.0";

        internal static new ManualLogSource Log;

        public static ModConfig ModConfig { get; private set; }
        public static BackendConfig BackendConfig { get; private set; }

        //public static UserSession UserSession { get; private set; }

        public static List<LocalPackageManager> LocalUserPackages { get; private set; }
        public static LocalPackageManager LocalWhiteLabelPackages { get; private set; }
        //public static LocalPackageManager LocalServerPackages { get; private set; }
        //public static SubmissionPackageManager SubmissionPackageManager { get; private set; }
        //public static OSUSongManager OSUSongManager { get; private set; }
        public static LocalPackageManager OSUSongManager { get; private set; }
        //public static PlayedPackageManager PlayedPackageManager { get; private set; }
        //public static ServerHighScoreManager ServerHighScoreManager { get; private set; }
        //public static BeatmapDownloader Downloader { get; private set; }
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

            CustomPackageHelper.TryAddCustomCategory();

            // Anything with Static access should be ALWAYS present.
            DefaultBeatmapIndex = Rhythm.BeatmapIndex.defaultIndex;
            //LocalUserPackages = new LocalPackageManager(OnError);
            LocalUserPackages = new List<LocalPackageManager>();
            LocalWhiteLabelPackages = new LocalPackageManager(OnError);
            //LocalServerPackages = new LocalPackageManager(OnError);
            //SubmissionPackageManager = new SubmissionPackageManager(OnError);
            OSUSongManager = new LocalPackageManager(OnError);
            //ServerHighScoreManager = new ServerHighScoreManager();

            if (!Directory.Exists("CustomBeatmapsV4-Data"))
                Directory.CreateDirectory("CustomBeatmapsV4-Data");

            // Load game memory from disk
            Memory = GameMemory.Load(MEMORY_LOCATION);

            ConfigHelper.LoadConfig("custombeatmaps_config.json", () => new ModConfig(), config =>
            {
                ModConfig = config;
                // Local package folders
                for (int i = 0; i < config.UserPackagesDir.Count(); i++)
                {
                    LocalUserPackages.Add(new LocalPackageManager(OnError));
                    LocalUserPackages.Last().SetFolder(config.UserPackagesDir[i], 7);
                }
                LocalWhiteLabelPackages.SetFolder(config.WhiteLabelPackagesDir, 8);
                //LocalServerPackages.SetFolder(config.ServerPackagesDir);
                //OSUSongManager.SetOverride(ref config.OsuSongsOverrideDirectory, 9);
                OSUSongManager.SetFolder(config.OsuSongsOverrideDirectory, 9);
                //PlayedPackageManager = new PlayedPackageManager(config.PlayedBeatmapList);
            });
            ConfigHelper.LoadConfig("CustomBeatmapsV4-Data/custombeatmaps_backend.json", () => new BackendConfig(), config => BackendConfig = config);

            //UserSession = new UserSession();
            //Downloader = new BeatmapDownloader();
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
            //Task.Run(UserSession.AttemptLogin);

            // Harmony Patching
            Type[] classesToPatch = {
                typeof(DebugLogPatch),
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
                typeof(AudioPatch),
                typeof(ChaboButtonPatch),
                //typeof(TextPatch)
                //typeof(TestPatches)
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

            CustomPackageHelper.TryAddCustomCategory();
        }

        public TestUI songHackUI = new TestUI();

        public void OnGUI()
        {
            //songHackUI.DrawUI();
        }

        public void Start()
        {
            // Add images to songs at later timing because the game will crash if loaded any earlier
            var pkglist = new List<CustomLocalPackage>();
            CustomBeatmaps.LocalUserPackages.ForEach((LocalPackageManager pkg) => pkglist.AddRange(pkg.Packages));
            pkglist.AddRange(CustomBeatmaps.LocalWhiteLabelPackages.Packages);
            pkglist.AddRange(CustomBeatmaps.OSUSongManager.Packages);

            var songl = new List<Song>();
            pkglist.ForEach((CustomLocalPackage p) => songl.AddRange(p.PkgSongs));
            //songl.AddRange(CustomBeatmaps.OSUSongManager.OsuBeatmaps);

            songl.ForEach((Song s) => ((CustomSongInfo)s).GetTexture() );

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
