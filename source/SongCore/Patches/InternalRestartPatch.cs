using SiraUtil.Affinity;

namespace SongCore.Patches
{
    // TODO: Find better naming.
    internal class InternalRestartPatch : IAffinity
    {
        private readonly Loader _loader;

        private InternalRestartPatch(Loader loader)
        {
            _loader = loader;
        }

        [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.RestartGame))]
        [AffinityPrefix]
        private void SaveLoadedLevels()
        {
            _loader.StoreLoadedBeatmapSaveData();
        }
    }
}
