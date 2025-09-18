using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Arcade.UI;
using Arcade.UI.MenuStates;
using Arcade.UI.SongSelect;
using CustomBeatmaps.UI;
using CustomBeatmaps.Util;
using FMOD.Studio;
using HarmonyLib;
using InGameCutsceneStuff.Runtime;
using Rewired;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CustomBeatmaps.Patches
{
    public class ChaboButtonPatch
    {
        public static GameObject testobj;
        private static CustomBeatmapsUIBehaviour _customBeatmapsUIBehaviour;
        public static CustomBeatmapsUIBehaviour CustomBeatmapsUI => _customBeatmapsUIBehaviour;

        [HarmonyPatch(typeof(ArcadeMenuStateMachine), "Start")]
        [HarmonyPrefix]
        static void PreStart(WhiteLabelMainMenu __instance)
        {
            _customBeatmapsUIBehaviour = new GameObject("CustomBeatmaps UI").AddComponent<CustomBeatmapsUIBehaviour>();
        }

        [HarmonyPatch(typeof(FMODButton), "OnSubmit")]
        [HarmonyPatch(typeof(FMODButton), "OnPointerClick")]
        [HarmonyPrefix]
        public static void ChaboButton(ref FMODButton __instance)
        {
            if (__instance.name == "ChaboButton")
            {
                CustomBeatmaps.Log.LogMessage("Button Clicked");
                //ArcadeHelper.ReloadArcadeList();
                _customBeatmapsUIBehaviour.Open();
                //MainMenuCheck();
            }

        }

        [HarmonyPatch(typeof(UIRewiredReceiver), "Update")]
        [HarmonyPostfix]
        static void PostUpdateCustomMenuEscape(UIRewiredReceiver __instance, Player ____rewired)
        {
            // Escape our menu
            if (CustomBeatmapsUIBehaviour.Opened)
            {
                if (____rewired.GetButtonDown("Cancel") || ____rewired.GetButtonDown("Back"))
                {
                    _customBeatmapsUIBehaviour.Close();
                    //ChooseCamera(__instance, __instance.defaultCam);
                    //__instance.menuState = WhiteLabelMainMenu.MenuState.DEFAULT;
                    //RuntimeManager.PlayOneShot(__instance.menuBackEvent);
                    //_customBeatmapsUIBehaviour.Close();
                }
            }
        }

        [HarmonyPatch(typeof(ArcadeBGMManager), "OnSelectedSongChanged")]
        [HarmonyPrefix]
        static bool DontChangeSongsForUI()
        {
            if (CustomBeatmapsUIBehaviour.Opened)
                return false;
            return true;
        }


        [HarmonyPatch(typeof(ArcadeBGMManager), "PlaySongPreview")]
        [HarmonyPrefix]
        static bool DontChangeSongsForPath(ref ArcadeSongDatabase.BeatmapItem item)
        {
            var currentItem = Traverse.Create(ArcadeBGMManager.Instance).Field("currentItem").GetValue<ArcadeSongDatabase.BeatmapItem>();
            var songPreviewInstance = Traverse.Create(ArcadeBGMManager.Instance).Field("songPreviewInstance").GetValue<EventInstance>();

            if (songPreviewInstance.isValid())
            {
                if (item != null && item.Song == currentItem.Song || item.Path == currentItem.Path)
                    return false;
            }
            return true;
        }

    }
}
