using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SongCore.Utilities;

namespace SongCore.HarmonyPatches
{
    //Fixes bug where V3 -> V4 conversion doesn't update the V3 map
    [HarmonyPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.HandleBeatmapEditorSceneDidFinish))]
    internal class MenuTransitionsHelperHandleBeatmapEditorSceneDidFinish
    {
        private static void Postfix()
        {
            Loader.Instance.RefreshSongs(true);
            return;
        }
    }
}
