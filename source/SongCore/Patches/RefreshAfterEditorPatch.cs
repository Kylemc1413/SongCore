using HarmonyLib;

namespace SongCore.Patches
{
    // Ensures songs are refreshed when creating or converting maps in the editor.
    // TODO: Only do this if needed?
    [HarmonyPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.HandleBeatmapEditorSceneDidFinish))]
    internal class MenuTransitionsHelperHandleBeatmapEditorSceneDidFinish
    {
        private static void Postfix()
        {
            Loader.Instance.RefreshSongs();
        }
    }
}
