using System;
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
using SiraUtil.Affinity;
using Tweening;
using Zenject;

namespace SongCore.UI
{
    public class RequirementsUI : NotifiableBase, IInitializable, IAffinity
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly TimeTweeningManager _tweeningManager;
        private readonly BSMLParser _bsmlParser;
        private readonly ColorsUI _colorsUI;

        private RequirementsUI(StandardLevelDetailViewController standardLevelDetailViewController, CustomLevelLoader customLevelLoader, TimeTweeningManager tweeningManager, BSMLParser bsmlParser, ColorsUI colorsUI)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _customLevelLoader = customLevelLoader;
            _tweeningManager = tweeningManager;
            _bsmlParser = bsmlParser;
            _colorsUI = colorsUI;
            instance = this;
        }

        public static RequirementsUI instance { get; set; }

        private const string BUTTON_BSML = "<bg id='root'><action-button id='info-button' text='?' active='~button-glow' interactable='~button-interactable' anchor-pos-x='31' anchor-pos-y='0' pref-width='12' pref-height='9' on-click='button-click'/></bg>";
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
        internal Sprite? OneSaberIcon;
        internal Sprite? EnvironmentIcon;
        internal Sprite? StandardIcon;

        //Currently selected song data
        public BeatmapLevel? beatmapLevel;
        public BeatmapKey? beatmapKey;
        public Data.ExtraSongData? songData;
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

        public void Initialize()
        {
            GetIcons();
            _bsmlParser.Parse(BUTTON_BSML, _standardLevelDetailViewController._standardLevelDetailView.gameObject, this);

            infoButtonTransform.localScale *= 0.7f; //no scale property in bsml as of now so manually scaling it
            ((RectTransform)_standardLevelDetailViewController._standardLevelDetailView._favoriteToggle.transform).anchoredPosition = new Vector2(3, -2);
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

            if (!EnvironmentIcon)
            {
                EnvironmentIcon = Utils.LoadSpriteFromResources("SongCore.Icons.Environment.png")!;
            }

            if (!OneSaberIcon)
            {
                OneSaberIcon = Utils.LoadSpriteFromResources("SongCore.Icons.OneSaber.png")!;
            }

            if (!StandardIcon)
            {
                StandardIcon = Utils.LoadSpriteFromResources("SongCore.Icons.Standard.png")!;
            }
        }

        [UIAction("button-click")]
        internal void ShowRequirements()
        {
            if (modal == null)
            {
                _bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.requirements.bsml"), _root.gameObject, this);
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
                            author.icon = Utils.LoadSpriteFromFile(Path.Combine(_customLevelLoader._loadedBeatmapSaveData[beatmapLevel.levelID].customLevelFolderInfo.folderPath, author._iconPath));
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
                string? environmentName = null;

                if (diffData._environmentNameIdx != null)
                {
                    var environmentInfoName = songData._environmentNames.ElementAtOrDefault(diffData._environmentNameIdx.Value);
                    if (environmentInfoName != null)
                    {
                        if (environmentInfoName != beatmapLevel.GetEnvironmentName(beatmapKey.Value.beatmapCharacteristic, beatmapKey.Value.difficulty))
                        {
                            environmentName = Loader.CustomLevelLoader._environmentsListModel.GetEnvironmentInfoBySerializedNameSafe(environmentInfoName).environmentName;
                        }
                    }
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

                if (diffData._oneSaber != null)
                {
                    string enabledText = Plugin.Configuration.DisableOneSaberOverride ? "[<color=#ff5072>Disabled</color>]" : "[<color=#89ff89>Enabled</color>]";
                    string enabledSubtext = Plugin.Configuration.DisableOneSaberOverride ? "enable" : "disable";
                    string saberCountText = diffData._oneSaber.Value ? "Forced One Saber" : "Forced Standard";
                    customListTableData.data.Add(new CustomCellInfo($"<size=75%>{saberCountText} {enabledText}", $"Map changes saber count, click here to {enabledSubtext}.", diffData._oneSaber.Value ? OneSaberIcon : StandardIcon));
                }

                if (customListTableData.data.Count > 0)
                {
                    if (environmentName == null && beatmapLevel != null)
                        environmentName = beatmapLevel.GetEnvironmentName(beatmapKey.Value.beatmapCharacteristic, beatmapKey.Value.difficulty);
                    customListTableData.data.Add(new CustomCellInfo("<size=75%>Environment Info", $"This Map uses the Environment: {environmentName}", EnvironmentIcon));

                }
            }

            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
        }

        [UIAction("list-select")]
        private void Select(TableView _, int index)
        {
            customListTableData.tableView.ClearSelection();
            if (diffData != null)
            {
                var iconSelected = customListTableData.data[index].icon;
                if (iconSelected == ColorsIcon)
                {
                    modal.Hide(false, () => _colorsUI.ShowColors(diffData));
                }
                else if (iconSelected == StandardIcon || iconSelected == OneSaberIcon)
                {
                    Plugin.Configuration.DisableOneSaberOverride = !Plugin.Configuration.DisableOneSaberOverride;
                    modal.Hide(true);
                }
            }
        }

        internal void SetRainbowColors(bool shouldSet, bool firstPulse = true)
        {
            _tweeningManager.KillAllTweens(buttonBG);
            if (shouldSet)
            {
                FloatTween tween = new FloatTween(firstPulse ? 0 : 1, firstPulse ? 1 : 0, val =>
                {
                    buttonBG.color0 = new Color(1 - val, val, 0);
                    buttonBG.color1 = new Color(0, 1 - val, val);
                    buttonBG.SetAllDirty();
                }, 5f, EaseType.InOutSine);
                _tweeningManager.AddTween(tween, buttonBG);
                tween.onCompleted = delegate ()
                { SetRainbowColors(true, !firstPulse); };
            }
            else
            {
                buttonBG.color0 = originalColor0;
                buttonBG.color1 = originalColor1;
            }
        }

        [AffinityPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists))]
        private void EnableOrDisableSongButtons()
        {
            ButtonGlowColor = false;
            ButtonInteractable = false;
            if (_standardLevelDetailViewController._beatmapLevel.hasPrecalculatedData)
            {
                return;
            }

            var extraSongData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(_standardLevelDetailViewController._beatmapLevel));
            if (extraSongData == null)
            {
                ButtonGlowColor = false;
                ButtonInteractable = false;
                return;
            }

            var wipFolderSong = false;
            var actionButton = _standardLevelDetailViewController._standardLevelDetailView.actionButton;
            var practiceButton = _standardLevelDetailViewController._standardLevelDetailView.practiceButton;
            var difficultyData = Collections.RetrieveDifficultyData(_standardLevelDetailViewController._beatmapLevel, _standardLevelDetailViewController.beatmapKey);
            if (difficultyData != null)
            {
                //If no additional information is present
                if (!difficultyData.additionalDifficultyData._requirements.Any() &&
                    !difficultyData.additionalDifficultyData._suggestions.Any() &&
                    !difficultyData.additionalDifficultyData._warnings.Any() &&
                    !difficultyData.additionalDifficultyData._information.Any() &&
                    !extraSongData.contributors.Any() && !Utils.DiffHasColors(difficultyData))
                {
                    ButtonGlowColor = false;
                    ButtonInteractable = false;
                }
                else if (!difficultyData.additionalDifficultyData._warnings.Any())
                {
                    ButtonGlowColor = true;
                    ButtonInteractable = true;
                    SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
                else if (difficultyData.additionalDifficultyData._warnings.Any())
                {
                    ButtonGlowColor = true;
                    ButtonInteractable = true;
                    if (difficultyData.additionalDifficultyData._warnings.Contains("WIP"))
                    {
                        actionButton.interactable = false;
                    }
                    SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
            }

            if (_standardLevelDetailViewController._beatmapLevel.levelID.EndsWith(" WIP", StringComparison.Ordinal))
            {
                ButtonGlowColor = true;
                ButtonInteractable = true;
                actionButton.interactable = false;
                wipFolderSong = true;
            }

            if (difficultyData != null)
            {
                foreach (var requirement in difficultyData.additionalDifficultyData._requirements)
                {
                    if (!Collections.capabilities.Contains(requirement))
                    {
                        actionButton.interactable = false;
                        practiceButton.interactable = false;
                        ButtonGlowColor = true;
                        ButtonInteractable = true;
                    }
                }
            }

            if (_standardLevelDetailViewController.beatmapKey.beatmapCharacteristic.serializedName == "MissingCharacteristic")
            {
                actionButton.interactable = false;
                practiceButton.interactable = false;
                ButtonGlowColor = true;
                ButtonInteractable = true;
            }

            beatmapLevel = _standardLevelDetailViewController._beatmapLevel;
            beatmapKey = _standardLevelDetailViewController.beatmapKey;
            songData = extraSongData;
            diffData = difficultyData;
            wipFolder = wipFolderSong;
        }
    }
}