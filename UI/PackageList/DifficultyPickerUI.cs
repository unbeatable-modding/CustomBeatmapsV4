using System;
using UnityEngine;

namespace CustomBeatmaps.UI.PackageList
{
    public static class DifficultyPickerUI
    {
        public static void Render(Difficulty difficulty, Action<Difficulty> setDifficulty)
        {
            GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            GUILayout.Label("Difficulty", GUILayout.ExpandWidth(false));
            EnumTooltipPickerUI.Render(difficulty, setDifficulty);
            GUILayout.EndHorizontal();
        }
    }
}