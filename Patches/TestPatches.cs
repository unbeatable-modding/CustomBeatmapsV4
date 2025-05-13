using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Arcade.UI.MenuStates;
using UI;
using Arcade.UI.AnimationSystem;

namespace CustomBeatmaps.Patches
{
    public class TestPatches
    {
        [HarmonyPatch(typeof(ArcadeMenuStateMachine), "SetState")]
        [HarmonyPrefix]
        public static void OnLoaded(ref ArcadeMenuStateMachine __instance)
        {
            CustomBeatmaps.Log.LogDebug($"OnFinishedLoading Triggered");
            if (SceneManager.GetActiveScene().name != "ArcadeModeMenu") { return; }
            CustomBeatmaps.Log.LogMessage("We are in arcade!");
            var toSet = "<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.";
            var TextIdle = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle").GetComponent<TextMeshProUGUI>();
            var TextActive = GameObject.Find("New Arcade Menu/ScreenArea/MainScreens/MainMenu/Buttons/ChaboButtonContainer/ChaboButton/Button/TextIdle/StateActive/TextActive").GetComponent<TextMeshProUGUI>();
            var objectToCheck = GameObject.Find("New Arcade Menu");
            var texts = __instance.GetComponentsInChildren<TextMeshProUGUI>(true);
            //var texts2 = texts.AddRangeToArray(Traverse.Create(__instance).Field("menuTransitionsRoot").GetValue<Transform>().GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true));
            //var texts3 = texts.AddRangeToArray(Traverse.Create(__instance).Field("menuTransitionsRoot").GetValue<Transform>().GetComponentsInChildren<TextMeshProUGUI>());


            foreach (var txt in texts)
            {
                txt.SetText("fuck");

            }
            //TextIdle.SetText("<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.");
            //TextActive.SetText("<mspace=11>//<mspace=17> </mspace><cspace=0.35em>beatmap manager.");
        }

        [HarmonyPatch(typeof(ClickButtonOnAnyKeyPress), "OnAnyButtonPressed")]
        [HarmonyPrefix]
        public static bool OverrideAnyButton()
        {
            return false;
        }
    }
}
