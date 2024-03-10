using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using SiraUtil.Affinity;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.HarmonyPatches
{
    internal class CosmeticCharacteristicsPatches : IAffinity
    {
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly BeatmapKey _beatmapKey;

        private CosmeticCharacteristicsPatches(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, BeatmapKey beatmapKey)
        {
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _beatmapKey = beatmapKey;
        }

        [AffinityPatch(typeof(BeatLineManager), nameof(BeatLineManager.HandleNoteWasSpawned))]
        [AffinityPrefix]
        private bool ShowOrHideRotationNoteSpawnLines()
        {
            if (Plugin.Configuration.DisableRotationSpawnLinesOverride)
            {
                return true;
            }

            var difficultyData = Collections.RetrieveDifficultyData(_gameplayCoreSceneSetupData.beatmapLevel, _beatmapKey);
            return difficultyData?._showRotationNoteSpawnLines == null || difficultyData._showRotationNoteSpawnLines.Value;
        }


        [AffinityPatch(typeof(SaberManager.InitData), "ctor", AffinityMethodType.Constructor, null, typeof(bool), typeof(SaberType))]
        private void ForceOneSaber(ref SaberManager.InitData __instance)
        {
            if (Plugin.Configuration.DisableOneSaberOverride || _gameplayCoreSceneSetupData.beatmapLevel.hasPrecalculatedData)
            {
                return;
            }

            var difficultyData = Collections.RetrieveDifficultyData(_gameplayCoreSceneSetupData.beatmapLevel, _beatmapKey);
            if (difficultyData is { _oneSaber: not null })
            {
                __instance.SetField(nameof(__instance.oneSaberMode), difficultyData._oneSaber.Value);
            }
        }

        // TODO
        [HarmonyPatch(typeof(BeatmapCharacteristicSegmentedControlController))]
        [HarmonyPatch(nameof(BeatmapCharacteristicSegmentedControlController.SetData), MethodType.Normal)]
        internal class CosmeticCharacteristicsPatch
        {
            private static void Postfix(BeatmapCharacteristicSegmentedControlController __instance, BeatmapCharacteristicSO selectedBeatmapCharacteristic)
            {
                if (!Plugin.Configuration.DisplayCustomCharacteristics) return;

                var level = Object.FindObjectOfType<StandardLevelDetailViewController>()._beatmapLevel;

                if (level.hasPrecalculatedData) return;
                var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(level));

                if (songData == null) return;
                if (songData._characteristicDetails == null) return;
                if (__instance._segmentedControl == null) return;

                if (songData._characteristicDetails.Length > 0)
                {
                    var dataItems = __instance._segmentedControl._dataItems;
                    List<IconSegmentedControl.DataItem> newDataItems = new List<IconSegmentedControl.DataItem>();

                    int i = 0;
                    int cell = 0;
                    foreach (var item in dataItems)
                    {
                        var characteristic = __instance._beatmapCharacteristics[i];
                        string serializedName = characteristic.serializedName;
                        ExtraSongData.CharacteristicDetails? detail = songData._characteristicDetails.Where(x => x._beatmapCharacteristicName == serializedName).FirstOrDefault();

                        if (detail != null)
                        {
                            Sprite sprite = characteristic.icon;
                            if (detail._characteristicIconFilePath != null && Collections.LevelPathDictionary.TryGetValue(level.levelID, out var customLevelPath))
                                sprite = Utilities.Utils.LoadSpriteFromFile(Path.Combine(customLevelPath, detail._characteristicIconFilePath)) ?? characteristic.icon;
                            string label = detail._characteristicLabel ?? BGLib.Polyglot.Localization.Get(characteristic.descriptionLocalizationKey);
                            newDataItems.Add(new IconSegmentedControl.DataItem(sprite, label));
                        }
                        else
                        {
                            newDataItems.Add(item);
                        }

                        if (characteristic == selectedBeatmapCharacteristic)
                        {
                            cell = i;
                        }
                        i++;
                    }
                    __instance._segmentedControl.SetData(newDataItems.ToArray());
                    __instance._segmentedControl.SelectCellWithNumber(cell);
                }
            }
        }

    }
}
