using SongCore.Patches;
using Zenject;

namespace SongCore.Installers
{
    internal class GameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SongDataGamePatches>().AsSingle();
            Container.BindInterfacesTo<DisableSubmissionPatches>().AsSingle();
        }
    }
}
