using SongCore.Hooks;
using Zenject;

namespace SongCore.Installers
{
    internal class GameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SongDataGameHooks>().AsSingle();
            Container.BindInterfacesTo<DisableSubmissionHooks>().AsSingle();
            Container.BindInterfacesTo<AllowNegativeVariableNoteJumpSpeedHook>().AsSingle();
            Container.BindInterfacesTo<ConfigWatchersToggle>().AsSingle();
        }
    }
}
