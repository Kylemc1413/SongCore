using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using SongCore.Utilities;
namespace SongCore
{
    public class ProgressBar : MonoBehaviour
    {
        private Canvas _canvas;
        private TMP_Text _authorNameText;
        private TMP_Text _pluginNameText;
        private TMP_Text _headerText;
        internal Image _loadingBackg;
        internal Image _loadingBar;

        private static readonly Vector3 Position = new Vector3(0, 2.5f, 2.5f);
        private static readonly Vector3 Rotation = new Vector3(0, 0, 0);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        private static readonly Vector2 CanvasSize = new Vector2(100, 50);

        private const string AuthorNameText = "";
        private const float AuthorNameFontSize = 7f;
        private static readonly Vector2 AuthorNamePosition = new Vector2(10, 31);

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
            StopAllCoroutines();
            _showingMessage = true;
            _headerText.text = message;
            _loadingBar.enabled = false;
            _loadingBackg.enabled = false;
            _canvas.enabled = true;
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
            if (newScene.name == "MenuCore")
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
            _headerText.text = HeaderText;
            _loadingBar.enabled = true;
            _loadingBackg.enabled = true;
            _canvas.enabled = true;
        }

        private void SongLoaderOnSongsLoadedEvent(Loader arg1, Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            _showingMessage = false;
            _headerText.text = arg2.Count + " songs loaded.";
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
            gameObject.transform.position = Position;
            gameObject.transform.eulerAngles = Rotation;
            gameObject.transform.localScale = Scale;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = CanvasSize;

            _authorNameText = Utils.CreateText(_canvas.transform as RectTransform, AuthorNameText, AuthorNamePosition);
            rectTransform = _authorNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = AuthorNamePosition;
            rectTransform.sizeDelta = HeaderSize;
            _authorNameText.text = AuthorNameText;
            _authorNameText.fontSize = AuthorNameFontSize;

            _pluginNameText = Utils.CreateText(_canvas.transform as RectTransform, PluginNameText, PluginNamePosition);
            rectTransform = _pluginNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = HeaderSize;
            rectTransform.anchoredPosition = PluginNamePosition;
            _pluginNameText.text = PluginNameText;
            _pluginNameText.fontSize = PluginNameFontSize;

            _headerText = Utils.CreateText(_canvas.transform as RectTransform, HeaderText, HeaderPosition);
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

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_canvas.enabled) return;
            _loadingBar.fillAmount = Loader.LoadingProgress;
            
            _loadingBar.color =  HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * 0.35f, 1), 1, 1));
            _headerText.color = HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * 0.35f, 1), 1, 1));
        }
    }
}