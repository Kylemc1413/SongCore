using SongCore.Patches;
using Zenject;

namespace SongCore.Installers
{
    internal class GameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<BeatmapLevelDifficultyDataPatches>().AsSingle();
            Container.BindInterfacesTo<AllowNegativeNjsValuesPatch>().AsSingle();
            Container.BindInterfacesTo<DisableSubmissionPatches>().AsSingle();
        }
    }
}
