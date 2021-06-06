using SongCore.Data;
using SongCore.OverrideClasses;
using IPA.Utilities;
using SongCore.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogSeverity = IPA.Logging.Logger.Level;

namespace SongCore
{
    public class Loader : MonoBehaviour
    {
        // Actions for loading and refreshing beatmaps
        public static event Action<Loader> LoadingStartedEvent;
        public static event Action<Loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel>> SongsLoadedEvent;
        public static event Action OnLevelPacksRefreshed;
        public static event Action DeletingSong;
        public static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CustomLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();
        public static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CustomWIPLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();
        public static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CachedWIPLevels = new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();
        public static List<SeperateSongFolder> SeperateSongFolders = new List<SeperateSongFolder>();
        public static SongCoreCustomLevelCollection CustomLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection WIPLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection CachedWIPLevelCollection { get; private set; }
        public static SongCoreCustomBeatmapLevelPack CustomLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack WIPLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack CachedWIPLevelsPack { get; private set; }
        public static SongCoreBeatmapLevelPackCollectionSO CustomBeatmapLevelPackCollectionSO { get; private set; }

        private static readonly ConcurrentDictionary<string, OfficialSongEntry> OfficialSongs = new ConcurrentDictionary<string, OfficialSongEntry>();

        private static readonly ConcurrentDictionary<string, CustomPreviewBeatmapLevel> CustomLevelsById =
            new ConcurrentDictionary<string, CustomPreviewBeatmapLevel>();

        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; internal set; }
        internal ProgressBar _progressBar;
        private Task _loadingTask;
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

        private void Awake()
        {
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
                    Logging.Log("Loading was cancelled by player since they loaded another scene.");
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
            CustomLevelsCollection?.UpdatePreviewLevels(CustomLevels?.Values?.OrderBy(l => l.songName).ToArray());
            WIPLevelsCollection?.UpdatePreviewLevels(CustomWIPLevels?.Values?.OrderBy(l => l.songName).ToArray());
            CachedWIPLevelCollection?.UpdatePreviewLevels(CachedWIPLevels?.Values?.OrderBy(l => l.songName).ToArray());

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
                    if (folderEntry.Levels.Count > 0 || (folderEntry is ModSeperateSongFolder && (folderEntry as ModSeperateSongFolder).AlwaysShow))
                    {
                        if (!CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                        {
                            CustomBeatmapLevelPackCollectionSO.AddLevelPack(folderEntry.LevelPack);
                        }
                    }
                    //          else if (CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                    //              CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Remove(folderEntry.LevelPack);
                }
            }

            BeatmapLevelsModelSO.SetField<BeatmapLevelsModel, IBeatmapLevelPackCollection>("_customLevelPackCollection", CustomBeatmapLevelPackCollectionSO as IBeatmapLevelPackCollection);
            await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                BeatmapLevelsModelSO.UpdateAllLoadedBeatmapLevelPacks();
                BeatmapLevelsModelSO.UpdateLoadedPreviewLevels();
                var filterNav = Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().FirstOrDefault();
                //   filterNav.InitPlaylists();
                //   filterNav.UpdatePlaylistsData();
                if (filterNav != null && filterNav.isActiveAndEnabled)
                {
                    filterNav?.UpdateCustomSongs();
                }

                //      AttemptReselectCurrentLevelPack(filterNav);
                OnLevelPacksRefreshed?.Invoke();
            });
        }

        internal void AttemptReselectCurrentLevelPack(LevelFilteringNavigationController controller)
        {
            /*
            var collectionview = Resources.FindObjectsOfTypeAll<LevelCollectionViewController>().FirstOrDefault();
            var levelflow = Resources.FindObjectsOfTypeAll<LevelSelectionFlowCoordinator>().FirstOrDefault();
            var pack = levelflow.GetProperty<IBeatmapLevelPack>("selectedBeatmapLevelPack");
            IBeatmapLevelPack[] sectionpacks = new IBeatmapLevelPack[0];
            var selectedcategory = levelflow.GetProperty<SelectLevelCategoryViewController.LevelCategory>("selectedLevelCategory");
            switch (selectedcategory)
            {
                case SelectLevelCategoryViewController.LevelCategory.OstAndExtras:
                    sectionpacks = controller.GetField<IBeatmapLevelPack[]>("_ostBeatmapLevelPacks");
                    break;
                case SelectLevelCategoryViewController.LevelCategory.MusicPacks:
                    sectionpacks = controller.GetField<IBeatmapLevelPack[]>("_musicPacksBeatmapLevelPacks");
                    break;
                case SelectLevelCategoryViewController.LevelCategory.CustomSongs:
                    sectionpacks = controller.GetField<IBeatmapLevelPack[]>("_customLevelPacks");
                    break;

                case SelectLevelCategoryViewController.LevelCategory.All:
                    sectionpacks = controller.GetField<IBeatmapLevelPack[]>("_allBeatmapLevelPacks");
                    break;
                case SelectLevelCategoryViewController.LevelCategory.Favorites:
                    return;
            }
            if (!sectionpacks.ToList().Contains(pack))
                pack = sectionpacks.FirstOrDefault();
            if (pack == null) return;
            controller.Setup(SongPackMask.all, pack, selectedcategory, false, true);
            */
            //controller.SelectAnnotatedBeatmapLevelCollection(pack);
            // collectionview.SetData(pack.beatmapLevelCollection, pack.packName, pack.coverImage, false, controller.GetField<GameObject>("_currentNoDataInfoPrefab"));
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

            Logging.Log(fullRefresh ? "Starting full song refresh" : "Starting song refresh");
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
                    Logging.Log("Some plugin is throwing exception from the LoadingStartedEvent!", IPA.Logging.Logger.Level.Error);
                    Logging.Log(e.ToString(), IPA.Logging.Logger.Level.Error);
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

            var baseProjectPath = CustomLevelPathHelper.baseProjectPath;
            var customLevelsPath = CustomLevelPathHelper.customLevelsDirectoryPath;

            Action job = delegate
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
                                OfficialSongs[level.levelID] = new OfficialSongEntry() {LevelPackCollection = packCollection, LevelPack = pack, PreviewBeatmapLevel = level};
                            }
                        }
                    }

                    OfficialSongs.Clear();

                    AddOfficialPackCollection(BeatmapLevelsModelSO.ostAndExtrasPackCollection);
                    AddOfficialPackCollection(BeatmapLevelsModelSO.dlcBeatmapLevelPackCollection);
                }
                catch (Exception ex)
                {
                    Logging.logger.Error($"Error populating official songs: {ex.Message}");
                    Logging.logger.Debug(ex);
                }

                #endregion

                #region AddCustomBeatmaps

                try
                {
                    #region DirectorySetup

                    var path = CustomLevelPathHelper.baseProjectPath;
                    path = path.Replace('\\', '/');

                    if (!Directory.Exists(customLevelsPath))
                    {
                        Directory.CreateDirectory(customLevelsPath);
                    }

                    if (!Directory.Exists(baseProjectPath + "/CustomWIPLevels"))
                    {
                        Directory.CreateDirectory(baseProjectPath + "/CustomWIPLevels");
                    }

                    #endregion

                    #region CacheZipWIPs

                    // Get zip files in CustomWIPLevels and extract them to Cache folder
                    if (fullRefresh)
                    {
                        try
                        {
                            var wipPath = Path.Combine(path, "CustomWIPLevels");
                            var cachePath = Path.Combine(path, "CustomWIPLevels", "Cache");
                            CacheZIPs(cachePath, wipPath);

                            var cacheFolders = Directory.GetDirectories(cachePath).ToArray();
                            LoadCachedZIPs(cacheFolders, fullRefresh, CachedWIPLevels);
                        }
                        catch (Exception ex)
                        {
                            Logging.logger.Error("Failed To Load Cached WIP Levels: " + ex);
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
                                SeperateSongFolder cacheFolder = songFolder.CacheFolder;
                                try
                                {
                                    CacheZIPs(cacheFolder.SongFolderEntry.Path, songFolder.SongFolderEntry.Path);
                                }
                                catch (Exception ex)
                                {
                                    Logging.logger.Error("Failed To Load Cached WIP Levels: " + ex);
                                }
                            }
                        }
                    }

                    #endregion

                    stopwatch.Start();

                    #region LoadCustomLevels

                    // Get Levels from CustomLevels and CustomWIPLevels folders
                    var songFolders = Directory.GetDirectories(Path.Combine(path, "CustomLevels")).ToList().Concat(Directory.GetDirectories(Path.Combine(path, "CustomWIPLevels"))).ToList();
                    var loadedData = new ConcurrentBag<string>();

                    var processedSongsCount = 0;
                    Parallel.ForEach(songFolders, new ParallelOptions {MaxDegreeOfParallelism = Math.Max(1, (Environment.ProcessorCount / 2) - 1)}, (folder) =>
                    {
                        string[] results;
                        try
                        {
                            results = Directory.GetFiles(folder, "info.dat", SearchOption.TopDirectoryOnly);
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            Logging.Log($"Skipping missing or corrupt folder: '{folder}'", LogSeverity.Warning);
                            return;
                        }

                        if (results.Length == 0)
                        {
                            Logging.Log("Folder: '" + folder + "' is missing info.dat files!", LogSeverity.Notice);
                            return;
                        }

                        foreach (var result in results)
                        {
                            try
                            {
                                var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));
                                if (Directory.GetParent(songPath).Name == "Backups")
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
                                StandardLevelInfoSaveData saveData = GetStandardLevelInfoSaveData(songPath);
                                if (saveData == null)
                                {
                                    //       Logging.Log("Null save data", LogSeverity.Notice);
                                    continue;
                                }
                                //          if (loadedData.Any(x => x == saveData.))
                                //          {
                                //              Logging.Log("Duplicate song found at " + songPath, LogSeverity.Notice);
                                //              continue;
                                //          }
                                //             loadedData.Add(saveDat);

                                //HMMainThreadDispatcher.instance.Enqueue(delegate
                                //{
                                if (_loadingCancelled)
                                {
                                    return;
                                }

                                var level = LoadSongAndAddToDictionaries(_loadingTaskCancellationTokenSource.Token, saveData, songPath);
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
                                Logging.Log("Failed to load song folder: " + result, LogSeverity.Error);
                                Logging.Log(e.ToString(), LogSeverity.Error);
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
                            Instance._progressBar.ShowMessage("Loading " + (SeperateSongFolders.Count - k) + " Additional Song folders");
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
                                catch (DirectoryNotFoundException ex)
                                {
                                    Logging.Log($"Skipping missing or corrupt folder: '{folder}'", LogSeverity.Warning);
                                    continue;
                                }

                                if (results.Length == 0)
                                {
                                    Logging.Log("Folder: '" + folder + "' is missing info.dat files!", LogSeverity.Notice);
                                    continue;
                                }

                                foreach (var result in results)
                                {
                                    try
                                    {
                                        // On quick refresh: Check if the beatmap directory is already present in the respective beatmap dictionary
                                        // If it is already present on a non full refresh, it will be ignored (changes to the beatmap will not be applied)
                                        var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));
                                        if (!fullRefresh)
                                        {
                                            if (entry.SongFolderEntry.Pack == FolderLevelPack.NewPack && SearchBeatmapInMapPack(entry.Levels, songPath))
                                            {
                                                continue;
                                            }
                                            else if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomLevels && SearchBeatmapInMapPack(CustomLevels, songPath))
                                            {
                                                continue;
                                            }
                                            else if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomWIPLevels && SearchBeatmapInMapPack(CustomWIPLevels, songPath))
                                            {
                                                continue;
                                            }
                                            else if (entry.SongFolderEntry.Pack == FolderLevelPack.CachedWIPLevels && SearchBeatmapInMapPack(CachedWIPLevels, songPath))
                                            {
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

                                        StandardLevelInfoSaveData saveData = GetStandardLevelInfoSaveData(songPath);
                                        if (saveData == null)
                                        {
                                            //       Logging.Log("Null save data", LogSeverity.Notice);
                                            continue;
                                        }

                                        var count = i2;
                                        //HMMainThreadDispatcher.instance.Enqueue(delegate
                                        //{
                                        if (_loadingCancelled)
                                        {
                                            return;
                                        }

                                        var level = LoadSongAndAddToDictionaries(_loadingTaskCancellationTokenSource.Token, saveData, songPath, entry.SongFolderEntry);
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
                                        Logging.Log("Failed to load song folder: " + result, LogSeverity.Error);
                                        Logging.Log(e.ToString(), LogSeverity.Error);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log($"Failed to load Seperate Folder{SeperateSongFolders[k].SongFolderEntry.Name}" + ex, LogSeverity.Error);
                        }
                    }

                    #endregion

                    _loadingTaskCancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    Logging.Log("RetrieveAllSongs failed:", LogSeverity.Error);
                    Logging.Log(e.ToString(), LogSeverity.Error);
                }

                #endregion
            };

            Action finish = async delegate
            {
                #region CountBeatmapsAndUpdateLevelPacks

                stopwatch.Stop();
                var songCount = CustomLevels.Count + CustomWIPLevels.Count;
                var songCountWSF = songCount;
                foreach (var f in SeperateSongFolders)
                {
                    songCount += f.Levels.Count;
                }

                Logging.Log($"Loaded {songCount}  new songs ({songCountWSF}) in CustomLevels | {songCount - songCountWSF} in seperate folders) in {stopwatch.Elapsed.TotalSeconds} seconds");
                try
                {
                    //Handle LevelPacks
                    if (CustomBeatmapLevelPackCollectionSO == null || CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Length == 0)
                    {
                        // var beatmapLevelPackCollectionSO = Resources.FindObjectsOfTypeAll<BeatmapLevelPackCollectionSO>().FirstOrDefault();
                        CustomBeatmapLevelPackCollectionSO =
                            await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() => { return SongCoreBeatmapLevelPackCollectionSO.CreateNew(); });

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
                                default:
                                    break;
                            }
                        }

                        #endregion

                        #region CreateLevelPacks

                        // Create level collections and level packs
                        // Add level packs to the custom levels pack collection
                        CustomLevelsCollection = new SongCoreCustomLevelCollection(CustomLevels.Values.ToArray());
                        WIPLevelsCollection = new SongCoreCustomLevelCollection(CustomWIPLevels.Values.ToArray());
                        CachedWIPLevelCollection = new SongCoreCustomLevelCollection(CachedWIPLevels.Values.ToArray());

                        CustomLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomLevels", "Custom Levels", defaultCoverImage, CustomLevelsCollection);
                        WIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomWIPLevels", "WIP Levels", UI.BasicUI.WIPIcon, WIPLevelsCollection);
                        CachedWIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CachedWIPLevels", "Cached WIP Levels", UI.BasicUI.WIPIcon,
                            CachedWIPLevelCollection);

                        CustomBeatmapLevelPackCollectionSO.AddLevelPack(CustomLevelsPack);
                        CustomBeatmapLevelPackCollectionSO.AddLevelPack(WIPLevelsPack);
                        CustomBeatmapLevelPackCollectionSO.AddLevelPack(CachedWIPLevelsPack);

                        #endregion
                    }

                    //Level Packs
                    RefreshLevelPacks();
                }
                catch (Exception ex)
                {
                    Logging.logger.Error("Failed to Setup LevelPacks: " + ex);
                }

                #endregion

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    SongsLoadedEvent?.Invoke(this, CustomLevels);
                });

                // Write our cached hash info and
                Hashing.UpdateCachedHashesInternal(foundSongPaths.Keys);
                Hashing.UpdateCachedAudioDataInternal(foundSongPaths.Keys);
                Collections.SaveExtraSongData();
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
                Utilities.Logging.logger.Warn($"Song Loading Task Failed. {ex.Message}");
                return;
            }

            if (_loadingTask.IsCompleted && !_loadingTask.IsCanceled)
                await Task.Run(finish).ConfigureAwait(false);
            else
                Utilities.Logging.logger.Warn($"Song Loading Task Cancelled.");
            // await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(finish).ConfigureAwait(false);
            //  _loadingTask = new HMTask(job, finish);
            //  _loadingTask.Run();
        }

        public static StandardLevelInfoSaveData GetStandardLevelInfoSaveData(string path)
        {
            var text = File.ReadAllText(path + "/info.dat");
            return StandardLevelInfoSaveData.DeserializeFromJSONString(text);
        }

        /// <summary>
        /// Delete a beatmap (is only used by other mods)
        /// </summary>
        /// <param name="folderPath">Directory of the beatmap</param>
        /// <param name="deleteFolder">Option to delete the base folder of the beatmap</param>
        public void DeleteSong(string folderPath, bool deleteFolder = true)
        {
            DeletingSong?.Invoke();
            //Remove the level from SongCore Collections
            try
            {
                if (CustomLevels.TryRemove(folderPath, out var level))
                {
                }
                else if (CustomWIPLevels.TryRemove(folderPath, out level))
                {
                }
                else if (CachedWIPLevels.TryRemove(folderPath, out level))
                {
                }
                else
                {
                    foreach (var folderEntry in SeperateSongFolders)
                    {
                        if (folderEntry.Levels.TryRemove(folderPath, out level))
                        {
                        }
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

                    CustomLevelsById.TryRemove(level.levelID, out var deletedLevel);
                    Hashing.UpdateCachedHashes(new HashSet<string>((CustomLevels.Keys.Concat(CustomWIPLevels.Keys))));
                }

                //Delete the directory
                if (deleteFolder)
                {
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                    }
                }

                RefreshLevelPacks();
            }
            catch (Exception ex)
            {
                Logging.Log("Exception trying to Delete song: " + folderPath, LogSeverity.Error);
                Logging.Log(ex.ToString(), LogSeverity.Error);
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
        public static CustomPreviewBeatmapLevel LoadSong(StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry folderEntry = null)
        {
            CustomPreviewBeatmapLevel result;
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

            hash = Hashing.GetCustomLevelHash(saveData, songPath);
            try
            {
                string folderName = new DirectoryInfo(songPath).Name;
                string levelID = CustomLevelLoader.kCustomLevelPrefixId + hash;
                // Fixed WIP status for duplicate song hashes
                if (Collections.LevelHashDictionary.ContainsKey(levelID + (wip ? " WIP" : "")))
                {
                    levelID += "_" + folderName;
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
                List<PreviewDifficultyBeatmapSet> list = new List<PreviewDifficultyBeatmapSet>();
                foreach (StandardLevelInfoSaveData.DifficultyBeatmapSet difficultyBeatmapSet in saveData.difficultyBeatmapSets)
                {
                    BeatmapCharacteristicSO beatmapCharacteristicBySerializedName =
                        beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(difficultyBeatmapSet.beatmapCharacteristicName);
                    BeatmapDifficulty[] array = new BeatmapDifficulty[difficultyBeatmapSet.difficultyBeatmaps.Length];
                    for (var j = 0; j < difficultyBeatmapSet.difficultyBeatmaps.Length; j++)
                    {
                        BeatmapDifficulty beatmapDifficulty;
                        difficultyBeatmapSet.difficultyBeatmaps[j].difficulty.BeatmapDifficultyFromSerializedName(out beatmapDifficulty);
                        array[j] = beatmapDifficulty;
                    }

                    list.Add(new PreviewDifficultyBeatmapSet(beatmapCharacteristicBySerializedName, array));
                }

                result = new CustomPreviewBeatmapLevel(defaultCoverImage, saveData, songPath,
                    cachedMediaAsyncLoaderSO, cachedMediaAsyncLoaderSO, levelID, songName, songSubName,
                    songAuthorName, levelAuthorName, beatsPerMinute, songTimeOffset, shuffle, shufflePeriod,
                    previewStartTime, previewDuration, environmentSceneInfo, allDirectionEnvironmentInfo, list.ToArray());

                GetSongDuration(result, songPath, Path.Combine(songPath, saveData.songFilename));
            }
            catch
            {
                Logging.Log("Failed to Load Song: " + songPath, LogSeverity.Error);
                result = null;
            }

            return result;
        }

        public static CustomPreviewBeatmapLevel LoadSong(CancellationToken token, StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry folderEntry = null)
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

            if (Input.GetKeyDown(KeyCode.X))
            {
                if (Input.GetKey(KeyCode.LeftControl) && _loadingTask != null)
                    CancelSongLoading();
            }
        }

        #region HelperFunctionsZIP

        /// <summary>
        /// Extracts beatmap ZIP files to the cache folder
        /// </summary>
        /// <param name="cachePath">Directory of cache folder</param>
        /// <param name="songFolderPath">Directory of folder containing the zips</param>
        private void CacheZIPs(string cachePath, string songFolderPath)
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
                var unzip = new Unzip(zip);
                try
                {
                    unzip.ExtractToDirectory(Path.Combine(cachePath, new FileInfo(zip).Name));
                }
                catch (Exception ex)
                {
                    Logging.logger.Warn("Failed to extract zip: " + zip + ": " + ex);
                }

                unzip.Dispose();
            }
        }

        /// <summary>
        /// Loads the beatmaps of the cached
        /// </summary>
        /// <param name="cacheFolders">Directory of cache folder</param>
        /// <param name="fullRefresh"></param>
        /// <param name="BeatmapDictionary"></param>
        /// <param name="folderEntry"></param>
        private void LoadCachedZIPs(string[] cacheFolders, bool fullRefresh, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> BeatmapDictionary, SongFolderEntry folderEntry = null)
        {
            foreach (var cachedFolder in cacheFolders)
            {
                string[] results;
                try
                {
                    results = Directory.GetFiles(cachedFolder, "info.dat", SearchOption.AllDirectories);
                }
                catch (DirectoryNotFoundException ex)
                {
                    Logging.Log($"Skipping missing or corrupt folder: '{cachedFolder}'", LogSeverity.Warning);
                    continue;
                }

                if (results.Length == 0)
                {
                    Logging.Log("Folder: '" + cachedFolder + "' is missing info.dat files!", LogSeverity.Notice);
                    continue;
                }

                foreach (var result in results)
                {
                    try
                    {
                        var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));
                        if (!fullRefresh && BeatmapDictionary != null)
                        {
                            if (SearchBeatmapInMapPack(BeatmapDictionary, songPath))
                            {
                                continue;
                            }
                        }

                        StandardLevelInfoSaveData saveData = GetStandardLevelInfoSaveData(songPath);
                        if (saveData == null)
                        {
                            continue;
                        }

                        HMMainThreadDispatcher.instance.Enqueue(delegate
                        {
                            if (_loadingCancelled)
                            {
                                return;
                            }

                            var level = LoadSong(saveData, songPath, out string hash, folderEntry);
                            if (level != null)
                            {
                                BeatmapDictionary[songPath] = level;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logging.logger.Notice("Failed to load song from " + cachedFolder + ": " + ex);
                    }
                }
            }
        }

        #endregion

        #region HelperFunctionsLoading

        private bool SearchBeatmapInMapPack(ConcurrentDictionary<string, CustomPreviewBeatmapLevel> mapPack, string songPath)
        {
            if (mapPack.TryGetValue(songPath, out var c))
            {
                if (c != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AssignBeatmapToSeperateFolder(ConcurrentDictionary<string, CustomPreviewBeatmapLevel> mapPack, string songPath,
            ConcurrentDictionary<string, CustomPreviewBeatmapLevel> seperateFolder)
        {
            if (mapPack.TryGetValue(songPath, out var c))
            {
                if (c != null)
                {
                    seperateFolder[songPath] = c;
                    return true;
                }
            }

            return false;
        }

        private CustomPreviewBeatmapLevel LoadSongAndAddToDictionaries(CancellationToken token, StandardLevelInfoSaveData saveData, string songPath, SongFolderEntry entry = null)
        {
            var level = LoadSong(saveData, songPath, out string hash, entry);
            if (level != null)
            {
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
                        levels = new List<string>();
                        levels.Add(level.levelID);
                        Collections.HashLevelDictionary.TryAdd(hash, levels);
                    }
                }
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
        public static IPreviewBeatmapLevel GetLevelById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            IPreviewBeatmapLevel level = null;
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
        public static CustomPreviewBeatmapLevel GetLevelByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return null;
            }

            CustomLevelsById.TryGetValue("custom_level_" + hash.ToUpper(), out CustomPreviewBeatmapLevel level);
            return level;
        }

        private static BeatmapDataLoader loader = new BeatmapDataLoader();

        static void GetSongDuration(CustomPreviewBeatmapLevel level, string songPath, string oggfile)
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
                        length = GetLengthFromOgg(oggfile);
                    }
                    catch (Exception ex)
                    {
                        length = -1;
                    }

                    if (length <= 1)
                    {
                        // janky, but whatever
                        Logging.logger.Warn($"Failed to parse song length from Ogg file, Approximating using Map length. Song: {level.customLevelPath}");
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

                level.SetField<CustomPreviewBeatmapLevel, float>("_songDuration", length);

                if (Plugin.forceLongPreviews)
                {
                    level.SetField("_previewDuration", Mathf.Max(level.previewDuration, length - level.previewStartTime));
                }
            }
            catch (Exception ex)
            {
                Logging.logger.Warn("Failed to Parse Song Duration" + ex);
            }
        }

        public static float GetLengthFromMap(CustomPreviewBeatmapLevel level, string songPath)
        {
            var diff = level.standardLevelInfoSaveData.difficultyBeatmapSets.First().difficultyBeatmaps.Last().beatmapFilename;
            var beatmapsave = BeatmapSaveData.DeserializeFromJSONString(File.ReadAllText(Path.Combine(songPath, diff)));
            float highestTime = 0;
            if (beatmapsave.notes.Count > 0)
            {
                highestTime = beatmapsave.notes.Max(x => x.time);
            }
            else if (beatmapsave.events.Count > 0)
            {
                highestTime = beatmapsave.events.Max(x => x.time);
            }

            return loader.GetRealTimeFromBPMTime(highestTime, level.beatsPerMinute, level.shuffle, level.shufflePeriod);
        }


        private static byte[] oggBytes = {
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
                    Logging.logger.Warn($"could not find rate for {oggFile}");
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
                    Logging.logger.Warn($"could not find lastSample for {oggFile}");
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