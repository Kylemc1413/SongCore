using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using ModestTree;
using MonoMod.RuntimeDetour;
using SongCore.Utilities;
using Zenject;

namespace SongCore.Hooks.BeatmapLevelCache
{
    /// <summary>
    /// This implements a way to cache beatmap data when selecting levels for later use by the game or mods.
    /// The execution flow is as follows:
    /// <list>
    ///   <item><see cref="LevelCollectionTableView.didSelectLevelEvent"/> fires.</item>
    ///   <item><see cref="HandleDidSelectLevel(LevelCollectionTableView, BeatmapLevel)"/> invokes base game handlers.</item>
    ///   <item>Base game handler fires <see cref="LevelCollectionViewController.didSelectLevelEvent"/>.</item>
    ///   <item><see cref="HandleDidSelectLevel(LevelCollectionViewController, BeatmapLevel)"/> invokes base game handlers.</item>
    ///   <item>Base game handler fires <see cref="StandardLevelDetailViewController.didChangeContentEvent"/>.</item>
    ///   <item><see cref="HandleDidChangeContent"/> invokes base game handlers. <see cref="StandardLevelDetailView.CreateBeatmapKey"/> is called here.</item>
    ///   <item><see cref="HandleDidChangeContent"/> waits for <see cref="InitializeBeatmapLevelCacheAsync"/> to be done.</item>
    ///   <item><see cref="HandleDidChangeContent"/> waits for <see cref="SetBeatmapLevelCacheBeatmapKeyAsync"/> to be done.</item>
    ///   <item><see cref="HandleDidChangeContent"/> invokes mod handlers.</item>
    ///   <item><see cref="HandleDidSelectLevel(LevelCollectionViewController, BeatmapLevel)"/> invokes mod handlers.</item>
    ///   <item><see cref="HandleDidSelectLevel(LevelCollectionTableView, BeatmapLevel)"/> invokes mod handlers.</item>
    /// </list>
    /// Ultimately, it ensures the cache is ready and prevents race conditions.
    /// </summary>
    internal class BeatmapDataCacheHooks : IInitializable, IDisposable
    {
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly BeatmapLevelsEntitlementModel _beatmapLevelsEntitlementModel;
        private readonly BeatmapDataLoader _beatmapDataLoader;
        private readonly BeatmapLevelCache _beatmapLevelCache;
        private readonly EventProxyHooks _eventProxyHooks;

        private Hook _readAllTextFromPathAsyncHook = null!;
        private Hook _replaceDidSelectLevelEventHook = null!;
        private Hook _replaceDidChangeContentEventHook = null!;
        private Hook _createbeatmapKeyHook = null!;
        private Hook _loadBeatmapDataAsyncHook = null!;
        private CancellationToken _cancellationToken;

        private BeatmapDataCacheHooks(CustomLevelLoader customLevelLoader, BeatmapLevelsModel beatmapLevelsModel, BeatmapLevelsEntitlementModel beatmapLevelsEntitlementModel, BeatmapDataLoader beatmapDataLoader, BeatmapLevelCache beatmapLevelCache, EventProxyHooks eventProxyHooks)
        {
            _customLevelLoader = customLevelLoader;
            _beatmapLevelsModel = beatmapLevelsModel;
            _beatmapLevelsEntitlementModel = beatmapLevelsEntitlementModel;
            _beatmapDataLoader = beatmapDataLoader;
            _beatmapLevelCache = beatmapLevelCache;
            _eventProxyHooks = eventProxyHooks;
        }

        public void Initialize()
        {
            _readAllTextFromPathAsyncHook = new Hook(typeof(BeatmapLevelDataUtils).GetMethod(nameof(BeatmapLevelDataUtils.ReadAllTextFromPathAsync))!, LogReadAllTextFromPathAsync, true);
            _replaceDidSelectLevelEventHook = new Hook(typeof(LevelCollectionViewController).GetMethod(nameof(LevelCollectionViewController.DidActivate), BindingFlags.Instance | BindingFlags.NonPublic)!, ReplaceDidSelectLevelEvent, true);
            _replaceDidChangeContentEventHook = new Hook(typeof(StandardLevelDetailViewController).GetMethod(nameof(StandardLevelDetailViewController.DidActivate), BindingFlags.Instance | BindingFlags.NonPublic)!, ReplaceDidChangeContentEvent, true);
            _createbeatmapKeyHook = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.CreateBeatmapKey), BindingFlags.Instance | BindingFlags.NonPublic)!, SetBeatmapLevelCacheBeatmapKey, true);
            _loadBeatmapDataAsyncHook = new Hook(typeof(BeatmapDataLoader).GetMethod(nameof(BeatmapDataLoader.LoadBeatmapDataAsync))!, LoadBeatmapDataWithCacheAsync, true);
        }

        public void Dispose()
        {
            _readAllTextFromPathAsyncHook.Dispose();
            _replaceDidSelectLevelEventHook.Dispose();
            _replaceDidChangeContentEventHook.Dispose();
            _createbeatmapKeyHook.Dispose();
            _loadBeatmapDataAsyncHook.Dispose();
        }

        private async Task<IBeatmapLevelData?> InitializeBeatmapLevelCacheAsync(BeatmapLevel beatmapLevel, CancellationToken cancellationToken)
        {
            try
            {
                var beatmapLevelDataVersion = await _beatmapLevelsEntitlementModel.GetLevelDataVersionAsync(beatmapLevel.levelID, cancellationToken);
                var beatmapLevelDataResult = await _beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapLevel.levelID, beatmapLevelDataVersion, cancellationToken);

                if (beatmapLevelDataResult.isError)
                {
                    Plugin.Log.Error($"Failed to load beatmap level data for {beatmapLevel.levelID}: {beatmapLevelDataResult.errorMessage}");

                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                Plugin.Log.Debug($"Initialized beatmap level cache for {beatmapLevel.levelID}");

                return _beatmapLevelCache.BeatmapLevelData = beatmapLevelDataResult.beatmapLevelData;
            }
            catch (OperationCanceledException)
            {
                Plugin.Log.Debug($"Beatmap level cache initialization was cancelled for {beatmapLevel.levelID}");

                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"An error occurred during beatmap level cache initialization.\n{ex}");

                return null;
            }
        }

        private async Task InvokeEventAsync<TSender, TArgs>(TSender sender, TArgs? eventArgs, Delegate? originalDelegate, CancellationToken cancellationToken)
        {
            if (originalDelegate == null)
            {
                return;
            }

            var baseHandlers = new List<Action<TSender, TArgs?>>();
            var modHandlers = new List<Action<TSender, TArgs?>>();

            foreach (var handler in originalDelegate.GetInvocationList().Cast<Action<TSender, TArgs?>>())
            {
                if (Directory.GetParent(handler.Method.DeclaringType!.Assembly.Location)!.Name == "Managed")
                {
                    baseHandlers.Add(handler);
                }
                else
                {
                    modHandlers.Add(handler);
                }
            }

            foreach (var action in baseHandlers)
            {
                action(sender, eventArgs);
            }

            await _beatmapLevelCache.WaitUntilReadyAsync(cancellationToken);

            foreach (var action in modHandlers)
            {
                action(sender, eventArgs);
            }
        }

        private async void HandleDidSelectLevel(LevelCollectionTableView levelCollectionTableView, BeatmapLevel beatmapLevel)
        {
            Plugin.Log.Trace("LevelCollectionTableView.didSelectLevelEvent");

            var message = $"Selected level: {beatmapLevel.levelID} | {beatmapLevel.songName}";

            if (!beatmapLevel.hasPrecalculatedData)
            {
                message += " | v" + BeatmapSaveDataHelpers.GetVersion(_customLevelLoader._loadedBeatmapSaveData[beatmapLevel.levelID].customLevelFolderInfo.levelInfoJsonString);
            }

            Plugin.Log.Debug(message);

            _beatmapLevelCache.Init(beatmapLevel, InitializeBeatmapLevelCacheAsync);
            _cancellationToken = _beatmapLevelCache.CancellationTokenSource!.Token;

            await InvokeEventAsync(levelCollectionTableView, beatmapLevel, _eventProxyHooks.LevelCollectionTableViewDidSelectLevelDelegate!, _cancellationToken);
        }

        private async void HandleDidSelectLevel(LevelCollectionViewController levelCollectionViewController, BeatmapLevel beatmapLevel)
        {
            Plugin.Log.Trace("LevelCollectionViewController.didSelectLevelEvent");

            Assert.That(beatmapLevel == _beatmapLevelCache.BeatmapLevel);

            await InvokeEventAsync(levelCollectionViewController, beatmapLevel, _eventProxyHooks.LevelCollectionViewControllerDidSelectLevelDelegate!, _cancellationToken);
        }

        private async void HandleDidChangeContent(StandardLevelDetailViewController? standardLevelDetailViewController, StandardLevelDetailViewController.ContentType contentType)
        {
            Plugin.Log.Trace($"StandardLevelDetailViewController.didChangeContentEvent {contentType}");

            if (standardLevelDetailViewController != null)
            {
                Assert.That(standardLevelDetailViewController.beatmapLevel == _beatmapLevelCache.BeatmapLevel);
            }

            await InvokeEventAsync(standardLevelDetailViewController, contentType, _eventProxyHooks.StandardLevelDetailViewControllerDidChangeContentDelegate!, _cancellationToken);
        }

        private Task<string?> LogReadAllTextFromPathAsync(Func<string, CancellationToken, Task<string?>> original, string path, CancellationToken cancellationToken)
        {
            Plugin.Log.Debug($"ReadAllTextFromPathAsync {_beatmapLevelCache.BeatmapKey.ToString()} {path}");
            return original(path, cancellationToken);
        }

        private void ReplaceDidSelectLevelEvent(Action<LevelCollectionViewController, bool, bool, bool> original, LevelCollectionViewController instance, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (!firstActivation)
            {
                original(instance, firstActivation, addedToHierarchy, screenSystemEnabling);
                return;
            }

            ref var didSelectLevelViewControllerEvent = ref Accessors.ViewControllerDidSelectLevelEventAccessor(ref instance);
            didSelectLevelViewControllerEvent = HandleDidSelectLevel;

            ref var didSelectLevelViewEvent = ref Accessors.TableViewDidSelectLevelEventAccessor(ref instance._levelCollectionTableView);
            didSelectLevelViewEvent = HandleDidSelectLevel;

            original(instance, firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        private void ReplaceDidChangeContentEvent(Action<StandardLevelDetailViewController, bool, bool, bool> original, StandardLevelDetailViewController instance, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (!firstActivation)
            {
                original(instance, firstActivation, addedToHierarchy, screenSystemEnabling);
                return;
            }

            ref var didChangeContentEvent = ref Accessors.DidChangeContentEventAccessor(ref instance);
            didChangeContentEvent = HandleDidChangeContent;

            original(instance, firstActivation, addedToHierarchy, screenSystemEnabling);
        }

        private BeatmapKey SetBeatmapLevelCacheBeatmapKey(Func<StandardLevelDetailView, BeatmapKey> original, StandardLevelDetailView instance)
        {
            var result = original(instance);
            SetBeatmapLevelCacheBeatmapKeyAsync(result);
            return result;
        }

        private async void SetBeatmapLevelCacheBeatmapKeyAsync(BeatmapKey beatmapKey)
        {
            Plugin.Log.Debug("Attempting to set beatmap level cache beatmap key");

            var tcs = _beatmapLevelCache.BeatmapKeyTaskCompletionSource!;
            var beatmapLevelData = await _beatmapLevelCache.BeatmapLevelDataLoadingTask!;

            // Task errored, faulted or canceled.
            if (beatmapLevelData == null)
            {
                tcs.SetResult(false);
                return;
            }

            Assert.That(beatmapLevelData == _beatmapLevelCache.BeatmapLevelData);

            if (!_beatmapLevelCache.DifficultyMatches(beatmapKey))
            {
                Plugin.Log.Debug($"Setting beatmap level cache beatmap key to {beatmapKey}");

                _beatmapLevelCache.InvalidateDifficulty();
                _beatmapLevelCache.BeatmapKey = beatmapKey;
            }

            tcs.TrySetResult(true);
        }

        private Task<IReadonlyBeatmapData?> LoadBeatmapDataWithCacheAsync(Func<BeatmapDataLoader, IBeatmapLevelData, BeatmapKey, float, bool, IEnvironmentInfo?, IEnvironmentInfo?, BeatmapLevelDataVersion, GameplayModifiers?, PlayerSpecificSettings?, Task<IReadonlyBeatmapData?>> original, BeatmapDataLoader instance, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey, float startBpm, bool loadingForDesignatedEnvironment, IEnvironmentInfo? targetEnvironmentInfo, IEnvironmentInfo? originalEnvironmentInfo, BeatmapLevelDataVersion beatmapLevelDataVersion, GameplayModifiers? gameplayModifiers, PlayerSpecificSettings? playerSpecificSettings)
        {
            Assert.That(UnityGame.OnMainThread, "This method must be called on the main thread.");

            var request = new BeatmapDataRequest(beatmapLevelData, beatmapKey, startBpm, loadingForDesignatedEnvironment, targetEnvironmentInfo, originalEnvironmentInfo, beatmapLevelDataVersion, gameplayModifiers, playerSpecificSettings);

            if (!_beatmapLevelCache.LevelMatches(beatmapLevelData))
            {
                Plugin.Log.Debug("Level data changed, returning original method");
                return original(instance, beatmapLevelData, beatmapKey, startBpm, loadingForDesignatedEnvironment, targetEnvironmentInfo, originalEnvironmentInfo, beatmapLevelDataVersion, gameplayModifiers, playerSpecificSettings);
            }

            if (_beatmapLevelCache.BeatmapDataRequest?.Equals(request) == true)
            {
                Plugin.Log.Debug("Returning stored beatmap data task");
                return _beatmapLevelCache.BeatmapDataLoadingTask!;
            }

            Plugin.Log.Debug("Starting new beatmap data request");

            _beatmapLevelCache.BeatmapDataRequest = request;
            return _beatmapLevelCache.BeatmapDataLoadingTask = request.Start(original, _beatmapDataLoader);
        }
    }
}
