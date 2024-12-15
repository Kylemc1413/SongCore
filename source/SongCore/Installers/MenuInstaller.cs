using SongCore.Patches;
using SongCore.UI;
using Zenject;

namespace SongCore.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<SettingsController>().AsSingle();
            Container.BindInterfacesAndSelfTo<Loader>().AsSingle();
            Container.BindInterfacesAndSelfTo<ColorsUI>().AsSingle();
            Container.Bind<ProgressBar>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesAndSelfTo<RequirementsUI>().AsSingle();
            Container.BindInterfacesTo<SongDataMenuPatches>().AsSingle();
            Container.BindInterfacesTo<InternalRestartPatch>().AsSingle();
        }
    }
}
