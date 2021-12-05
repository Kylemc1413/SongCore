using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using UnityEngine;

namespace SongCore.UI
{
    public class ColorsUI : NotifiableSingleton<ColorsUI>
    {
        private ColorSchemeView colorSchemeView;

        [UIComponent("horizontal")]
        private readonly RectTransform horizontalTransform;

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
            colorSchemeView = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<ColorSchemeView>().First(), horizontalTransform);
        }
    }
}