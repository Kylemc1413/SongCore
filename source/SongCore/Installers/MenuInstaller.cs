using System.Threading.Tasks;
using SongCore.HarmonyPatches;
using SongCore.UI;
using UnityEngine;
using Zenject;

namespace SongCore.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<Loader>().AsSingle();
            Container.BindInterfacesAndSelfTo<ColorsUI>().AsSingle();
            Container.Bind<ProgressBar>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesAndSelfTo<RequirementsUI>().AsSingle();
            Container.BindInterfacesTo<CosmeticCharacteristicsPatch>().AsSingle();
            Container.Bind<IReferenceCountingCache<int, Task<Sprite>>>().To<SpriteReferenceCountingCache>().AsSingle();
            Container.Bind<SpriteAsyncLoaderFixed>().AsSingle();
            Container.BindInterfacesTo<FixSpriteAsyncLoaderLeakPatch>().AsSingle();
            Container.BindInterfacesTo<FixAudioClipAsyncLoaderCrashPatch>().AsSingle();
        }
    }
}
