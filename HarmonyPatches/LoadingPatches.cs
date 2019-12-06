using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Harmony;
using SongCore.Utilities;
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
    [HarmonyPatch("ClearIfLoading", MethodType.Normal)]
    internal class StopVanillaLoadingPatch2
    {

        static void Postfix(ref LevelFilteringNavigationController __instance, ref TabBarViewController ____tabBarViewController, ref bool __result)
        {
            if (____tabBarViewController.selectedCellNumber == 3)
            {
                __result = false;
            }


        }
    }

}
