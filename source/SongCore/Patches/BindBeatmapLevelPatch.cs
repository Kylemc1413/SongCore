using HarmonyLib;

namespace SongCore.Patches
{
    [HarmonyPatch(typeof(GameplayCoreInstaller), nameof(GameplayCoreInstaller.InstallBindings))]
    internal static class BindBeatmapLevelPatch
    {
        private static void Postfix(GameplayCoreInstaller __instance)
        {
            __instance.Container.Bind<BeatmapLevel>().FromInstance(__instance._sceneSetupData.beatmapLevel).AsSingle();
        }
    }
}
