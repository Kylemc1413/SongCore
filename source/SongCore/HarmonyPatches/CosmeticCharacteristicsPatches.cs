using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using HMUI;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.HarmonyPatches
{
    internal class CosmeticCharacteristicsPatches
    {


        [HarmonyPatch(typeof(BeatLineManager))]
        [HarmonyPatch(nameof(BeatLineManager.HandleNoteWasSpawned))]
        internal class BeatLineManager_HandleNoteWasSpawned
        {
            private static bool Prefix()
            {
                if (Plugin.Configuration.DisableRotationSpawnLinesOverride)
                    return true;

                var sceneSetupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
                if (sceneSetupData.beatmapLevel == null)
                    return true;
                var beatmapData = Collections.RetrieveDifficultyData(sceneSetupData.beatmapLevel, sceneSetupData.beatmapKey);
                if (beatmapData == null)
                    return true;
                if (beatmapData._showRotationNoteSpawnLines == null)
                    return true;
                return beatmapData._showRotationNoteSpawnLines.Value;
            }
        }


        [HarmonyPatch(typeof(GameplayCoreInstaller))]
        [HarmonyPatch(nameof(GameplayCoreInstaller.InstallBindings))]
        internal class GameplayCoreInstaller_InstallBindingsPatch
        {
            private static ExtraSongData.DifficultyData? diffData = null;
            private static int numberOfColors = -1;
            private static GameplayCoreSceneSetupData sceneSetupData = null;
            private static void Prefix(GameplayCoreInstaller __instance)
            {
                if (Plugin.Configuration.DisableOneSaberOverride)
                    return;

                sceneSetupData = __instance._sceneSetupData;

                var beatmapLevel = sceneSetupData.beatmapLevel;
                if (beatmapLevel.hasPrecalculatedData)
                {
                    diffData = null;
                    return;
                }
                diffData = Collections.RetrieveDifficultyData(beatmapLevel, sceneSetupData.beatmapKey);
                if (diffData == null)
                    return;
                if (diffData._oneSaber != null && !Plugin.Configuration.DisableOneSaberOverride)
                {
                    numberOfColors = sceneSetupData.beatmapKey.beatmapCharacteristic.numberOfColors;
                    sceneSetupData.beatmapKey.beatmapCharacteristic._numberOfColors = diffData._oneSaber.Value == true ? 1 : 2;
                }

            }
            private static void Postfix()
            {
                if (Plugin.Configuration.DisableOneSaberOverride)
                    return;
                if (diffData == null)
                    return;

                if (diffData._oneSaber != null && !Plugin.Configuration.DisableOneSaberOverride)
                {
                    sceneSetupData.beatmapKey.beatmapCharacteristic._numberOfColors = numberOfColors;
                }
            }

        }


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
