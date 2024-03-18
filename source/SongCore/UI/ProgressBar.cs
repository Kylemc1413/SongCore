using System;
using System.Collections;
using System.Collections.Concurrent;
using SongCore.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SongCore.UI
{
    public class ProgressBar : MonoBehaviour
    {
        private Canvas? _canvas;
        private TMP_Text? _pluginNameText;
        private TMP_Text? _headerText;
        private Image? _loadingBackg;
        private Image? _loadingBar;

        private static bool _jokeTime = false;
        private static readonly Vector3 Position = new Vector3(0, 2.5f, 2.5f);
        private static readonly Vector3 Rotation = new Vector3(0, 0, 0);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        private static readonly Vector2 CanvasSize = new Vector2(100, 50);

        private const string PluginNameText = "SongCore Loader";
        private const float PluginNameFontSize = 9f;
        private static readonly Vector2 PluginNamePosition = new Vector2(10, 23);

        private static readonly Vector2 HeaderPosition = new Vector2(10, 15);
        private static readonly Vector2 HeaderSize = new Vector2(100, 20);
        private const string HeaderText = "Loading songs...";
        private const float HeaderFontSize = 15f;

        private static readonly Vector2 LoadingBarSize = new Vector2(100, 10);
        private static readonly Color BackgroundColor = new Color(0, 0, 0, 0.2f);

        private bool _showingMessage;

        public static ProgressBar Create()
        {
            return new GameObject("Progress Bar").AddComponent<ProgressBar>();
        }

        public void ShowMessage(string message, float time)
        {
            ShowMessage(message);
            StartCoroutine(DisableCanvasRoutine(time));
        }

        public void ShowMessage(string message)
        {
            StopAllCoroutines();
            _showingMessage = true;
            _headerText.text = message;
            _loadingBar.enabled = false;
            _loadingBackg.enabled = false;
            _canvas.enabled = true;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            Loader.LoadingStartedEvent += SongLoaderOnLoadingStartedEvent;
            Loader.SongsLoadedEvent += SongLoaderOnSongsLoadedEvent;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            Loader.LoadingStartedEvent -= SongLoaderOnLoadingStartedEvent;
            Loader.SongsLoadedEvent -= SongLoaderOnSongsLoadedEvent;
        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MainMenu")
            {
                if (_showingMessage)
                {
                    _canvas.enabled = true;
                }
            }
            else
            {
                _canvas.enabled = false;
            }
        }

        private void SongLoaderOnLoadingStartedEvent(Loader obj)
        {
            StopAllCoroutines();
            _showingMessage = false;
            _headerText.text = _jokeTime ? "Deleting songs..." : HeaderText;
            _loadingBar.enabled = true;
            _loadingBackg.enabled = true;
            _canvas.enabled = true;
        }

        private void SongLoaderOnSongsLoadedEvent(Loader loader, ConcurrentDictionary<string, BeatmapLevel> customLevels)
        {
            _showingMessage = false;
            string songOrSongs = customLevels.Count == 1 ? "song" : "songs";
            _headerText.text = $"{customLevels.Count} {(_jokeTime ? $"{songOrSongs} deleted" : $"{songOrSongs} loaded")}";
            _loadingBar.enabled = false;
            _loadingBackg.enabled = false;
            StartCoroutine(DisableCanvasRoutine(5f));
        }

        private IEnumerator DisableCanvasRoutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            _canvas.enabled = false;
            _showingMessage = false;
        }

        private void Awake()
        {
            var time = IPA.Utilities.Utils.CanUseDateTimeNowSafely ? DateTime.Now : DateTime.UtcNow;

            _jokeTime = time is { Day: 18, Month: 6 } or { Day: 1, Month: 4 };

            gameObject.transform.position = Position;
            gameObject.transform.eulerAngles = Rotation;
            gameObject.transform.localScale = Scale;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = CanvasSize;

            var pluginText = _jokeTime ? "SongCore Cleaner" : PluginNameText;
            _pluginNameText = BeatSaberMarkupLanguage.BeatSaberUI.CreateText(_canvas.transform as RectTransform, pluginText, PluginNamePosition);
            rectTransform = _pluginNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = HeaderSize;
            rectTransform.anchoredPosition = PluginNamePosition;
            _pluginNameText.text = pluginText;
            _pluginNameText.fontSize = PluginNameFontSize;

            _headerText = BeatSaberMarkupLanguage.BeatSaberUI.CreateText(_canvas.transform as RectTransform, HeaderText, HeaderPosition);
            rectTransform = _headerText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = HeaderPosition;
            rectTransform.sizeDelta = HeaderSize;
            _headerText.text = HeaderText;
            _headerText.fontSize = HeaderFontSize;

            _loadingBackg = new GameObject("Background").AddComponent<Image>();
            rectTransform = _loadingBackg.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = LoadingBarSize;
            _loadingBackg.color = BackgroundColor;

            _loadingBar = new GameObject("Loading Bar").AddComponent<Image>();
            rectTransform = _loadingBar.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = LoadingBarSize;
            var tex = Texture2D.whiteTexture;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
            _loadingBar.sprite = sprite;
            _loadingBar.type = Image.Type.Filled;
            _loadingBar.fillMethod = Image.FillMethod.Horizontal;
            _loadingBar.color = new Color(1, 1, 1, 0.5f);
        }

        private void Update()
        {
            if (!_canvas.enabled)
            {
                return;
            }

            _loadingBar.fillAmount = Loader.LoadingProgress;

            _loadingBar.color = _jokeTime ? Color.red : HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * 0.35f, 1), 1, 1));
            _headerText.color = _jokeTime ? Color.red : HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * 0.35f, 1), 1, 1));
        }
    }
}