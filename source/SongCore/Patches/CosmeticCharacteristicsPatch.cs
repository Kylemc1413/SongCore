using System.Collections.Generic;
using System.IO;
using System.Linq;
using BGLib.Polyglot;
using HMUI;
using SiraUtil.Affinity;
using SongCore.Utilities;
using UnityEngine;

namespace SongCore.Patches
{
    internal class CosmeticCharacteristicsPatch : IAffinity
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly PluginConfig _config;

        private CosmeticCharacteristicsPatch(StandardLevelDetailViewController standardLevelDetailViewController, CustomLevelLoader customLevelLoader, PluginConfig config)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _customLevelLoader = customLevelLoader;
            _config = config;
        }

        [AffinityPatch(typeof(BeatmapCharacteristicSegmentedControlController), nameof(BeatmapCharacteristicSegmentedControlController.SetData))]
        private void SetCosmeticCharacteristic(BeatmapCharacteristicSegmentedControlController __instance, BeatmapCharacteristicSO selectedBeatmapCharacteristic)
        {
            if (!_config.DisplayCustomCharacteristics)
            {
                return;
            }

            var beatmapLevel = _standardLevelDetailViewController.beatmapLevel;
            if (beatmapLevel.hasPrecalculatedData)
            {
                return;
            }

            var extraSongData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(beatmapLevel));
            if (extraSongData?._characteristicDetails == null || extraSongData._characteristicDetails.Length == 0)
            {
                return;
            }

            var newDataItems = new List<IconSegmentedControl.DataItem>();
            var i = 0;
            var cellIndex = 0;
            foreach (var dataItem in __instance._segmentedControl._dataItems)
            {
                var beatmapCharacteristic = __instance._currentlyAvailableBeatmapCharacteristics[i];
                var serializedName = beatmapCharacteristic.serializedName;
                var characteristicDetails = extraSongData._characteristicDetails.FirstOrDefault(c => c._beatmapCharacteristicName == serializedName);

                if (characteristicDetails != null)
                {
                    Sprite? icon = null;

                    if (characteristicDetails._characteristicIconFilePath != null)
                    {
                        icon = Utils.LoadSpriteFromFile(Path.Combine(_customLevelLoader._loadedBeatmapSaveData[beatmapLevel.levelID].customLevelFolderInfo.folderPath, characteristicDetails._characteristicIconFilePath));
                    }

                    if (icon == null)
                    {
                        icon = beatmapCharacteristic.icon;
                    }

                    var label = characteristicDetails._characteristicLabel ?? Localization.Get(beatmapCharacteristic.descriptionLocalizationKey);
                    newDataItems.Add(new IconSegmentedControl.DataItem(icon, label));
                }
                else
                {
                    newDataItems.Add(dataItem);
                }

                if (beatmapCharacteristic == selectedBeatmapCharacteristic)
                {
                    cellIndex = i;
                }

                i++;
            }

            __instance._segmentedControl.SetData(newDataItems.ToArray());
            __instance._segmentedControl.SelectCellWithNumber(cellIndex);
        }
    }
}