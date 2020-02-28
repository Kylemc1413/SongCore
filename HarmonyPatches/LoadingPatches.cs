using HarmonyLib;
using SongCore.Utilities;
using System.Linq;
using System.Threading;
namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(BeatmapLevelsModel))]
    [HarmonyPatch("GetCustomLevelPackCollectionAsync", MethodType.Normal)]
    internal class StopVanillaLoadingPatch
    {
        static void Prefix()
        {
            var cancel = UnityEngine.Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().First().GetField<CancellationTokenSource>("_cancellationTokenSource");
            cancel.Cancel();

        }
    }

    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch("InitializeIfNeeded", MethodType.Normal)]
    internal class StopVanillaLoadingPatch2
    {

        static void Postfix(ref LevelFilteringNavigationController __instance, ref TabBarViewController ____tabBarViewController)
        {
      //      Logging.logger.Info("Set CustomLevelCollection");
            __instance.GetField("_customLevelsTabBarData")?.SetField("annotatedBeatmapLevelCollections", Loader.CustomBeatmapLevelPackCollectionSO?.beatmapLevelPacks);

        }
    }

    [HarmonyPatch(typeof(LevelFilteringNavigationController))]
    [HarmonyPatch("ReloadSongListIfNeeded", MethodType.Normal)]
    internal class StopVanillaLoadingPatch3
    {

        static bool Prefix(ref LevelFilteringNavigationController __instance, ref TabBarViewController ____tabBarViewController)
        {
            __instance.GetField("_customLevelsTabBarData")?.SetField("annotatedBeatmapLevelCollections", Loader.CustomBeatmapLevelPackCollectionSO?.beatmapLevelPacks);
            return false;
        }
    }

}
