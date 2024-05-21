using BGLib.DotnetExtension.Collections;
using HarmonyLib;
using UnityEngine;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// This partially fixes a bug that was introduced in v1.36.0, which saves null covers when the loading operation is canceled.
    /// It still shows the default cover at times, but at least it reloads it now.
    /// </summary>
    // TODO: Remove when fixed.
    [HarmonyPatch(typeof(LRUCache<string, Sprite>), nameof(LRUCache<string, Sprite>.Add))]
    internal class CoversLoadingFixPatch
    {
        private static bool Prefix(Sprite value)
        {
            return value != null;
        }
    }
}
