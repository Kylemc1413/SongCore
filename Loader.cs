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
using LogSeverity = IPA.Logging.Logger.Level;
namespace SongCore
{
    public class Loader : MonoBehaviour
    {
        public static event Action<Loader> LoadingStartedEvent;
        public static event Action<Loader, List<CustomPreviewBeatmapLevel>> SongsLoadedEvent;
        public static List<CustomPreviewBeatmapLevel> CustomLevels = new List<CustomPreviewBeatmapLevel>();
        public static List<CustomPreviewBeatmapLevel> CustomWIPLevels = new List<CustomPreviewBeatmapLevel>();
        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; private set; }
        private ProgressBar _progressBar;
        private HMTask _loadingTask;
        private bool _loadingCancelled;

        private static CustomLevelLoaderSO _customLevelLoader;
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
                //Handle LevelPacks


            }



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
            var levelList = new List<CustomPreviewBeatmapLevel>();
            var wipLevelList = new List<CustomPreviewBeatmapLevel>();
            if (fullRefresh)
            {
                CustomLevels.Clear();
            }

            Action job = delegate
            {
                try
                {
                    stopwatch.Start();
                    var path = CustomLevelPathHelper.baseProjectPath;
                    path = path.Replace('\\', '/');

                    if (!Directory.Exists(CustomLevelPathHelper.customLevelsDirectoryPath))
                    {
                        Directory.CreateDirectory(CustomLevelPathHelper.customLevelsDirectoryPath);
                    }

                    if (!Directory.Exists(path + "/CustomWIPLevels"))
                    {
                        Directory.CreateDirectory(path + "/CustomWIPLevels");
                    }

                    var songFolders = Directory.GetDirectories(path + "/CustomLevels").ToList().Concat(Directory.GetDirectories(path + "/CustomWIPLevels")).ToList();
                    var loadedData = new List<string>();

                    float i = 0;
                    foreach (var folder in songFolders)
                    {
                        i++;
                        var results = Directory.GetFiles(folder, "info.dat", SearchOption.AllDirectories);
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
                                    var c = CustomLevels.FirstOrDefault(x => x.customLevelPath == songPath);
                                    if (c != null)
                                    {
                                        loadedData.Add(c.levelID);
                                        continue;
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
                                    var level = LoadSong(saveData, songPath);
                                    if (level != null)
                                    {
                                        if (!wip)
                                            levelList.Add(level);
                                        else
                                            wipLevelList.Add(level);
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
                Logging.Log("Loaded " + levelList.Count + " new songs in " + stopwatch.Elapsed.Seconds + " seconds");

                CustomLevels.AddRange(levelList);
                var orderedList = CustomLevels.OrderBy(x => x.songName);
                CustomLevels = orderedList.ToList();
                CustomWIPLevels.AddRange(wipLevelList);
                orderedList = CustomWIPLevels.OrderBy(x => x.songName);
                CustomWIPLevels = orderedList.ToList();
                //Level Packs

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;

                SongsLoadedEvent?.Invoke(this, CustomLevels);

                foreach(var level in CustomWIPLevels)
                {
                    Logging.Log(level.levelID);
                }
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
        public static CustomPreviewBeatmapLevel LoadSong(StandardLevelInfoSaveData saveData, string songPath)
        {
            CustomPreviewBeatmapLevel result;
            try
            {
                string levelID = songPath.Contains("CustomWIPLevels") ? CustomLevelLoaderSO.kCustomLevelPrefixId + new DirectoryInfo(songPath).Name + " WIP" : CustomLevelLoaderSO.kCustomLevelPrefixId + new DirectoryInfo(songPath).Name;
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
