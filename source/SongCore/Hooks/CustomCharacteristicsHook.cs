using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    // TODO: Remove missing characteristic. Might end up in wiped save data.
    internal class CustomCharacteristicsHook : IInitializable, IDisposable
    {
        private const string kMissingCharacteristicName = "MissingCharacteristic";

        private Hook _getBeatmapCharacteristicBySerializedNameHook = null!;
        private Hook _getBeatmapCharacteristicIconHook = null!;
        private Hook _fromSerializedNameHook = null!;
        private Hook _serializedNameHook = null!;
        private Hook _compoundIdPartNameHook = null!;
        private Hook _sortingOrderHook = null!;
        private Hook _containsRotationEventsHook = null!;
        private Hook _requires360MovementHook = null!;
        private Hook _numberOfColorsHook = null!;
        private Hook _nameLocalizationKeyHook = null!;
        private Hook _hintLocalizationKeyHook = null!;

        public void Initialize()
        {
            var extensions = typeof(BeatmapCharacteristicExtensions);
            _getBeatmapCharacteristicBySerializedNameHook = new Hook(typeof(BeatmapCharacteristicCollection).GetMethod(nameof(BeatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName))!, GetCustomCharacteristic, true);
            _getBeatmapCharacteristicIconHook = new Hook(typeof(BeatmapCharacteristicCollection).GetMethod(nameof(BeatmapCharacteristicCollection.GetBeatmapCharacteristicIcon))!, GetCustomCharacteristicIcon, true);
            _fromSerializedNameHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.BeatmapCharacteristicFromSerializedName))!, FromSerializedName, true);
            _serializedNameHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.SerializedName), new[] { typeof(BeatmapCharacteristic) })!, GetSerializedName, true);
            _compoundIdPartNameHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.CompoundIdPartName))!, GetCompoundIdPartName, true);
            _sortingOrderHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.SortingOrder))!, GetSortingOrder, true);
            _containsRotationEventsHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.ContainsRotationEvents))!, GetContainsRotationEvents, true);
            _requires360MovementHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.Requires360Movement))!, GetRequires360Movement, true);
            _numberOfColorsHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.NumberOfColors))!, GetNumberOfColors, true);
            _nameLocalizationKeyHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.NameLocalizationKey))!, GetNameLocalizationKey, true);
            _hintLocalizationKeyHook = new Hook(extensions.GetMethod(nameof(BeatmapCharacteristicExtensions.HintLocalizationKey))!, GetHintLocalizationKey, true);
        }

        public void Dispose()
        {
            _getBeatmapCharacteristicBySerializedNameHook.Dispose();
            _getBeatmapCharacteristicIconHook.Dispose();
            _fromSerializedNameHook.Dispose();
            _serializedNameHook.Dispose();
            _compoundIdPartNameHook.Dispose();
            _sortingOrderHook.Dispose();
            _containsRotationEventsHook.Dispose();
            _requires360MovementHook.Dispose();
            _numberOfColorsHook.Dispose();
            _nameLocalizationKeyHook.Dispose();
            _hintLocalizationKeyHook.Dispose();
        }

        private BeatmapCharacteristicSO? GetCustomCharacteristic(Func<BeatmapCharacteristicCollection, string, BeatmapCharacteristicSO> original, BeatmapCharacteristicCollection instance, string serializedName)
        {
            var result = original(instance, serializedName);

            if (result != null)
            {
                return result;
            }

            var customCharacteristic = Collections.customCharacteristics.FirstOrDefault(c => c.serializedName == serializedName);
            return customCharacteristic != null ? customCharacteristic : Collections.customCharacteristics.FirstOrDefault(c => c.serializedName == kMissingCharacteristicName);
        }

        private Sprite GetCustomCharacteristicIcon(Func<BeatmapCharacteristicCollection, BeatmapCharacteristic, Sprite> original, BeatmapCharacteristicCollection instance, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.icon : original(instance, characteristic);
        }

        private delegate bool FromSerializedNameDelegate(string name, out BeatmapCharacteristic characteristic);

        private bool FromSerializedName(FromSerializedNameDelegate original, string name, out BeatmapCharacteristic characteristic)
        {
            if (original(name, out characteristic))
            {
                return true;
            }

            if (Collections.TryGetCustomCharacteristic(name, out characteristic))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(name) && Collections.TryGetCustomCharacteristic(kMissingCharacteristicName, out characteristic);
        }

        private string GetSerializedName(Func<BeatmapCharacteristic, string> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.serializedName : original(characteristic);
        }

        private string GetCompoundIdPartName(Func<BeatmapCharacteristic, string> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.compoundIdPartName : original(characteristic);
        }

        private int GetSortingOrder(Func<BeatmapCharacteristic, int> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.sortingOrder : original(characteristic);
        }

        private bool GetContainsRotationEvents(Func<BeatmapCharacteristic, bool> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.containsRotationEvents : original(characteristic);
        }

        private bool GetRequires360Movement(Func<BeatmapCharacteristic, bool> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.requires360Movement : original(characteristic);
        }

        private int GetNumberOfColors(Func<BeatmapCharacteristic, int> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.numberOfColors : original(characteristic);
        }

        private string GetNameLocalizationKey(Func<BeatmapCharacteristic, string> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.characteristicNameLocalizationKey : original(characteristic);
        }

        private string GetHintLocalizationKey(Func<BeatmapCharacteristic, string> original, BeatmapCharacteristic characteristic)
        {
            return Collections.TryGetCustomCharacteristicData(characteristic, out var data) ? data.descriptionLocalizationKey : original(characteristic);
        }
    }
}
