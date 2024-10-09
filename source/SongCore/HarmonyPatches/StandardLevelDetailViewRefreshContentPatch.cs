using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SongCore.UI;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    internal class StandardLevelDetailViewRefreshContentPatch
    {
        private static readonly Dictionary<string, OverrideLabels> LevelLabels = new Dictionary<string, OverrideLabels>();

        public static readonly OverrideLabels currentLabels = new OverrideLabels();

        private static BeatmapLevel lastLevel;

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
            var beatmapLevel = __instance._beatmapLevel;
            var beatmapKey = __instance.beatmapKey;
            var actionButton = __instance.actionButton;
            var practiceButton = __instance.practiceButton;
            var requirementsUI = RequirementsUI.instance;

            actionButton.interactable = true;
            practiceButton.interactable = true;

            requirementsUI.ButtonGlowColor = false;
            requirementsUI.ButtonInteractable = false;

            if (beatmapLevel != lastLevel)
            {
                firstSelection = true;
                lastLevel = beatmapLevel;
            }

            if (beatmapLevel.hasPrecalculatedData)
            {
                return;
            }

            __instance.ShowContent(StandardLevelDetailViewController.ContentType.OwnedAndReady, 0f);

            var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(beatmapLevel));
            if (songData == null)
            {
                requirementsUI.ButtonGlowColor = false;
                requirementsUI.ButtonInteractable = false;

                return;
            }

            var wipFolderSong = false;
            var difficultyData = Collections.RetrieveDifficultyData(beatmapLevel, beatmapKey);
            if (difficultyData != null)
            {
                //If no additional information is present
                if (!difficultyData.additionalDifficultyData._requirements.Any() &&
                    !difficultyData.additionalDifficultyData._suggestions.Any() &&
                    !difficultyData.additionalDifficultyData._warnings.Any() &&
                    !difficultyData.additionalDifficultyData._information.Any() &&
                    !songData.contributors.Any() && !Utils.DiffHasColors(difficultyData))
                {
                    requirementsUI.ButtonGlowColor = false;
                    requirementsUI.ButtonInteractable = false;
                }
                else if (!difficultyData.additionalDifficultyData._warnings.Any())
                {
                    requirementsUI.ButtonGlowColor = true;
                    requirementsUI.ButtonInteractable = true;
                    requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
                else if (difficultyData.additionalDifficultyData._warnings.Any())
                {
                    requirementsUI.ButtonGlowColor = true;
                    requirementsUI.ButtonInteractable = true;
                    if (difficultyData.additionalDifficultyData._warnings.Contains("WIP"))
                    {
                        actionButton.interactable = false;
                    }

                    requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
            }

            if (beatmapLevel.levelID.EndsWith(" WIP", StringComparison.Ordinal))
            {
                requirementsUI.ButtonGlowColor = true;
                requirementsUI.ButtonInteractable = true;
                actionButton.interactable = false;
                wipFolderSong = true;

                if (difficultyData != null)
                {
                    requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
            }

            if (difficultyData != null)
            {
                foreach (var requirement in difficultyData.additionalDifficultyData._requirements)
                {
                    if (!Collections.capabilities.Contains(requirement))
                    {
                        actionButton.interactable = false;
                        practiceButton.interactable = false;
                        requirementsUI.ButtonGlowColor = true;
                        requirementsUI.ButtonInteractable = true;
                    }
                }
            }

            if (beatmapKey.beatmapCharacteristic.serializedName == "MissingCharacteristic")
            {
                actionButton.interactable = false;
                practiceButton.interactable = false;
                requirementsUI.ButtonGlowColor = true;
                requirementsUI.ButtonInteractable = true;
            }

            requirementsUI.beatmapLevel = beatmapLevel;
            requirementsUI.beatmapKey = beatmapKey;
            requirementsUI.songData = songData;
            requirementsUI.diffData = difficultyData;
            requirementsUI.wipFolder = wipFolderSong;

            //Difficulty Label Handling
            LevelLabels.Clear();
            string currentCharacteristic = string.Empty;
            foreach (Data.ExtraSongData.DifficultyData diffLevel in songData._difficulties)
            {
                var difficulty = diffLevel._difficulty;
                string characteristic = diffLevel._beatmapCharacteristicName;
                if (characteristic == beatmapKey.beatmapCharacteristic.serializedName)
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

            __instance._beatmapDifficultySegmentedControlController.SetData(beatmapLevel.GetDifficulties(beatmapKey.beatmapCharacteristic),
                __instance._beatmapDifficultySegmentedControlController.selectedDifficulty, __instance._allowedBeatmapDifficultyMask);
            ClearOverrideLabels();

            // TODO: Check if this whole if block is still needed
            if (songData._defaultCharacteristic != null && firstSelection)
            {
                if (__instance._beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName != songData._defaultCharacteristic)
                {
                    var chars = __instance._beatmapCharacteristicSegmentedControlController._currentlyAvailableBeatmapCharacteristics;
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
                        __instance._beatmapCharacteristicSegmentedControlController.HandleBeatmapCharacteristicSegmentedControlDidSelectCell(
                            __instance._beatmapCharacteristicSegmentedControlController._segmentedControl, index);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists))]
    internal class StandardLevelDetailViewCheckIfBeatmapLevelDataExistsPatch
    {
        private static void Prefix(StandardLevelDetailView __instance, out (bool, bool) __state)
        {
            __state = (__instance._actionButton.interactable, __instance._practiceButton.interactable);
        }

        private static void Postfix(StandardLevelDetailView __instance, (bool, bool) __state)
        {
            __instance._actionButton.interactable = __state.Item1;
            __instance._practiceButton.interactable = __state.Item2;

            // This fixes base game trying to load non-existing difficulties.
            // TODO: Remove when fixed.
            if (Loader.LoadedBeatmapLevelsData.TryGetValue(__instance._beatmapLevel.levelID, out var beatmapLevelData) && !File.Exists(((FileSystemBeatmapLevelData)beatmapLevelData).GetDifficultyBeatmap(__instance.beatmapKey)!._beatmapPath))
            {
                __instance.ShowContent(StandardLevelDetailViewController.ContentType.Error, 0f);
            }
        }
    }
}
