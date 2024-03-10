using System;
using System.Collections.Concurrent;
using Zenject;

namespace SongCore.HarmonyPatches
{
    internal class MainSystemsInitRefreshablePatch
    {
        public const string refreshableID = "SongCore.Loader.Refresh";
        public const string didLoadEventID = "SongCore.Loader.Loaded";

        public static void Postfix(DiContainer container)
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

            private void Loader_SongsLoadedEvent(Loader _, ConcurrentDictionary<string, BeatmapLevel> __)
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