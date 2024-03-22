using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Settings;
using IPA.Utilities.Async;
using SongCore.Data;
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
        private readonly LevelCollectionViewController _levelCollectionViewController;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly CachedMediaAsyncLoader _cachedMediaAsyncLoader;
        private readonly BeatmapCharacteristicCollection _beatmapCharacteristicCollection;
        private readonly ProgressBar _progressBar;
        private readonly BSMLSettings _bsmlSettings;
        private readonly SCSettingsController _settingsController;
        private readonly string _customWIPPath;
        private readonly string _customLevelsPath;

        private Task? _loadingTask;
        private CancellationTokenSource _loadingTaskCancellationTokenSource = new CancellationTokenSource();

        private Loader(GameScenesManager gameScenesManager, LevelFilteringNavigationController levelFilteringNavigationController, LevelCollectionViewController levelCollectionViewController, BeatmapLevelsModel beatmapLevelsModel, CustomLevelLoader customLevelLoader, CachedMediaAsyncLoader cachedMediaAsyncLoader,  BeatmapCharacteristicCollection beatmapCharacteristicCollection, ProgressBar progressBar, BSMLSettings bsmlSettings)

        {
            _gameScenesManager = gameScenesManager;
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelCollectionViewController = levelCollectionViewController;
            _beatmapLevelsModel = beatmapLevelsModel;
            _customLevelLoader = customLevelLoader;
            _cachedMediaAsyncLoader = cachedMediaAsyncLoader;
            _beatmapCharacteristicCollection = beatmapCharacteristicCollection;
            _progressBar = progressBar;
            _bsmlSettings = bsmlSettings;
            _settingsController = new SCSettingsController();
            _customWIPPath = Path.Combine(Application.dataPath, "CustomWIPLevels");
            _customLevelsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            Instance = this;
        }

        // Actions for loading and refreshing beatmaps
        public static event Action<Loader>? LoadingStartedEvent;
        public static event Action<Loader, ConcurrentDictionary<string, BeatmapLevel>>? SongsLoadedEvent;
        public static event Action? OnLevelPacksRefreshed;
        public static event Action? DeletingSong;

        private static readonly ConcurrentDictionary<string, OfficialSongEntry> OfficialSongs = new ConcurrentDictionary<string, OfficialSongEntry>();
        private static readonly ConcurrentDictionary<string, BeatmapLevel> CustomLevelsById = new ConcurrentDictionary<string, BeatmapLevel>();
        private static readonly ConcurrentDictionary<string, IBeatmapLevelData> LoadedBeatmapLevelsData = new ConcurrentDictionary<string, IBeatmapLevelData>();
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
        public static CachedMediaAsyncLoader cachedMediaAsyncLoaderSO { get; private set; }
        public static BeatmapCharacteristicCollection beatmapCharacteristicCollection { get; private set; }

        public void Initialize()
        {
            _gameScenesManager.transitionDidFinishEvent += MenuLoaded;
        }

        private async void MenuLoaded(ScenesTransitionSetupDataSO scenesTransitionSetupData, DiContainer container)
        {
            _gameScenesManager.transitionDidFinishEvent -= MenuLoaded;

            // Ensures that the static references are still valid Unity objects.
            // They'll be destroyed on internal restart.
            BeatmapLevelsModelSO = _beatmapLevelsModel;
            CustomLevelLoader = _customLevelLoader;
            cachedMediaAsyncLoaderSO = _cachedMediaAsyncLoader;
            defaultCoverImage = Resources.FindObjectsOfTypeAll<Sprite>().First(s => s.name.Contains("CustomLevelsPack"));
            beatmapCharacteristicCollection = _beatmapCharacteristicCollection;

            if (Hashing.cachedSongHashData.Count == 0)
            {
                var cancellationToken = _loadingTaskCancellationTokenSource.Token;
                await Hashing.ReadCachedSongHashesAsync(cancellationToken);
                await Hashing.ReadCachedAudioDataAsync(cancellationToken);
                RefreshSongs();
            }
            else
            {
                RefreshLevelPacks();
                RefreshLoadedBeatmapLevelsData();
            }

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

            _gameScenesManager.transitionDidStartEvent += CancelSongLoading;
            _levelCollectionViewController.didSelectLevelEvent += HandleDidSelectLevel;
            _bsmlSettings.AddSettingsMenu(nameof(SongCore), "SongCore.UI.settings.bsml", _settingsController);
        }

        public void Dispose()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            _gameScenesManager.transitionDidStartEvent -= CancelSongLoading;
            _levelCollectionViewController.didSelectLevelEvent -= HandleDidSelectLevel;
            _bsmlSettings.RemoveSettingsMenu(_settingsController);
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

        private void CancelSongLoading(float minDuration)
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
                _progressBar.ShowMessage("Loading cancelled\n<size=80%>Press Ctrl+R to refresh</size>");
            }
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (_loadingTaskCancellationTokenSource.IsCancellationRequested && nextScene.name == "MainMenu")
            {
                Logging.Logger.Notice("Song loading was cancelled. Resuming...");
                RefreshSongs();
            }
        }

        private static void HandleDidSelectLevel(LevelCollectionViewController levelCollectionViewController, BeatmapLevel beatmapLevel)
        {
            Logging.Logger.Debug($"Selected level: {beatmapLevel.levelID} | {beatmapLevel.songName}");

            if (!beatmapLevel.hasPrecalculatedData && Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(beatmapLevel)) is { } songData)
            {
                if (Plugin.Configuration.CustomSongPlatforms && !string.IsNullOrWhiteSpace(songData._customEnvironmentName))
                {
                    Logging.Logger.Debug("Custom song with platform selected");
                    Plugin.CustomSongPlatformSelectionDidChange?.Invoke(true, songData._customEnvironmentName, songData._customEnvironmentHash, beatmapLevel);
                }
                else
                {
                    Plugin.CustomSongPlatformSelectionDidChange?.Invoke(false, songData._customEnvironmentName, songData._customEnvironmentHash, beatmapLevel);
                }
            }
        }

        /// <summary>
        /// This function will add/remove Level Packs from the Custom Levels tab if applicable
        /// </summary>
        public async void RefreshLevelPacks()
        {
            CustomLevelsPack?.UpdateBeatmapLevels(CustomLevels.Values.OrderBy(l => l.songName).ToArray());
            WIPLevelsPack?.UpdateBeatmapLevels(CustomWIPLevels.Values.OrderBy(l => l.songName).ToArray());
            CachedWIPLevelsPack?.UpdateBeatmapLevels(CachedWIPLevels.Values.OrderBy(l => l.songName).ToArray());

            if (CachedWIPLevelsPack != null && CustomLevelsRepository != null)
            {
                if (CachedWIPLevels.Count > 0)
                {
                    CustomLevelsRepository.AddLevelPack(CachedWIPLevelsPack);
                }
                else if (CachedWIPLevels.Count == 0)
                {
                    CustomLevelsRepository.RemoveLevelPack(CachedWIPLevelsPack);
                }
            }

            foreach (var folderEntry in SeparateSongFolders)
            {
                if (folderEntry.SongFolderEntry.Pack == FolderLevelPack.NewPack)
                {
                    folderEntry.LevelPack.UpdateBeatmapLevels(folderEntry.Levels.Values.OrderBy(l => l.songName).ToArray());
                    if (CustomLevelsRepository != null && (folderEntry.Levels.Count > 0 || folderEntry is ModSeparateSongFolder { AlwaysShow: true }))
                    {
                        CustomLevelsRepository.AddLevelPack(folderEntry.LevelPack);
                    }
                }
            }

            _beatmapLevelsModel._customLevelsRepository = CustomLevelsRepository;
            _beatmapLevelsModel._allLoadedBeatmapLevelsRepository = _beatmapLevelsModel.CreateAllLoadedBeatmapLevelPacks();
            _beatmapLevelsModel.UpdateLoadedPreviewLevels();

            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                if (!_loadingTaskCancellationTokenSource.IsCancellationRequested && _levelFilteringNavigationController.isActiveAndEnabled)
                {
                    _levelFilteringNavigationController.UpdateCustomSongs();
                }

                OnLevelPacksRefreshed?.Invoke();
            });
        }

        public void RefreshSongs(bool fullRefresh = true)
        {
            if (AreSongsLoading || SceneManager.GetActiveScene().name == "GameCore")
            {
                return;
            }

            Logging.Logger.Info(fullRefresh ? "Starting full song refresh" : "Starting song refresh");
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
                    Logging.Logger.Error("Some plugin is throwing exception from the LoadingStartedEvent!");
                    Logging.Logger.Error(e);
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
                LoadedBeatmapLevelsData.Clear();
                _beatmapLevelsModel.ClearLoadedBeatmapLevelsCaches();
                Collections.LevelHashDictionary.Clear();
                Collections.HashLevelDictionary.Clear();
                foreach (var folder in SeparateSongFolders)
                {
                    folder.Levels.Clear();
                }
            }

            #endregion

            ConcurrentDictionary<string, bool> foundSongPaths = fullRefresh
                ? new ConcurrentDictionary<string, bool>()
                : new ConcurrentDictionary<string, bool>(Hashing.cachedSongHashData.Keys.ToDictionary(Hashing.GetAbsolutePath, _ => false));

            Action job = () =>
            {
                #region AddOfficialBeatmaps

                try
                {
                    void AddOfficialBeatmapLevelsRepository(BeatmapLevelsRepository levelsRepository)
                    {
                        foreach (var pack in levelsRepository.beatmapLevelPacks)
                        {
                            foreach (var level in pack.beatmapLevels)
                            {
                                OfficialSongs[level.levelID] = new OfficialSongEntry { LevelsRepository = levelsRepository, LevelPack = pack, BeatmapLevel = level };
                            }
                        }
                    }

                    OfficialSongs.Clear();

                    AddOfficialBeatmapLevelsRepository(_beatmapLevelsModel.ostAndExtrasBeatmapLevelsRepository);
                    AddOfficialBeatmapLevelsRepository(_beatmapLevelsModel.dlcBeatmapLevelsRepository);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error($"Error populating official songs: {ex.Message}");
                    Logging.Logger.Error(ex);
                }

                #endregion

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

                            var cacheFolders = Directory.GetDirectories(cachePath);
                            LoadCachedZIPs(cacheFolders, fullRefresh, CachedWIPLevels);
                        }
                        catch (Exception ex)
                        {
                            Logging.Logger.Error("Failed to load cached WIP levels: ");
                            Logging.Logger.Error(ex);
                        }
                    }

                    #endregion

                    #region CacheSeparateZIPs

                    if (fullRefresh)
                    {
                        foreach (SeparateSongFolder songFolder in SeparateSongFolders)
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
                                    Logging.Logger.Error("Failed to load cached WIP levels:");
                                    Logging.Logger.Error(ex);
                                }
                            }
                        }
                    }

                    #endregion

                    stopwatch.Start();

                    #region LoadCustomLevels

                    // Get Levels from CustomLevels and CustomWIPLevels folders
                    var songFolders = Directory.GetDirectories(_customLevelsPath).Concat(Directory.GetDirectories(_customWIPPath)).ToArray();
                    var loadedData = new ConcurrentBag<string>();

                    var processedSongsCount = 0;
                    Parallel.ForEach(songFolders, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, (Environment.ProcessorCount / 2) - 1), CancellationToken = _loadingTaskCancellationTokenSource.Token }, (folder) =>
                      {
                          string[] results;
                          try
                          {
                              results = Directory.GetFiles(folder, CustomLevelPathHelper.kStandardLevelInfoFilename, SearchOption.TopDirectoryOnly);
                          }
                          catch (Exception ex)
                          {
                              Logging.Logger.Warn($"Skipping missing or corrupt folder: '{folder}'");
                              Logging.Logger.Warn(ex);
                              return;
                          }

                          if (results.Length == 0)
                          {
                              Logging.Logger.Notice($"Folder: '{folder}' is missing {CustomLevelPathHelper.kStandardLevelInfoFilename} file!");
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

                                  if (!fullRefresh)
                                  {
                                      if (CustomLevels.TryGetValue(songPath, out BeatmapLevel c))
                                      {
                                          if (c != null)
                                          {
                                              loadedData.Add(c.levelID);
                                              continue;
                                          }
                                      }
                                  }

                                  var wip = songPath.Contains("CustomWIPLevels");
                                  var level = LoadCustomLevel(songPath);
                                  if (level == null)
                                  {
                                      Logging.Logger.Error($"Failed to load custom level: {folder}");
                                      continue;
                                  }

                                  if (!wip)
                                  {
                                      CustomLevelsById[level.levelID] = level;
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
                                  Logging.Logger.Error($"Failed to load song folder: {result}");
                                  Logging.Logger.Error(e);
                              }
                          }

                          LoadingProgress = (float) Interlocked.Increment(ref processedSongsCount) / songFolders.Length;
                      });

                    RefreshLoadedBeatmapLevelsData();

                    #endregion

                    #region LoadSeparateFolders

                    // Load beatmaps in separate song folders (created in folders.xml or by other mods)
                    // Assign beatmaps to their respective pack (custom levels, wip levels, or separate)
                    for (var k = 0; k < SeparateSongFolders.Count; k++)
                    {
                        try
                        {
                            SeparateSongFolder entry = SeparateSongFolders[k];
                            UnityMainThreadTaskScheduler.Factory.StartNew(() => Instance._progressBar.ShowMessage($"Loading {SeparateSongFolders.Count - k} Additional Song folders"));
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
                                    results = Directory.GetFiles(folder, CustomLevelPathHelper.kStandardLevelInfoFilename, SearchOption.TopDirectoryOnly);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Logger.Warn($"Skipping missing or corrupt folder: '{folder}'");
                                    Logging.Logger.Warn(ex);
                                    continue;
                                }

                                if (results.Length == 0)
                                {
                                    Logging.Logger.Notice($"Folder: '{folder}' is missing {CustomLevelPathHelper.kStandardLevelInfoFilename} file!");
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

                                        var level = LoadCustomLevel(songPath, entry.SongFolderEntry);
                                        if (level == null)
                                        {
                                            Logging.Logger.Error($"Failed to load custom level: {folder}");
                                        }
                                        else
                                        {
                                            entry.Levels[songPath] = level;
                                            CustomLevelsById[level.levelID] = level;
                                            foundSongPaths.TryAdd(songPath, false);
                                        }

                                        LoadingProgress = count / entryFolders.Length;
                                    }
                                    catch (Exception e)
                                    {
                                        Logging.Logger.Error($"Failed to load song folder: {result}");
                                        Logging.Logger.Error(e);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Logger.Error($"Failed to load separate folder {SeparateSongFolders[k].SongFolderEntry.Name}");
                            Logging.Logger.Error(ex);
                        }
                    }

                    #endregion

                    _loadingTaskCancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Logging.Logger.Error("RetrieveAllSongs failed:");
                    Logging.Logger.Error(e);
                }

                #endregion
            };

            Action finish = async () =>
            {
                #region CountBeatmapsAndUpdateLevelPacks

                stopwatch.Stop();
                var songCountWSF = CustomLevels.Count + CustomWIPLevels.Count;
                var songCount = songCountWSF + SeparateSongFolders.Sum(f => f.Levels.Count);

                int folderCount = songCount - songCountWSF;
                string songOrSongs = songCount == 1 ? "song" : "songs";
                string folderOrFolders = folderCount == 1 ? "folder" : "folders";
                Logging.Logger.Info($"Loaded {songCount} new {songOrSongs} ({songCountWSF}) in CustomLevels | {folderCount} in separate {folderOrFolders}) in {stopwatch.Elapsed.TotalSeconds} seconds");
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

                    //Handle LevelPacks
                    if (CustomLevelsRepository == null || CustomLevelsRepository.beatmapLevelPacks.Count == 0)
                    {
                        if (CustomLevelsRepository == null)
                        {
                            CustomLevelsRepository = await UnityMainThreadTaskScheduler.Factory.StartNew(SongCoreBeatmapLevelsRepository.CreateNew);
                        }
                        else
                        {
                            CustomLevelsRepository.ClearLevelPacks();
                        }

                        #region CreateLevelPacks

                        // Create level collections and level packs
                        // Add level packs to the custom levels pack collection

                        // This creates unity sprites, so it needs to be on the main thread
                        await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                        {
                            CustomLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + CustomLevelPathHelper.kCustomLevelsDirectoryName, "Custom Levels", defaultCoverImage, CustomLevels.Values.ToArray());
                            WIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomWIPLevels", "WIP Levels", UI.BasicUI.WIPIcon, CustomWIPLevels.Values.ToArray());
                            CachedWIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CachedWIPLevels", "Cached WIP Levels", UI.BasicUI.WIPIcon,
                                CachedWIPLevels.Values.ToArray());

                            CustomLevelsRepository.AddLevelPack(CustomLevelsPack);
                            CustomLevelsRepository.AddLevelPack(WIPLevelsPack);
                            CustomLevelsRepository.AddLevelPack(CachedWIPLevelsPack);
                        });

                        #endregion
                    }

                    //Level Packs
                    RefreshLevelPacks();
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error("Failed to setup LevelPacks:");
                    Logging.Logger.Error(ex);
                }

                #endregion

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;
                await UnityMainThreadTaskScheduler.Factory.StartNew(() => SongsLoadedEvent?.Invoke(this, CustomLevels));

                // Write our cached hash info and
                var cancellationToken = _loadingTaskCancellationTokenSource.Token;
                await Hashing.UpdateCachedHashesInternalAsync(foundSongPaths.Keys, cancellationToken);
                await Hashing.UpdateCachedAudioDataInternalAsync(foundSongPaths.Keys, cancellationToken);
                await Collections.SaveExtraSongDataAsync(cancellationToken);
            };

            try
            {
                _loadingTask = new Task(job, _loadingTaskCancellationTokenSource.Token);
                var loadingAwaiter = _loadingTask.ConfigureAwait(false);
                _loadingTask.Start();
                await loadingAwaiter;
            }
            catch (Exception ex)
            {
                Logging.Logger.Warn($"Song loading task failed. {ex.Message}");
                return;
            }

            if (_loadingTask.IsCompleted && !_loadingTask.IsCanceled)
            {
                await Task.Run(finish).ConfigureAwait(false);
            }
            else
            {
                Logging.Logger.Warn($"Song loading task cancelled.");
            }
        }

        private void RefreshLoadedBeatmapLevelsData()
        {
            foreach (var beatmapLevelData in LoadedBeatmapLevelsData)
            {
                if (!_customLevelLoader._loadedBeatmapLevelsData.ContainsKey(beatmapLevelData.Key))
                {
                    _customLevelLoader._loadedBeatmapLevelsData.Add(beatmapLevelData.Key, beatmapLevelData.Value);
                }
            }
        }

        /// <summary>
        /// Delete a beatmap (is only used by other mods)
        /// </summary>
        /// <param name="folderPath">Directory of the beatmap</param>
        /// <param name="deleteFolder">Option to delete the base folder of the beatmap</param>
        public void DeleteSong(string folderPath, bool deleteFolder = true)
        {
            DeletingSong?.Invoke();
            DeleteSingleSong(folderPath, deleteFolder);
            Hashing.UpdateCachedHashes(new HashSet<string>((CustomLevels.Keys.Concat(CustomWIPLevels.Keys))));
            RefreshLevelPacks();
        }

        /// <summary>
        /// Delete multiple beatmaps in bulk (is only used by other mods)
        /// </summary>
        /// <param name="folderPaths">Directories of the beatmaps</param>
        /// <param name="deleteFolder">Option to delete the base folder of the beatmap</param>
        public async Task DeleteSongsAsync(List<string> folderPaths, bool deleteFolder = true)
        {
            DeletingSong?.Invoke();
            foreach (string folderPath in folderPaths)
            {
                await Task.Run(() => DeleteSingleSong(folderPath, deleteFolder));
            }

            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                Hashing.UpdateCachedHashes(new HashSet<string>(CustomLevels.Keys.Concat(CustomWIPLevels.Keys)));
                RefreshLevelPacks();
            });
        }

        private void DeleteSingleSong(string folderPath, bool deleteFolder)
        {
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
                    if (Collections.LevelHashDictionary.ContainsKey(level.levelID))
                    {
                        string hash = Collections.hashForLevelID(level.levelID);
                        Collections.LevelHashDictionary.TryRemove(level.levelID, out _);
                        if (Collections.HashLevelDictionary.ContainsKey(hash))
                        {
                            Collections.HashLevelDictionary[hash].Remove(level.levelID);
                            if (Collections.HashLevelDictionary[hash].Count == 0)
                            {
                                Collections.HashLevelDictionary.TryRemove(hash, out _);
                            }
                        }
                    }

                    CustomLevelsById.TryRemove(level.levelID, out _);
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
                Logging.Logger.Error($"Exception trying to delete song: {folderPath}");
                Logging.Logger.Error(ex);
            }
        }

        /// <summary>
        /// Load a beatmap, gather all beatmap information and create beatmap preview
        /// </summary>
        /// <param name="saveData">Save data of beatmap</param>
        /// <param name="songPath">Directory of beatmap</param>
        /// <param name="hash">Resulting hash for the beatmap, may contain beatmap folder name or 'WIP' at the end</param>
        /// <param name="folderEntry">Folder entry for beatmap folder</param>
        /// <returns></returns>
        public static BeatmapLevel? LoadSong(StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry? folderEntry = null)
        {
            var wip = songPath.Contains("CustomWIPLevels") || folderEntry != null && (folderEntry.Pack == FolderLevelPack.CustomWIPLevels || folderEntry.Pack == FolderLevelPack.CachedWIPLevels || folderEntry.WIP);
            BeatmapLevel? beatmapLevel;
            try
            {
                hash = Hashing.GetCustomLevelHash(saveData, songPath);
                beatmapLevel = CustomLevelLoader.CreateBeatmapLevelFromV3(songPath, saveData);
                IBeatmapLevelData beatmapLevelData = CustomLevelLoader.CreateBeatmapLevelDataFromV3(saveData, songPath);
                string levelID = CustomLevelLoader.kCustomLevelPrefixId + hash;
                string folderName = new DirectoryInfo(songPath).Name;
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
                Collections.LevelSaveDataDictionary.TryAdd(levelID, saveData);
                Collections.LevelPathDictionary.TryAdd(levelID, songPath);
                LoadedBeatmapLevelsData.TryAdd(levelID, beatmapLevelData);

                Accessors.LevelIDAccessor(ref beatmapLevel) = levelID;
                GetSongDuration(saveData, beatmapLevel, songPath, Path.Combine(songPath, saveData.songFilename));
            }
            catch (Exception e)
            {
                Logging.Logger.Error($"Failed to load song: {songPath}");
                Logging.Logger.Error(e);
                beatmapLevel = null;
                hash = null;
            }

            return beatmapLevel;
        }

        public static BeatmapLevel? LoadSong(CancellationToken token, StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry? folderEntry = null)
        {
            token.ThrowIfCancellationRequested();
            return LoadSong(saveData, songPath, out hash, folderEntry);
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
            foreach (var file in cache.GetFiles())
            {
                file.Delete();
            }

            foreach (var folder in cache.GetDirectories())
            {
                folder.Delete(true);
            }

            var zips = Directory.GetFiles(songFolderPath, "*.zip", SearchOption.TopDirectoryOnly);

            foreach (var zip in zips)
            {
                using var unzip = new Unzip(zip);
                try
                {
                    unzip.ExtractToDirectory(Path.Combine(cachePath, new FileInfo(zip).Name));
                }
                catch (Exception ex)
                {
                    Logging.Logger.Warn($"Failed to extract zip: {zip}:");
                    Logging.Logger.Warn(ex);
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
        private void LoadCachedZIPs(IEnumerable<string> cacheFolders, bool fullRefresh, ConcurrentDictionary<string, BeatmapLevel> beatmapDictionary, SongFolderEntry? folderEntry = null)
        {
            foreach (var cachedFolder in cacheFolders)
            {
                string[] results;
                try
                {
                    results = Directory.GetFiles(cachedFolder, CustomLevelPathHelper.kStandardLevelInfoFilename, SearchOption.AllDirectories);
                }
                catch (DirectoryNotFoundException)
                {
                    Logging.Logger.Warn($"Skipping missing or corrupt folder: '{cachedFolder}'");
                    continue;
                }

                if (results.Length == 0)
                {
                    Logging.Logger.Notice($"Folder: '{cachedFolder}' is missing {CustomLevelPathHelper.kStandardLevelInfoFilename} files!");
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

                        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                        {
                            try
                            {
                                var level = LoadCustomLevel(songPath, folderEntry);
                                if (level == null)
                                {
                                    Logging.Logger.Error($"Failed to load custom level: {folderEntry}");
                                    return;
                                }

                                beatmapDictionary[songPath] = level;
                            }
                            catch (Exception ex)
                            {
                                Logging.Logger.Notice($"Failed to load song from {cachedFolder}:");
                                Logging.Logger.Notice(ex);
                            }
                        }, _loadingTaskCancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Notice($"Failed to load song from {cachedFolder}:");
                        Logging.Logger.Notice(ex);
                    }
                }
            }
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

        // TODO: Return beatmap level data?
        public BeatmapLevel? LoadCustomLevel(string customLevelPath, SongFolderEntry? entry = null)
        {
            var infoFilePath = Path.Combine(customLevelPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            if (!File.Exists(infoFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(infoFilePath);
            if (BeatmapSaveDataHelpers.GetVersion(json) < BeatmapSaveDataHelpers.version4)
            {
                var standardLevelInfoSaveData = StandardLevelInfoSaveData.DeserializeFromJSONString(json);
                if (standardLevelInfoSaveData == null)
                {
                    return null;
                }

                var beatmapLevel = LoadSong(standardLevelInfoSaveData, customLevelPath, out var hash, entry);
                if (beatmapLevel == null)
                {
                    return null;
                }

                Collections.AddExtraSongData(hash, customLevelPath, json);

                return beatmapLevel;
            }

            // TODO: V4
            return null;
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

            BeatmapLevel? level = null;
            if (levelId.StartsWith(CustomLevelLoader.kCustomLevelPrefixId, StringComparison.Ordinal))
            {
                if (CustomLevelsById.TryGetValue(levelId, out BeatmapLevel customLevel))
                {
                    level = customLevel;
                }
            }
            else if (OfficialSongs.TryGetValue(levelId, out var song))
            {
                level = song.BeatmapLevel;
            }

            return level;
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

            CustomLevelsById.TryGetValue(CustomLevelLoader.kCustomLevelPrefixId + hash.ToUpperInvariant(), out BeatmapLevel level);
            return level;
        }

        private static void GetSongDuration(StandardLevelInfoSaveData saveData, BeatmapLevel level, string songPath, string oggFile)
        {
            try
            {
                string levelID = level.levelID;
                float length = 0;
                if (Hashing.cachedAudioData.TryGetValue(Hashing.GetRelativePath(songPath), out var data))
                {
                    if (data.id == levelID)
                    {
                        length = data.duration;
                    }
                }

                if (length == 0)
                {
                    try
                    {
                        length = GetLengthFromOgg(oggFile);
                    }
                    catch (Exception)
                    {
                        length = -1;
                    }

                    if (length <= 1)
                    {
                        // janky, but whatever
                        Collections.LevelPathDictionary.TryGetValue(level.levelID, out var customLevelPath);
                        Logging.Logger.Warn($"Failed to parse song length from audio file, approximating using map length. Song: {customLevelPath}");
                        length = GetLengthFromMap(saveData, level, songPath);
                    }
                }

                if (data != null)
                {
                    data.duration = length;
                    data.id = levelID;
                }
                else
                {
                    Hashing.cachedAudioData[Hashing.GetRelativePath(songPath)] = new AudioCacheData(levelID, length);
                }

                if (Plugin.Configuration.ForceLongPreviews)
                {
                    Accessors.PreviewDurationAccessor(ref level) = Mathf.Max(level.previewDuration, length - level.previewStartTime);
                }

                Accessors.SongDurationAccessor(ref level) = length;
            }
            catch (Exception ex)
            {
                Logging.Logger.Warn("Failed to parse song duration");
                Logging.Logger.Warn(ex);
            }
        }

        public static float GetLengthFromMap(StandardLevelInfoSaveData saveData, BeatmapLevel level, string songPath)
        {
            var diff = saveData.difficultyBeatmapSets.First().difficultyBeatmaps.Last().beatmapFilename;
            var saveDataString = File.ReadAllText(Path.Combine(songPath, diff));
            var beatmapSaveData = JsonUtility.FromJson<BeatmapSaveDataVersion3.BeatmapSaveData>(saveDataString);

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
            return bpmTimeProcessor.ConvertBeatToTime(highestTime);
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
            using FileStream fs = File.OpenRead(oggFile);
            using BinaryReader br = new BinaryReader(fs, Encoding.ASCII);

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
                Logging.Logger.Warn($"Could not find rate for {oggFile}");
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
                Logging.Logger.Warn($"Could not find lastSample for {oggFile}");
                return -1;
            }

            var length = lastSample / (float) rate;
            return length;
        }

        /// <summary>
        /// Attempts to get an official level by LevelId. Returns false if a matching level isn't found.
        /// </summary>
        /// <param name="levelId"></param>
        /// <param name="song"></param>
        /// <returns></returns>
        public static bool TryGetOfficialLevelById(string levelId, out OfficialSongEntry song)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                song = default(OfficialSongEntry);
                return false;
            }

            return OfficialSongs.TryGetValue(levelId, out song);
        }

        #endregion

        public struct OfficialSongEntry
        {
            public BeatmapLevelsRepository LevelsRepository;
            public BeatmapLevelPack LevelPack;
            public BeatmapLevel BeatmapLevel;
        }
    }
}