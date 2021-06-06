using HarmonyLib;
using System.Linq;

namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(BeatmapCharacteristicCollectionSO))]
    [HarmonyPatch("GetBeatmapCharacteristicBySerializedName", MethodType.Normal)]
    internal class CustomCharacteristicsPatch
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
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


    /*
    [HarmonyPatch(typeof(BeatmapCharacteristicSO))]
    [HarmonyPatch("descriptionLocalizationKey", MethodType.Getter)]
    class CustomCharacteristicsPatch2
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Postfix(BeatmapCharacteristicSO __instance, ref string __result)
        {
            if (__result == null)
            {
                __result = __instance.descriptionLocalizationKey;
            }
        }
    }

    [HarmonyPatch(typeof(BeatmapCharacteristicSO))]
    [HarmonyPatch("characteristicNameLocalizationKey", MethodType.Getter)]
    class CustomCharacteristicsPatch3
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Postfix(BeatmapCharacteristicSO __instance, ref string __result)
        {
            if (__result == null)
            {
                __result = __instance.characteristicNameLocalizationKey;
            }
        }
    }
    */
}
