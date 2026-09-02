using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HMUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SongCore.Data;
using SongCore.UI;
using SongCore.Utilities;
using TMPro;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    internal class SongDataMenuHooks : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly RequirementsUI _requirementsUI;
        private readonly PluginConfig _config;
        private readonly Dictionary<string, Dictionary<BeatmapDifficulty, string>> _characteristicDifficultyLabels = new();
        private readonly Dictionary<string, Sprite> _characteristicDetailsSprites = new();

        private Hook _getSongDataHook = null!;
        private Hook _textSizeLimitHook = null!;
        private Hook _customDifficultyLabelsHook = null!;
        private Hook _customDifficultyTextHook = null!;
        private Hook _defaultCharacteristicHook = null!;
        private Hook _cosmeticCharacteristicHook = null!;
        private Hook _requirementsHook = null!;
        private Hook _buttonsStateHook = null!;
        private ILHook _overrideColorSchemeHook = null!;
        private ILHook _overrideMultiplayerColorSchemeHook = null!;
        private SongData? _songData;

        private SongDataMenuHooks(StandardLevelDetailViewController standardLevelDetailViewController, CustomLevelLoader customLevelLoader, RequirementsUI requirementsUI, PluginConfig config)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _customLevelLoader = customLevelLoader;
            _requirementsUI = requirementsUI;
            _config = config;
        }

        public void Initialize()
        {
            _getSongDataHook = new Hook(typeof(LevelCollectionViewController).GetMethod(nameof(LevelCollectionViewController.HandleLevelCollectionTableViewDidSelectLevel), BindingFlags.Instance | BindingFlags.NonPublic)!, HandleDidSelectLevel, true);
            _textSizeLimitHook = new Hook(typeof(BeatmapDifficultySegmentedControlController).GetMethod(nameof(BeatmapDifficultySegmentedControlController.SetData))!, LimitTextSize, true);
            _customDifficultyLabelsHook = new Hook(typeof(BeatmapDifficultySegmentedControlController).GetMethod(nameof(BeatmapDifficultySegmentedControlController.SetData))!, SetData, true);
            _customDifficultyTextHook = new Hook(typeof(LevelBar).GetMethod(nameof(LevelBar.SetupData), BindingFlags.Instance | BindingFlags.NonPublic)!, SetupData, true);
            _defaultCharacteristicHook = new Hook(typeof(BeatmapCharacteristicSegmentedControlController).GetMethod(nameof(BeatmapCharacteristicSegmentedControlController.SetData))!, SelectDefaultCharacteristic, true);
            _cosmeticCharacteristicHook = new Hook(typeof(BeatmapCharacteristicSegmentedControlController).GetMethod(nameof(BeatmapCharacteristicSegmentedControlController.SetData))!, SetCosmeticCharacteristic, true);
            _requirementsHook = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.RefreshContent))!, ProcessBeatmapRequirements, true);
            _buttonsStateHook = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists), BindingFlags.Instance | BindingFlags.NonPublic)!, SaveAndRestoreButtonsState, true);
            _overrideColorSchemeHook = new ILHook(typeof(StandardLevelScenesTransitionSetupDataSO).GetMethod(nameof(StandardLevelScenesTransitionSetupDataSO.Init), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, OverrideColorSchemeManipulator, true);
            _overrideMultiplayerColorSchemeHook = new ILHook(typeof(MultiplayerLevelScenesTransitionSetupDataSO).GetMethod(nameof(MultiplayerLevelScenesTransitionSetupDataSO.Init), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, OverrideColorSchemeManipulator, true);
        }

        public void Dispose()
        {
            _getSongDataHook.Dispose();
            _textSizeLimitHook.Dispose();
            _customDifficultyLabelsHook.Dispose();
            _customDifficultyTextHook.Dispose();
            _defaultCharacteristicHook.Dispose();
            _cosmeticCharacteristicHook.Dispose();
            _requirementsHook.Dispose();
            _buttonsStateHook.Dispose();
            _overrideColorSchemeHook.Dispose();
            _overrideMultiplayerColorSchemeHook.Dispose();

            foreach (var sprite in _characteristicDetailsSprites.Values)
            {
                SpriteAsyncLoader.DestroySprite(sprite);
            }
        }

        private void HandleDidSelectLevel(Action<LevelCollectionViewController, LevelCollectionTableView, BeatmapLevel> original, LevelCollectionViewController instance, LevelCollectionTableView tableView, BeatmapLevel level)
        {
            _songData = Collections.GetCustomLevelSongData(level.levelID);

            if (_songData == null)
            {
                original(instance, tableView, level);
                return;
            }

            _characteristicDifficultyLabels.Clear();
            foreach (var difficultyData in _songData._difficulties)
            {
                _characteristicDifficultyLabels.TryAdd(difficultyData._beatmapCharacteristicName, new Dictionary<BeatmapDifficulty, string>());
                if (!string.IsNullOrWhiteSpace(difficultyData._difficultyLabel))
                {
                    _characteristicDifficultyLabels[difficultyData._beatmapCharacteristicName].TryAdd(difficultyData._difficulty, difficultyData._difficultyLabel);
                }
            }

            original(instance, tableView, level);
        }

        // TODO: Add Hover Hints that shows full text when text is too large?
        private void LimitTextSize(Action<BeatmapDifficultySegmentedControlController, IEnumerable<BeatmapDifficulty>, BeatmapDifficulty, BeatmapDifficultyMask> original, BeatmapDifficultySegmentedControlController instance, IEnumerable<BeatmapDifficulty> difficultyBeatmaps, BeatmapDifficulty selectedDifficulty, BeatmapDifficultyMask allowedBeatmapDifficultyMask)
        {
            original(instance, difficultyBeatmaps, selectedDifficulty, allowedBeatmapDifficultyMask);

            if (_songData == null || !_config.DisplayDiffLabels)
            {
                return;
            }

            for (var i = 0; i < instance._difficultySegmentedControl._texts.Count; i++)
            {
                var cell = (TextSegmentedControlCell)instance._difficultySegmentedControl.cells[i];

                if (cell._text.alignment == TextAlignmentOptions.Midline)
                {
                    continue;
                }

                cell._text.enableAutoSizing = true;
                cell._text.fontSizeMin = 2.125f;
                cell._text.fontSizeMax = 3f;
                cell._text.alignment = TextAlignmentOptions.Midline;
                cell._text.m_textAlignment = TextAlignmentOptions.Converted; // needed to stop Awake() from resetting alignment
                cell._text.rectTransform.sizeDelta = new Vector2(0f, 1.2f);
                cell._text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void SetData(Action<BeatmapDifficultySegmentedControlController, IEnumerable<BeatmapDifficulty>, BeatmapDifficulty, BeatmapDifficultyMask> original, BeatmapDifficultySegmentedControlController instance, IEnumerable<BeatmapDifficulty> difficultyBeatmaps, BeatmapDifficulty selectedDifficulty, BeatmapDifficultyMask allowedBeatmapDifficultyMask)
        {
            original(instance, difficultyBeatmaps, selectedDifficulty, allowedBeatmapDifficultyMask);

            if (_songData == null || !_config.DisplayDiffLabels)
            {
                return;
            }

            var selectedBeatmapCharacteristic = _standardLevelDetailViewController._standardLevelDetailView._beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName;
            instance._difficultySegmentedControl._texts = instance._difficulties
                .Select((diff, i) => _characteristicDifficultyLabels.TryGetValue(selectedBeatmapCharacteristic, out var difficultyLabels) && difficultyLabels.TryGetValue(diff, out var difficultyLabel)
                    ? GetDifficultyLabel(difficultyLabel) ?? instance._difficultySegmentedControl._texts[i]
                    : instance._difficultySegmentedControl._texts[i])
                .ToArray();

            for (var i = 0; i < instance._difficultySegmentedControl._texts.Count; i++)
            {
                var cell = (TextSegmentedControlCell)instance._difficultySegmentedControl.cells[i];
                cell.text = instance._difficultySegmentedControl._texts[i];
            }
        }

        private Task SetupData(Func<LevelBar, BeatmapLevel, BeatmapDifficulty, BeatmapCharacteristicSO, Task> original, LevelBar instance, BeatmapLevel beatmapLevel, BeatmapDifficulty beatmapDifficulty, BeatmapCharacteristicSO beatmapCharacteristic)
        {
            var result = original(instance, beatmapLevel, beatmapDifficulty, beatmapCharacteristic);

            if (_songData == null || !_config.DisplayDiffLabels || !instance._showDifficultyAndCharacteristic)
            {
                return result;
            }

            if (_characteristicDifficultyLabels.TryGetValue(beatmapCharacteristic.serializedName, out var difficultyLabels) && difficultyLabels.TryGetValue(beatmapDifficulty, out var difficultyLabel))
            {
                instance._difficultyText.textWrappingMode = TextWrappingModes.NoWrap;
                instance._difficultyText.overflowMode = TextOverflowModes.Ellipsis;
                instance._difficultyText.text = GetDifficultyLabel(difficultyLabel) ?? instance._difficultyText.text;
            }

            var characteristicDetails = _songData._characteristicDetails?.FirstOrDefault(d => d._beatmapCharacteristicName == beatmapCharacteristic.serializedName);
            if (characteristicDetails != null)
            {
                var sprite = GetCharacteristicIcon(characteristicDetails._characteristicIconFilePath);
                if (sprite != null)
                {
                    instance._characteristicIconImageView.sprite = sprite;
                }
            }

            return result;
        }

        private string? GetDifficultyLabel(string difficultyLabel)
        {
            return string.IsNullOrWhiteSpace(difficultyLabel) ? null : difficultyLabel.Replace("<", "<\u200B").Replace(">", ">\u200B");
        }

        private void SelectDefaultCharacteristic(Action<BeatmapCharacteristicSegmentedControlController, IEnumerable<BeatmapCharacteristicSO>, BeatmapCharacteristicSO, HashSet<BeatmapCharacteristicSO>> original, BeatmapCharacteristicSegmentedControlController instance, IEnumerable<BeatmapCharacteristicSO> beatmapCharacteristics, BeatmapCharacteristicSO selectedBeatmapCharacteristic, HashSet<BeatmapCharacteristicSO> notAllowedCharacteristics)
        {
            original(instance, beatmapCharacteristics, selectedBeatmapCharacteristic, notAllowedCharacteristics);

            if (_songData == null || _songData._defaultCharacteristic == null || _songData._defaultCharacteristic == instance.selectedBeatmapCharacteristic.serializedName)
            {
                return;
            }

            var index = instance._currentlyAvailableBeatmapCharacteristics.FindIndex(c => c.serializedName == _songData._defaultCharacteristic);
            if (index != -1)
            {
                instance._segmentedControl.SelectCellWithNumber(index);
                instance._selectedBeatmapCharacteristic = instance._currentlyAvailableBeatmapCharacteristics[index];
            }
        }

        private void SetCosmeticCharacteristic(Action<BeatmapCharacteristicSegmentedControlController, IEnumerable<BeatmapCharacteristicSO>, BeatmapCharacteristicSO, HashSet<BeatmapCharacteristicSO>> original, BeatmapCharacteristicSegmentedControlController instance, IEnumerable<BeatmapCharacteristicSO> beatmapCharacteristics, BeatmapCharacteristicSO selectedBeatmapCharacteristic, HashSet<BeatmapCharacteristicSO> notAllowedCharacteristics)
        {
            original(instance, beatmapCharacteristics, selectedBeatmapCharacteristic, notAllowedCharacteristics);

            if (_songData == null || _songData._characteristicDetails == null || !_config.DisplayCustomCharacteristics)
            {
                return;
            }

            foreach (var characteristicDetails in _songData._characteristicDetails)
            {
                var index = instance._currentlyAvailableBeatmapCharacteristics.FindIndex(c => c.serializedName == characteristicDetails._beatmapCharacteristicName);

                if (index == -1)
                {
                    continue;
                }

                var dataItem = instance._segmentedControl._dataItems[index];
                var cell = (IconSegmentedControlCell)instance._segmentedControl.cells[index];

                if (!string.IsNullOrWhiteSpace(characteristicDetails._characteristicLabel))
                {
                    dataItem.hintText = characteristicDetails._characteristicLabel;
                    cell.hintText = characteristicDetails._characteristicLabel;
                }

                var icon = GetCharacteristicIcon(characteristicDetails._characteristicIconFilePath);
                if (icon != null)
                {
                    dataItem.icon = icon;
                    cell.sprite = icon;
                }
            }
        }

        private Sprite? GetCharacteristicIcon(string? characteristicIconFilePath)
        {
            if (string.IsNullOrWhiteSpace(characteristicIconFilePath))
            {
                return null;
            }

            var spritePath = Path.Combine(_customLevelLoader._loadedBeatmapSaveData[_standardLevelDetailViewController.beatmapLevel.levelID].customLevelFolderInfo.folderPath, characteristicIconFilePath);
            if (!_characteristicDetailsSprites.TryGetValue(spritePath, out var icon))
            {
                if ((icon = Utils.LoadSpriteFromFile(spritePath)) != null)
                {
                    _characteristicDetailsSprites.Add(spritePath, icon);
                }
            }

            return icon;
        }

        private void ProcessBeatmapRequirements(Action<StandardLevelDetailView> original, StandardLevelDetailView instance)
        {
            original(instance);

            var beatmapLevel = _standardLevelDetailViewController.beatmapLevel;
            var beatmapKey = instance.beatmapKey;
            var actionButton = instance.actionButton;
            var practiceButton = instance.practiceButton;

            actionButton.interactable = true;
            practiceButton.interactable = true;

            _requirementsUI.ButtonGlowColor = false;
            _requirementsUI.ButtonInteractable = false;

            if (_songData == null)
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

            if (beatmapLevel.levelID.EndsWith(" WIP", StringComparison.Ordinal))
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

            _requirementsUI.beatmapLevel = beatmapLevel;
            _requirementsUI.beatmapKey = beatmapKey;
            _requirementsUI.songData = _songData;
            _requirementsUI.diffData = difficultyData;
            _requirementsUI.wipFolder = wipFolderSong;
        }

        private void SaveAndRestoreButtonsState(Action<StandardLevelDetailView> original, StandardLevelDetailView instance)
        {
            var actionButtonInteractable = instance.actionButton.interactable;
            var practiceButtonInteractable = instance.practiceButton.interactable;
            original(instance);
            instance.actionButton.interactable = actionButtonInteractable;
            instance.practiceButton.interactable = practiceButtonInteractable;
        }

        private void OverrideColorSchemeManipulator(ILContext context)
        {
            var cursor = new ILCursor(context);
            cursor.GotoNext(MoveType.Before, i => i.MatchCall(out var method) && method.Name == "set_colorScheme");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate(SetOverrideColorScheme);
        }

        private ColorScheme SetOverrideColorScheme(ColorScheme colorScheme, LevelScenesTransitionSetupDataSO levelScenesTransitionSetupData, in BeatmapKey beatmapKey)
        {
            var overrideColorScheme = GetOverrideColorScheme(colorScheme, beatmapKey);

            if (overrideColorScheme == null)
            {
                return colorScheme;
            }

            if (levelScenesTransitionSetupData is StandardLevelScenesTransitionSetupDataSO standardLevelScenesTransitionSetupData)
            {
                standardLevelScenesTransitionSetupData.usingOverrideColorScheme = true;
            }
            else if (levelScenesTransitionSetupData is MultiplayerLevelScenesTransitionSetupDataSO multiplayerLevelScenesTransitionSetupData)
            {
                multiplayerLevelScenesTransitionSetupData.usingOverrideColorScheme = true;
            }

            return overrideColorScheme;
        }

        private ColorScheme? GetOverrideColorScheme(ColorScheme currentColorScheme, BeatmapKey beatmapKey)
        {
            if (_config is { CustomSongNoteColors: false, CustomSongEnvironmentColors: false, CustomSongObstacleColors: false })
            {
                return null;
            }

            var songDifficultyData = Collections.GetCustomLevelSongDifficultyData(beatmapKey);

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
