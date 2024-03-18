using HarmonyLib;
using System.Linq;

namespace SongCore.HarmonyPatches
{
    // TODO: Remove missing characteristic. Might end up in wiped save data.
    [HarmonyPatch(typeof(BeatmapCharacteristicCollection))]
    [HarmonyPatch(nameof(BeatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName), MethodType.Normal)]
    internal class CustomCharacteristicsPatch
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