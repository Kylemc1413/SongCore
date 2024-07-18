using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SiraUtil.Affinity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SongCore.HarmonyPatches
{
    /// <summary>
    /// The game isn't destroying covers when it's done with them, meaning they all stay in memory.
    /// This implementation uses base game reference cache and destroys them when unused.
    /// </summary>
    // TODO: Remove when fixed.
    internal class FixSpriteAsyncLoaderLeakPatch : IAffinity
    {
        private readonly SpriteAsyncLoaderFixed _spriteAsyncLoader;
        // This shouldn't need to be a ConcurrentQueue but mscorlib conflicts.
        private readonly ConcurrentQueue<string> _spriteQueue = new();
        private readonly SemaphoreSlim _semaphore = new(1);

        private string? _currentCoverSpritePath;

        private FixSpriteAsyncLoaderLeakPatch(SpriteAsyncLoaderFixed spriteAsyncLoader)
        {
            _spriteAsyncLoader = spriteAsyncLoader;
        }

        [AffinityPatch(typeof(SpriteAsyncLoader), nameof(SpriteAsyncLoader.LoadSpriteAsync))]
        [AffinityPrefix]
        private bool FixSpriteLoading(ref Task<Sprite> __result, string path, CancellationToken cancellationToken)
        {
            __result = LoadSpriteAsync(path, cancellationToken);

            return false;
        }

        private async Task<Sprite> LoadSpriteAsync(string path, CancellationToken cancellationToken)
        {
            while (_spriteQueue.Count > 100 && _spriteQueue.TryDequeue(out var spriteFilePath))
            {
                await _semaphore.WaitAsync(cancellationToken);

                try
                {
                    if (spriteFilePath != _currentCoverSpritePath || _spriteAsyncLoader.GetReferenceCount(spriteFilePath) > 1)
                    {
                        await _spriteAsyncLoader.Unload(spriteFilePath);
                    }
                    else
                    {
                        _spriteQueue.Enqueue(spriteFilePath);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            _spriteQueue.Enqueue(path);

            return await _spriteAsyncLoader.Load(path, cancellationToken);
        }

        [AffinityPatch(typeof(SpriteAsyncLoader), nameof(SpriteAsyncLoader.ClearCache))]
        private void Clear()
        {
            _spriteAsyncLoader.ClearCache(_spriteQueue);
        }

        [AffinityPatch(typeof(LevelBar), nameof(LevelBar.SetupData))]
        [AffinityPrefix]
        private void GetCurrentLevelSprite(BeatmapLevel beatmapLevel)
        {
            if (beatmapLevel.previewMediaData is FileSystemPreviewMediaData fileSystemPreviewMediaData)
            {
                _currentCoverSpritePath = fileSystemPreviewMediaData._coverSpritePath;
            }
        }
    }

    internal class SpriteReferenceCountingCache : ReferenceCountingCache<int, Task<Sprite>>;

    // This is more or less a copy-paste of existing base game code.
    internal class SpriteAsyncLoaderFixed
    {
        private readonly IReferenceCountingCache<int, Task<Sprite>> _cache;

        private SpriteAsyncLoaderFixed(IReferenceCountingCache<int, Task<Sprite>> cache)
        {
            _cache = cache;
        }

        public Task<Sprite> Load(string spriteFilePath, CancellationToken cancellationToken)
        {
            return Load(GetCacheKey(spriteFilePath), () => MediaAsyncLoader.LoadSpriteAsync(spriteFilePath, cancellationToken));
        }

        private Task<Sprite> Load(int cacheKey, LoadMethodDelegate loadMethodDelegate)
        {
            if (_cache.TryGet(cacheKey, out var task))
            {
                _cache.AddReference(cacheKey);
                return task;
            }

            var loadTask = loadMethodDelegate();
            _ = loadTask.ContinueWith(t =>
            {
                if (t.Result != null)
                {
                    _cache.Insert(cacheKey, t);
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

            return loadTask;
        }

        public Task Unload(string spriteFilePath)
        {
            return Unload(GetCacheKey(spriteFilePath), sprite =>
            {
                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            });
        }

        private async Task Unload(int cacheKey, Action<Sprite> onDelete)
        {
            if (_cache.TryGet(cacheKey, out var task) && _cache.RemoveReference(cacheKey) == 0)
            {
                onDelete(await task);
            }
        }

        public void ClearCache(ConcurrentQueue<string> spriteFilePaths)
        {
            foreach (var spriteFilePath in spriteFilePaths)
            {
                while (_cache.RemoveReference(GetCacheKey(spriteFilePath)) != 0)
                {
                    // TODO: Is there a saner way to do this?
                    Unload(spriteFilePath).GetAwaiter().GetResult();
                }
            }

            spriteFilePaths.Clear();
        }

        private int GetCacheKey(string spriteFilePath)
        {
            return spriteFilePath.GetHashCode();
        }

        public int GetReferenceCount(string spriteFilePath)
        {
            return _cache.GetReferenceCount(GetCacheKey(spriteFilePath));
        }

        private delegate Task<Sprite> LoadMethodDelegate();
    }
}
