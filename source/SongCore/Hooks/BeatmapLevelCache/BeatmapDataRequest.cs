using System;
using System.Threading.Tasks;
using BeatSaber.Destinations;
using ModestTree;
using UnityEngine.SceneManagement;

namespace SongCore.Hooks.BeatmapLevelCache
{
    internal record BeatmapDataRequest(IBeatmapLevelData BeatmapLevelData, BeatmapKey BeatmapKey, float StartBpm, bool LoadingForDesignatedEnvironment, IEnvironmentInfo? TargetEnvironmentInfo, IEnvironmentInfo? OriginalEnvironmentInfo, BeatmapLevelDataVersion BeatmapLevelDataVersion, GameplayModifiers? GameplayModifiers, PlayerSpecificSettings? PlayerSpecificSettings)
    {
        public Task<IReadonlyBeatmapData?> Start(Func<BeatmapDataLoader, IBeatmapLevelData, BeatmapKey, float, bool, IEnvironmentInfo?, IEnvironmentInfo?, BeatmapLevelDataVersion, GameplayModifiers?, PlayerSpecificSettings?, Task<IReadonlyBeatmapData?>> original, BeatmapDataLoader beatmapDataLoader)
        {
            Assert.That(SceneManager.GetActiveScene().name != SceneNames.kGameCoreSceneName, "Beatmap data should not be loaded in the game scene, as garbage collection is disabled.");

            return original(beatmapDataLoader, BeatmapLevelData, BeatmapKey, StartBpm, LoadingForDesignatedEnvironment, TargetEnvironmentInfo, OriginalEnvironmentInfo, BeatmapLevelDataVersion, GameplayModifiers, PlayerSpecificSettings);
        }
    }
}
