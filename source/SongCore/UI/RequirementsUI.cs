using System.IO;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using SongCore.Utilities;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;
using HMUI;
using Tweening;

namespace SongCore.UI
{
    public class RequirementsUI : NotifiableSingleton<RequirementsUI>
    {
        private const string BUTTON_BSML = "<bg id='root'><action-button id='info-button' text='?' active='~button-glow' interactable='~button-interactable' anchor-pos-x='31' anchor-pos-y='0' pref-width='12' pref-height='9' on-click='button-click'/></bg>";
        private StandardLevelDetailViewController standardLevel;
        private TweeningManager tweenyManager;
        private ImageView buttonBG;
        private Color originalColor0;
        private Color originalColor1;

        internal Sprite? HaveReqIcon;
        internal Sprite? MissingReqIcon;
        internal Sprite? HaveSuggestionIcon;
        internal Sprite? MissingSuggestionIcon;
        internal Sprite? WarningIcon;
        internal Sprite? InfoIcon;
        internal Sprite? ColorsIcon;

        //Currently selected song data
        public CustomPreviewBeatmapLevel level;
        public Data.ExtraSongData songData;
        public Data.ExtraSongData.DifficultyData? diffData;
        public bool wipFolder;

        [UIComponent("list")]
        public CustomListTableData customListTableData;

        private bool _buttonGlow = false;

        [UIValue("button-glow")]
        public bool ButtonGlowColor
        {
            get => _buttonGlow;
            set
            {
                _buttonGlow = value;
                NotifyPropertyChanged();
            }
        }

        private bool buttonInteractable = false;

        [UIValue("button-interactable")]
        public bool ButtonInteractable
        {
            get => buttonInteractable;
            set
            {
                buttonInteractable = value;
                NotifyPropertyChanged();
            }
        }

        [UIComponent("modal")]
        private ModalView modal;

        private Vector3 modalPosition;

        [UIComponent("info-button")]
        private Transform infoButtonTransform;

        [UIComponent("root")]
        protected readonly RectTransform _root = null!;

        internal void Setup()
        {
            GetIcons();
            standardLevel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            tweenyManager = Object.FindObjectOfType<TweeningManager>();
            BSMLParser.instance.Parse(BUTTON_BSML, standardLevel.transform.Find("LevelDetail").gameObject, this);

            infoButtonTransform.localScale *= 0.7f; //no scale property in bsml as of now so manually scaling it
            (standardLevel.transform.Find("LevelDetail").Find("FavoriteToggle")?.transform as RectTransform)!.anchoredPosition = new Vector2(3, -2);
            buttonBG = infoButtonTransform.Find("BG").GetComponent<ImageView>();
            originalColor0 = buttonBG.color0;
            originalColor1 = buttonBG.color1;
        }

        private void GetIcons()
        {
            if (!MissingReqIcon)
            {
                MissingReqIcon = Utils.LoadSpriteFromResources("SongCore.Icons.RedX.png")!;
            }

            if (!HaveReqIcon)
            {
                HaveReqIcon = Utils.LoadSpriteFromResources("SongCore.Icons.GreenCheck.png")!;
            }

            if (!HaveSuggestionIcon)
            {
                HaveSuggestionIcon = Utils.LoadSpriteFromResources("SongCore.Icons.YellowCheck.png")!;
            }

            if (!MissingSuggestionIcon)
            {
                MissingSuggestionIcon = Utils.LoadSpriteFromResources("SongCore.Icons.YellowX.png")!;
            }

            if (!WarningIcon)
            {
                WarningIcon = Utils.LoadSpriteFromResources("SongCore.Icons.Warning.png")!;
            }

            if (!InfoIcon)
            {
                InfoIcon = Utils.LoadSpriteFromResources("SongCore.Icons.Info.png")!;
            }

            if (!ColorsIcon)
            {
                ColorsIcon = Utils.LoadSpriteFromResources("SongCore.Icons.Colors.png")!;
            }
        }

        [UIAction("button-click")]
        internal void ShowRequirements()
        {
            if (modal == null)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.requirements.bsml"), _root.gameObject, this);
                modalPosition = modal!.transform.localPosition;

            }

            modal.transform.localPosition = modalPosition;
            modal.Show(true);
            customListTableData.data.Clear();

            //Requirements
            if (diffData != null)
            {
                if (diffData.additionalDifficultyData._requirements.Any())
                {
                    foreach (string req in diffData.additionalDifficultyData._requirements)
                    {
                        customListTableData.data.Add(!Collections.capabilities.Contains(req)
                            ? new CustomCellInfo($"<size=75%>{req}", "Missing Requirement", MissingReqIcon)
                            : new CustomCellInfo($"<size=75%>{req}", "Requirement", HaveReqIcon));
                    }
                }
            }

            //Contributors
            if (songData.contributors.Length > 0)
            {
                foreach (Data.ExtraSongData.Contributor author in songData.contributors)
                {
                    if (author.icon == null)
                    {
                        if (!string.IsNullOrWhiteSpace(author._iconPath))
                        {
                            author.icon = Utils.LoadSpriteFromFile(Path.Combine(level.customLevelPath, author._iconPath));
                            customListTableData.data.Add(new CustomCellInfo(author._name, author._role, author.icon != null ? author.icon : InfoIcon));
                        }
                        else
                        {
                            customListTableData.data.Add(new CustomCellInfo(author._name, author._role, InfoIcon));
                        }
                    }
                    else
                    {
                        customListTableData.data.Add(new CustomCellInfo(author._name, author._role, author.icon));
                    }
                }
            }

            //WIP Check
            if (wipFolder)
            {
                customListTableData.data.Add(new CustomCellInfo("<size=70%>WIP Song. Please Play in Practice Mode", "Warning", WarningIcon));
            }

            //Additional Diff Info
            if (diffData != null)
            {
                if (Utils.DiffHasColors(diffData))
                {
                    customListTableData.data.Add(new CustomCellInfo($"<size=75%>Custom Colors Available", $"Click here to preview & enable or disable it.", ColorsIcon));
                }

                if (diffData.additionalDifficultyData._warnings.Length > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._warnings)
                    {
                        customListTableData.data.Add(new CustomCellInfo($"<size=75%>{req}", "Warning", WarningIcon));
                    }
                }

                if (diffData.additionalDifficultyData._information.Length > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._information)
                    {
                        customListTableData.data.Add(new CustomCellInfo($"<size=75%>{req}", "Info", InfoIcon));
                    }
                }

                if (diffData.additionalDifficultyData._suggestions.Length > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._suggestions)
                    {
                        customListTableData.data.Add(!Collections.capabilities.Contains(req)
                            ? new CustomCellInfo($"<size=75%>{req}", "Missing Suggestion", MissingSuggestionIcon)
                            : new CustomCellInfo($"<size=75%>{req}", "Suggestion", HaveSuggestionIcon));
                    }
                }
            }

            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
        }

        [UIAction("list-select")]
        private void Select(TableView _, int index)
        {
            customListTableData.tableView.ClearSelection();
            if (diffData != null && customListTableData.data[index].icon == ColorsIcon)
            {
                modal.Hide(false, () => ColorsUI.instance.ShowColors(diffData));
            }
        }

        internal void SetRainbowColors(bool shouldSet, bool firstPulse = true)
        {
            tweenyManager.KillAllTweens(buttonBG);
            if (shouldSet)
            {
                FloatTween tween = new FloatTween(firstPulse ? 0 : 1, firstPulse ? 1 : 0, val =>
                {
                    buttonBG.color0 = new Color(1 - val, val, 0);
                    buttonBG.color1 = new Color(0, 1 - val, val);
                    buttonBG.SetAllDirty();
                }, 5f, EaseType.InOutSine);
                tweenyManager.AddTween(tween, buttonBG);
                tween.onCompleted = delegate ()
                { SetRainbowColors(true, !firstPulse); };
            }
            else
            {
                buttonBG.color0 = originalColor0;
                buttonBG.color1 = originalColor1;
            }
        }
    }
}