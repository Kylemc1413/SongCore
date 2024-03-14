using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(GameplayCoreInstaller), nameof(GameplayCoreInstaller.InstallBindings))]
    internal class BindBeatmapLevelPatch
    {
        private static void Postfix(GameplayCoreInstaller __instance)
        {
            __instance.Container.Bind<BeatmapLevel>().FromInstance(__instance._sceneSetupData.beatmapLevel).AsSingle();
        }
    }
}
