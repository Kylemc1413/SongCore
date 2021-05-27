using HarmonyLib;
using System;
using System.Collections.Concurrent;
using Zenject;

namespace SongCore.HarmonyPatches
{

    [HarmonyPatch(typeof(MainSystemInit), nameof(MainSystemInit.InstallBindings))]
    public class MainSystemsInitRefreshablePatch
    {
        public const string refreshableID = "SongCore.Loader.Refresh";
        public const string didLoadEventID = "SongCore.Loader.Loaded";

        static void Postfix(DiContainer container)
        {
            container.Bind<IRefreshable>().WithId(refreshableID).To<SongCoreRefreshable>().AsSingle();
            container.Bind(typeof(IInitializable), typeof(IDisposable), typeof(SongCoreLoaderDidLoad)).To<SongCoreLoaderDidLoad>().AsSingle();
            IObservableChange loadEvent = container.Resolve<SongCoreLoaderDidLoad>();
            container.BindInstance(loadEvent).WithId(didLoadEventID).AsSingle();
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

        private class SongCoreLoaderDidLoad : IInitializable, IDisposable, IObservableChange
        {
            public event Action didChangeEvent;

            public void Initialize()
            {
                Loader.SongsLoadedEvent += Loader_SongsLoadedEvent;
            }

            private void Loader_SongsLoadedEvent(Loader _, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> __)
            {
                didChangeEvent?.Invoke();
            }

            public void Dispose()
            {
                Loader.SongsLoadedEvent -= Loader_SongsLoadedEvent;
            }
        }
    }
}