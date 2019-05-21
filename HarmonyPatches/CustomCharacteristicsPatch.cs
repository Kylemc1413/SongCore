using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapCharacteristicCollectionSO))]
    [HarmonyPatch("GetBeatmapCharacteristicBySerialiedName", MethodType.Normal)]
    class CustomCharacteristicsPatch
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Postfix(string serializedName, ref BeatmapCharacteristicSO __result)
        {
            if(__result == null)
            {
                if (Collections.customCharacteristics.Any(x => x.characteristicName == serializedName))
                    __result = Collections.customCharacteristics.FirstOrDefault(x => x.characteristicName == serializedName);
                else
                    __result = Collections.customCharacteristics.FirstOrDefault(x => x.characteristicName == "Missing Characteristic");
            }
        }
    }



    [HarmonyPatch(typeof(BeatmapCharacteristicSO))]
    [HarmonyPatch("hintTextLocalized", MethodType.Getter)]
    class CustomCharacteristicsPatch2
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Postfix(BeatmapCharacteristicSO __instance, ref string __result)
        {
            if (__result == null)
            {
                __result = __instance.hintText;
            }
        }
    }

    [HarmonyPatch(typeof(BeatmapCharacteristicSO))]
    [HarmonyPatch("characteristicNameLocalized", MethodType.Getter)]
    class CustomCharacteristicsPatch3
    {
        //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Postfix(BeatmapCharacteristicSO __instance, ref string __result)
        {
            if (__result == null)
            {
                __result = __instance.characteristicName;
            }
        }
    }

}
