using System;
using HarmonyLib;
using SongCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    [HarmonyPatch(nameof(StandardLevelDetailView.RefreshContent), MethodType.Normal)]
    internal class StandardLevelDetailViewRefreshContent
    {
        private static readonly Dictionary<string, OverrideLabels> LevelLabels = new Dictionary<string, OverrideLabels>();

        public static readonly OverrideLabels currentLabels = new OverrideLabels();

        private static IPreviewBeatmapLevel lastLevel;

        public class OverrideLabels
        {
            internal string? EasyOverride;
            internal string? NormalOverride;
            internal string? HardOverride;
            internal string? ExpertOverride;
            internal string? ExpertPlusOverride;
        }

        internal static void SetCurrentLabels(OverrideLabels labels)
        {
            currentLabels.EasyOverride = labels.EasyOverride;
            currentLabels.NormalOverride = labels.NormalOverride;
            currentLabels.HardOverride = labels.HardOverride;
            currentLabels.ExpertOverride = labels.ExpertOverride;
            currentLabels.ExpertPlusOverride = labels.ExpertPlusOverride;
        }

        internal static void ClearOverrideLabels()
        {
            currentLabels.EasyOverride = null;
            currentLabels.NormalOverride = null;
            currentLabels.HardOverride = null;
            currentLabels.ExpertOverride = null;
            currentLabels.ExpertPlusOverride = null;
        }

        private static void Postfix(StandardLevelDetailView __instance)
        {
            var firstSelection = false;
            var selectedDifficultyBeatmap = __instance.selectedDifficultyBeatmap;
            var actionButton = __instance.actionButton;
            var practiceButton = __instance.practiceButton;
            var level = selectedDifficultyBeatmap.level is CustomBeatmapLevel ? selectedDifficultyBeatmap.level as CustomPreviewBeatmapLevel : null;

            if (level != lastLevel)
            {
                firstSelection = true;
                lastLevel = level;
            }

            actionButton.interactable = true;
            practiceButton.interactable = true;

            RequirementsUI.instance.ButtonGlowColor = false;
            RequirementsUI.instance.ButtonInteractable = false;
            if (level == null)
            {
                return;
            }

            var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(level));

            if (songData == null)
            {
                RequirementsUI.instance.ButtonGlowColor = false;
                RequirementsUI.instance.ButtonInteractable = false;
                return;
            }

            var wipFolderSong = false;
            var diffData = Collections.RetrieveDifficultyData(selectedDifficultyBeatmap);

            if (diffData != null)
            {
                //If no additional information is present
                if (!diffData.additionalDifficultyData._requirements.Any() &&
                    !diffData.additionalDifficultyData._suggestions.Any() &&
                    !diffData.additionalDifficultyData._warnings.Any() &&
                    !diffData.additionalDifficultyData._information.Any() &&
                    !songData.contributors.Any() && !Utilities.Utils.DiffHasColors(diffData))
                {
                    RequirementsUI.instance.ButtonGlowColor = false;
                    RequirementsUI.instance.ButtonInteractable = false;
                }
                else if (!diffData.additionalDifficultyData._warnings.Any())
                {
                    RequirementsUI.instance.ButtonGlowColor = true;
                    RequirementsUI.instance.ButtonInteractable = true;
                    RequirementsUI.instance.SetRainbowColors(Utilities.Utils.DiffHasColors(diffData));
                }
                else if (diffData.additionalDifficultyData._warnings.Any())
                {
                    RequirementsUI.instance.ButtonGlowColor = true;
                    RequirementsUI.instance.ButtonInteractable = true;
                    if (diffData.additionalDifficultyData._warnings.Contains("WIP"))
                    {
                        actionButton.interactable = false;
                    }
                    RequirementsUI.instance.SetRainbowColors(Utilities.Utils.DiffHasColors(diffData));
                }
            }

            if (level.levelID.EndsWith(" WIP", StringComparison.Ordinal))
            {
                RequirementsUI.instance.ButtonGlowColor = true;
                RequirementsUI.instance.ButtonInteractable = true;
                actionButton.interactable = false;
                wipFolderSong = true;
            }

            if (diffData != null)
            {
                foreach (var requirement in diffData.additionalDifficultyData._requirements)
                {
                    if (!Collections.capabilities.Contains(requirement))
                    {
                        actionButton.interactable = false;
                        practiceButton.interactable = false;
                        RequirementsUI.instance.ButtonGlowColor = true;
                        RequirementsUI.instance.ButtonInteractable = true;
                    }
                }
            }

            if (selectedDifficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName == "MissingCharacteristic")
            {
                actionButton.interactable = false;
                practiceButton.interactable = false;
                RequirementsUI.instance.ButtonGlowColor = true;
                RequirementsUI.instance.ButtonInteractable = true;
            }

            RequirementsUI.instance.level = level;
            RequirementsUI.instance.difficultyBeatmap = selectedDifficultyBeatmap;
            RequirementsUI.instance.songData = songData;
            RequirementsUI.instance.diffData = diffData;
            RequirementsUI.instance.wipFolder = wipFolderSong;


            //Difficulty Label Handling
            LevelLabels.Clear();
            string currentCharacteristic = string.Empty;
            foreach (Data.ExtraSongData.DifficultyData diffLevel in songData._difficulties)
            {
                var difficulty = diffLevel._difficulty;
                string characteristic = diffLevel._beatmapCharacteristicName;
                if (characteristic == selectedDifficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName)
                {
                    currentCharacteristic = characteristic;
                }

                if (!LevelLabels.ContainsKey(characteristic))
                {
                    LevelLabels.Add(characteristic, new OverrideLabels());
                }

                var charLabels = LevelLabels[characteristic];
                if (!string.IsNullOrWhiteSpace(diffLevel._difficultyLabel))
                {
                    switch (difficulty)
                    {
                        case BeatmapDifficulty.Easy:
                            charLabels.EasyOverride = diffLevel._difficultyLabel;
                            break;
                        case BeatmapDifficulty.Normal:
                            charLabels.NormalOverride = diffLevel._difficultyLabel;
                            break;
                        case BeatmapDifficulty.Hard:
                            charLabels.HardOverride = diffLevel._difficultyLabel;
                            break;
                        case BeatmapDifficulty.Expert:
                            charLabels.ExpertOverride = diffLevel._difficultyLabel;
                            break;
                        case BeatmapDifficulty.ExpertPlus:
                            charLabels.ExpertPlusOverride = diffLevel._difficultyLabel;
                            break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentCharacteristic))
            {
                SetCurrentLabels(LevelLabels[currentCharacteristic]);
            }
            else
            {
                ClearOverrideLabels();
            }

            __instance._beatmapDifficultySegmentedControlController.SetData(selectedDifficultyBeatmap.parentDifficultyBeatmapSet.difficultyBeatmaps,
                __instance._beatmapDifficultySegmentedControlController.selectedDifficulty);
            ClearOverrideLabels();

            // TODO: Check if this whole if block is still needed
            if (songData._defaultCharacteristic != null && firstSelection)
            {
                if (__instance._beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName != songData._defaultCharacteristic)
                {
                    var chars = __instance._beatmapCharacteristicSegmentedControlController._beatmapCharacteristics;
                    var index = 0;
                    foreach (var characteristic in chars)
                    {
                        if (songData._defaultCharacteristic == characteristic.serializedName)
                        {
                            break;
                        }

                        index++;
                    }

                    if (index != chars.Count)
                    {
                        __instance._beatmapCharacteristicSegmentedControlController._segmentedControl
                            .SelectCellWithNumber(index);
                        __instance._beatmapCharacteristicSegmentedControlController.HandleDifficultySegmentedControlDidSelectCell(
                            __instance._beatmapCharacteristicSegmentedControlController._segmentedControl, index);
                    }
                }
            }
        }
    }
}