using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BS_Utils.Utilities;
using SongCore.Utilities;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;

namespace SongCore.UI
{
    public class RequirementsUI : NotifiableSingleton<RequirementsUI>
    {
        private StandardLevelDetailViewController standardLevel;


        internal static BS_Utils.Utilities.Config ModPrefs = new BS_Utils.Utilities.Config("SongCore/SongCore");

        internal Sprite HaveReqIcon;
        internal Sprite MissingReqIcon;
        internal Sprite HaveSuggestionIcon;
        internal Sprite MissingSuggestionIcon;
        internal Sprite WarningIcon;
        internal Sprite InfoIcon;

        //Currently selected song data
        public CustomPreviewBeatmapLevel level;
        public Data.ExtraSongData songData;
        public Data.ExtraSongData.DifficultyData diffData;
        public bool wipFolder;

        [UIComponent("list")]
        public CustomListTableData customListTableData;

        private string buttonGlowColor = "none";
        [UIValue("button-glow")]
        public string ButtonGlowColor
        {
            get => buttonGlowColor;
            set
            {
                buttonGlowColor = value;
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

        [UIComponent("info-button")]
        private Transform infoButtonTransform;

        internal void Setup()
        {
            GetIcons();
            standardLevel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SongCore.UI.requirements.bsml"), standardLevel.transform.Find("LevelDetail").gameObject, this);
            infoButtonTransform.localScale *= 0.7f;//no scale property in bsml as of now so manually scaling it
        }

        internal void GetIcons()
        {
            if (!MissingReqIcon)
                MissingReqIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.RedX.png");
            if (!HaveReqIcon)
                HaveReqIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.GreenCheck.png");
            if (!HaveSuggestionIcon)
                HaveSuggestionIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.YellowCheck.png");
            if (!MissingSuggestionIcon)
                MissingSuggestionIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.YellowX.png");
            if (!WarningIcon)
                WarningIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.Warning.png");
            if (!InfoIcon)
                InfoIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.Info.png");
        }

        [UIAction("button-click")]
        internal void ShowRequirements()
        {
            //   suggestionsList.text = "";

            customListTableData.data.Clear();
            //Requirements
            if (diffData != null)
            {
                if (diffData.additionalDifficultyData._requirements.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._requirements)
                    {
                        //    Console.WriteLine(req);
                        if (!Collections.capabilities.Contains(req))
                            customListTableData.data.Add(new CustomCellInfo("<size=75%>" + req, "Missing Requirement", MissingReqIcon));
                        else
                            customListTableData.data.Add(new CustomCellInfo("<size=75%>" + req, "Requirement", HaveReqIcon));
                    }
                }
            }
            //Contributors
            if (songData.contributors.Count() > 0)
            {
                foreach (Data.ExtraSongData.Contributor author in songData.contributors)
                {
                    if (author.icon == null)
                        if (!string.IsNullOrWhiteSpace(author._iconPath))
                        {
                            author.icon = Utilities.Utils.LoadSpriteFromFile(level.customLevelPath + "/" + author._iconPath);
                            customListTableData.data.Add(new CustomCellInfo(author._name, author._role, author.icon ?? InfoIcon));
                        }
                        else
                            customListTableData.data.Add(new CustomCellInfo(author._name, author._role, InfoIcon));
                    else
                        customListTableData.data.Add(new CustomCellInfo(author._name, author._role, author.icon));
                }
            }
            //WIP Check
            if (wipFolder)
                customListTableData.data.Add(new CustomCellInfo("<size=70%>" + "WIP Song. Please Play in Practice Mode", "Warning", WarningIcon));
            //Additional Diff Info
            if (diffData != null)
            {
                if (diffData.additionalDifficultyData._warnings.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._warnings)
                    {

                        //    Console.WriteLine(req);

                        customListTableData.data.Add(new CustomCellInfo("<size=75%>" + req, "Warning", WarningIcon));
                    }
                }
                if (diffData.additionalDifficultyData._information.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._information)
                    {

                        //    Console.WriteLine(req);

                        customListTableData.data.Add(new CustomCellInfo("<size=75%>" + req, "Info", InfoIcon));
                    }
                }
                if (diffData.additionalDifficultyData._suggestions.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData._suggestions)
                    {

                        //    Console.WriteLine(req);
                        if (!Collections.capabilities.Contains(req))
                            customListTableData.data.Add(new CustomCellInfo("<size=75%>" + req, "Missing Suggestion", MissingSuggestionIcon));
                        else
                            customListTableData.data.Add(new CustomCellInfo("<size=75%>" + req, "Suggestion", HaveSuggestionIcon));
                    }
                }
            }
            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, HMUI.TableViewScroller.ScrollPositionType.Beginning, false);

        }
    }
}
