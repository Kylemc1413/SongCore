using SongCore.Data;
using SongCore.OverrideClasses;
using SongCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogSeverity = IPA.Logging.Logger.Level;

namespace SongCore
{
    public class Loader : MonoBehaviour
    {
        public static event Action<Loader> LoadingStartedEvent;
        public static event Action<Loader, Dictionary<string, CustomPreviewBeatmapLevel>> SongsLoadedEvent;
        public static event Action OnLevelPacksRefreshed;
        public static event Action DeletingSong;
        public static Dictionary<string, CustomPreviewBeatmapLevel> CustomLevels = new Dictionary<string, CustomPreviewBeatmapLevel>();
        public static Dictionary<string, CustomPreviewBeatmapLevel> CustomWIPLevels = new Dictionary<string, CustomPreviewBeatmapLevel>();
        public static Dictionary<string, CustomPreviewBeatmapLevel> CachedWIPLevels = new Dictionary<string, CustomPreviewBeatmapLevel>();
        public static List<SeperateSongFolder> SeperateSongFolders = new List<SeperateSongFolder>();
        public static SongCoreCustomLevelCollection CustomLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection WIPLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection CachedWIPLevelCollection { get; private set; }
        public static SongCoreCustomBeatmapLevelPack CustomLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack WIPLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack CachedWIPLevelsPack { get; private set; }
        public static SongCoreBeatmapLevelPackCollectionSO CustomBeatmapLevelPackCollectionSO { get; private set; }

        private static Dictionary<string, CustomPreviewBeatmapLevel> customLevelsById =
            new Dictionary<string, CustomPreviewBeatmapLevel>();
        /// <summary>
        /// Attempts to get a custom level by LevelId ('custom_level_HASH'). Returns null a matching custom level isn't found.
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        public static CustomPreviewBeatmapLevel GetLevelById(string levelId)
        {
            customLevelsById.TryGetValue(levelId, out CustomPreviewBeatmapLevel level);
            return level;
        }

        /// <summary>
        /// Attempts to get a custom level by hash (case-insensitive). Returns null a matching custom level isn't found.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static CustomPreviewBeatmapLevel GetLevelByHash(string hash)
        {
            customLevelsById.TryGetValue("custom_level_" + hash.ToUpper(), out CustomPreviewBeatmapLevel level);
            return level;
        }
        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; internal set; }
        internal ProgressBar _progressBar;
        private HMTask _loadingTask;
        private bool _loadingCancelled;

        private static CustomLevelLoader _customLevelLoader;
        public static BeatmapLevelsModel BeatmapLevelsModelSO
        {
            get
            {
                if (_beatmapLevelsModel == null) _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
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
            DontDestroyOnLoad(gameObject);
            BS_Utils.Utilities.BSEvents.menuSceneLoaded += MenuLoaded;
            Initialize();
        }

        private void Initialize()
        {
            if (Directory.Exists(Converter.oldFolderPath))
                Converter.PrepareExistingLibrary();
            else
                RefreshSongs();
        }
        internal void MenuLoaded()
        {
            if (AreSongsLoading)
            {
                //Scene changing while songs are loading. Since we are using a separate thread while loading, this is bad and could cause a crash.
                //So we have to stop loading.
                if (_loadingTask != null)
                {
                    _loadingTask.Cancel();
                    _loadingCancelled = true;
                    AreSongsLoading = false;
                    LoadingProgress = 0;
                    StopAllCoroutines();
                    _progressBar.ShowMessage("Loading cancelled\n<size=80%>Press Ctrl+R to refresh</size>");
                    Logging.Log("Loading was cancelled by player since they loaded another scene.");
                }
            }
            BS_Utils.Gameplay.Gamemode.Init();
            if (_customLevelLoader == null)
            {
                _customLevelLoader = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().FirstOrDefault();
                if (_customLevelLoader)
                {
                    Texture2D defaultCoverTex = _customLevelLoader.GetField<Texture2D>("_defaultPackCoverTexture2D");
                    defaultCoverImage = Sprite.Create(defaultCoverTex, new Rect(0f, 0f,
                        defaultCoverTex.width, defaultCoverTex.height), new Vector2(0.5f, 0.5f));

                    cachedMediaAsyncLoaderSO = _customLevelLoader.GetField<CachedMediaAsyncLoader>("_cachedMediaAsyncLoaderSO");
                    beatmapCharacteristicCollection = _customLevelLoader.GetField<BeatmapCharacteristicCollectionSO>("_beatmapCharacteristicCollection");
                }
                else
                {
                    Texture2D defaultCoverTex = Texture2D.blackTexture;
                    defaultCoverImage = Sprite.Create(defaultCoverTex, new Rect(0f, 0f,
                        defaultCoverTex.width, defaultCoverTex.height), new Vector2(0.5f, 0.5f));
                }
            }




        }

        public void RefreshLevelPacks()
        {
            CustomLevelsCollection.UpdatePreviewLevels(CustomLevels.Values.OrderBy(l => l.songName).ToArray());
            WIPLevelsCollection.UpdatePreviewLevels(CustomWIPLevels.Values.OrderBy(l => l.songName).ToArray());
            CachedWIPLevelCollection?.UpdatePreviewLevels(CachedWIPLevels?.Values?.OrderBy(l => l.songName).ToArray());
            if (CachedWIPLevels.Count > 0)
            {
                if (CachedWIPLevelsPack != null && !CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(CachedWIPLevelsPack))
                    CustomBeatmapLevelPackCollectionSO.AddLevelPack(CachedWIPLevelsPack);
            }
            //     else if (CachedWIPLevelsPack != null && CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(CachedWIPLevelsPack))
            //         CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Remove(CachedWIPLevelsPack);
            foreach (var folderEntry in SeperateSongFolders)
            {
                if (folderEntry.SongFolderEntry.Pack == FolderLevelPack.NewPack)
                {

                    folderEntry.LevelCollection.UpdatePreviewLevels(folderEntry.Levels.Values.OrderBy(l => l.songName).ToArray());
                    if (folderEntry.Levels.Count > 0 || (folderEntry is ModSeperateSongFolder && (folderEntry as ModSeperateSongFolder).AlwaysShow))
                    {
                        if (!CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                            CustomBeatmapLevelPackCollectionSO.AddLevelPack(folderEntry.LevelPack);
                    }
                    //          else if (CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                    //              CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Remove(folderEntry.LevelPack);
                }
            }
            BeatmapLevelsModelSO.SetField("_customLevelPackCollection", CustomBeatmapLevelPackCollectionSO);
            BeatmapLevelsModelSO.UpdateAllLoadedBeatmapLevelPacks();
            BeatmapLevelsModelSO.UpdateLoadedPreviewLevels();
            var filterNav = Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>().FirstOrDefault();
            filterNav.InitPlaylists();
            filterNav.UpdatePlaylistsData();
            AttemptReselectCurrentLevelPack(filterNav);
            OnLevelPacksRefreshed?.Invoke();
        }
        internal void AttemptReselectCurrentLevelPack(LevelFilteringNavigationController controller)
        {
            var tabBarView = controller.GetField<TabBarViewController>("_tabBarViewController");
            if (tabBarView?.selectedCellNumber != 3) return;
            var tabBarDatas = controller.GetField<object[]>("_tabBarDatas");
            if (tabBarDatas == null) return;
            int selectedPackNum = tabBarDatas[tabBarView.selectedCellNumber].GetField<int>("selectedItem");
            var currentLevelPacksCollection = tabBarDatas[tabBarView.selectedCellNumber].GetField<IAnnotatedBeatmapLevelCollection[]>("annotatedBeatmapLevelCollections");
            if (currentLevelPacksCollection == null) return;
            int packCount = currentLevelPacksCollection.Length;
            if (!(selectedPackNum < packCount)) return;
            controller.SelectBeatmapLevelPackOrPlayList(currentLevelPacksCollection[selectedPackNum] as IBeatmapLevelPack, null);

        }
        public void RefreshSongs(bool fullRefresh = true)
        {
            if (SceneManager.GetActiveScene().name == "GameCore") return;
            if (AreSongsLoading) return;

            Logging.Log(fullRefresh ? "Starting full song refresh" : "Starting song refresh");
            AreSongsLoaded = false;
            AreSongsLoading = true;
            LoadingProgress = 0;
            _loadingCancelled = false;

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


            //LevelPacks Handling


            RetrieveAllSongs(fullRefresh);
        }
        private void RetrieveAllSongs(bool fullRefresh)
        {
            var stopwatch = new Stopwatch();
            if (fullRefresh)
            {
                CustomLevels.Clear();
                CustomWIPLevels.Clear();
                CachedWIPLevels.Clear();
                Collections.levelHashDictionary.Clear();
                Collections.hashLevelDictionary.Clear();
                foreach (var folder in SeperateSongFolders) folder.Levels.Clear();
            }
            HashSet<string> foundSongPaths = fullRefresh ? new HashSet<string>() : new HashSet<string>(Hashing.cachedSongHashData.Keys);

            var baseProjectPath = CustomLevelPathHelper.baseProjectPath;
            var customLevelsPath = CustomLevelPathHelper.customLevelsDirectoryPath;

            Action job = delegate
            {
                try
                {
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

                    #region CacheZipWIPs
                    // Get zip files in CustomWIPLevels and extract them to Cache folder
                    if (fullRefresh)
                    {
                        try
                        {
                            var wipPath = Path.Combine(path, "CustomWIPLevels");
                            var cachePath = Path.Combine(path, "CustomWIPLevels", "Cache");
                            if (!Directory.Exists(cachePath))
                                Directory.CreateDirectory(cachePath);
                            var cache = new DirectoryInfo(cachePath);
                            foreach (var file in cache.GetFiles())
                                file.Delete();
                            foreach (var folder in cache.GetDirectories())
                                folder.Delete(true);
                            var zips = Directory.GetFiles(wipPath, "*.zip", SearchOption.TopDirectoryOnly);

                            foreach (var zip in zips)
                            {

                                var unzip = new Unzip(zip);
                                try
                                {
                                    unzip.ExtractToDirectory(cachePath + "/" + new FileInfo(zip).Name);
                                }
                                catch (Exception ex)
                                {
                                    Logging.logger.Warn("Failed to extract zip: " + zip + ": " + ex);
                                }
                                unzip.Dispose();
                            }

                            var cacheFolders = Directory.GetDirectories(cachePath).ToArray();
                            foreach (var cachedFolder in cacheFolders)
                            {
                                string[] results;
                                try
                                {
                                    results = Directory.GetFiles(cachedFolder, "info.dat", SearchOption.TopDirectoryOnly);
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
                                        if (!fullRefresh)
                                        {
                                            if (CachedWIPLevels.ContainsKey(songPath))
                                            {
                                                var c = CachedWIPLevels[songPath];//.FirstOrDefault(x => x.customLevelPath == songPath);
                                                if (c != null)
                                                {
                                                    continue;
                                                }
                                            }

                                        }
                                        StandardLevelInfoSaveData saveData = GetStandardLevelInfoSaveData(songPath);
                                        if (saveData == null)
                                        {
                                            continue;
                                        }

                                        HMMainThreadDispatcher.instance.Enqueue(delegate
                                        {
                                            if (_loadingCancelled) return;
                                            var level = LoadSong(saveData, songPath, out string hash);
                                            if (level != null)
                                            {

                                                CachedWIPLevels[songPath] = level;
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
                        catch (Exception ex)
                        {
                            Logging.logger.Error("Failed To Load Cached WIP Levels: " + ex);
                        }
                    }
                    #endregion

                    stopwatch.Start();

                    #region LoadCustomLevels
                    // Get Levels from CustomLevels and CustomWIPLevels folders
                    var songFolders = Directory.GetDirectories(path + "/CustomLevels").ToList().Concat(Directory.GetDirectories(path + "/CustomWIPLevels")).ToList();
                    var loadedData = new List<string>();

                    float i = 0;
                    foreach (var folder in songFolders)
                    {
                        i++;
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
                                bool wip = false;
                                if (songPath.Contains("CustomWIPLevels"))
                                    wip = true;
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

                                var count = i;
                                //HMMainThreadDispatcher.instance.Enqueue(delegate
                                //{
                                    if (_loadingCancelled) return;
                                    var level = LoadSong(saveData, songPath, out string hash);
                                    if (level != null)
                                    {
                                        if (!Collections.levelHashDictionary.ContainsKey(level.levelID))
                                        {
                                            Collections.levelHashDictionary.Add(level.levelID, hash);
                                            if (Collections.hashLevelDictionary.TryGetValue(hash, out var levels))
                                                levels.Add(level.levelID);
                                            else
                                            {
                                                levels = new List<string>();
                                                levels.Add(level.levelID);
                                                Collections.hashLevelDictionary.Add(hash, levels);
                                            }
                                        }

                                        if (!wip)
                                        {
                                            customLevelsById[level.levelID] = level;
                                            CustomLevels[songPath] = level;
                                        }
                                        else
                                            CustomWIPLevels[songPath] = level;
                                        foundSongPaths.Add(songPath);
                                    }
                                    LoadingProgress = count / songFolders.Count;
                                //});

                            }
                            catch (Exception e)
                            {
                                Logging.Log("Failed to load song folder: " + result, LogSeverity.Error);
                                Logging.Log(e.ToString(), LogSeverity.Error);
                            }
                        }
                    }
                    #endregion

                    #region LoadSeperateFolders
                    for (int k = 0; k < SeperateSongFolders.Count; k++)
                    {
                        try
                        {
                            SeperateSongFolder entry = SeperateSongFolders[k];
                            Instance._progressBar.ShowMessage("Loading " + (SeperateSongFolders.Count - k) + " Additional Song folders");
                            if (!Directory.Exists(entry.SongFolderEntry.Path)) continue;

                            var entryFolders = Directory.GetDirectories(entry.SongFolderEntry.Path).ToList();

                            float i2 = 0;
                            foreach (var folder in entryFolders)
                            {
                                i2++;
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
                                        var songPath = Path.GetDirectoryName(result.Replace('\\', '/'));
                                        if (!fullRefresh)
                                        {
                                            if (entry.Levels.TryGetValue(songPath, out var c))
                                            {
                                                if (c != null) continue;
                                            }
                                        }
                                        if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomLevels)
                                        {
                                            if (CustomLevels.TryGetValue(songPath, out var c))
                                            {
                                                if (c != null)
                                                {
                                                    continue;
                                                }

                                            }
                                        }
                                        else if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomWIPLevels)
                                        {
                                            if (CustomWIPLevels.TryGetValue(songPath, out var c))
                                            {
                                                if (c != null)
                                                {
                                                    continue;
                                                }

                                            }
                                        }
                                        else
                                        {
                                            if (entry.SongFolderEntry.Pack == FolderLevelPack.CustomLevels || (entry.SongFolderEntry.Pack == FolderLevelPack.NewPack && entry.SongFolderEntry.WIP == false))
                                            {
                                                if (CustomLevels.TryGetValue(songPath, out var c))
                                                {
                                                    if (c != null)
                                                    {
                                                        entry.Levels[songPath] = c;
                                                        continue;
                                                    }
                                                }
                                                if (CustomWIPLevels.TryGetValue(songPath, out var wip))
                                                {
                                                    if (wip != null)
                                                    {
                                                        entry.Levels[songPath] = wip;
                                                        continue;
                                                    }

                                                }
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
                                        if (_loadingCancelled) return;
                                        var level = LoadSong(saveData, songPath, out string hash, entry.SongFolderEntry);
                                        if (level != null)
                                        {
                                            if (!Collections.levelHashDictionary.ContainsKey(level.levelID))
                                            {
                                                Collections.levelHashDictionary.Add(level.levelID, hash);
                                                if (Collections.hashLevelDictionary.TryGetValue(hash, out var levels))
                                                    levels.Add(level.levelID);
                                                else
                                                {
                                                    levels = new List<string>();
                                                    levels.Add(level.levelID);
                                                    Collections.hashLevelDictionary.Add(hash, levels);
                                                }
                                            }

                                            entry.Levels[songPath] = level;
                                            customLevelsById[level.levelID] = level;
                                            foundSongPaths.Add(songPath);
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
                }
                catch (Exception e)
                {
                    Logging.Log("RetrieveAllSongs failed:", LogSeverity.Error);
                    Logging.Log(e.ToString(), LogSeverity.Error);
                }
            };

            Action finish = delegate
            {
                stopwatch.Stop();
                int songCount = CustomLevels.Count + CustomWIPLevels.Count;
                int songCountWSF = songCount;
                foreach (var f in SeperateSongFolders)
                    songCount += f.Levels.Count;
                Logging.Log("Loaded " + songCount + " new songs (" + songCountWSF + " in CustomLevels | " + (songCount - songCountWSF) + " in seperate folders) in " + stopwatch.Elapsed.TotalSeconds + " seconds");
                try
                {
                    //Handle LevelPacks
                    if (CustomBeatmapLevelPackCollectionSO == null)
                    {
                        var beatmapLevelPackCollectionSO = Resources.FindObjectsOfTypeAll<BeatmapLevelPackCollectionSO>().FirstOrDefault();
                        CustomBeatmapLevelPackCollectionSO = SongCoreBeatmapLevelPackCollectionSO.CreateNew(); // (beatmapLevelPackCollectionSO);
                        foreach (var folderEntry in SeperateSongFolders)
                        {
                            switch (folderEntry.SongFolderEntry.Pack)
                            {
                                case FolderLevelPack.CustomWIPLevels:
                                    {
                                        CustomWIPLevels = CustomWIPLevels.Concat(folderEntry.Levels.Where(x => !CustomWIPLevels.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value);
                                        break;
                                    }
                                case FolderLevelPack.CustomLevels:
                                    CustomLevels = CustomLevels.Concat(folderEntry.Levels.Where(x => !CustomLevels.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value);
                                    break;
                                default:
                                    break;
                            }
                        }

                        CustomLevelsCollection = new SongCoreCustomLevelCollection(CustomLevels.Values.ToArray());
                        WIPLevelsCollection = new SongCoreCustomLevelCollection(CustomWIPLevels.Values.ToArray());
                        CustomLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomLevels", "Custom Levels", defaultCoverImage, CustomLevelsCollection);
                        WIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CustomWIPLevels", "WIP Levels", UI.BasicUI.WIPIcon, WIPLevelsCollection);
                        CustomBeatmapLevelPackCollectionSO.AddLevelPack(CustomLevelsPack);
                        CustomBeatmapLevelPackCollectionSO.AddLevelPack(WIPLevelsPack);

                        if (CachedWIPLevels.Count > 0)
                        {
                            if (CachedWIPLevelCollection == null)
                            {
                                CachedWIPLevelCollection = new SongCoreCustomLevelCollection(CachedWIPLevels.Values.ToArray());
                                CachedWIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoader.kCustomLevelPackPrefixId + "CachedWIPLevels", "Cached WIP Levels", UI.BasicUI.WIPIcon, CachedWIPLevelCollection);
                                CustomBeatmapLevelPackCollectionSO.AddLevelPack(CachedWIPLevelsPack);
                            }

                        }
                        //       else if (CachedWIPLevelsPack != null && CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(CachedWIPLevelsPack))
                        //           CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Remove(CachedWIPLevelsPack);

                        foreach (var folderEntry in SeperateSongFolders)
                        {
                            if (folderEntry.SongFolderEntry.Pack == FolderLevelPack.NewPack)
                            {

                                folderEntry.LevelCollection.UpdatePreviewLevels(folderEntry.Levels.Values.OrderBy(l => l.songName).ToArray());
                                if (folderEntry.Levels.Count > 0)
                                {
                                    if (!CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                                        CustomBeatmapLevelPackCollectionSO.AddLevelPack(folderEntry.LevelPack);
                                }
                                //         else if (CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Contains(folderEntry.LevelPack))
                                //             CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Remove(folderEntry.LevelPack);
                            }
                        }
                        //           CustomBeatmapLevelPackCollectionSO.ReplaceReferences();
                    }

                    //RefreshLevelPacks();
                    //Level Packs
                    RefreshLevelPacks();
                }
                catch (Exception ex)
                {
                    Logging.logger.Error("Failed to Setup LevelPacks: " + ex);
                }

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;

                SongsLoadedEvent?.Invoke(this, CustomLevels);

                // Write our cached hash info and 

                Hashing.UpdateCachedHashes(foundSongPaths);
                SongCore.Collections.SaveExtraSongData();


            };

            _loadingTask = new HMTask(job, finish);
            _loadingTask.Run();



        }

        public static StandardLevelInfoSaveData GetStandardLevelInfoSaveData(string path)
        {
            var text = File.ReadAllText(path + "/info.dat");
            return StandardLevelInfoSaveData.DeserializeFromJSONString(text);

        }
        public void DeleteSong(string folderPath, bool deleteFolder = true)
        {
            DeletingSong?.Invoke();
            //Remove the level from SongCore Collections
            try
            {
                CustomPreviewBeatmapLevel level = null;
                if (CustomLevels.TryGetValue(folderPath, out level))
                {
                    CustomLevels.Remove(folderPath);
                }
                else if (CustomWIPLevels.TryGetValue(folderPath, out level))
                {
                    CustomWIPLevels.Remove(folderPath);
                }
                else if (CachedWIPLevels.TryGetValue(folderPath, out level))
                {
                    CachedWIPLevels.Remove(folderPath);
                }
                else
                {
                    foreach (var folderEntry in SeperateSongFolders)
                    {
                        if (folderEntry.Levels.TryGetValue(folderPath, out level))
                            folderEntry.Levels.Remove(folderPath);
                    }
                }

                if (level != null)
                {
                    if (Collections.levelHashDictionary.ContainsKey(level.levelID))
                    {
                        string hash = Collections.hashForLevelID(level.levelID);
                        Collections.levelHashDictionary.Remove(level.levelID);
                        if (Collections.hashLevelDictionary.ContainsKey(hash))
                        {
                            Collections.hashLevelDictionary[hash].Remove(level.levelID);
                            if (Collections.hashLevelDictionary[hash].Count == 0)
                                Collections.hashLevelDictionary.Remove(hash);
                        }
                    }
                    Hashing.UpdateCachedHashes(new HashSet<string>((CustomLevels.Keys.Concat(CustomWIPLevels.Keys))));
                }

                //Delete the directory
                if (deleteFolder)
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                    }
                RefreshLevelPacks();
            }
            catch (Exception ex)
            {
                Logging.Log("Exception trying to Delete song: " + folderPath, LogSeverity.Error);
                Logging.Log(ex.ToString(), LogSeverity.Error);
            }

        }
        /*
        public void RetrieveNewSong(string folderPath)
        {
            try
            {
                bool wip = false;
                if (folderPath.Contains("CustomWIPLevels"))
                    wip = true;
                StandardLevelInfoSaveData saveData = GetStandardLevelInfoSaveData(folderPath);
                var level = LoadSong(saveData, folderPath, out string hash);
                if (level != null)
                {
                    if (!wip)
                        CustomLevels[folderPath] = level;
                    else
                        CustomWIPLevels[folderPath] = level;
                    if (!Collections.levelHashDictionary.ContainsKey(level.levelID))
                    {
                        Collections.levelHashDictionary.Add(level.levelID, hash);
                        if (Collections.hashLevelDictionary.ContainsKey(hash))
                            Collections.hashLevelDictionary[hash].Add(level.levelID);
                        else
                        {
                            var levels = new List<string>();
                            levels.Add(level.levelID);
                            Collections.hashLevelDictionary.Add(hash, levels);
                        }
                    }
                }
                HashSet<string> paths = new HashSet<string>( Hashing.cachedSongHashData.Keys);
                paths.Add(folderPath);
                Hashing.UpdateCachedHashes(paths);
                RefreshLevelPacks();
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to Retrieve New Song from: " + folderPath, LogSeverity.Error);
                Logging.Log(ex.ToString(), LogSeverity.Error);
            }
        }
        */
        public static CustomPreviewBeatmapLevel LoadSong(StandardLevelInfoSaveData saveData, string songPath, out string hash, SongFolderEntry folderEntry = null)
        {
            CustomPreviewBeatmapLevel result;
            bool wip = false;
            if (songPath.Contains("CustomWIPLevels")) wip = true;
            if (folderEntry != null)
            {
                if (folderEntry.Pack == FolderLevelPack.CustomWIPLevels)
                    wip = true;
                else if (folderEntry.WIP)
                    wip = true;
            }
            hash = Hashing.GetCustomLevelHash(saveData, songPath);
            try
            {
                string folderName = new DirectoryInfo(songPath).Name;
                string levelID = CustomLevelLoader.kCustomLevelPrefixId + hash;
                if (wip) levelID += " WIP";
                if (Collections.levelHashDictionary.ContainsKey(levelID))
                    levelID += "_" + folderName;
                string songName = saveData.songName;
                string songSubName = saveData.songSubName;
                string songAuthorName = saveData.songAuthorName;
                string levelAuthorName = saveData.levelAuthorName;
                float beatsPerMinute = saveData.beatsPerMinute;
                float songTimeOffset = saveData.songTimeOffset;
                float shuffle = saveData.shuffle;
                float shufflePeriod = saveData.shufflePeriod;
                float previewStartTime = saveData.previewStartTime;
                float previewDuration = saveData.previewDuration;
                EnvironmentInfoSO environmentSceneInfo = _customLevelLoader.LoadEnvironmentInfo(saveData.environmentName, false);
                EnvironmentInfoSO allDirectionEnvironmentInfo = _customLevelLoader.LoadEnvironmentInfo(saveData.allDirectionsEnvironmentName, true);
                List<PreviewDifficultyBeatmapSet> list = new List<PreviewDifficultyBeatmapSet>();
                foreach (StandardLevelInfoSaveData.DifficultyBeatmapSet difficultyBeatmapSet in saveData.difficultyBeatmapSets)
                {
                    BeatmapCharacteristicSO beatmapCharacteristicBySerializedName = beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(difficultyBeatmapSet.beatmapCharacteristicName);
                    BeatmapDifficulty[] array = new BeatmapDifficulty[difficultyBeatmapSet.difficultyBeatmaps.Length];
                    for (int j = 0; j < difficultyBeatmapSet.difficultyBeatmaps.Length; j++)
                    {
                        BeatmapDifficulty beatmapDifficulty;
                        difficultyBeatmapSet.difficultyBeatmaps[j].difficulty.BeatmapDifficultyFromSerializedName(out beatmapDifficulty);
                        array[j] = beatmapDifficulty;
                    }
                    list.Add(new PreviewDifficultyBeatmapSet(beatmapCharacteristicBySerializedName, array));
                }

                result = new CustomPreviewBeatmapLevel(defaultCoverImage.texture, saveData, songPath,
                    cachedMediaAsyncLoaderSO, cachedMediaAsyncLoaderSO, levelID, songName, songSubName,
                    songAuthorName, levelAuthorName, beatsPerMinute, songTimeOffset, shuffle, shufflePeriod,
                    previewStartTime, previewDuration, environmentSceneInfo, allDirectionEnvironmentInfo, list.ToArray());
            }
            catch
            {
                Logging.Log("Failed to Load Song: " + songPath, LogSeverity.Error);
                result = null;
            }
            return result;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RefreshSongs(Input.GetKey(KeyCode.LeftControl));
            }
        }

    }
}
