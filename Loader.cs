using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongCore.Utilities;
using SongCore.OverrideClasses;
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
        public static SongCoreCustomLevelCollection CustomLevelsCollection { get; private set; }
        public static SongCoreCustomLevelCollection WIPLevelsCollection { get; private set; }
        public static SongCoreCustomBeatmapLevelPack CustomLevelsPack { get; private set; }
        public static SongCoreCustomBeatmapLevelPack WIPLevelsPack { get; private set; }
        public static SongCoreBeatmapLevelPackCollectionSO CustomBeatmapLevelPackCollectionSO { get; private set; }


        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; internal set; }
        internal ProgressBar _progressBar;
        private HMTask _loadingTask;
        private bool _loadingCancelled;

        private static CustomLevelLoaderSO _customLevelLoader;
        public static BeatmapLevelsModelSO BeatmapLevelsModelSO { get; private set; }
        public static Sprite defaultCoverImage;
        public static CachedMediaAsyncLoaderSO cachedMediaAsyncLoaderSO { get; private set; }
        public static BeatmapCharacteristicCollectionSO beatmapCharacteristicCollection { get; private set; }

        public static Loader Instance;

        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("SongCore Loader").AddComponent<Loader>();
        }

        private void Awake()
        {
            Instance = this;
            _progressBar = ProgressBar.Create();
            OnSceneChanged(SceneManager.GetActiveScene(), SceneManager.GetActiveScene());
            Hashing.ReadCachedSongHashes();
            if (Directory.Exists(Converter.oldFolderPath)) Converter.PrepareExistingLibrary();
            else
                RefreshSongs();
            DontDestroyOnLoad(gameObject);

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        internal void OnSceneChanged(Scene oldScene, Scene newScene)
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
            if (newScene.name == "MenuCore")
            {
                BS_Utils.Gameplay.Gamemode.Init();
                if (_customLevelLoader == null)
                {
                    _customLevelLoader = Resources.FindObjectsOfTypeAll<CustomLevelLoaderSO>().FirstOrDefault();
                    if (_customLevelLoader)
                    {
                        Texture2D defaultCoverTex = _customLevelLoader.GetField<Texture2D>("_defaultPackCoverTexture2D");
                        defaultCoverImage = Sprite.Create(defaultCoverTex, new Rect(0f, 0f,
                            (float)defaultCoverTex.width, (float)defaultCoverTex.height), new Vector2(0.5f, 0.5f));

                        cachedMediaAsyncLoaderSO = _customLevelLoader.GetField<CachedMediaAsyncLoaderSO>("_cachedMediaAsyncLoaderSO");
                        beatmapCharacteristicCollection = _customLevelLoader.GetField<BeatmapCharacteristicCollectionSO>("_beatmapCharacteristicCollection");
                    }
                    else
                    {
                        Texture2D defaultCoverTex = Texture2D.blackTexture;
                        defaultCoverImage = Sprite.Create(defaultCoverTex, new Rect(0f, 0f,
                            (float)defaultCoverTex.width, (float)defaultCoverTex.height), new Vector2(0.5f, 0.5f));
                    }
                }

                if (BeatmapLevelsModelSO == null)
                    BeatmapLevelsModelSO = Resources.FindObjectsOfTypeAll<BeatmapLevelsModelSO>().FirstOrDefault();

                //Handle LevelPacks
                if (CustomBeatmapLevelPackCollectionSO == null)
                {
                    var beatmapLevelPackCollectionSO = Resources.FindObjectsOfTypeAll<BeatmapLevelPackCollectionSO>().FirstOrDefault();
                    CustomBeatmapLevelPackCollectionSO = SongCoreBeatmapLevelPackCollectionSO.ReplaceOriginal(beatmapLevelPackCollectionSO);
                    CustomLevelsCollection = new SongCoreCustomLevelCollection(CustomLevels.Values.ToArray());
                    WIPLevelsCollection = new SongCoreCustomLevelCollection(CustomWIPLevels.Values.ToArray());
                    CustomLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoaderSO.kCustomLevelPackPrefixId + "CustomLevels", "Custom Maps", defaultCoverImage, CustomLevelsCollection);
                    WIPLevelsPack = new SongCoreCustomBeatmapLevelPack(CustomLevelLoaderSO.kCustomLevelPackPrefixId + "CustomWIPLevels", "WIP Maps", UI.BasicUI.WIPIcon, WIPLevelsCollection);
                    CustomBeatmapLevelPackCollectionSO.AddLevelPack(CustomLevelsPack);
                    CustomBeatmapLevelPackCollectionSO.AddLevelPack(WIPLevelsPack);

                    //    CustomBeatmapLevelPackSO = CustomBeatmapLevelPackSO.GetPack(CustomLevelCollectionSO);
                    //    CustomBeatmapLevelPackCollectionSO.AddLevelPack(CustomBeatmapLevelPackSO);
                    //    WIPCustomBeatmapLevelPackSO = CustomBeatmapLevelPackSO.GetPack(WIPCustomLevelCollectionSO, true);
                    //    CustomBeatmapLevelPackCollectionSO.AddLevelPack(WIPCustomBeatmapLevelPackSO);
                    CustomBeatmapLevelPackCollectionSO.ReplaceReferences();
                }
                else
                {
                    CustomBeatmapLevelPackCollectionSO.ReplaceReferences();
                }
                //RefreshLevelPacks();
                var soloFreePlay = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault();
                LevelPacksViewController levelPacksViewController = soloFreePlay?.GetField<LevelPacksViewController>("_levelPacksViewController");
                levelPacksViewController?.SetData(CustomBeatmapLevelPackCollectionSO, 0);
            }
        }

        public void RefreshLevelPacks()
        {
            CustomLevelsCollection.UpdatePreviewLevels(CustomLevels.Values.OrderBy(l => l.songName).ToArray());
            WIPLevelsCollection.UpdatePreviewLevels(CustomWIPLevels.Values.OrderBy(l => l.songName).ToArray());
            BeatmapLevelsModelSO.SetField("_loadedBeatmapLevelPackCollection", CustomBeatmapLevelPackCollectionSO);
            BeatmapLevelsModelSO.SetField("_allLoadedBeatmapLevelPackCollection", CustomBeatmapLevelPackCollectionSO);
            BeatmapLevelsModelSO.UpdateLoadedPreviewLevels();

            OnLevelPacksRefreshed?.Invoke();
        }

        public void RefreshSongs(bool fullRefresh = true)
        {
            if (SceneManager.GetActiveScene().name != "MenuCore") return;
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
            }
            HashSet<string> foundSongPaths = fullRefresh ? new HashSet<string>() : new HashSet<string>(Hashing.cachedSongHashData.Keys);

            var baseProjectPath = CustomLevelPathHelper.baseProjectPath;
            var customLevelsPath = CustomLevelPathHelper.customLevelsDirectoryPath;

            Action job = delegate
            {
                try
                {
                    stopwatch.Start();
                    baseProjectPath = baseProjectPath.Replace('\\', '/');

                    if (!Directory.Exists(customLevelsPath))
                    {
                        Directory.CreateDirectory(customLevelsPath);
                    }

                    if (!Directory.Exists(baseProjectPath + "/CustomWIPLevels"))
                    {
                        Directory.CreateDirectory(baseProjectPath + "/CustomWIPLevels");
                    }

                    var songFolders = Directory.GetDirectories(baseProjectPath + "/CustomLevels").ToList().Concat(Directory.GetDirectories(baseProjectPath + "/CustomWIPLevels")).ToList();
                    var loadedData = new List<string>();

                    float i = 0;
                    foreach (var folder in songFolders)
                    {
                        i++;
                        var results = Directory.GetFiles(folder, "info.dat", SearchOption.TopDirectoryOnly);
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
                                    if (CustomLevels.ContainsKey(songPath))
                                    {
                                        var c = CustomLevels[songPath];//.FirstOrDefault(x => x.customLevelPath == songPath);
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
                                HMMainThreadDispatcher.instance.Enqueue(delegate
                                {
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

                                            /*
                                            if (Collections.hashLevelDictionary.ContainsKey(hash))
                                                Collections.hashLevelDictionary[hash].Add(level.levelID);
                                            else
                                            {
                                                var levels = new List<string>();
                                                levels.Add(level.levelID);
                                                Collections.hashLevelDictionary.Add(hash, levels);
                                            }
                                            */
                                        }
                                        /*
                                        string hash = Utils.GetCustomLevelHash(level);
                                        if (!Collections._loadedHashes.ContainsKey(hash))
                                        {
                                            List<CustomPreviewBeatmapLevel> value = new List<CustomPreviewBeatmapLevel>();
                                            value.Add(level);
                                            Collections._loadedHashes.Add(hash, value);
                                        }
                                        else
                                            Collections._loadedHashes[hash].Add(level);
                                            */
                                        if (!wip)
                                            CustomLevels[songPath] = level;
                                        else
                                            CustomWIPLevels[songPath] = level;
                                        foundSongPaths.Add(songPath);
                                    }
                                    LoadingProgress = count / songFolders.Count;
                                });

                            }
                            catch (Exception e)
                            {
                                Logging.Log("Failed to load song folder: " + result, LogSeverity.Error);
                                Logging.Log(e.ToString(), LogSeverity.Error);
                            }
                        }
                    }
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
                Logging.Log("Loaded " + (CustomLevels.Count + CustomWIPLevels.Count) + " new songs in " + stopwatch.Elapsed.TotalSeconds + " seconds");

                //Level Packs
                RefreshLevelPacks();

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
                CustomPreviewBeatmapLevel level = CustomLevels[folderPath];//.FirstOrDefault(x => x.customLevelPath == folderPath);
                if (level != null)
                {
                    CustomLevels.Remove(folderPath);
                }
                else
                {
                    level = CustomWIPLevels[folderPath];//.FirstOrDefault(x => x.customLevelPath == folderPath);
                }
                if (level != null)
                {
                    CustomWIPLevels.Remove(folderPath);

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
        public static CustomPreviewBeatmapLevel LoadSong(StandardLevelInfoSaveData saveData, string songPath, out string hash)
        {
            CustomPreviewBeatmapLevel result;
            hash = Hashing.GetCustomLevelHash(saveData, songPath);
            try
            {
                string folderName = new DirectoryInfo(songPath).Name;
                string levelID = "custom_level_" + (songPath.Contains("CustomWIPLevels") ? hash + " WIP" : hash);
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
                SceneInfo environmentSceneInfo = _customLevelLoader.LoadSceneInfo(saveData.environmentName);
                List<BeatmapCharacteristicSO> list = new List<BeatmapCharacteristicSO>();
                foreach (StandardLevelInfoSaveData.DifficultyBeatmapSet difficultyBeatmapSet in saveData.difficultyBeatmapSets)
                {
                    BeatmapCharacteristicSO beatmapCharacteristicBySerialiedName = beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerialiedName(difficultyBeatmapSet.beatmapCharacteristicName);
                    if (beatmapCharacteristicBySerialiedName != null)
                    {
                        list.Add(beatmapCharacteristicBySerialiedName);
                    }
                }
                result = new CustomPreviewBeatmapLevel(defaultCoverImage.texture, saveData, songPath,
                    cachedMediaAsyncLoaderSO, cachedMediaAsyncLoaderSO, levelID, songName, songSubName,
                    songAuthorName, levelAuthorName, beatsPerMinute, songTimeOffset, shuffle, shufflePeriod,
                    previewStartTime, previewDuration, environmentSceneInfo, list.ToArray());
            }
            catch (Exception ex)
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
