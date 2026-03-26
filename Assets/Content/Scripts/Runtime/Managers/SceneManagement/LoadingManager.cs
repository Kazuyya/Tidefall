using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LittleHeroJourney.UI;

namespace LittleHeroJourney
{
    public class LoadingManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float defaultMinDisplayTime = 1.0f;
        [SerializeField] private bool showDebugLog = false;
        
        private const string DefaultLoadingSceneId = "Loading";
        
        [Header("Readiness")]
        [SerializeField] private bool requiredReadiness = false;
        [SerializeField] private bool retryOnFail = false;
        [SerializeField] private float readinessTimeout = 5f;

        private static LoadingManager _instance;
        public static LoadingManager Instance => _instance;

        public bool IsLoading { get; private set; }

        public static event Action<float> OnProgress;

        private GameCanvas _loadingCanvas;
        private LittleHeroJourney.UI.LoadingUIController _ui;
        private bool _loadingSceneLoaded = false;
        private readonly List<Func<IEnumerator>> _preLoadTasks = new List<Func<IEnumerator>>();
        private readonly List<Func<IEnumerator>> _postLoadTasks = new List<Func<IEnumerator>>();

        private bool _delayCloseUntilSignaled;
        private bool _closeAllowedSignal;

        private void Awake()
        {
            _instance = this;
        }

        public void DelayCloseUntilSignaled()
        {
            _delayCloseUntilSignaled = true;
            _closeAllowedSignal = false;
        }

        public void SignalCloseAllowed()
        {
            _closeAllowedSignal = true;
        }

        public void RegisterPreLoadTask(Func<IEnumerator> task)
        {
            if (task != null) _preLoadTasks.Add(task);
        }

        public void RegisterPostLoadTask(Func<IEnumerator> task)
        {
            if (task != null) _postLoadTasks.Add(task);
        }

        public void LoadSceneWithLoading(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, float minDisplayTime = -1f, Action onComplete = null, bool closeBeforeLoading = false)
        {
            StartCoroutine(LoadSceneSequence(sceneName, mode, minDisplayTime, onComplete, closeBeforeLoading));
        }

        public IEnumerator LoadSceneSequence(string sceneName, LoadSceneMode mode, float minDisplayTime, Action onComplete, bool closeBeforeLoading)
        {
            if (showDebugLog) Debug.Log($"[LoadingManager] LoadSceneSequence: Starting for scene '{sceneName}'");
            IsLoading = true;
            GameEventSystem.Publish(new UIActionEvent("LoadingStart", sceneName));

            yield return OpenPhaseRoutine(closeBeforeLoading);

            for (int i = 0; i < _preLoadTasks.Count; i++)
            {
                if (showDebugLog) Debug.Log($"[LoadingManager] LoadSceneSequence: Running preload task {i}");
                yield return _preLoadTasks[i].Invoke();
            }

            if (minDisplayTime <= 0f) minDisplayTime = defaultMinDisplayTime;
            
            float startTime = Time.time;
            
            LoadSceneMode loadMode = (mode == LoadSceneMode.Single) ? LoadSceneMode.Additive : mode;
            if (showDebugLog) Debug.Log($"[LoadingManager] LoadSceneSequence: Creating load operation for '{sceneName}' with mode {loadMode}");
            AsyncOperation asyncOperation = CreateLoadOperation(sceneName, loadMode);
            if (asyncOperation == null)
            {
                if (showDebugLog) Debug.LogWarning($"[LoadingManager] Failed to load scene: {sceneName}");
                yield return HideLoadingSceneRoutine();
                IsLoading = false;
                GameEventSystem.Publish(new UIActionEvent("LoadingFinished", string.Empty));
                onComplete?.Invoke();
                yield break;
            }

            asyncOperation.allowSceneActivation = false;

            while (!asyncOperation.isDone)
            {
                float sceneProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                float timeProgress = Mathf.Clamp01((Time.time - startTime) / minDisplayTime);
                float progress = Mathf.Min(sceneProgress, timeProgress);
                UpdateProgress(progress);

                if (asyncOperation.progress >= 0.9f && timeProgress >= 1f)
                {
                    if (showDebugLog) Debug.Log($"[LoadingManager] LoadSceneSequence: Scene ready, activating...");
                    GameEventSystem.Publish(new UIActionEvent("SceneReady", sceneName));
                    asyncOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Scene loaded");
            UpdateProgress(1f);
            
            yield return VerifySceneReadinessRoutine();

            for (int i = 0; i < _postLoadTasks.Count; i++)
            {
                if (showDebugLog) Debug.Log($"[LoadingManager] LoadSceneSequence: Running postload task {i}");
                yield return _postLoadTasks[i].Invoke();
            }

            Scene targetScene = GetLoadedScene(sceneName);
            string loadedSceneName = targetScene.IsValid() ? targetScene.name : string.Empty;
            if (targetScene.IsValid())
            {
                if (showDebugLog) Debug.Log($"[LoadingManager] LoadSceneSequence: Setting active scene to '" + loadedSceneName + "'");
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(targetScene);
            }

            onComplete?.Invoke();

            yield return ClosePhaseRoutine(mode, targetScene, loadedSceneName);
            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Completed! Firing LoadingFinished for '" + loadedSceneName + "'");
            IsLoading = false;
            GameEventSystem.Publish(new UIActionEvent("LoadingFinished", loadedSceneName));
        }

        private IEnumerator OpenPhaseRoutine(bool closeBeforeLoading)
        {
            if (closeBeforeLoading)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Closing current canvases before loading");
                yield return CloseCurrentCanvasesRoutine();
            }
            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Showing loading scene...");
            yield return ShowLoadingSceneRoutine();
            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Loading scene shown");
            UpdateProgress(0f);
            if (_ui != null)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Playing open sequence...");
                yield return _ui.PlayOpenSequenceRoutine();
                if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Open sequence completed");
            }
            else if (showDebugLog)
                Debug.LogWarning("[LoadingManager] LoadSceneSequence: LoadingUIController is NULL! Skipping open sequence");
            if (!closeBeforeLoading)
            {
                var canvasManager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
                if (canvasManager != null) canvasManager.CloseAllCanvasesImmediate();
            }
            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Unloading previous scene...");
            UnloadAllScenesExcept(default(Scene), GetLoadingSceneName());
        }

        private IEnumerator ClosePhaseRoutine(LoadSceneMode mode, Scene targetScene, string loadedSceneName)
        {
            PrepareTargetSceneCanvases(loadedSceneName);
            if (_delayCloseUntilSignaled)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] Close phase delayed until external signal.");
                while (!_closeAllowedSignal)
                    yield return null;
                _delayCloseUntilSignaled = false;
                _closeAllowedSignal = false;
                if (showDebugLog) Debug.Log("[LoadingManager] External close signal received. Continuing close phase.");
            }
            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Playing close sequence...");
            if (_ui != null)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] PlayCloseSequenceRoutine: UI Controller found, playing close sequence");
                yield return _ui.PlayCloseSequenceRoutine();
                if (showDebugLog) Debug.Log("[LoadingManager] PlayCloseSequenceRoutine: Close sequence completed");
            }
            else if (showDebugLog)
                Debug.LogWarning("[LoadingManager] PlayCloseSequenceRoutine: UI Controller is NULL!");
            if (mode == LoadSceneMode.Single)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Unloading remaining scenes except target...");
                UnloadAllScenesExcept(targetScene, GetLoadingSceneName());
            }
            if (showDebugLog) Debug.Log("[LoadingManager] LoadSceneSequence: Hiding loading scene...");
            yield return HideLoadingSceneRoutine();
        }

        private AsyncOperation CreateLoadOperation(string sceneString, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(sceneString)) return null;
            bool looksLikePath = sceneString.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                                 sceneString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
            if (looksLikePath)
            {
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneString);
                if (buildIndex >= 0)
                {
                    return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, mode);
                }
                if (showDebugLog) Debug.LogWarning($"[LoadingManager] Scene path not in Build Settings: {sceneString}");
            }
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneString, mode);
        }

        private string _loadedLoadingScenePath;

        private IEnumerator ShowLoadingSceneRoutine()
        {
            if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Starting...");
            
            if (_loadingSceneLoaded)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Loading scene already loaded, reusing...");
                if (_loadingCanvas != null)
                {
                    _loadingCanvas.gameObject.SetActive(true);
                }
                yield break;
            }

            string loadingPath = null;
            if (GameManager.Instance != null && GameManager.Instance.SceneManager != null)
                loadingPath = GameManager.Instance.SceneManager.ResolvePathById(DefaultLoadingSceneId);
            if (string.IsNullOrEmpty(loadingPath)) loadingPath = DefaultLoadingSceneId;

            if (showDebugLog) Debug.Log($"[LoadingManager] ShowLoadingSceneRoutine: Loading scene from path '{loadingPath}'");

            AsyncOperation op = null;
            bool looksLikePath = loadingPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                                 loadingPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
            if (looksLikePath)
            {
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(loadingPath);
                if (buildIndex >= 0)
                {
                    op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
                    _loadedLoadingScenePath = loadingPath;
                    if (showDebugLog) Debug.Log($"[LoadingManager] ShowLoadingSceneRoutine: Loaded via build index {buildIndex}");
                }
                else
                {
                    op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(DefaultLoadingSceneId, LoadSceneMode.Additive);
                    _loadedLoadingScenePath = null;
                    if (showDebugLog) Debug.LogWarning($"[LoadingManager] ShowLoadingSceneRoutine: Build index not found, falling back to scene ID '{DefaultLoadingSceneId}'");
                }
            }
            else
            {
                op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(loadingPath, LoadSceneMode.Additive);
                _loadedLoadingScenePath = null;
            }
            if (op != null)
            {
                if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Waiting for async operation...");
                while (!op.isDone)
                {
                    yield return null;
                }
                if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Loading scene loaded");
            }

            _loadingSceneLoaded = true;

            _ui = FindObjectOfType<LittleHeroJourney.UI.LoadingUIController>(true);
            if (_ui != null)
            {
                _loadingCanvas = _ui.GetComponent<GameCanvas>() ?? _ui.GetComponentInParent<GameCanvas>(true);
                if (showDebugLog) Debug.Log($"[LoadingManager] ShowLoadingSceneRoutine: Found LoadingUIController, canvas={(_loadingCanvas != null ? _loadingCanvas.gameObject.name : "null")}");
                if (showDebugLog) Debug.Log($"[LoadingManager] ShowLoadingSceneRoutine: UIController - Name='{_ui.gameObject.name}', Active={_ui.gameObject.activeSelf}, Parent='{_ui.gameObject.transform.parent?.name ?? "null"}'");
                
                if (!_ui.gameObject.activeSelf)
                {
                    if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Activating LoadingUIController GameObject");
                    _ui.gameObject.SetActive(true);
                }
                
                _ui.UpdateProgress(0f);
                
                if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Calling PrepareForOpenImmediate...");
                _ui.PrepareForOpenImmediate();
                if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: PrepareForOpenImmediate completed");
            }
            else
            {
                if (showDebugLog) Debug.LogWarning("[LoadingManager] ShowLoadingSceneRoutine: LoadingUIController NOT FOUND!");
            }
            
            if (_loadingCanvas != null)
            {
                if (showDebugLog) Debug.Log($"[LoadingManager] ShowLoadingSceneRoutine: Activating loading canvas '{_loadingCanvas.gameObject.name}'");
                _loadingCanvas.gameObject.SetActive(true);
            }
            else if (_ui == null && showDebugLog)
            {
                Debug.LogWarning("[LoadingManager] ShowLoadingSceneRoutine: LoadingUIController NOT FOUND!");
            }
            
            if (showDebugLog) Debug.Log("[LoadingManager] ShowLoadingSceneRoutine: Completed");
        }

        private IEnumerator HideLoadingSceneRoutine()
        {
            if (showDebugLog) Debug.Log("[LoadingManager] HideLoadingSceneRoutine: Starting...");
            Scene loadingScene = default;
            if (!string.IsNullOrEmpty(_loadedLoadingScenePath))
                loadingScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(_loadedLoadingScenePath);
            else
                loadingScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(DefaultLoadingSceneId);
            
            if (loadingScene.IsValid())
            {
                if (showDebugLog) Debug.Log($"[LoadingManager] HideLoadingSceneRoutine: Unloading loading scene '{loadingScene.name}'");
                AsyncOperation op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(loadingScene);
                if (op != null)
                {
                    while (!op.isDone)
                    {
                        yield return null;
                    }
                    if (showDebugLog) Debug.Log("[LoadingManager] HideLoadingSceneRoutine: Loading scene unloaded");
                }
            }
            else
            {
                if (showDebugLog) Debug.LogWarning("[LoadingManager] HideLoadingSceneRoutine: Loading scene not found!");
            }

            _loadingSceneLoaded = false;
            _loadingCanvas = null;
            _ui = null;
            _loadedLoadingScenePath = null;
            
            if (showDebugLog) Debug.Log("[LoadingManager] HideLoadingSceneRoutine: Completed");
        }

        private void UpdateProgress(float progress)
        {
            if (_ui != null)
            {
                _ui.UpdateProgress(progress);
            }
            OnProgress?.Invoke(progress);
        }
        
        private Scene GetLoadedScene(string sceneString)
        {
            if (string.IsNullOrEmpty(sceneString)) return default;
            bool looksLikePath = sceneString.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                                 sceneString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
            string name = sceneString;
            if (looksLikePath)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(sceneString);
            }
            var byName = UnityEngine.SceneManagement.SceneManager.GetSceneByName(name);
            if (byName.IsValid()) return byName;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return s;
                }
            }
            return default;
        }
        
        private string GetLoadingSceneName()
        {
            if (!string.IsNullOrEmpty(_loadedLoadingScenePath))
            {
                return System.IO.Path.GetFileNameWithoutExtension(_loadedLoadingScenePath);
            }
            return DefaultLoadingSceneId;
        }
        
        private void UnloadAllScenesExcept(Scene targetScene, string loadingSceneName)
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.IsValid()) continue;
                if (scene == targetScene) continue;
                if (!string.IsNullOrEmpty(loadingSceneName) && scene.name == loadingSceneName) continue;
                if (scene.name == "DontDestroyOnLoad") continue;
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
        }
        
        private IEnumerator VerifySceneReadinessRoutine()
        {
            if (!requiredReadiness) yield break;
            var anchor = UnityEngine.Object.FindObjectOfType<SceneReadyAnchor>();
            if (anchor == null) yield break;
            
            float start = Time.time;
            while (Time.time - start < readinessTimeout)
            {
                if (anchor.IsReady) break;
                yield return null;
            }
            
            if (!anchor.IsReady && retryOnFail)
            {
                start = Time.time;
                while (Time.time - start < readinessTimeout)
                {
                    if (anchor.IsReady) break;
                    yield return null;
                }
            }
        }

        private IEnumerator CloseCurrentCanvasesRoutine()
        {
            yield return Helper.CloseAllActiveCanvasesRoutine();
        }

        private void PrepareTargetSceneCanvases(string loadedSceneName)
        {
            if (string.IsNullOrEmpty(loadedSceneName)) return;
            var canvases = FindObjectsOfType<GameCanvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null) continue;
                if (canvas.gameObject.scene.name != loadedSceneName) continue;
                canvas.PrepareForLoadingOpen(loadedSceneName);
            }
        }
        
    }
}
