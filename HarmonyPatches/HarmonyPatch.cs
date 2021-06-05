using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(Harmony))]
    [HarmonyPatch("UnpatchAll", MethodType.Normal)]
    public class StopModsFromBreakingOtherModsAccidentallyPatch
    {
        public static void Prefix(Harmony __instance, ref string harmonyID)
        {
            if (string.IsNullOrWhiteSpace(harmonyID))
            {
                harmonyID = __instance.Id;
                Utilities.Logging.logger.Error($"HEY {__instance.Id} YOU'RE TRYING TO UNPATCH EVERY SINGLE MOD, PLEASE PROVIDE YOUR HARMONY ID WHEN USING UNPATCHALL");
            }
        }
    }

}
