﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SiraUtil.Affinity;
using SongCore.Data;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.Patches
{
    internal class SongDataMenuPatches : IAffinity, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly BeatmapCharacteristicSegmentedControlController _beatmapCharacteristicSegmentedControlController;
        private readonly RequirementsUI _requirementsUI;
        private readonly PluginConfig _config;
        private readonly Dictionary<string, Dictionary<BeatmapDifficulty, string>> _characteristicDifficultyLabels = new();
        private readonly Dictionary<string, Sprite> _characteristicDetailsSprites = new();

        private BeatmapLevel? _beatmapLevel;
        private SongData? _songData;
        private bool _actionButtonInteractable;
        private bool _practiceButtonInteractable;

        private SongDataMenuPatches(StandardLevelDetailViewController standardLevelDetailViewController, CustomLevelLoader customLevelLoader, RequirementsUI requirementsUI, PluginConfig config)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _customLevelLoader = customLevelLoader;
            _beatmapCharacteristicSegmentedControlController = standardLevelDetailViewController._standardLevelDetailView._beatmapCharacteristicSegmentedControlController;
            _requirementsUI = requirementsUI;
            _config = config;
        }

        public void Dispose()
        {
            foreach (var sprite in _characteristicDetailsSprites.Values)
            {
                SpriteAsyncLoader.DestroySprite(sprite);
            }
        }

        [AffinityPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.ShowOwnedContent))]
        [AffinityPrefix]
        private void GetSongData()
        {
            _beatmapLevel = _standardLevelDetailViewController.beatmapLevel;

            if (_beatmapLevel.hasPrecalculatedData)
            {
                return;
            }

            _songData = Collections.GetCustomLevelSongData(_beatmapLevel.levelID)!;
        }

        [AffinityPatch(typeof(BeatmapCharacteristicSegmentedControlController), nameof(BeatmapCharacteristicSegmentedControlController.SetData))]
        private void GetDifficultyLabels()
        {
            if (_beatmapLevel!.hasPrecalculatedData)
            {
                return;
            }

            foreach (var difficultyData in _songData!._difficulties)
            {
                _characteristicDifficultyLabels.TryAdd(difficultyData._beatmapCharacteristicName, new Dictionary<BeatmapDifficulty, string>());
                if (!string.IsNullOrWhiteSpace(difficultyData._difficultyLabel))
                {
                    _characteristicDifficultyLabels[difficultyData._beatmapCharacteristicName].TryAdd(difficultyData._difficulty, difficultyData._difficultyLabel);
                }
            }
        }

        // TODO: Find a way to add a limitation to the size of the text.
        [AffinityPatch(typeof(BeatmapDifficultyMethods), nameof(BeatmapDifficultyMethods.Name))]
        private void SetDifficultyLabel(ref string __result, BeatmapDifficulty difficulty)
        {
            if (_beatmapLevel == null || _beatmapLevel.hasPrecalculatedData || !_config.DisplayDiffLabels)
            {
                return;
            }

            if (_characteristicDifficultyLabels.TryGetValue(_beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName, out var difficultyLabels)
                && difficultyLabels.TryGetValue(difficulty, out var difficultyLabel))
            {
                __result = difficultyLabel.Replace("<", "<\u200B").Replace(">", ">\u200B");
            }
        }

        [AffinityPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
        private void ClearDifficultyLabels()
        {
            _characteristicDifficultyLabels.Clear();
        }

        [AffinityPatch(typeof(BeatmapCharacteristicSegmentedControlController), nameof(BeatmapCharacteristicSegmentedControlController.SetData))]
        private void SelectDefaultCharacteristic()
        {
            if (_beatmapLevel!.hasPrecalculatedData || _songData!._defaultCharacteristic == null || _beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName == _songData._defaultCharacteristic)
            {
                return;
            }

            var index = _beatmapCharacteristicSegmentedControlController._currentlyAvailableBeatmapCharacteristics.FindIndex(c => c.serializedName == _songData._defaultCharacteristic);
            if (index != -1)
            {
                _beatmapCharacteristicSegmentedControlController._segmentedControl.SelectCellWithNumber(index);
                _beatmapCharacteristicSegmentedControlController._selectedBeatmapCharacteristic = _beatmapCharacteristicSegmentedControlController._currentlyAvailableBeatmapCharacteristics[index];
            }
        }

        [AffinityPatch(typeof(BeatmapCharacteristicSegmentedControlController), nameof(BeatmapCharacteristicSegmentedControlController.SetData))]
        private void SetCosmeticCharacteristic(BeatmapCharacteristicSO selectedBeatmapCharacteristic)
        {
            if (_beatmapLevel!.hasPrecalculatedData || !_config.DisplayCustomCharacteristics || _songData!._characteristicDetails == null)
            {
                return;
            }

            var hasCosmeticCharacteristic = false;

            foreach (var characteristicDetails in _songData._characteristicDetails)
            {
                var index = _beatmapCharacteristicSegmentedControlController._currentlyAvailableBeatmapCharacteristics.FindIndex(c => c.serializedName == characteristicDetails._beatmapCharacteristicName);

                if (index == -1)
                {
                    continue;
                }

                hasCosmeticCharacteristic = true;

                var dataItem = _beatmapCharacteristicSegmentedControlController._segmentedControl._dataItems[index];

                if (!string.IsNullOrWhiteSpace(characteristicDetails._characteristicLabel))
                {
                    dataItem.hintText = characteristicDetails._characteristicLabel;
                }

                if (!string.IsNullOrWhiteSpace(characteristicDetails._characteristicIconFilePath))
                {
                    var spritePath = Path.Combine(_customLevelLoader._loadedBeatmapSaveData[_beatmapLevel.levelID].customLevelFolderInfo.folderPath, characteristicDetails._characteristicIconFilePath);

                    if (!_characteristicDetailsSprites.TryGetValue(spritePath, out var icon))
                    {
                        if ((icon = Utils.LoadSpriteFromFile(spritePath)) == null)
                        {
                            continue;
                        }

                        _characteristicDetailsSprites.Add(spritePath, icon);
                    }

                    dataItem.icon = icon;
                }
            }

            if (hasCosmeticCharacteristic)
            {
                var selectedCellNumber = _beatmapCharacteristicSegmentedControlController._segmentedControl.selectedCellNumber;

                _beatmapCharacteristicSegmentedControlController._segmentedControl.SetData(_beatmapCharacteristicSegmentedControlController._segmentedControl._dataItems);

                if (selectedCellNumber != 0)
                {
                    _beatmapCharacteristicSegmentedControlController._segmentedControl.SelectCellWithNumber(selectedCellNumber);
                }
            }
        }

        [AffinityPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
        private void HandleBeatmapRequirements(StandardLevelDetailView __instance)
        {
            var beatmapKey = __instance.beatmapKey;
            var actionButton = __instance.actionButton;
            var practiceButton = __instance.practiceButton;

            actionButton.interactable = true;
            practiceButton.interactable = true;

            _requirementsUI.ButtonGlowColor = false;
            _requirementsUI.ButtonInteractable = false;

            if (_beatmapLevel!.hasPrecalculatedData)
            {
                return;
            }

            var wipFolderSong = false;
            var difficultyData = Collections.GetCustomLevelSongDifficultyData(beatmapKey);
            if (difficultyData != null)
            {
                //If no additional information is present
                if (!difficultyData.additionalDifficultyData._requirements.Any() &&
                    !difficultyData.additionalDifficultyData._suggestions.Any() &&
                    !difficultyData.additionalDifficultyData._warnings.Any() &&
                    !difficultyData.additionalDifficultyData._information.Any() &&
                    !_songData!.contributors.Any() && !Utils.DiffHasColors(difficultyData))
                {
                    _requirementsUI.ButtonGlowColor = false;
                    _requirementsUI.ButtonInteractable = false;
                }
                else if (!difficultyData.additionalDifficultyData._warnings.Any())
                {
                    _requirementsUI.ButtonGlowColor = true;
                    _requirementsUI.ButtonInteractable = true;
                    _requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
                else if (difficultyData.additionalDifficultyData._warnings.Any())
                {
                    _requirementsUI.ButtonGlowColor = true;
                    _requirementsUI.ButtonInteractable = true;
                    if (difficultyData.additionalDifficultyData._warnings.Contains("WIP"))
                    {
                        actionButton.interactable = false;
                    }

                    _requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
            }

            if (_beatmapLevel.levelID.EndsWith(" WIP", StringComparison.Ordinal))
            {
                _requirementsUI.ButtonGlowColor = true;
                _requirementsUI.ButtonInteractable = true;
                actionButton.interactable = false;
                wipFolderSong = true;

                if (difficultyData != null)
                {
                    _requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
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
                        _requirementsUI.ButtonGlowColor = true;
                        _requirementsUI.ButtonInteractable = true;
                    }
                }
            }

            if (beatmapKey.beatmapCharacteristic.serializedName == "MissingCharacteristic")
            {
                actionButton.interactable = false;
                practiceButton.interactable = false;
                _requirementsUI.ButtonGlowColor = true;
                _requirementsUI.ButtonInteractable = true;
            }

            _requirementsUI.beatmapLevel = _beatmapLevel;
            _requirementsUI.beatmapKey = beatmapKey;
            _requirementsUI.songData = _songData;
            _requirementsUI.diffData = difficultyData;
            _requirementsUI.wipFolder = wipFolderSong;
        }

        [AffinityPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists))]
        [AffinityPrefix]
        private void SaveButtonsState(StandardLevelDetailView __instance)
        {
            _actionButtonInteractable = __instance.actionButton.interactable;
            _practiceButtonInteractable = __instance.practiceButton.interactable;
        }

        [AffinityPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists))]
        private void RestoreButtonsState(StandardLevelDetailView __instance)
        {
            __instance.actionButton.interactable = _actionButtonInteractable;
            __instance.practiceButton.interactable = _practiceButtonInteractable;
        }

        [AffinityPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo))]
        private void SetSoloOverrideColorScheme(StandardLevelScenesTransitionSetupDataSO __instance)
        {
            if (_config is { CustomSongNoteColors: false, CustomSongEnvironmentColors: false, CustomSongObstacleColors: false })
            {
                return;
            }

            var songData = Collections.GetCustomLevelSongDifficultyData(__instance.beatmapKey);
            var overrideColorScheme = GetOverrideColorScheme(songData, __instance.colorScheme);
            if (overrideColorScheme is null)
            {
                return;
            }

            __instance.usingOverrideColorScheme = true;
            __instance.colorScheme = overrideColorScheme;
        }

        [AffinityPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.InitColorInfo))]
        private void SetMultiplayerOverrideColorScheme(MultiplayerLevelScenesTransitionSetupDataSO __instance)
        {
            if (_config is { CustomSongNoteColors: false, CustomSongEnvironmentColors: false, CustomSongObstacleColors: false })
            {
                return;
            }

            var songData = Collections.GetCustomLevelSongDifficultyData(__instance.beatmapKey);
            var overrideColorScheme = GetOverrideColorScheme(songData, __instance.colorScheme);
            if (overrideColorScheme is null)
            {
                return;
            }

            __instance.usingOverrideColorScheme = true;
            __instance.colorScheme = overrideColorScheme;
        }

        private ColorScheme? GetOverrideColorScheme(SongData.DifficultyData? songDifficultyData, ColorScheme currentColorScheme)
        {
            if (songDifficultyData is null || (songDifficultyData._colorLeft == null && songDifficultyData._colorRight == null && songDifficultyData._envColorLeft == null && songDifficultyData._envColorRight == null &&
                                               songDifficultyData._envColorWhite == null && songDifficultyData._obstacleColor == null && songDifficultyData._envColorLeftBoost == null && songDifficultyData._envColorRightBoost == null &&
                                               songDifficultyData._envColorWhiteBoost == null))
            {
                return null;
            }

            if (_config.CustomSongNoteColors)
            {
                Plugin.Log.Debug("Custom song note colors On");
            }

            if (_config.CustomSongEnvironmentColors)
            {
                Plugin.Log.Debug("Custom song environment colors On");
            }

            if (_config.CustomSongObstacleColors)
            {
                Plugin.Log.Debug("Custom song obstacle colors On");
            }

            var saberLeft = songDifficultyData._colorLeft == null || !_config.CustomSongNoteColors
                ? currentColorScheme.saberAColor
                : Utils.ColorFromMapColor(songDifficultyData._colorLeft);
            var saberRight = songDifficultyData._colorRight == null || !_config.CustomSongNoteColors
                ? currentColorScheme.saberBColor
                : Utils.ColorFromMapColor(songDifficultyData._colorRight);
            var envLeft = songDifficultyData._envColorLeft == null || !_config.CustomSongEnvironmentColors
                ? songDifficultyData._colorLeft == null ? currentColorScheme.environmentColor0 : Utils.ColorFromMapColor(songDifficultyData._colorLeft)
                : Utils.ColorFromMapColor(songDifficultyData._envColorLeft);
            var envRight = songDifficultyData._envColorRight == null || !_config.CustomSongEnvironmentColors
                ? songDifficultyData._colorRight == null ? currentColorScheme.environmentColor1 : Utils.ColorFromMapColor(songDifficultyData._colorRight)
                : Utils.ColorFromMapColor(songDifficultyData._envColorRight);
            var envWhite = songDifficultyData._envColorWhite == null || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColorW
                : Utils.ColorFromMapColor(songDifficultyData._envColorWhite);
            var envLeftBoost = songDifficultyData._envColorLeftBoost == null || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColor0Boost
                : Utils.ColorFromMapColor(songDifficultyData._envColorLeftBoost);
            var envRightBoost = songDifficultyData._envColorRightBoost == null|| !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColor1Boost
                : Utils.ColorFromMapColor(songDifficultyData._envColorRightBoost);
            var envWhiteBoost = songDifficultyData._envColorWhiteBoost == null  || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColorWBoost
                : Utils.ColorFromMapColor(songDifficultyData._envColorWhiteBoost);
            var obstacle = songDifficultyData._obstacleColor == null || !_config.CustomSongObstacleColors
                ? currentColorScheme.obstaclesColor
                : Utils.ColorFromMapColor(songDifficultyData._obstacleColor);

            return new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, true, saberLeft, saberRight, true,
                envLeft, envRight, envWhite, envLeftBoost != default && envRightBoost != default, envLeftBoost, envRightBoost, envWhiteBoost, obstacle);
        }
    }
}
