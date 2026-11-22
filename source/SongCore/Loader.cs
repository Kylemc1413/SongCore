using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatmapDataLoaderVersion4;
using BeatmapLevelSaveDataVersion4;
using BeatmapSaveDataVersion4;
using BeatSaber.Destinations;
using BeatSaberMarkupLanguage.Settings;
using BGLib.JsonExtension;
using IPA.Utilities;
using IPA.Utilities.Async;
using Newtonsoft.Json;
using SongCore.Data;
using SongCore.Hooks.BeatmapLevelCache;
using SongCore.OverrideClasses;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace SongCore
{
    public class Loader : IInitializable, IDisposable, ITickable
    {
        private readonly GameScenesManager _gameScenesManager;
        private readonly LevelFilteringNavigationController _levelFilteringNavigationController;
        private readonly LevelPackDetailViewController _levelPackDetailViewController;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly SpriteAsyncLoader _spriteAsyncLoader;
        private readonly BeatmapCharacteristicCollection _beatmapCharacteristicCollection;
        private readonly ProgressBar _progressBar;
        private readonly BSMLSettings _bsmlSettings;
        private readonly PluginConfig _config;
        private readonly SettingsController _settingsController;
        private readonly string _customWIPPath;
        private readonly string _customLevelsPath;

        private Task? _loadingTask;
        private CancellationTokenSource _loadingTaskCancellationTokenSource = new();

        private Loader(GameScenesManager gameScenesManager, LevelFilteringNavigationController levelFilteringNavigationController, LevelPackDetailViewController levelPackDetailViewController, BeatmapLevelsModel beatmapLevelsModel, CustomLevelLoader customLevelLoader, SpriteAsyncLoader spriteAsyncLoader, BeatmapCharacteristicCollection beatmapCharacteristicCollection, ProgressBar progressBar, PluginConfig config, SettingsController settingsController, BSMLSettings bsmlSettings)

        {
            _gameScenesManager = gameScenesManager;
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelPackDetailViewController = levelPackDetailViewController;
            _beatmapLevelsModel = beatmapLevelsModel;
            _customLevelLoader = customLevelLoader;
            _spriteAsyncLoader = spriteAsyncLoader;
            _beatmapCharacteristicCollection = beatmapCharacteristicCollection;
            _progressBar = progressBar;
            _bsmlSettings = bsmlSettings;
            _config = config;
            _settingsController = settingsController;
            // Path.GetFullPath is needed to normalize directory separators.
            _customWIPPath = Path.GetFullPath(Path.Combine(Application.dataPath, "CustomWIPLevels"));
            _customLevelsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            Instance = this;
        }

        // Actions for loading and refreshing beatmaps
        public static event Action<Loader>? LoadingStartedEvent;
        public static event Action<Loader, ConcurrentDictionary<string, BeatmapLevel>>? SongsLoadedEvent;
        public static event Action? OnLevelPacksRefreshed;

        // Temp collection to avoid concurrency issues with base game dictionary. Also used to store levels on internal restart.
        private static ConcurrentDictionary<string, CustomLevelLoader.LoadedSaveData> LoadedBeatmapSaveData = new ConcurrentDictionary<string, CustomLevelLoader.LoadedSaveData>();
        public static ConcurrentDictionary<string, BeatmapLevel> CustomLevels = new ConcurrentDictionary<string, BeatmapLevel>();
        public static ConcurrentDictionary<string, BeatmapLevel> CustomWIPLevels = new ConcurrentDictionary<string, BeatmapLevel>();
        public static ConcurrentDictionary<string, BeatmapLevel> CachedWIPLevels = new ConcurrentDictionary<string, BeatmapLevel>();
        public static readonly List<SeparateSongFolder> SeparateSongFolders = new List<SeparateSongFolder>();
        public static Sprite defaultCoverImage;
        public static Loader Instance;

        public static SongCoreCustomBeatmapLevelPack? CustomLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack? WIPLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack? CachedWIPLevelsPack { get; private set; }
        public static SongCoreBeatmapLevelsRepository? CustomLevelsRepository { get; private set; }
        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; private set; }
        public static BeatmapLevelsModel BeatmapLevelsModelSO { get; private set; }
        public static CustomLevelLoader CustomLevelLoader { get; private set; }
        public static SpriteAsyncLoader cachedMediaAsyncLoaderSO { get; private set; }
        public static BeatmapCharacteristicCollection beatmapCharacteristicCollection { get; private set; }

        public void Initialize()
        {
            _gameScenesManager.transitionDidFinishEvent += HandleSceneTransitionDidFinish;
            // BSML might fail to find the resource if done in a patched method.
            _bsmlSettings.AddSettingsMenu(nameof(SongCore), "SongCore.UI.settings.bsml", _settingsController);

            SongsLoadedEvent += HandleSongsLoaded;
        }

        public void Dispose()
        {
            _bsmlSettings.RemoveSettingsMenu(_settingsController);

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            _gameScenesManager.transitionDidStartEvent -= HandleSceneTransitionDidStart;

            SongsLoadedEvent -= HandleSongsLoaded;
        }

        /// <summary>
        /// Refresh songs on "R" key, full refresh on "Ctrl"+"R"
        /// </summary>
        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RefreshSongs(Input.GetKey(KeyCode.LeftControl));
            }

            if (Input.GetKeyDown(KeyCode.X) && Input.GetKey(KeyCode.LeftControl) && _loadingTask != null)
            {
                CancelSongLoading();
            }
        }

        private async void HandleSceneTransitionDidFinish(GameScenesManager.SceneTransitionType sceneTransitionType, ScenesTransitionSetupData scenesTransitionSetupData, DiContainer container)
        {
            _gameScenesManager.transitionDidFinishEvent -= HandleSceneTransitionDidFinish;

            // Ensures that the static references are still valid Unity objects.
            // They'll be destroyed on internal restart.
            BeatmapLevelsModelSO = _beatmapLevelsModel;
            CustomLevelLoader = _customLevelLoader;
            cachedMediaAsyncLoaderSO = _spriteAsyncLoader;
            defaultCoverImage = _levelPackDetailViewController._defaultCoverSprite;
            beatmapCharacteristicCollection = _beatmapCharacteristicCollection;

            if (Hashing.cachedSongHashData.IsEmpty)
            {
                await Task.WhenAll(Hashing.LoadCachedSongHashesAsync(), Hashing.LoadCachedAudioDataAsync());
                RefreshSongs();
            }
            else
            {
                RefreshLevelPacks();
            }

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

            _gameScenesManager.transitionDidStartEvent += HandleSceneTransitionDidStart;
        }

        private void HandleSongsLoaded(Loader loader, ConcurrentDictionary<string, BeatmapLevel> customLevels)
        {
            foreach (var (hash, levels) in Collections.HashLevelDictionary)
            {
                if (levels.Count <= 1)
                {
                    continue;
                }

                Plugin.Log.Notice("Found duplicates:");

                foreach (var levelPath in Hashing.cachedSongHashData
                             .Where(x => x.Value.songHash == hash)
                             .Select(x => x.Key))
                {
                    Plugin.Log.Notice($"  {levelPath}");
                }
            }
        }

        private void HandleSceneTransitionDidStart(GameScenesManager.SceneTransitionType sceneTransitionType, float duration)
        {
            CancelSongLoading();
        }

        private void CancelSongLoading()
        {
            if (AreSongsLoading)
            {
                _loadingTaskCancellationTokenSource.Cancel();
                AreSongsLoading = false;
                LoadingProgress = 0;
                _progressBar.ShowMessage("Loading cancelled\n<size=80%>Press R to resume</size>", false);
            }
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (_loadingTaskCancellationTokenSource.IsCancellationRequested && nextScene.name == "MainMenu")
            {
                Plugin.Log.Notice("Song loading was cancelled. Resuming...");
                RefreshSongs(false);
            }
        }

        /// <summary>
        /// This function will add/remove Level Packs from the Custom Levels tab if applicable
        /// </summary>
        public async void RefreshLevelPacks()
        {
            CustomLevelsPack?.UpdateBeatmapLevels([.. CustomLevels.Values]);
            WIPLevelsPack?.UpdateBeatmapLevels([.. CustomWIPLevels.Values]);
            CachedWIPLevelsPack?.UpdateBeatmapLevels([.. CachedWIPLevels.Values]);

            if (CachedWIPLevelsPack != null && CustomLevelsRepository != null)
            {
                if (!CachedWIPLevels.IsEmpty)
                {
                    CustomLevelsRepository.AddLevelPack(CachedWIPLevelsPack);
                }
                else
                {
                    CustomLevelsRepository.RemoveLevelPack(CachedWIPLevelsPack);
                }
            }

            foreach (var folderEntry in SeparateSongFolders)
            {
                if (folderEntry.SongFolderEntry.Pack == FolderLevelPack.NewPack)
                {
                    folderEntry.LevelPack.UpdateBeatmapLevels([.. folderEntry.Levels.Values]);
                    if (CustomLevelsRepository != null && (!folderEntry.Levels.IsEmpty || folderEntry is ModSeparateSongFolder { AlwaysShow: true }))
                    {
                        CustomLevelsRepository.AddLevelPack(folderEntry.LevelPack);
                    }
                }
            }

            foreach (var (levelID, loadedSaveData) in LoadedBeatmapSaveData)
            {
                _customLevelLoader._loadedBeatmapSaveData[levelID] = loadedSaveData;
            }

            LoadedBeatmapSaveData.Clear();

            await UnityGame.SwitchToMainThreadAsync();

            _beatmapLevelsModel.ClearLoadedBeatmapLevelsCaches();
            _beatmapLevelsModel._customLevelsRepository = CustomLevelsRepository;
            _beatmapLevelsModel.LoadAllBeatmapLevelPacks();

            if (!_loadingTaskCancellationTokenSource.IsCancellationRequested && _levelFilteringNavigationController.isActiveAndEnabled)
            {
                _levelFilteringNavigationController.UpdateCustomSongs();
            }

            OnLevelPacksRefreshed?.Invoke();
        }

        public void RefreshSongs(bool fullRefresh = true)
        {
            if (AreSongsLoading || SceneManager.GetActiveScene().name == SceneNames.kGameCoreSceneName)
            {
                return;
            }

            Plugin.Log.Info(fullRefresh ? "Starting full song refresh" : "Starting song refresh");
            AreSongsLoaded = false;
            AreSongsLoading = true;
            LoadingProgress = 0;
            _loadingTaskCancellationTokenSource = new CancellationTokenSource();
            if (LoadingStartedEvent != null)
            {
                try
                {
                    LoadingStartedEvent(this);
                }
                catch (Exception e)
                {
                    Plugin.Log.Error("Some plugin is throwing exception from the LoadingStartedEvent!");
                    Plugin.Log.Error(e);
                }
            }

            RetrieveAllSongs(fullRefresh);
        }

        private async void RetrieveAllSongs(bool fullRefresh)
        {
            var stopwatch = new Stopwatch();

            #region ClearAllDictionaries

            // Clear all beatmap dictionaries on full refresh
            if (fullRefresh)
            {
                CustomLevels.Clear();
                CustomWIPLevels.Clear();
                CachedWIPLevels.Clear();
                Collections.LevelHashDictionary.Clear();
                Collections.HashLevelDictionary.Clear();
                foreach (var folder in SeparateSongFolders)
                {
                    folder.Levels.Clear();
                }
            }

            #endregion

            var foundSongPaths = fullRefresh
                ? new ConcurrentDictionary<string, bool>()
                : new ConcurrentDictionary<string, bool>(Hashing.cachedSongHashData.Keys.ToDictionary(Hashing.GetAbsolutePath, _ => false));

            var job = async () =>
            {
                #region AddCustomBeatmaps

                try
                {
                    #region DirectorySetup

                    if (!Directory.Exists(_customLevelsPath))
                    {
                        Directory.CreateDirectory(_customLevelsPath);
                    }

                    if (!Directory.Exists(_customWIPPath))
                    {
                        Directory.CreateDirectory(_customWIPPath);
                    }

                    #endregion

                    #region CacheZipWIPs

                    // Get zip files in CustomWIPLevels and extract them to Cache folder
                    if (fullRefresh)
                    {
                        try
                        {
                            var cachePath = Path.Combine(_customWIPPath, "Cache");
                            CacheZIPs(cachePath, _customWIPPath);

                            var cacheFolders = Directory.EnumerateDirectories(cachePath);
                            await LoadCachedZIPs(cacheFolders, fullRefresh, CachedWIPLevels);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error("Failed to load cached WIP levels: ");
                            Plugin.Log.Error(ex);
                        }
                    }

                    #endregion

                    #region CacheSeparateZIPs

                    if (fullRefresh)
                    {
                        foreach (var songFolder in SeparateSongFolders)
                        {
                            if (songFolder.SongFolderEntry.CacheZIPs && songFolder.CacheFolder != null)
                            {
                                var cacheFolder = songFolder.CacheFolder;
                                try
                                {
                                    CacheZIPs(cacheFolder.SongFolderEntry.Path, songFolder.SongFolderEntry.Path);
                                }
                                catch (Exception ex)
                                {
                                    Plugin.Log.Error("Failed to load cached WIP levels:");
                                    Plugin.Log.Error(ex);
                                }
                            }
                        }
                    }

                    #endregion

                    stopwatch.Start();

                    #region LoadCustomLevels

                    // Get Levels from CustomLevels and CustomWIPLevels folders
                    var songFolders = new DirectoryInfo(_customLevelsPath).EnumerateDirectories()
                        .Concat(new DirectoryInfo(_customWIPPath).EnumerateDirectories())
                        .Where(d => d.Exists && !CustomLevelPathHelper.IsHiddenDirectory(d))
                        .Select(d => d.FullName)
                        .ToArray();
                    var songFoldersCount = songFolders.Length;
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2 - 1),
                        CancellationToken = _loadingTaskCancellationTokenSource.Token
                    };
                    var processedSongsCount = 0;

                    // Clear removed songs from loaded data, in case they were removed manually.
                    if (_customLevelLoader._loadedBeatmapSaveData.Count > 0)
                    {
                        var folders = songFolders
                            .Concat(SeparateSongFolders
                                .Select(f => Path.GetFullPath(f.SongFolderEntry.Path))
                                .Select(p => new DirectoryInfo(p))
                                .Where(d => d.Exists)
                                .SelectMany(d => d.EnumerateDirectories()
                                    .Where(d => d.Exists && !CustomLevelPathHelper.IsHiddenDirectory(d))
                                    .Select(d => d.FullName)))
                            .ToHashSet();

                        // Need to make a copy of the loaded save data since we might modify the iterated collection.
                        foreach (var loadedSaveData in _customLevelLoader._loadedBeatmapSaveData.Values.ToArray())
                        {
                            if (!folders.Contains(loadedSaveData.customLevelFolderInfo.folderPath))
                            {
                                Plugin.Log.Warn($"Removing {loadedSaveData.customLevelFolderInfo.folderPath} from loaded levels");
                                DeleteSingleSong(loadedSaveData.customLevelFolderInfo.folderPath, false);
                            }
                        }

                        if (!fullRefresh)
                        {
                            StoreLoadedBeatmapSaveData();
                        }
                    }

                    Parallel.ForEach(songFolders, parallelOptions, folder =>
                    {
                        string[] results;
                        try
                        {
                            results = Directory.GetFiles(folder, CustomLevelPathHelper.kStandardLevelInfoFilename);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Warn($"Skipping missing or corrupt folder: '{folder}'");
                            Plugin.Log.Warn(ex);
                            return;
                        }

                        if (results.Length == 0)
                        {
                            Plugin.Log.Warn($"Folder: '{folder}' is missing {CustomLevelPathHelper.kStandardLevelInfoFilename} file!");
                            return;
                        }

                        foreach (var result in results)
                        {
                            try
                            {
                                var songPath = Path.GetDirectoryName(result)!;
                                if (Directory.GetParent(songPath)?.Name == "Backups")
                                {
                                    continue;
                                }

                                if (!fullRefresh && (CustomLevels.ContainsKey(songPath) || CustomWIPLevels.ContainsKey(songPath)))
                                {
                                    continue;
                                }

                                var wip = songPath.Contains("CustomWIPLevels");
                                var customLevel = LoadCustomLevel(songPath);
                                if (!customLevel.HasValue)
                                {
                                    Plugin.Log.Error($"Failed to load custom level: {folder}");
                                    continue;
                                }

                                var (_, level) = customLevel.Value;
                                if (!wip)
                                {
                                    CustomLevels[songPath] = level;
                                }
                                else
                                {
                                    CustomWIPLevels[songPath] = level;
                                }

                                foundSongPaths.TryAdd(songPath, false);
                            }
                            catch (Exception e)
                            {
                                Plugin.Log.Error($"Failed to load song folder: {result}");
                                Plugin.Log.Error(e);
                            }
                        }

                        LoadingProgress = (float)Interlocked.Increment(ref processedSongsCount) / songFoldersCount;
                    });

                    #endregion

                    #region LoadSeparateFolders

                    // Load beatmaps in separate song folders (created in folders.xml or by other mods)
                    // Assign beatmaps to their respective pack (custom levels, wip levels, or separate)
                    await UnityMainThreadTaskScheduler.Factory.StartNew(() => _progressBar.ShowMessage($"Loading {SeparateSongFolders.Count} separate folders", true));
                    foreach (var entry in SeparateSongFolders)
                    {
                        try
                        {
                            if (!Directory.Exists(entry.SongFolderEntry.Path))
                            {
                                continue;
                            }

                            var entryFolders = Directory.GetDirectories(entry.SongFolderEntry.Path);

                            float count = 0;
                            foreach (var folder in entryFolders)
                            {
                                count++;
                                // Search for an info.dat in the beatmap folder
                                string[] results;
                                try
                                {
                                    results = Directory.GetFiles(folder, CustomLevelPathHelper.kStandardLevelInfoFilename);
                                }
                                catch (Exception ex)
                                {
                                    Plugin.Log.Warn($"Skipping missing or corrupt folder: '{folder}'");
                                    Plugin.Log.Warn(ex);
                                    continue;
                                }

                                if (results.Length == 0)
                                {
                                    Plugin.Log.Warn($"Folder: '{folder}' is missing {CustomLevelPathHelper.kStandardLevelInfoFilename} file!");
                                    continue;
                                }

                                foreach (var result in results)
                                {
                                    try
                                    {
                                        // On quick refresh: Check if the beatmap directory is already present in the respective beatmap dictionary
                                        // If it is already present on a non full refresh, it will be ignored (changes to the beatmap will not be applied)
                                        var songPath = Path.GetDirectoryName(result)!;
                                        if (!fullRefresh)
                                        {
                                            switch (entry.SongFolderEntry.Pack)
                                            {
                                                case FolderLevelPack.NewPack when SearchBeatmapInMapPack(entry.Levels, songPath):
                                                case FolderLevelPack.CustomLevels when SearchBeatmapInMapPack(CustomLevels, songPath):
                                                case FolderLevelPack.CustomWIPLevels when SearchBeatmapInMapPack(CustomWIPLevels, songPath):
                                                case FolderLevelPack.CachedWIPLevels when SearchBeatmapInMapPack(CachedWIPLevels, songPath):
                                                    continue;
                                            }
                                        }

                                        if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomLevels || entry.SongFolderEntry is { Pack: FolderLevelPack.NewPack, WIP: false })
                                        {
                                            if (AssignBeatmapToSeparateFolder(CustomLevels, songPath, entry.Levels))
                                            {
                                                continue;
                                            }

                                            if (AssignBeatmapToSeparateFolder(CustomWIPLevels, songPath, entry.Levels))
                                            {
                                                continue;
                                            }

                                            if (AssignBeatmapToSeparateFolder(CachedWIPLevels, songPath, entry.Levels))
                                            {
                                                continue;
                                            }
                                        }

                                        var customLevel = LoadCustomLevel(songPath, entry.SongFolderEntry);
                                        if (!customLevel.HasValue)
                                        {
                                            Plugin.Log.Error($"Failed to load custom level: {folder}");
                                        }
                                        else
                                        {
                                            var (_, level) = customLevel.Value;
                                            entry.Levels[songPath] = level;
                                            foundSongPaths.TryAdd(songPath, false);
                                        }

                                        LoadingProgress = count / entryFolders.Length;
                                    }
                                    catch (Exception e)
                                    {
                                        Plugin.Log.Error($"Failed to load song folder: {result}");
                                        Plugin.Log.Error(e);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error($"Failed to load separate folder {entry.SongFolderEntry.Name}");
                            Plugin.Log.Error(ex);
                        }
                    }

                    #endregion

                    _loadingTaskCancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Plugin.Log.Error("RetrieveAllSongs failed:");
                    Plugin.Log.Error(e);
                }

                #endregion
            };

            var finish = async () =>
            {
                #region CountBeatmapsAndUpdateLevelPacks

                stopwatch.Stop();
                var songCountWSF = CustomLevels.Count + CustomWIPLevels.Count;
                var songCount = songCountWSF + SeparateSongFolders.Sum(f => f.Levels.Count);

                var folderCount = songCount - songCountWSF;
                var songOrSongs = songCount == 1 ? "song" : "songs";
                var folderOrFolders = folderCount == 1 ? "folder" : "folders";
                Plugin.Log.Info($"Loaded {songCount} new {songOrSongs} ({songCountWSF}) in CustomLevels | {folderCount} in separate {folderOrFolders}) in {stopwatch.Elapsed.TotalSeconds} seconds");
                try
                {
                    #region AddSeparateFolderBeatmapsToRespectivePacks

                    foreach (var folderEntry in SeparateSongFolders)
                    {
                        switch (folderEntry.SongFolderEntry.Pack)
                        {
                            case FolderLevelPack.CustomLevels:
                                CustomLevels = new ConcurrentDictionary<string, BeatmapLevel>(CustomLevels.Concat(folderEntry.Levels.Where(x => !CustomLevels.ContainsKey(x.Key)))
                                    .ToDictionary(x => x.Key, x => x.Value));
                                break;
                            case FolderLevelPack.CustomWIPLevels:
                                CustomWIPLevels = new ConcurrentDictionary<string, BeatmapLevel>(CustomWIPLevels
                                    .Concat(folderEntry.Levels.Where(x => !CustomWIPLevels.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value));
                                break;
                            case FolderLevelPack.CachedWIPLevels:
                                CachedWIPLevels = new ConcurrentDictionary<string, BeatmapLevel>(CachedWIPLevels
                                    .Concat(folderEntry.Levels.Where(x => !CachedWIPLevels.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value));
                                break;
                        }
                    }

                    #endregion

                    #region CreateLevelPacks

                    CustomLevelsRepository ??= SongCoreBeatmapLevelsRepository.CreateNew();
                    if (CustomLevelsRepository.beatmapLevelPacks.Count == 0)
                    {
                        // Create level collections and level packs
                        // Add level packs to the custom levels pack collection

                        // This creates unity sprites, so it needs to be on the main thread
                        await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                        {
                            CustomLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + CustomLevelPathHelper.kCustomLevelsDirectoryName, "Custom Levels", defaultCoverImage, CustomLevels.Values.ToArray());
                            WIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomWIPLevels", "WIP Levels", UI.BasicUI.WIPIcon, CustomWIPLevels.Values.ToArray());
                            CachedWIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CachedWIPLevels", "Cached WIP Levels", UI.BasicUI.WIPIcon,
                                CachedWIPLevels.Values.ToArray());
                        });

                        CustomLevelsRepository.ClearLevelPacks();
                        CustomLevelsRepository.AddLevelPack(CustomLevelsPack);
                        CustomLevelsRepository.AddLevelPack(WIPLevelsPack);
                        CustomLevelsRepository.AddLevelPack(CachedWIPLevelsPack);
                    }

                    #endregion

                    RefreshLevelPacks();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error("Failed to setup LevelPacks:");
                    Plugin.Log.Error(ex);
                }

                #endregion

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;
                await UnityMainThreadTaskScheduler.Factory.StartNew(() => SongsLoadedEvent?.Invoke(this, CustomLevels));

                await Task.WhenAll(Hashing.SaveCachedSongHashesAsync(foundSongPaths.Keys), Hashing.SaveCachedAudioDataAsync(foundSongPaths.Keys), Collections.SaveCachedSongDataAsync());
            };

            try
            {
                await (_loadingTask = Task.Run(job, _loadingTaskCancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"Song loading task failed. {ex.Message}");
                return;
            }

            if (_loadingTask.IsCompleted && !_loadingTask.IsCanceled)
            {
                await Task.Run(finish);
            }
            else
            {
                Plugin.Log.Warn("Song loading task cancelled.");
            }
        }

        internal void StoreLoadedBeatmapSaveData()
        {
            LoadedBeatmapSaveData = new ConcurrentDictionary<string, CustomLevelLoader.LoadedSaveData>(_customLevelLoader._loadedBeatmapSaveData);
        }

        /// <summary>
        /// Delete a beatmap (is only used by other mods)
        /// </summary>
        /// <param name="folderPath">Directory of the beatmap</param>
        /// <param name="deleteFolder">Option to delete the base folder of the beatmap</param>
        public void DeleteSong(string folderPath, bool deleteFolder = true)
        {
            DeleteSingleSong(folderPath, deleteFolder);
            RefreshLevelPacks();
        }

        /// <summary>
        /// Delete multiple beatmaps in bulk (is only used by other mods)
        /// </summary>
        /// <param name="folderPaths">Directories of the beatmaps</param>
        /// <param name="deleteFolder">Option to delete the base folder of the beatmap</param>
        public async Task DeleteSongsAsync(List<string> folderPaths, bool deleteFolder = true)
        {
            foreach (var folderPath in folderPaths)
            {
                await Task.Run(() => DeleteSingleSong(folderPath, deleteFolder));
            }

            RefreshLevelPacks();
        }

        private void DeleteSingleSong(string folderPath, bool deleteFolder)
        {
            if (folderPath.EndsWith("(Built in)", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Cannot delete built-in levels.");
            }

            //Remove the level from SongCore Collections
            try
            {
                if (!(CustomLevels.TryRemove(folderPath, out var level) || CustomWIPLevels.TryRemove(folderPath, out level) || CachedWIPLevels.TryRemove(folderPath, out level)))
                {
                    foreach (var folderEntry in SeparateSongFolders)
                    {
                        folderEntry.Levels.TryRemove(folderPath, out level);
                    }
                }

                if (level != null)
                {
                    if (Collections.LevelHashDictionary.TryRemove(level.levelID, out var hash))
                    {
                        Collections.CustomSongsData.TryRemove(level.levelID, out _);
                        if (Collections.HashLevelDictionary.TryGetValue(hash, out var levels))
                        {
                            levels.Remove(level.levelID);
                            if (levels.Count == 0)
                            {
                                Collections.HashLevelDictionary.TryRemove(hash, out _);
                            }
                        }
                    }

                    _customLevelLoader._loadedBeatmapSaveData.Remove(level.levelID);
                }

                //Delete the directory
                if (deleteFolder)
                {
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Exception trying to delete song: {folderPath}");
                Plugin.Log.Error(ex);
            }
        }

        /// <summary>
        /// Load a beatmap, gather all beatmap information and create beatmap preview
        /// </summary>
        /// <param name="loadedSaveData">Loaded save data of beatmap</param>
        /// <param name="hash">Resulting hash for the beatmap, may contain beatmap folder name or 'WIP' at the end</param>
        /// <param name="folderEntry">Folder entry for beatmap folder</param>
        /// <returns></returns>
        private (string hash, BeatmapLevel beatmapLevel)? LoadSong(CustomLevelLoader.LoadedSaveData loadedSaveData, SongFolderEntry? folderEntry = null)
        {
            var wip = loadedSaveData.customLevelFolderInfo.folderPath.Contains("CustomWIPLevels") || folderEntry != null && (folderEntry.Pack == FolderLevelPack.CustomWIPLevels || folderEntry.Pack == FolderLevelPack.CachedWIPLevels || folderEntry.WIP);
            string hash;
            BeatmapLevel? beatmapLevel;
            try
            {
                if (loadedSaveData.standardLevelInfoSaveData != null)
                {
                    hash = Hashing.ComputeCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
                    beatmapLevel = _customLevelLoader.CreateBeatmapLevelFromV3(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
                }
                else if (loadedSaveData.beatmapLevelSaveData != null)
                {
                    hash = Hashing.ComputeCustomLevelHash(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
                    beatmapLevel = _customLevelLoader.CreateBeatmapLevelFromV4(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
                }
                else
                {
                    throw new InvalidOperationException("Level save data is missing.");
                }

                var levelID = CustomLevelLoader.kCustomLevelPrefixId + hash;
                var folderName = new DirectoryInfo(loadedSaveData.customLevelFolderInfo.folderPath).Name;
                while (!Collections.LevelHashDictionary.TryAdd(levelID + (wip ? " WIP" : ""), hash))
                {
                    levelID += $"_{folderName}";
                }

                if (wip)
                {
                    levelID += " WIP";
                }

                Collections.HashLevelDictionary.AddOrUpdate(hash, new List<string> { levelID }, (_, levels) =>
                {
                    lock (levels)
                    {
                        levels.Add(levelID);
                    }
                    return levels;
                });
                BlacklistLevelFiles(loadedSaveData);
                Collections.CreateCustomLevelSongData(levelID, loadedSaveData);
                LoadedBeatmapSaveData.TryAdd(levelID, loadedSaveData);

                Accessors.LevelIDAccessor(ref beatmapLevel) = levelID;
                GetSongDuration(loadedSaveData, beatmapLevel);
            }
            catch (Exception e)
            {
                Plugin.Log.Error($"Failed to load song: {loadedSaveData.customLevelFolderInfo.folderPath}");
                Plugin.Log.Error(e);
                return null;
            }

            return (hash, beatmapLevel);
        }

        private static void BlacklistLevelFiles(CustomLevelLoader.LoadedSaveData loadedSaveData)
        {
            const string reason = "Use IBeatmapLevelData to read level difficulty and audio data. Info.dat data can be found in CustomLevelLoader._loadedBeatmapSaveData";

            if (!string.IsNullOrEmpty(loadedSaveData.beatmapLevelSaveData?.audio.audioDataFilename))
            {
                IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, loadedSaveData.beatmapLevelSaveData!.audio.audioDataFilename)), reason);
            }

            foreach (var difficultyBeatmap in loadedSaveData.standardLevelInfoSaveData?.difficultyBeatmapSets.SelectMany(x => x.difficultyBeatmaps) ?? [])
            {
                if (!string.IsNullOrEmpty(difficultyBeatmap.beatmapFilename))
                {
                    IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapFilename)), reason);
                }
            }

            foreach (var difficultyBeatmap in loadedSaveData.beatmapLevelSaveData?.difficultyBeatmaps ?? [])
            {
                if (!string.IsNullOrEmpty(difficultyBeatmap.beatmapDataFilename))
                {
                    IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapDataFilename)), reason);
                }

                if (!string.IsNullOrEmpty(difficultyBeatmap.lightshowDataFilename))
                {
                    IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, difficultyBeatmap.lightshowDataFilename)), reason);
                }
            }
        }

        #region HelperFunctionsZIP

        /// <summary>
        /// Extracts beatmap ZIP files to the cache folder
        /// </summary>
        /// <param name="cachePath">Directory of cache folder</param>
        /// <param name="songFolderPath">Directory of folder containing the zips</param>
        private static void CacheZIPs(string cachePath, string songFolderPath)
        {
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            var cache = new DirectoryInfo(cachePath);
            foreach (var file in cache.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (var folder in cache.EnumerateDirectories())
            {
                folder.Delete(true);
            }

            var zips = Directory.EnumerateFiles(songFolderPath, "*.zip");

            foreach (var zip in zips)
            {
                try
                {
                    using var fileStream = File.OpenRead(zip);
                    using var archive = new ZipArchive(fileStream);
                    archive.ExtractToDirectory(Path.Combine(cachePath, Path.GetFileName(zip)));
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"Failed to extract zip: {zip}:");
                    Plugin.Log.Warn(ex);
                }
            }
        }

        /// <summary>
        /// Loads the beatmaps of the cached
        /// </summary>
        /// <param name="cacheFolders">Directory of cache folder</param>
        /// <param name="fullRefresh"></param>
        /// <param name="beatmapDictionary"></param>
        /// <param name="folderEntry"></param>
        private Task LoadCachedZIPs(IEnumerable<string> cacheFolders, bool fullRefresh, ConcurrentDictionary<string, BeatmapLevel> beatmapDictionary, SongFolderEntry? folderEntry = null)
        {
            var tasks = new List<Task>();
            foreach (var cachedFolder in cacheFolders)
            {
                string[] results;
                try
                {
                    results = Directory.GetFiles(cachedFolder, CustomLevelPathHelper.kStandardLevelInfoFilename);
                }
                catch (DirectoryNotFoundException)
                {
                    Plugin.Log.Warn($"Skipping missing or corrupt folder: '{cachedFolder}'");
                    continue;
                }

                if (results.Length == 0)
                {
                    Plugin.Log.Warn($"Folder: '{cachedFolder}' is missing {CustomLevelPathHelper.kStandardLevelInfoFilename} files!");
                    continue;
                }

                foreach (var result in results)
                {
                    try
                    {
                        var songPath = Path.GetDirectoryName(result)!;
                        if (!fullRefresh)
                        {
                            if (SearchBeatmapInMapPack(beatmapDictionary, songPath))
                            {
                                continue;
                            }
                        }

                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                var customLevel = LoadCustomLevel(songPath, folderEntry);
                                if (!customLevel.HasValue)
                                {
                                    Plugin.Log.Error($"Failed to load custom level: {folderEntry}");
                                    return;
                                }

                                var (_, level) = customLevel.Value;
                                beatmapDictionary[songPath] = level;
                            }
                            catch (Exception ex)
                            {
                                Plugin.Log.Error($"Failed to load song from {cachedFolder}:");
                                Plugin.Log.Error(ex);
                            }
                        }, _loadingTaskCancellationTokenSource.Token));
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Error($"Failed to load song from {cachedFolder}:");
                        Plugin.Log.Error(ex);
                    }
                }
            }

            return Task.WhenAll(tasks);
        }

        #endregion

        #region HelperFunctionsLoading

        private bool SearchBeatmapInMapPack(ConcurrentDictionary<string, BeatmapLevel> mapPack, string songPath)
        {
            if (mapPack.TryGetValue(songPath, out var beatmapLevel) && beatmapLevel != null)
            {
                return true;
            }

            return false;
        }

        private bool AssignBeatmapToSeparateFolder(
            ConcurrentDictionary<string, BeatmapLevel> mapPack,
            string songPath,
            ConcurrentDictionary<string, BeatmapLevel> separateFolder)
        {
            if (mapPack.TryGetValue(songPath, out var beatmapLevel) && beatmapLevel != null)
            {
                separateFolder[songPath] = beatmapLevel;
                return true;
            }

            return false;
        }

        public static (string hash, BeatmapLevel beatmapLevel)? LoadCustomLevel(string customLevelPath, SongFolderEntry? entry = null)
        {
            return Instance.LoadCustomLevelInternal(customLevelPath, entry);
        }

        private (string hash, BeatmapLevel beatmapLevel)? LoadCustomLevelInternal(string customLevelPath, SongFolderEntry? entry = null)
        {
            var infoFilePath = Path.Combine(customLevelPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            if (!File.Exists(infoFilePath))
            {
                return null;
            }

            CustomLevelLoader.LoadedSaveData loadedSaveData;
            var directoryInfo = new DirectoryInfo(customLevelPath);
            string json;

            IOBlacklistHook.AllowIO.Value = true;
            try
            {
                json = File.ReadAllText(infoFilePath);
            }
            finally
            {
                IOBlacklistHook.AllowIO.Value = false;
            }

            var version = BeatmapSaveDataHelpers.GetVersion(json);
            if (version < BeatmapSaveDataHelpers.version4)
            {
                var standardLevelInfoSaveData = StandardLevelInfoSaveData.DeserializeFromJSONString(json);
                if (standardLevelInfoSaveData == null || standardLevelInfoSaveData.difficultyBeatmapSets.Length == 0)
                {
                    return null;
                }

                var customLevelFolderInfo = new CustomLevelFolderInfo(directoryInfo.FullName, directoryInfo.Name, json);
                if (!CustomLevelLoader.CheckIfAudioExists(customLevelFolderInfo, standardLevelInfoSaveData.songFilename))
                {
                    return null;
                }

                loadedSaveData = new CustomLevelLoader.LoadedSaveData { customLevelFolderInfo = customLevelFolderInfo, standardLevelInfoSaveData = standardLevelInfoSaveData };
            }
            else
            {
                var beatmapLevelSaveData = JsonConvert.DeserializeObject<BeatmapLevelSaveData>(json, JsonSettings.readableWithDefault);
                if (beatmapLevelSaveData == null || beatmapLevelSaveData.difficultyBeatmaps.Length == 0)
                {
                    return null;
                }

                var customLevelFolderInfo = new CustomLevelFolderInfo(directoryInfo.FullName, directoryInfo.Name, json);
                if (!CustomLevelLoader.CheckIfAudioExists(customLevelFolderInfo, beatmapLevelSaveData.audio.songFilename))
                {
                    return null;
                }

                BeatmapLevelSaveDataUtils.MigrateBeatmapLevelSaveData(beatmapLevelSaveData);
                loadedSaveData = new CustomLevelLoader.LoadedSaveData { customLevelFolderInfo = customLevelFolderInfo, beatmapLevelSaveData = beatmapLevelSaveData };
            }

            return LoadSong(loadedSaveData, entry);
        }

        #endregion

        #region HelperFunctionsSearching

        /// <summary>
        /// Attempts to get a beatmap by LevelId. Returns null a matching level isn't found.
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        public static BeatmapLevel? GetLevelById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            return BeatmapLevelsModelSO.GetBeatmapLevel(levelId);
        }

        /// <summary>
        /// Attempts to get a custom level by hash (case-insensitive). Returns null a matching custom level isn't found.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static BeatmapLevel? GetLevelByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return null;
            }

            return BeatmapLevelsModelSO.GetBeatmapLevel(CustomLevelLoader.kCustomLevelPrefixId + hash, true);
        }

        private void GetSongDuration(CustomLevelLoader.LoadedSaveData loadedSaveData, BeatmapLevel beatmapLevel)
        {
            try
            {
                var levelID = beatmapLevel.levelID;
                float length = 0;
                Hashing.TryGetRelativePath(loadedSaveData.customLevelFolderInfo.folderPath, out var relativePath);
                if (Hashing.cachedAudioData.TryGetValue(relativePath, out var data))
                {
                    if (data.id == levelID)
                    {
                        length = data.duration;
                    }
                }

                if (length == 0)
                {
                    IOBlacklistHook.AllowIO.Value = true;
                    try
                    {
                        if (loadedSaveData.standardLevelInfoSaveData != null)
                        {
                            length = GetLengthFromOgg(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, loadedSaveData.standardLevelInfoSaveData.songFilename));
                        }
                        else if (loadedSaveData.beatmapLevelSaveData != null)
                        {
                            length = GetLengthFromOgg(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, loadedSaveData.beatmapLevelSaveData.audio.songFilename));
                        }
                    }
                    catch (Exception)
                    {
                        length = -1;
                    }
                    finally
                    {
                        IOBlacklistHook.AllowIO.Value = false;
                    }

                    if (length <= 1)
                    {
                        // janky, but whatever
                        Plugin.Log.Warn($"Failed to parse song length from audio file, approximating using map length. Song: {loadedSaveData.customLevelFolderInfo.folderPath}");

                        IBeatmapLevelData beatmapLevelData = null!;

                        if (loadedSaveData.standardLevelInfoSaveData != null)
                        {
                            beatmapLevelData = _customLevelLoader.CreateBeatmapLevelDataFromV3(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
                        }
                        else if (loadedSaveData.beatmapLevelSaveData != null)
                        {
                            beatmapLevelData = _customLevelLoader.CreateBeatmapLevelDataFromV4(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
                        }

                        length = GetLengthFromMap(loadedSaveData, beatmapLevelData, beatmapLevel);
                    }
                }

                if (data != null)
                {
                    data.duration = length;
                    data.id = levelID;
                }
                else
                {
                    Hashing.cachedAudioData[relativePath] = new AudioCacheData(levelID, length);
                }

                if (_config.ForceLongPreviews)
                {
                    Accessors.PreviewDurationAccessor(ref beatmapLevel) = Mathf.Max(beatmapLevel.previewDuration, length - beatmapLevel.previewStartTime);
                }

                Accessors.SongDurationAccessor(ref beatmapLevel) = length;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn("Failed to parse song duration");
                Plugin.Log.Warn(ex);
            }
        }

        public static float GetLengthFromMap(CustomLevelLoader.LoadedSaveData loadedSaveData, IBeatmapLevelData beatmapLevelData, BeatmapLevel level)
        {
            float length = 0;
            var fileSystemBeatmapLevelData = (FileSystemBeatmapLevelData)beatmapLevelData;

            if (loadedSaveData.standardLevelInfoSaveData != null)
            {
                var beatmapKey = level.GetBeatmapKeys().Last();
                string? json;

                IOBlacklistHook.AllowIO.Value = true;
                try
                {
                    json = OriginalMethods.FileSystemBeatmapLevelData.GetBeatmapString(fileSystemBeatmapLevelData, beatmapKey);
                }
                finally
                {
                    IOBlacklistHook.AllowIO.Value = false;
                }

                var beatmapSaveData = JsonUtility.FromJson<BeatmapSaveDataVersion3.BeatmapSaveData>(json);

                float highestTime = 0;
                if (beatmapSaveData.colorNotes.Count > 0)
                {
                    highestTime = beatmapSaveData.colorNotes.Max(x => x.beat);
                }
                else if (beatmapSaveData.basicBeatmapEvents.Count > 0)
                {
                    highestTime = beatmapSaveData.basicBeatmapEvents.Max(x => x.beat);
                }

                var bpmTimeProcessor = new BpmTimeProcessor(level.beatsPerMinute, beatmapSaveData.bpmEvents);
                length = bpmTimeProcessor.ConvertBeatToTime(highestTime);
            }
            else if (loadedSaveData.beatmapLevelSaveData != null)
            {
                var beatmapKey = level.GetBeatmapKeys().Last();
                string? beatmapJson;
                string? lightShowJson;
                string? audioJson;

                IOBlacklistHook.AllowIO.Value = true;
                try
                {
                    audioJson = OriginalMethods.FileSystemBeatmapLevelData.GetAudioDataString(fileSystemBeatmapLevelData);
                    beatmapJson = OriginalMethods.FileSystemBeatmapLevelData.GetBeatmapString(fileSystemBeatmapLevelData, beatmapKey);
                    lightShowJson = OriginalMethods.FileSystemBeatmapLevelData.GetLightshowString(fileSystemBeatmapLevelData, beatmapKey);
                }
                finally
                {
                    IOBlacklistHook.AllowIO.Value = false;
                }

                var beatmapSaveData = JsonUtility.FromJson<BeatmapSaveData>(beatmapJson);
                var lightshowSaveData = JsonUtility.FromJson<LightshowSaveData>(lightShowJson);
                var audioSaveData = JsonUtility.FromJson<AudioSaveData>(audioJson);

                float highestTime = 0;
                if (beatmapSaveData.colorNotes.Length > 0)
                {
                    highestTime = beatmapSaveData.colorNotes.Max(x => x.beat);
                }
                else if (lightshowSaveData.basicEvents.Length > 0)
                {
                    highestTime = lightshowSaveData.basicEvents.Max(x => x.beat);
                }

                var bpmTimeProcessor = new BpmTimeProcessor(audioSaveData);
                length = bpmTimeProcessor.ConvertBeatToTime(highestTime);
            }

            if (length == 0)
            {
                Plugin.Log.Warn($"Failed to parse song length from audio file. Song: {loadedSaveData.customLevelFolderInfo.folderPath}");
            }

            return length;
        }

        private static readonly byte[] oggBytes =
        {
            0x4F,
            0x67,
            0x67,
            0x53,
            0x00,
            0x04
        };

        public static float GetLengthFromOgg(string oggFile)
        {
            using var fs = File.OpenRead(oggFile);
            using var br = new BinaryReader(fs, Encoding.ASCII);

            /*
                 * Tries to find the array of bytes from the stream
                 */
            bool FindBytes(byte[] bytes, int searchLength)
            {
                for (var i = 0; i < searchLength; i++)
                {
                    var b = br.ReadByte();
                    if (b != bytes[0])
                    {
                        continue;
                    }

                    var by = br.ReadBytes(bytes.Length - 1);
                    // hardcoded 6 bytes compare, is fine because all inputs used are 6 bytes
                    // bitwise AND the last byte to read only the flag bit for lastSample searching
                    // shouldn't cause issues finding rate, hopefully
                    if (by[0] == bytes[1] && by[1] == bytes[2] && by[2] == bytes[3] && by[3] == bytes[4] && (by[4] & bytes[5]) == bytes[5])
                    {
                        return true;
                    }

                    var index = Array.IndexOf(by, bytes[0]);
                    if (index != -1)
                    {
                        fs.Position += index - (bytes.Length - 1);
                        i += index;
                    }
                    else
                    {
                        i += (bytes.Length - 1);
                    }
                }

                return false;
            }

            var rate = -1;
            long lastSample = -1;

            //Skip Capture Pattern
            fs.Position = 24;

            //{0x76, 0x6F, 0x72, 0x62, 0x69, 0x73} = "vorbis" in byte values
            var foundVorbis = FindBytes(new byte[]
            {
                0x76,
                0x6F,
                0x72,
                0x62,
                0x69,
                0x73
            }, 256);
            if (foundVorbis)
            {
                fs.Position += 5;
                rate = br.ReadInt32();
            }
            else
            {
                Plugin.Log.Warn($"Could not find rate for {oggFile}");
                return -1;
            }

            /*
                 * this finds the last occurrence of OggS in the file by checking for a bit flag (0x04)
                 * reads in blocks determined by seekBlockSize
                 * 6144 does not add significant overhead and speeds up the search significantly
                 */
            const int seekBlockSize = 6144;
            const int seekTries = 10; // 60 KiB should be enough for any sane ogg file
            for (var i = 0; i < seekTries; i++)
            {
                var seekPos = (i + 1) * seekBlockSize * -1;
                var overshoot = Math.Max((int) (-seekPos - fs.Length), 0);
                if (overshoot >= seekBlockSize)
                {
                    break;
                }

                fs.Seek(seekPos + overshoot, SeekOrigin.End);
                var foundOggS = FindBytes(oggBytes, seekBlockSize - overshoot);
                if (foundOggS)
                {
                    lastSample = br.ReadInt64();
                    break;
                }
            }

            if (lastSample == -1)
            {
                Plugin.Log.Warn($"Could not find lastSample for {oggFile}");
                return -1;
            }

            var length = lastSample / (float) rate;
            return length;
        }

        #endregion
    }
}
