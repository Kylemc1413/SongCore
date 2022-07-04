using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using IPA.Utilities.Async;
using SongCore.Data;
using SongCore.OverrideClasses;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SongCore
{
    public class Loader : MonoBehaviour
    {
        // Actions for loading and refreshing beatmaps
        public static event Action<Loader>? LoadingStartedEvent;
        public static event Action<Loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel>>? SongsLoadedEvent;
        public static event Action? OnLevelPacksRefreshed;
        public static event Action? DeletingSong;
        public static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CustomLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();
        public static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CustomWIPLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();
        public static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CachedWIPLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();
        public static readonly List<SeperateSongFolder> SeperateSongFolders = new List<SeperateSongFolder>();
        public static SongCoreCustomLevelCollection? CustomLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection? WIPLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection? CachedWIPLevelCollection { get; private set; }
        public static SongCoreCustomBeatmapLevelPack? CustomLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack? WIPLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack? CachedWIPLevelsPack { get; private set; }
        public static SongCoreBeatmapLevelPackCollectionSO? CustomBeatmapLevelPackCollectionSO { get; private set; }

        private static readonly ConcurrentDictionary<string, OfficialSongEntry> OfficialSongs = new ConcurrentDictionary<string, OfficialSongEntry>();

        private static readonly ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CustomLevelsById = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();

        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; internal set; }
        internal ProgressBar _progressBar;
        private Task? _loadingTask;
        private CancellationTokenSource _loadingTaskCancellationTokenSource = new CancellationTokenSource();
        private bool _loadingCancelled;

        private static CustomLevelLoader _customLevelLoader;

        public static BeatmapLevelsModel BeatmapLevelsModelSO
        {
            get
            {
                if (_beatmapLevelsModel == null)
                {
                    _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
                }

                return _beatmapLevelsModel;
            }
        }

        internal static BeatmapLevelsModel _beatmapLevelsModel;
        public static Sprite defaultCoverImage;
        public static CachedMediaAsyncLoader cachedMediaAsyncLoaderSO { get; private set; }
        public static BeatmapCharacteristicCollectionSO beatmapCharacteristicCollection { get; private set; }

        public static Loader Instance;

        public static void OnLoad()
        {
            if (Instance != null)
            {
                _beatmapLevelsModel = null;
                Instance.RefreshLevelPacks();
                return;
            }

            new GameObject("SongCore Loader").AddComponent<Loader>();
        }

        private string _customWIPPath;
        private string _customLevelsPath;

        private void Awake()
        {
            _customWIPPath = Path.Combine(Application.dataPath, "CustomWIPLevels");
            _customLevelsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            _ = BeatmapLevelsModelSO; // This is to cache the BeatmapLevelsModel while we're on the main thread, since we need to access it when we're not on the main thread (which Unity debug doesn't like).

            Instance = this;
            _progressBar = ProgressBar.Create();
            MenuLoaded();
            Hashing.ReadCachedSongHashes();
            Hashing.ReadCachedAudioData();
            DontDestroyOnLoad(gameObject);
            BS_Utils.Utilities.BSEvents.menuSceneLoaded += MenuLoaded;
            Initialize();
        }

        private void Initialize()
        {
            if (Directory.Exists(Converter.oldFolderPath))
            {
                Converter.PrepareExistingLibrary();
            }
            else
            {
                RefreshSongs();
            }
        }

        internal void CancelSongLoading()
        {
            _loadingTaskCancellationTokenSource.Cancel();
            _loadingCancelled = true;
            AreSongsLoading = false;
            LoadingProgress = 0;
            StopAllCoroutines();
            _progressBar.ShowMessage("Loading cancelled\n<size=80%>Press Ctrl+R to refresh</size>");
        }

        internal void MenuLoaded()
        {
            if (AreSongsLoading)
            {
                //Scene changing while songs are loading. Since we are using a separate thread while loading, this is bad and could cause a crash.
                //So we have to stop loading.
                if (_loadingTask != null)
                {
                    //_loadingTask.Cancel();
                    CancelSongLoading();
                    Logging.Logger.Info("Loading was cancelled by player since they loaded another scene.");
                }
            }

            BS_Utils.Gameplay.Gamemode.Init();
            if (_customLevelLoader == null)
            {
                _customLevelLoader = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().FirstOrDefault();
                if (_customLevelLoader)
                {
                    defaultCoverImage = _customLevelLoader.GetField<Sprite, CustomLevelLoader>("_defaultPackCover");

                    cachedMediaAsyncLoaderSO = _customLevelLoader.GetField<CachedMediaAsyncLoader, CustomLevelLoader>("_cachedMediaAsyncLoader");
                    beatmapCharacteristicCollection = _customLevelLoader.GetField<BeatmapCharacteristicCollectionSO, CustomLevelLoader>("_beatmapCharacteristicCollection");
                }
                else
                {
                    Texture2D defaultCoverTex = Texture2D.blackTexture;
                    defaultCoverImage = Sprite.Create(defaultCoverTex, new Rect(0f, 0f,
                        defaultCoverTex.width, defaultCoverTex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        /// <summary>
        /// This fuction will add/remove Level Packs from the Custom Levels tab if applicable
        /// </summary>
        public async void RefreshLevelPacks()
        {
            CustomLevelsCollection?.UpdatePreviewLevels(CustomLevels.Values.OrderBy(l => l.songName).ToArray());
            WIPLevelsCollection?.UpdatePreviewLevels(CustomWIPLevels.Values.OrderBy(l => l.songName).ToArray());
            CachedWIPLevelCollection?.UpdatePreviewLevels(CachedWIPLevels.Values.OrderBy(l => l.songName).ToArray());

            if (CachedWIPLevelsPack != null)
            {
                if (CachedWIPLevels.Count > 0 && !CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(CachedWIPLevelsPack))
                {
                    CustomBeatmapLevelPackCollectionSO.AddLevelPack(CachedWIPLevelsPack);
                }
                else if (CachedWIPLevels.Count == 0 && CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(CachedWIPLevelsPack))
                {
                    CustomBeatmapLevelPackCollectionSO.RemoveLevelPack(CachedWIPLevelsPack);
                }
            }

            foreach (var folderEntry in SeperateSongFolders)
            {
                if (folderEntry.SongFolderEntry.Pack == FolderLevelPack.NewPack)
                {
                    folderEntry.LevelCollection.UpdatePreviewLevels(folderEntry.Levels.Values.OrderBy(l => l.songName).ToArray());
                    if (folderEntry.Levels.Count > 0 || (folderEntry is ModSeperateSongFolder { AlwaysShow: true }))
                    {
                        if (!CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                        {
                            CustomBeatmapLevelPackCollectionSO.AddLevelPack(folderEntry.LevelPack);
                        }
                    }
                }
            }

            BeatmapLevelsModelSO.SetField("_customLevelPackCollection", CustomBeatmapLevelPackCollectionSO as IBeatmapLevelPackCollection);
            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                BeatmapLevelsModelSO.UpdateAllLoadedBeatmapLevelPacks();
                BeatmapLevelsModelSO.UpdateLoadedPreviewLevels();
                var filterNav = Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().FirstOrDefault();
                if (filterNav != null && filterNav.isActiveAndEnabled)
                {
                    filterNav.UpdateCustomSongs();
                }

                OnLevelPacksRefreshed?.Invoke();
            });
        }

        public void RefreshSongs(bool fullRefresh = true)
        {
            if (SceneManager.GetActiveScene().name == "GameCore")
            {
                return;
            }

            if (AreSongsLoading)
            {
                return;
            }

            Logging.Logger.Info(fullRefresh ? "Starting full song refresh" : "Starting song refresh");
            AreSongsLoaded = false;
            AreSongsLoading = true;
            LoadingProgress = 0;
            _loadingCancelled = false;
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
                CustomBeatmapLevelPackCollectionSO = null;
                CustomLevels.Clear();
                CustomWIPLevels.Clear();
                CachedWIPLevels.Clear();
                Collections.LevelHashDictionary.Clear();
                Collections.HashLevelDictionary.Clear();
                foreach (var folder in SeperateSongFolders)
                {
                    folder.Levels.Clear();
                }
            }

            #endregion

            ConcurrentDictionary<string, bool> foundSongPaths = fullRefresh
                ? new ConcurrentDictionary<string, bool>()
                : new ConcurrentDictionary<string, bool>(Hashing.cachedSongHashData.Keys.ToDictionary(x => x, _ => false));

            Action job = () =>
            {
                #region AddOfficialBeatmaps

                try
                {
                    void AddOfficialPackCollection(IBeatmapLevelPackCollection packCollection)
                    {
                        foreach (var pack in packCollection.beatmapLevelPacks)
                        {
                            foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                            {
                                OfficialSongs[level.levelID] = new OfficialSongEntry { LevelPackCollection = packCollection, LevelPack = pack, PreviewBeatmapLevel = level };
                            }
                        }
                    }

                    OfficialSongs.Clear();

                    AddOfficialPackCollection(BeatmapLevelsModelSO.ostAndExtrasPackCollection);
                    AddOfficialPackCollection(BeatmapLevelsModelSO.dlcBeatmapLevelPackCollection);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error($"Error populating official songs: {ex.Message}");
                    Logging.Logger.Debug(ex);
                }

                #endregion

                #region AddCustomBeatmaps

                var customLevelsPath = _customLevelsPath;

                try
                {
                    #region DirectorySetup

                    if (!Directory.Exists(customLevelsPath))
                    {
                        Directory.CreateDirectory(customLevelsPath);
                    }

                    var customWipLevelsPath = _customWIPPath;
                    if (!Directory.Exists(customWipLevelsPath))
                    {
                        Directory.CreateDirectory(customWipLevelsPath);
                    }

                    #endregion

                    #region CacheZipWIPs

                    // Get zip files in CustomWIPLevels and extract them to Cache folder
                    if (fullRefresh)
                    {
                        try
                        {
                            var cachePath = Path.Combine(customWipLevelsPath, "Cache");
                            CacheZIPs(cachePath, customWipLevelsPath);

                            var cacheFolders = Directory.GetDirectories(cachePath).ToArray();
                            LoadCachedZIPs(cacheFolders, fullRefresh, CachedWIPLevels);
                        }
                        catch (Exception ex)
                        {
                            Logging.Logger.Error("Failed To Load Cached WIP Levels: ");
                            Logging.Logger.Error(ex);
                        }
                    }

                    #endregion

                    #region CacheSeperateZIPs

                    if (fullRefresh)
                    {
                        foreach (SeperateSongFolder songFolder in SeperateSongFolders)
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
                                    Logging.Logger.Error("Failed To Load Cached WIP Levels:");
                                    Logging.Logger.Error(ex);
                                }
                            }
                        }
                    }

                    #endregion

                    stopwatch.Start();

                    #region LoadCustomLevels

                    // Get Levels from CustomLevels and CustomWIPLevels folders
                    var songFolders = Directory.GetDirectories(customLevelsPath).ToList().Concat(Directory.GetDirectories(customWipLevelsPath)).ToList();
                    var loadedData = new ConcurrentBag<string>();

                    var processedSongsCount = 0;
                    Parallel.ForEach(songFolders, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, (Environment.ProcessorCount / 2) - 1) }, (folder) =>
                      {
                          string[] results;
                          try
                          {
                              results = Directory.GetFiles(folder, "info.dat", SearchOption.TopDirectoryOnly);
                          }
                          catch (DirectoryNotFoundException)
                          {
                              Logging.Logger.Warn($"Skipping missing or corrupt folder: '{folder}'");
                              return;
                          }

                          if (results.Length == 0)
                          {
                              Logging.Logger.Notice($"Folder: '{folder}' is missing info.dat files!");
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
                                      if (CustomLevels.TryGetValue(songPath, out CustomPreviewBeatmapLevel c))
                                      {
                                          if (c != null)
                                          {
                                              loadedData.Add(c.levelID);
                                              continue;
                                          }
                                      }
                                  }

                                  var wip = songPath.Contains("CustomWIPLevels");
                                  var songData = LoadCustomLevelSongData(songPath);
                                  if (songData == null)
                                  {
                                      Logging.Logger.Notice($"Folder: '{folder}' contains invalid song data!");
                                      continue;
                                  }

                                  if (_loadingCancelled)
                                  {
                                      return;
                                  }

                                  var level = LoadSongAndAddToDictionaries(_loadingTaskCancellationTokenSource.Token, songData, songPath);
                                  if (level != null)
                                  {
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
                              }
                              catch (Exception e)
                              {
                                  Logging.Logger.Error($"Failed to load song folder: {result}");
                                  Logging.Logger.Error(e);
                              }
                          }

                          LoadingProgress = (float) Interlocked.Increment(ref processedSongsCount) / songFolders.Count;
                      });

                    #endregion

                    #region LoadSeperateFolders

                    // Load beatmaps in Seperate Song Folders (created in folders.xml or by other mods)
                    // Assign beatmaps to their respective pack (custom levels, wip levels, or seperate)
                    for (var k = 0; k < SeperateSongFolders.Count; k++)
                    {
                        try
                        {
                            SeperateSongFolder entry = SeperateSongFolders[k];
                            UnityMainThreadTaskScheduler.Factory.StartNew(() => Instance._progressBar.ShowMessage($"Loading {SeperateSongFolders.Count - k} Additional Song folders"));
                            if (!Directory.Exists(entry.SongFolderEntry.Path))
                            {
                                continue;
                            }

                            var entryFolders = Directory.GetDirectories(entry.SongFolderEntry.Path).ToList();

                            float i2 = 0;
                            foreach (var folder in entryFolders)
                            {
                                i2++;
                                // Search for an info.dat in the beatmap folder
                                string[] results;
                                try
                                {
                                    results = Directory.GetFiles(folder, "info.dat", SearchOption.TopDirectoryOnly);
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    Logging.Logger.Warn($"Skipping missing or corrupt folder: '{folder}'");
                                    continue;
                                }

                                if (results.Length == 0)
                                {
                                    Logging.Logger.Notice($"Folder: '{folder}' is missing info.dat files!");
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

                                        if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomLevels || (entry.SongFolderEntry.Pack == FolderLevelPack.NewPack && entry.SongFolderEntry.WIP == false))
                                        {
                                            if (AssignBeatmapToSeperateFolder(CustomLevels, songPath, entry.Levels))
                                            {
                                                continue;
                                            }

                                            if (AssignBeatmapToSeperateFolder(CustomWIPLevels, songPath, entry.Levels))
                                            {
                                                continue;
                                            }

                                            if (AssignBeatmapToSeperateFolder(CachedWIPLevels, songPath, entry.Levels))
                                            {
                                                continue;
                                            }
                                        }

                                        var songData = LoadCustomLevelSongData(songPath);
                                        if (songData == null)
                                        {
                                            Logging.Logger.Notice($"Folder: '{folder}' contains invalid song data!");
                                            continue;
                                        }

                                        var count = i2;

                                        if (_loadingCancelled)
                                        {
                                            return;
                                        }

                                        var level = LoadSongAndAddToDictionaries(_loadingTaskCancellationTokenSource.Token, songData, songPath, entry.SongFolderEntry);
                                        if (level != null)
                                        {
                                            entry.Levels[songPath] = level;
                                            CustomLevelsById[level.levelID] = level;
                                            foundSongPaths.TryAdd(songPath, false);
                                        }

                                        LoadingProgress = count / entryFolders.Count;
                                        //});
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
                            Logging.Logger.Error($"Failed to load Separate Folder{SeperateSongFolders[k].SongFolderEntry.Name}");
                            Logging.Logger.Error(ex);
                        }
                    }

                    #endregion

                    _loadingTaskCancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
                catch (Exception e) when (!(e is OperationCanceledException))
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
                var songCount = songCountWSF + SeperateSongFolders.Sum(f => f.Levels.Count);

                int folderCount = songCount - songCountWSF;
                string songOrSongs = songCount == 1 ? "song" : "songs";
                string folderOrFolders = folderCount == 1 ? "folder" : "folders";
                Logging.Logger.Info($"Loaded {songCount} new {songOrSongs} ({songCountWSF}) in CustomLevels | {folderCount} in seperate {folderOrFolders}) in {stopwatch.Elapsed.TotalSeconds} seconds");
                try
                {
                    #region AddSeperateFolderBeatmapsToRespectivePacks

                    foreach (var folderEntry in SeperateSongFolders)
                    {
                        switch (folderEntry.SongFolderEntry.Pack)
                        {
                            case FolderLevelPack.CustomLevels:
                                CustomLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>(CustomLevels.Concat(folderEntry.Levels.Where(x => !CustomLevels.ContainsKey(x.Key)))
                                    .ToDictionary(x => x.Key, x => x.Value));
                                break;
                            case FolderLevelPack.CustomWIPLevels:
                                CustomWIPLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>(CustomWIPLevels
                                    .Concat(folderEntry.Levels.Where(x => !CustomWIPLevels.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value));
                                break;
                            case FolderLevelPack.CachedWIPLevels:
                                CachedWIPLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>(CachedWIPLevels
                                    .Concat(folderEntry.Levels.Where(x => !CachedWIPLevels.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value));
                                break;
                        }
                    }

                    #endregion

                    //Handle LevelPacks
                    if (CustomBeatmapLevelPackCollectionSO == null || CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Length == 0)
                    {
                        // var beatmapLevelPackCollectionSO = Resources.FindObjectsOfTypeAll<BeatmapLevelPackCollectionSO>().FirstOrDefault();
                        CustomBeatmapLevelPackCollectionSO = await UnityMainThreadTaskScheduler.Factory.StartNew(SongCoreBeatmapLevelPackCollectionSO.CreateNew);

                        #region CreateLevelPacks

                        // Create level collections and level packs
                        // Add level packs to the custom levels pack collection

                        CustomLevelsCollection = new SongCoreCustomLevelCollection(CustomLevels.Values.ToArray());
                        WIPLevelsCollection = new SongCoreCustomLevelCollection(CustomWIPLevels.Values.ToArray());
                        CachedWIPLevelCollection = new SongCoreCustomLevelCollection(CachedWIPLevels.Values.ToArray());
                        // This creates unity sprites, so it needs to be on the main thread
                        await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                        {
                            CustomLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomLevels", "Custom Levels", defaultCoverImage, CustomLevelsCollection);
                            WIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomWIPLevels", "WIP Levels", UI.BasicUI.WIPIcon, WIPLevelsCollection);
                            CachedWIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CachedWIPLevels", "Cached WIP Levels", UI.BasicUI.WIPIcon,
                                CachedWIPLevelCollection);

                            CustomBeatmapLevelPackCollectionSO.AddLevelPack(CustomLevelsPack);
                            CustomBeatmapLevelPackCollectionSO.AddLevelPack(WIPLevelsPack);
                            CustomBeatmapLevelPackCollectionSO.AddLevelPack(CachedWIPLevelsPack);
                        });

                        #endregion
                    }

                    //Level Packs
                    RefreshLevelPacks();
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error("Failed to Setup LevelPacks:");
                    Logging.Logger.Error(ex);
                }

                #endregion

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;
                await UnityMainThreadTaskScheduler.Factory.StartNew(() => SongsLoadedEvent?.Invoke(this, CustomLevels));

                // Write our cached hash info and
                Hashing.UpdateCachedHashesInternal(foundSongPaths.Keys);
                Hashing.UpdateCachedAudioDataInternal(foundSongPaths.Keys);
                await Collections.SaveExtraSongDataAsync();
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
                Logging.Logger.Warn($"Song Loading Task Failed. {ex.Message}");
                return;
            }

            if (_loadingTask.IsCompleted && !_loadingTask.IsCanceled)
            {
                await Task.Run(finish).ConfigureAwait(false);
            }
            else
            {
                Logging.Logger.Warn($"Song Loading Task Cancelled.");
            }

            // await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(finish).ConfigureAwait(false);
            //  _loadingTask = new HMTask(job, finish);
            //  _loadingTask.Run();
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
                    foreach (var folderEntry in SeperateSongFolders)
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
                Logging.Logger.Error($"Exception trying to Delete song: {folderPath}");
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
        public static CustomPreviewBeatmapLevel? LoadSong(StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry? folderEntry = null)
        {
            var wip = songPath.Contains("CustomWIPLevels");
            if (folderEntry != null)
            {
                if ((folderEntry.Pack == FolderLevelPack.CustomWIPLevels) || (folderEntry.Pack == FolderLevelPack.CachedWIPLevels))
                {
                    wip = true;
                }
                else if (folderEntry.WIP)
                {
                    wip = true;
                }
            }

            CustomPreviewBeatmapLevel? result;
            hash = Hashing.GetCustomLevelHash(saveData, songPath);
            try
            {
                string folderName = new DirectoryInfo(songPath).Name;
                string levelID = CustomLevelLoader.kCustomLevelPrefixId + hash;
                // Fixed WIP status for duplicate song hashes
                if (Collections.LevelHashDictionary.ContainsKey(levelID + (wip ? " WIP" : "")))
                {
                    levelID += $"_{folderName}";
                }

                if (wip)
                {
                    levelID += " WIP";
                }

                string songName = saveData.songName;
                string songSubName = saveData.songSubName;
                string songAuthorName = saveData.songAuthorName;
                string levelAuthorName = saveData.levelAuthorName;
                var beatsPerMinute = saveData.beatsPerMinute;
                var songTimeOffset = saveData.songTimeOffset;
                var shuffle = saveData.shuffle;
                var shufflePeriod = saveData.shufflePeriod;
                var previewStartTime = saveData.previewStartTime;
                var previewDuration = saveData.previewDuration;
                EnvironmentInfoSO environmentSceneInfo = _customLevelLoader.LoadEnvironmentInfo(saveData.environmentName, false);
                EnvironmentInfoSO allDirectionEnvironmentInfo = _customLevelLoader.LoadEnvironmentInfo(saveData.allDirectionsEnvironmentName, true);
                PreviewDifficultyBeatmapSet[] beatmapsets = new PreviewDifficultyBeatmapSet[saveData.difficultyBeatmapSets.Length];

                for (var i = 0; i < saveData.difficultyBeatmapSets.Length; i++)
                {
                    var difficultyBeatmapSet = saveData.difficultyBeatmapSets[i];

                    var beatmapCharacteristicBySerializedName = beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(difficultyBeatmapSet.beatmapCharacteristicName);
                    var array = new BeatmapDifficulty[difficultyBeatmapSet.difficultyBeatmaps.Length];
                    for (var j = 0; j < difficultyBeatmapSet.difficultyBeatmaps.Length; j++)
                    {
                        difficultyBeatmapSet.difficultyBeatmaps[j].difficulty.BeatmapDifficultyFromSerializedName(out var beatmapDifficulty);
                        array[j] = beatmapDifficulty;
                    }

                    beatmapsets[i] = new PreviewDifficultyBeatmapSet(beatmapCharacteristicBySerializedName, array);
                }

                result = new CustomPreviewBeatmapLevel(defaultCoverImage, saveData, songPath,
                    cachedMediaAsyncLoaderSO, levelID, songName, songSubName,
                    songAuthorName, levelAuthorName, beatsPerMinute, songTimeOffset, shuffle, shufflePeriod,
                    previewStartTime, previewDuration, environmentSceneInfo, allDirectionEnvironmentInfo, beatmapsets);

                GetSongDuration(result, songPath, Path.Combine(songPath, saveData.songFilename));
            }
            catch (Exception e)
            {
                Logging.Logger.Error($"Failed to Load Song: {songPath}");
                Logging.Logger.Error(e);
                result = null;
            }

            return result;
        }

        public static CustomPreviewBeatmapLevel? LoadSong(CancellationToken token, StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry? folderEntry = null)
        {
            token.ThrowIfCancellationRequested();
            return LoadSong(saveData, songPath, out hash, folderEntry);
        }

        /// <summary>
        /// Refresh songs on "R" key, full refresh on "Ctrl"+"R"
        /// </summary>
        private void Update()
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
        private void LoadCachedZIPs(IEnumerable<string> cacheFolders, bool fullRefresh, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> beatmapDictionary, SongFolderEntry? folderEntry = null)
        {
            foreach (var cachedFolder in cacheFolders)
            {
                string[] results;
                try
                {
                    results = Directory.GetFiles(cachedFolder, "info.dat", SearchOption.AllDirectories);
                }
                catch (DirectoryNotFoundException)
                {
                    Logging.Logger.Warn($"Skipping missing or corrupt folder: '{cachedFolder}'");
                    continue;
                }

                if (results.Length == 0)
                {
                    Logging.Logger.Notice($"Folder: '{cachedFolder}' is missing info.dat files!");
                    continue;
                }

                foreach (var result in results)
                {
                    try
                    {
                        var songPath = Path.GetDirectoryName(result)!;
                        if (!fullRefresh && beatmapDictionary != null)
                        {
                            if (SearchBeatmapInMapPack(beatmapDictionary, songPath))
                            {
                                continue;
                            }
                        }

                        var songData = LoadCustomLevelSongData(songPath);
                        if (songData == null)
                        {
                            continue;
                        }

                        HMMainThreadDispatcher.instance.Enqueue(() =>
                        {
                            if (_loadingCancelled)
                            {
                                return;
                            }

                            var level = LoadSong(songData.SaveData, songPath, out var hash, folderEntry);
                            if (level != null)
                            {
                                beatmapDictionary[songPath] = level;
                                Collections.AddExtraSongData(hash, songPath, songData.RawSongData);
                            }
                        });
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

        private bool SearchBeatmapInMapPack(ConcurrentDictionary<string, CustomPreviewBeatmapLevel> mapPack, string songPath)
        {
            if (mapPack.TryGetValue(songPath, out var customPreviewBeatmapLevel) && customPreviewBeatmapLevel != null)
            {
                return true;
            }

            return false;
        }

        private bool AssignBeatmapToSeperateFolder(ConcurrentDictionary<string, CustomPreviewBeatmapLevel> mapPack, string songPath,
            ConcurrentDictionary<string, CustomPreviewBeatmapLevel> seperateFolder)
        {
            if (mapPack.TryGetValue(songPath, out var customPreviewBeatmapLevel) && customPreviewBeatmapLevel != null)
            {
                seperateFolder[songPath] = customPreviewBeatmapLevel;
                return true;
            }

            return false;
        }

        public SongData? LoadCustomLevelSongData(string customLevelPath)
        {
            var path = Path.Combine(customLevelPath, "Info.dat");
            if (File.Exists(path))
            {
                var rawSongData = File.ReadAllText(path);
                var saveData = StandardLevelInfoSaveData.DeserializeFromJSONString(rawSongData);

                if (saveData == null)
                    return null;
                return new SongData(rawSongData, saveData);
            }
            return null;
        }

        private CustomPreviewBeatmapLevel? LoadSongAndAddToDictionaries(CancellationToken token, SongData songData, string songPath, SongFolderEntry? entry = null)
        {
            var level = LoadSong(songData.SaveData, songPath, out string hash, entry);
            if (level == null)
            {
                return level;
            }

            if (!Collections.LevelHashDictionary.ContainsKey(level.levelID))
            {
                // Add level to LevelHash-Dictionary
                Collections.LevelHashDictionary.TryAdd(level.levelID, hash);
                // Add hash to HashLevel-Dictionary
                if (Collections.HashLevelDictionary.TryGetValue(hash, out var levels))
                {
                    levels.Add(level.levelID);
                }
                else
                {
                    levels = new List<string> { level.levelID };
                    Collections.HashLevelDictionary.TryAdd(hash, levels);
                }
                Collections.AddExtraSongData(hash, songPath, songData.RawSongData);
            }

            return level;
        }

        #endregion

        #region HelperFunctionsSearching

        /// <summary>
        /// Attempts to get a beatmap by LevelId. Returns null a matching level isn't found.
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        public static IPreviewBeatmapLevel? GetLevelById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            IPreviewBeatmapLevel? level = null;
            if (levelId.StartsWith("custom_level_"))
            {
                if (CustomLevelsById.TryGetValue(levelId, out CustomPreviewBeatmapLevel customLevel))
                {
                    level = customLevel;
                }
            }
            else if (OfficialSongs.TryGetValue(levelId, out var song))
            {
                level = song.PreviewBeatmapLevel;
            }

            return level;
        }

        /// <summary>
        /// Attempts to get a custom level by hash (case-insensitive). Returns null a matching custom level isn't found.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static CustomPreviewBeatmapLevel? GetLevelByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return null;
            }

            CustomLevelsById.TryGetValue($"custom_level_{hash.ToUpper()}", out CustomPreviewBeatmapLevel level);
            return level;
        }

        private static readonly BeatmapDataLoader loader = new BeatmapDataLoader();

        static void GetSongDuration(CustomPreviewBeatmapLevel level, string songPath, string oggFile)
        {
            try
            {
                string levelid = level.levelID;
                float length = 0;
                if (Hashing.cachedAudioData.TryGetValue(songPath, out var data))
                {
                    if (data.id == levelid)
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
                        Logging.Logger.Warn($"Failed to parse song length from Ogg file, Approximating using Map length. Song: {level.customLevelPath}");
                        length = GetLengthFromMap(level, songPath);
                    }
                }

                if (data != null)
                {
                    data.duration = length;
                    data.id = levelid;
                }
                else
                {
                    Hashing.cachedAudioData[songPath] = new AudioCacheData(levelid, length);
                }

                Accessors.SongDurationSetter(ref level) = length;

                if (Plugin.Configuration.ForceLongPreviews)
                {
                   level.SetField($"<{nameof(CustomPreviewBeatmapLevel.previewDuration)}>k__BackingField", Mathf.Max(level.previewDuration, length - level.previewStartTime));
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Warn("Failed to Parse Song Duration");
                Logging.Logger.Warn(ex);
            }
        }

        public static float GetLengthFromMap(CustomPreviewBeatmapLevel level, string songPath)
        {
            var diff = level.standardLevelInfoSaveData.difficultyBeatmapSets.First().difficultyBeatmaps.Last().beatmapFilename;
            var version = level.standardLevelInfoSaveData.version.Trim();
            var saveDataString = File.ReadAllText(Path.Combine(songPath, diff));
            var beatmapsave = BeatmapSaveDataVersion3.BeatmapSaveData.DeserializeFromJSONString(saveDataString);

            float highestTime = 0;
            if (beatmapsave.colorNotes.Count > 0)
            {
                highestTime = beatmapsave.colorNotes.Max(x => x.beat);
            }
            else if (beatmapsave.basicBeatmapEvents.Count > 0)
            {
                highestTime = beatmapsave.basicBeatmapEvents.Max(x => x.beat);
            }

            var bpmtimeprocessor = new BeatmapDataLoader.BpmTimeProcessor(level.beatsPerMinute, beatmapsave.bpmEvents);
            return bpmtimeprocessor.ConvertBeatToTime(highestTime);
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
            using (FileStream fs = File.OpenRead(oggFile))
            using (BinaryReader br = new BinaryReader(fs, Encoding.ASCII))
            {
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

                        var index = Array.IndexOf(@by, bytes[0]);
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
                    Logging.Logger.Warn($"could not find rate for {oggFile}");
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
                    Logging.Logger.Warn($"could not find lastSample for {oggFile}");
                    return -1;
                }

                var length = lastSample / (float) rate;
                return length;
            }
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
            public IBeatmapLevelPackCollection LevelPackCollection;
            public IBeatmapLevelPack LevelPack;
            public IPreviewBeatmapLevel PreviewBeatmapLevel;
        }
    }
}