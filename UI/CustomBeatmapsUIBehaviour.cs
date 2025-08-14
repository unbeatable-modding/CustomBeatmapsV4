using System;
using System.Collections.Generic;
using Arcade.UI;
using Arcade.UI.MenuStates;
using CustomBeatmaps.Patches;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using DG.Tweening;
using FMOD;
using RJBuildScripts;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class CustomBeatmapsUIBehaviour : MonoBehaviour
    {
        public static bool Opened => _open;

        private readonly ReaccStore _store = new ReaccStore();
        private Vector2 _windowOffset;

        private static readonly float WindowPadding = 8;

        private int _releaseInputTimer;
        private static bool _open;
        private readonly List<Exception> _errors = new List<Exception>();

        private void Awake()
        {
            EventBus.ExceptionThrown += e =>
            {
                _errors.Add(e);
            };
        }

        private void OnDestroy()
        {
            _open = false;
        }

        public void Open()
        {
            if (_open)
                return;
            // This is lazy but it works for now
            GameObject.Find("New Arcade Menu/ScreenArea/MainScreens").SetActive(false);
            GameObject.Find("New Arcade Menu/ScreenArea/OptionsCorner").SetActive(false);
            ArcadeBGMManager.Instance.StopSongPreview();

            GUIHelper.AvoidInputOneFrame();
            _releaseInputTimer = 3;
            _open = true;
            DOTween.Kill(this);
            DOTween.To(() => _windowOffset, value => _windowOffset = value, Vector2.zero, 0.2f)
                .SetEase(Ease.OutBounce)
                .SetId(this);
            OnGUI();
            //WhiteLabelMainMenuPatch.DisableBGM();

            // Reload our high scores + submissions just to be clean/up to date
            //CustomBeatmaps.ServerHighScoreManager.Reload();
            //CustomBeatmaps.SubmissionPackageManager.RefreshServerSubmissions();
        }

        public void Close()
        {
            _open = false;
            GameObject.Find("New Arcade Menu/ScreenArea/MainScreens").SetActive(true);
            GameObject.Find("New Arcade Menu/ScreenArea/OptionsCorner").SetActive(true);
            //ArcadeBGMManager.Instance.PlaySongPreview();
        }

        public void oldClose()
        {
            if (!_open || DOTween.IsTweening(this))
                return;

            _releaseInputTimer = -1;
            DOTween.Kill(this);
            DOTween.To(() => _windowOffset, value => _windowOffset = value,
                new Vector2(-Screen.width, -1f * (float)Screen.height / 4f) // Looks better if we move more to the left when exiting
                , 0.5f)
                .SetEase(Ease.OutExpo)
                .SetId(this)
                .OnComplete(() =>
                {
                    _open = false;
                    //WhiteLabelMainMenuPatch.EnableBGM();
                    //WhiteLabelMainMenuPatch.StopSongPreview();
                    _windowOffset = new Vector2(-1 * Screen.width, -1 * Screen.height);
                });

            
        }

        private void Update()
        {
            // We do this to prevent first frame input from switching to this UI.
            // Janky ish but it's the easiest/most concise fix
            if (--_releaseInputTimer == 0)
            {
                GUIHelper.FreeInput();
                _releaseInputTimer = -1;
            }
        }

        private void OnGUI()
        {
            if (!_open)
            {
                if (ArcadeMenuStateMachine.Instance.CurrentState.StateName.ToString() == "SongSelect")
                {
                    //float tmpScale = 1;
                    float bp = WindowPadding;
                    //float bw = (120 / tmpScale) - bp * 2,
                    //    bh = (30 / tmpScale) - bp * 2;
                    if (GUI.Button(new Rect(bp + _windowOffset.x, bp + _windowOffset.y, 120, 30), "Custom Beatmaps"))
                        this.Open();
                }
                
                return;
            }
                

            // Dark mode
            GUIHelper.SetDarkMode(Config.Mod.DarkMode);

            Reacc.SetStore(_store);

            // TODO: Incorporate scaling and fix the UI list bugs
            // Currently this BREAKS the UI further down the list, might be a fixable thing
            //float scale = GUIHelper.PerformScreenScale();
            float scale = 1;

            float p = WindowPadding;
            float w = (Screen.width / scale) - p * 2,
                h = (Screen.height / scale) - p * 2;
            GUILayout.Window(Reacc.GetUniqueId(), new Rect(p + _windowOffset.x, p + _windowOffset.y, w, h), id =>
            {
                try
                {
                    // Main UI
                    // Does nothing
                    //GUILayout.BeginVertical(GUILayout.MaxWidth(w));
                    CustomBeatmapsUI.Render();
                    //GUILayout.EndVertical();
                }
                catch (ArgumentException e)
                {
                    // Skip if we're just doing a "Getting Control" exception.
                    if (!e.Message.Contains("Getting control"))
                    {
                        _errors.Add(e);
                        throw;
                    }
                }

            }, "Custom Beatmaps v" + VersionHelper.GetModVersion());
            // Extra Error UI
            ErrorUI.Render(_errors);
        }
    }
}
