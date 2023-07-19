using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;
using static IPA.Logging.Logger;

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

                var beatmap = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap;
                if (beatmap == null)
                    return true;
                var beatmapData = Collections.RetrieveDifficultyData(beatmap);
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

                sceneSetupData = __instance.GetField<GameplayCoreSceneSetupData, GameplayCoreInstaller>("_sceneSetupData");

                var diffBeatmapLevel = sceneSetupData.difficultyBeatmap.level;
                var level = diffBeatmapLevel is CustomBeatmapLevel ? diffBeatmapLevel as CustomPreviewBeatmapLevel : null;
                if (level == null)
                {
                    diffData = null;
                    return;
                }
                diffData = Collections.RetrieveDifficultyData(sceneSetupData.difficultyBeatmap);
                if (diffData == null)
                    return;
                if (diffData._oneSaber != null && !Plugin.Configuration.DisableOneSaberOverride)
                {
                    numberOfColors = sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.numberOfColors;
                    sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.SetField("_numberOfColors", diffData._oneSaber.Value == true ? 1 : 2);
                }

            }
            private static void Postfix(GameplayCoreInstaller __instance)
            {
                if (Plugin.Configuration.DisableOneSaberOverride)
                    return;
                if (diffData == null)
                    return;

                if (diffData._oneSaber != null && !Plugin.Configuration.DisableOneSaberOverride)
                {
                    sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.SetField("_numberOfColors", numberOfColors);
                }
            }

        }


        [HarmonyPatch(typeof(BeatmapCharacteristicSegmentedControlController))]
        [HarmonyPatch(nameof(BeatmapCharacteristicSegmentedControlController.SetData), MethodType.Normal)]
        internal class CosmeticCharacteristicsPatch
        {
            //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
            private static void Postfix(IReadOnlyList<IDifficultyBeatmapSet> difficultyBeatmapSets, BeatmapCharacteristicSO selectedBeatmapCharacteristic, ref List<BeatmapCharacteristicSO> ____beatmapCharacteristics, ref IconSegmentedControl ____segmentedControl)
            {
                if (!Plugin.Configuration.DisplayCustomCharacteristics)
                    return;
                var diffBeatmapLevel = difficultyBeatmapSets.FirstOrDefault().difficultyBeatmaps.FirstOrDefault().level;
                var level = diffBeatmapLevel is CustomBeatmapLevel ? diffBeatmapLevel as CustomPreviewBeatmapLevel : null;

                if (level == null)
                    return;

                var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(level));
                if (songData == null)
                    return;
                if (songData._characteristicDetails == null)
                    return;


                if (songData._characteristicDetails.Length > 0)
                {
                    var dataItems = ____segmentedControl.GetField<IconSegmentedControl.DataItem[], IconSegmentedControl>("_dataItems");
                    List<IconSegmentedControl.DataItem> newDataItems = new List<IconSegmentedControl.DataItem>();

                    int i = 0;
                    int cell = 0;
                    foreach (var item in dataItems)
                    {
                        var characteristic = ____beatmapCharacteristics[i];
                        string serializedName = characteristic.serializedName;
                        ExtraSongData.CharacteristicDetails? detail = songData._characteristicDetails.Where(x => x._beatmapCharacteristicName == serializedName).FirstOrDefault();

                        if (detail != null)
                        {
                            Sprite sprite = characteristic.icon;
                            if (detail._characteristicIconFilePath != null)
                                sprite = Utilities.Utils.LoadSpriteFromFile(Path.Combine(level.customLevelPath, detail._characteristicIconFilePath)) ?? characteristic.icon;
                            string label = detail._characteristicLabel ?? Polyglot.Localization.Get(characteristic.descriptionLocalizationKey);
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
                    ____segmentedControl.SetData(newDataItems.ToArray());
                    ____segmentedControl.SelectCellWithNumber(cell);
                }
            }
        }

    }
}
