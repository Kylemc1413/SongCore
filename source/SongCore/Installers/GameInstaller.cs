using SongCore.HarmonyPatches;
using Zenject;

namespace SongCore.Installers
{
    internal class GameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<CosmeticCharacteristicsPatches>().AsSingle();
            Container.BindInterfacesTo<AllowNegativeNjsValuesPatch>().AsSingle();
        }
    }
}
