using System.Linq;
using HarmonyLib;

namespace SongCore.Patches
{
    // TODO: Remove missing characteristic. Might end up in wiped save data.
    [HarmonyPatch(typeof(BeatmapCharacteristicCollection), nameof(BeatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName))]
    internal static class CustomCharacteristicsPatch
    {
        private static void Postfix(string serializedName, ref BeatmapCharacteristicSO __result)
        {
            if (__result == null)
            {
                if (Collections.customCharacteristics.Any(x => x.serializedName == serializedName))
                {
                    __result = Collections.customCharacteristics.FirstOrDefault(x => x.serializedName == serializedName);
                }
                else
                {
                    __result = Collections.customCharacteristics.FirstOrDefault(x => x.serializedName == "MissingCharacteristic");
                }
            }
        }
    }
}
