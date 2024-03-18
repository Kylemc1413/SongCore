using System;
using System.Collections.Concurrent;
using Zenject;

namespace SongCore.Installers
{
    internal class AppInstaller : Installer
    {
        private const string refreshableID = "SongCore.Loader.Refresh";
        private const string didLoadEventID = "SongCore.Loader.Loaded";

        public override void InstallBindings()
        {
            Container.Bind<IRefreshable>().WithId(refreshableID).To<SongCoreRefreshable>().AsSingle();
            Container.Bind(typeof(IInitializable), typeof(IDisposable), typeof(SongCoreLoaderDidLoad)).To<SongCoreLoaderDidLoad>().AsSingle();
            Container.Bind<IObservableChange>().WithId(didLoadEventID).FromMethod(ctx => ctx.Container.Resolve<SongCoreLoaderDidLoad>()).AsSingle();
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
            public event Action? didChangeEvent;

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
