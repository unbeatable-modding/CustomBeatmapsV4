using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Arcade.UI;
using Arcade.UI.MenuStates;
using CustomBeatmaps.Util;
using HarmonyLib;
using InGameCutsceneStuff.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CustomBeatmaps.Patches
{
    public class ChaboButtonPatch
    {
        public static GameObject testobj;

        [HarmonyPatch(typeof(FMODButton), "OnSubmit")]
        [HarmonyPatch(typeof(FMODButton), "OnPointerClick")]
        [HarmonyPrefix]
        public static void ChaboButton(ref FMODButton __instance)
        {
            if (__instance.name == "ChaboButton")
            {
                CustomBeatmaps.Log.LogMessage("Button Clicked");
                ArcadeHelper.ReloadArcadeList();
                //MainMenuCheck();
            }

        }

        public static void MainMenuCheck()//(ref TextMeshProUGUI __instance)
        {
            CustomBeatmaps.Log.LogMessage("loading thing");
            var TextIdle = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle").GetComponent<TextMeshProUGUI>();
            //CustomBeatmaps.Log.LogMessage("menu loaded?");
            if (TextIdle)//(__instance == GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle").GetComponent<TextMeshProUGUI>())
            {
                CustomBeatmaps.Log.LogMessage($"starting text: {TextIdle.text}");
                //Traverse.Create(TextIdle).Field("text").SetValue("<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.");
                TextIdle.text = "<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.";
                //Traverse.Create(TextIdle).Method("Awake");

                CustomBeatmaps.Log.LogMessage($"new text: {TextIdle.text}");
            }
            //CustomBeatmaps.Log.LogDebug($"ignore this is a test");
            //TextMeshProUGUI TextActive = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle").GetComponent<TextMeshProUGUI>();
            //TextMeshProUGUI TextIdle = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle/StateActive/TextActive").GetComponent<TextMeshProUGUI>();
            //TextActive.text = "<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.";
            //TextIdle.text = "<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.";
            //CustomBeatmaps.Log.LogMessage($"found {TextActive.text} & {TextIdle.text}");
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CustomBeatmaps.Log.LogInfo($"Scene {scene.name} loaded. Applying patches...");
            if (scene.name == "ArcadeModeMenu")
            {
                //CustomBeatmaps.StartCoroutine(MainMenuCheck());
            }
            //ApplyPatches();
            // Unsubscribe to avoid re-patching on subsequent scene loads
            //SceneManager.sceneLoaded -= OnSceneLoaded;
        }

    }
}
