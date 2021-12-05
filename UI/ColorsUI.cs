using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.UI
{
    public class ColorsUI : NotifiableSingleton<ColorsUI>
    {
        private ColorSchemeView colorSchemeView;

        private readonly Color voidColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        [UIComponent("selected-color")]
        private readonly RectTransform selectedColorTransform;

        [UIValue("colors")]
        public bool Colors
        {
            get => BasicUI.ModPrefs.GetBool("SongCore", "customSongColors", true, true);
            set
            {
                Plugin.CustomSongColors = value;
                BasicUI.ModPrefs.SetBool("SongCore", "customSongColors", value);
            }
        }

        internal void Setup()
        {
            ColorsOverrideSettingsPanelController colorsOverrideSettings = Resources.FindObjectsOfTypeAll<ColorsOverrideSettingsPanelController>().First();
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.colors.bsml"), colorsOverrideSettings.transform.Find("Settings").gameObject, this);            
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            colorSchemeView = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<ColorSchemeView>().First(), selectedColorTransform);
            selectedColorTransform.gameObject.SetActive(false);
        }

        internal void SetColors(ExtraSongData.DifficultyData songData)
        {
            if (songData._colorLeft == null && songData._colorRight == null && songData._envColorLeft == null &&
                songData._envColorRight == null && songData._obstacleColor == null)
            {
                HideColors();
                return;
            }

            selectedColorTransform.gameObject.SetActive(true);

            Color saberLeft = songData._colorLeft == null ? voidColor : Utils.ColorFromMapColor(songData._colorLeft);
            Color saberRight = songData._colorRight == null ? voidColor : Utils.ColorFromMapColor(songData._colorRight);
            Color envLeft = songData._envColorLeft == null
                ? songData._colorLeft == null ? voidColor : Utils.ColorFromMapColor(songData._colorLeft)
                : Utils.ColorFromMapColor(songData._envColorLeft);
            Color envRight = songData._envColorRight == null
                ? songData._colorRight == null ? voidColor : Utils.ColorFromMapColor(songData._colorRight)
                : Utils.ColorFromMapColor(songData._envColorRight);
            Color obstacle = songData._obstacleColor == null ? voidColor : Utils.ColorFromMapColor(songData._obstacleColor);

            colorSchemeView.SetColors(saberLeft, saberRight, envLeft, envRight, obstacle);
        }

        internal void HideColors()
        {
            selectedColorTransform.gameObject.SetActive(false);
        }
    }
}
