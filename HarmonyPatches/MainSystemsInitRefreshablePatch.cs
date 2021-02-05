using HarmonyLib;
using Zenject;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(MainSystemInit), nameof(MainSystemInit.InstallBindings))]
    public class MainSystemsInitRefreshablePatch
    {
        static void Postfix(DiContainer container)
        {
            container.Bind<IRefreshable>().WithId($"{nameof(SongCore)}.{nameof(Loader)}").To<SongCoreRefreshable>().AsSingle();
        }

        private class SongCoreRefreshable : IRefreshable
        {
            public void Refresh()
            {
                if (Loader.AreSongsLoaded)
                {
                    Loader.Instance.RefreshSongs();
                }
            }
        }
    }
}