using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BS_Utils.Utilities;
using HMUI;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.UI
{
    public class ColorsUI : NotifiableSingleton<ColorsUI>
    {
        private BoostedColorSchemeView boostedColorSchemeView;

        private readonly Color voidColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        [UIComponent("modal")]
        private readonly ModalView modal;

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
                StandardLevelDetailViewController standardLevel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.colors.bsml"),
                    standardLevel.transform.Find("LevelDetail").gameObject, this);
            }
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            ColorSchemeView colorSchemeView = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<ColorSchemeView>().First(), selectedColorTransform);
            boostedColorSchemeView = (BoostedColorSchemeView) ReflectionUtil.CopyComponent(colorSchemeView, typeof(ColorSchemeView), typeof(BoostedColorSchemeView), colorSchemeView.gameObject);
            DestroyImmediate(colorSchemeView);
            boostedColorSchemeView.Setup();
            modal.blockerClickedEvent += Dismiss;
        }

        private void Dismiss()
        {
            modal.Hide(false, () => RequirementsUI.instance.ShowRequirements());
        }

        private void SetColors(ExtraSongData.DifficultyData songData)
        {
            Color saberLeft = songData._colorLeft == null ? voidColor : Utils.ColorFromMapColor(songData._colorLeft);
            Color saberRight = songData._colorRight == null ? voidColor : Utils.ColorFromMapColor(songData._colorRight);
            Color envLeft = songData._envColorLeft == null
                ? songData._colorLeft == null ? voidColor : Utils.ColorFromMapColor(songData._colorLeft)
                : Utils.ColorFromMapColor(songData._envColorLeft);
            Color envRight = songData._envColorRight == null
                ? songData._colorRight == null ? voidColor : Utils.ColorFromMapColor(songData._colorRight)
                : Utils.ColorFromMapColor(songData._envColorRight);
            var envLeftBoost = songData._envColorLeftBoost == null ? voidColor : Utils.ColorFromMapColor(songData._envColorLeftBoost);
            var envRightBoost = songData._envColorRightBoost == null ? voidColor : Utils.ColorFromMapColor(songData._envColorRightBoost);
            Color obstacle = songData._obstacleColor == null ? voidColor : Utils.ColorFromMapColor(songData._obstacleColor);

            boostedColorSchemeView.SetColors(saberLeft, saberRight, envLeft, envRight, envLeftBoost, envRightBoost, obstacle);
        }
    }
}
