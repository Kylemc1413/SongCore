using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.UI
{
    public class ColorsUI : NotifiableBase
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly GameplaySetupViewController _gameplaySetupViewController;
        private readonly BSMLParser _bsmlParser;

        private ColorsUI(StandardLevelDetailViewController standardLevelDetailViewController, GameplaySetupViewController gameplaySetupViewController, BSMLParser bsmlParser)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _gameplaySetupViewController = gameplaySetupViewController;
            _bsmlParser = bsmlParser;
        }

        private ColorSchemeView colorSchemeView;

        private readonly Color voidColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        [UIComponent("modal")]
        private readonly ModalView modal;

        private Vector3 modalPosition;

        [UIComponent("selected-color")]
        private readonly RectTransform selectedColorTransform;

        [UIValue("noteColors")]
        public bool NoteColors
        {
            get => Plugin.Configuration.CustomSongNoteColors;
            set => Plugin.Configuration.CustomSongNoteColors = value;
        }

        [UIValue("obstacleColors")]
        public bool ObstacleColors
        {
            get => Plugin.Configuration.CustomSongObstacleColors;
            set => Plugin.Configuration.CustomSongObstacleColors = value;
        }

        [UIValue("environmentColors")]
        public bool EnvironmentColors
        {
            get => Plugin.Configuration.CustomSongEnvironmentColors;
            set => Plugin.Configuration.CustomSongEnvironmentColors = value;
        }

        internal void ShowColors(ExtraSongData.DifficultyData songData)
        {
            Parse();
            modal.Show(true);
            SetColors(songData);
        }

        private void Parse()
        {
            if (!modal)
            {
                _bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.colors.bsml"),
                    _standardLevelDetailViewController._standardLevelDetailView.gameObject, this);
            }
            modal.transform.localPosition = modalPosition;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            ColorSchemeView colorSchemeViewPrefab = Object.Instantiate(_gameplaySetupViewController._colorsOverrideSettingsPanelController._colorSchemeDropDown._cellPrefab._colorSchemeView, selectedColorTransform);
            colorSchemeView = IPA.Utilities.ReflectionUtil.CopyComponent<ColorSchemeView>(colorSchemeViewPrefab, colorSchemeViewPrefab.gameObject);
            Object.DestroyImmediate(colorSchemeViewPrefab);
            modalPosition = modal.transform.localPosition;
            modal.blockerClickedEvent += Dismiss;
        }

        private void Dismiss()
        {
            modal.Hide(true);
        }

        private void SetColors(ExtraSongData.DifficultyData songData)
        {
            Color saberLeft = songData._colorLeft == null ? voidColor : Utils.ColorFromMapColor(songData._colorLeft);
            Color saberRight = songData._colorRight == null ? voidColor : Utils.ColorFromMapColor(songData._colorRight);
            Color envLeft = songData._envColorLeft == null ? voidColor : Utils.ColorFromMapColor(songData._envColorLeft);
            Color envRight = songData._envColorRight == null ? voidColor : Utils.ColorFromMapColor(songData._envColorRight);
            Color envLeftBoost = songData._envColorLeftBoost == null ? voidColor : Utils.ColorFromMapColor(songData._envColorLeftBoost);
            Color envRightBoost = songData._envColorRightBoost == null ? voidColor : Utils.ColorFromMapColor(songData._envColorRightBoost);
            Color obstacle = songData._obstacleColor == null ? voidColor : Utils.ColorFromMapColor(songData._obstacleColor);

            colorSchemeView.SetColors(saberLeft, saberRight, envLeft, envRight, envLeftBoost, envRightBoost, obstacle);
        }
    }
}