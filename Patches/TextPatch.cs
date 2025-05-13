using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Arcade.UI;
using Arcade.UI.MenuStates;
using Arcade.UI.Options;
using HarmonyLib;
using Rewired.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CustomBeatmaps.Patches
{
    public class TextPatch
    {

  
        //[HarmonyPatch(typeof(ArcadeMenuTitleScreenState), "Enter")]
        //[HarmonyPrefix]
        public static void GetProviderName()
        {
            CustomBeatmaps.Log.LogMessage($"Trying to replace text");
            var TextIdle = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle").GetComponent<TextMeshProUGUI>();
            var TextActive = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle/StateActive/TextActive").GetComponent<TextMeshProUGUI>();
            Traverse.Create(TextIdle).Method("Awake");
            Traverse.Create(TextActive).Method("Awake");
            TextActive.enabled = true;
            TextIdle.enabled = true;
            TextIdle.SetText("<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.");
            TextActive.SetText("<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.");

        }

        [HarmonyPatch(typeof(TextMeshProUGUI), "GenerateTextMesh")]
        [HarmonyPrefix]
        public static bool ItsAllFuck(ref TextMeshProUGUI __instance)
        {
            //if (__instance is not TextMeshProUGUI) { return true; }
            Traverse.Create(__instance).Field("m_text").SetValue("fuck");
            //__instance.text = "fuck";
            return true;
            
        }

        

        //[HarmonyPatch(typeof(TextMeshProUGUI), "GetCanvas")]
        //[HarmonyPrefix]
        public static void MainMenuCheck(ref TextMeshProUGUI __instance)
        {
            if (SceneManager.GetActiveScene().name != "ArcadeModeMenu") { return; }
            //var textMesh = (TextMeshProUGUI)__instance;
            var TextIdle = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle").GetComponent<TextMeshProUGUI>();
            var TextActive = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle/StateActive/TextActive").GetComponent<TextMeshProUGUI>();
            //CustomBeatmaps.Log.LogMessage("menu loaded?");
            if (__instance == TextIdle || __instance == TextActive)
            {
                __instance.SetText("<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.");
                //__instance.text = "<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.";
                //__result = "<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.";

                CustomBeatmaps.Log.LogMessage($"Trying to replace text");
                //return false;
            }
            //return true;
        }



    }
}
