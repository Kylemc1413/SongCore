using SongCore.HarmonyPatches;
using Zenject;

namespace SongCore.Installers
{
    internal class AppInstaller : Installer
    {
        public override void InstallBindings()
        {
            MainSystemsInitRefreshablePatch.Postfix(Container);
        }
    }
}
