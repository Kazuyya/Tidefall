using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LittleHeroJourney.UI;

namespace LittleHeroJourney
{
    public class SceneManager : MonoBehaviour
    {
        [SerializeField] private SceneFlowConfigSO config;
#pragma warning disable 0414
        [SerializeField] private bool showDebugLog = false;
#pragma warning restore 0414

        private string _currentId;
        private string _previousId;
        private Dictionary<string, string> _idToPath = new Dictionary<string, string>();
        private Dictionary<string, Action> _sceneActionHandlers = new Dictionary<string, Action>();

        public static SceneManager Instance { get; private set; }
        public string CurrentId => _currentId;
        public string PreviousId => _previousId;
        public SceneFlowConfigSO Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildIdMap();
        }

        private void OnEnable()
        {
            foreach (var kv in _idToPath)
            {
                var sceneId = kv.Key;
                var handler = new Action(() => GoToSceneById(sceneId));
                _sceneActionHandlers[sceneId] = handler;
                GameEventSystem.SubscribeAction(sceneId, handler);
            }
        }

        private void OnDisable()
        {
            foreach (var kv in _sceneActionHandlers)
                GameEventSystem.UnsubscribeAction(kv.Key, kv.Value);
            _sceneActionHandlers.Clear();
        }

        private void BuildIdMap()
        {
            _idToPath.Clear();
            if (config != null && config.scenes != null)
            {
                foreach (var entry in config.scenes)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.id) && !string.IsNullOrEmpty(entry.scenePath))
                    {
                        _idToPath[entry.id] = entry.scenePath;
                    }
                }
            }
        }

        public void StartFlow()
        {
            if (config != null && config.useSplash && !string.IsNullOrEmpty(config.splashId))
            {
                _currentId = config.splashId;
            }
            else
            {
                GoToId(config != null ? config.mainMenuId : null);
            }
        }
        public void GoToSceneById(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGameState();
            }
            GoToId(sceneId);
        }

        public void GoToId(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            string path = ResolvePathById(targetId);
            if (string.IsNullOrEmpty(path)) return;

            if (GameManager.Instance != null) GameManager.Instance.ResetGameState();

            _previousId = _currentId;
            bool useLoading = ShouldUseLoading(_currentId, targetId, out bool closeBeforeLoading);
            if (showDebugLog)
                Debug.Log($"[SceneManager] GoToId: currentId={_currentId}, targetId={targetId}, useLoading={useLoading}, hasLoadingManager={GameManager.Instance != null && GameManager.Instance.LoadingManager != null}");

            Action onComplete = null;

            if (useLoading && GameManager.Instance != null && GameManager.Instance.LoadingManager != null)
            {
                GameManager.Instance.LoadingManager.LoadSceneWithLoading(path, LoadSceneMode.Single, -1f, onComplete, closeBeforeLoading);
            }
            else
            {
                StartCoroutine(LoadSceneWithoutLoadingRoutine(path, onComplete));
            }

            _currentId = targetId;
            GameEventSystem.Publish(new UIActionEvent("SceneIdChanged", _currentId));
        }

        public void StartStageScene(string sceneName, string targetId = "gameplay", Action onComplete = null)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (string.IsNullOrEmpty(targetId)) targetId = "gameplay";
            _previousId = _currentId;
            bool useLoading = ShouldUseLoading(_currentId, targetId, out bool closeBeforeLoading);
            if (useLoading && GameManager.Instance != null && GameManager.Instance.LoadingManager != null)
            {
                GameManager.Instance.LoadingManager.LoadSceneWithLoading(sceneName, LoadSceneMode.Single, -1f, onComplete, closeBeforeLoading);
            }
            else
            {
                StartCoroutine(LoadSceneWithoutLoadingRoutine(sceneName, onComplete));
            }
            _currentId = targetId;
            GameEventSystem.Publish(new UIActionEvent("SceneIdChanged", _currentId));
        }

        public void ReturnToMainMenu()
        {
            GoToId(config != null ? config.mainMenuId : null);
        }

        public void UnloadAllScenesExcept(string sceneNameToKeep)
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name != sceneNameToKeep && scene.name != "DontDestroyOnLoad")
                {
                    UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        public string ResolvePathById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_idToPath.TryGetValue(id, out var path))
            {
                return path;
            }
            return id;
        }
        

        private bool ShouldUseLoading(string fromId, string toId, out bool closeBeforeLoading)
        {
            closeBeforeLoading = false;
            if (config == null) return false;
            if (config.useSplash && !string.IsNullOrEmpty(config.splashId) && !string.IsNullOrEmpty(config.mainMenuId) &&
                string.Equals(fromId, config.splashId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(toId, config.mainMenuId, StringComparison.OrdinalIgnoreCase))
                return true;
            if (config.transitions == null) return false;
            if (TryGetTransitionRule(fromId, toId, out bool useLoading, out closeBeforeLoading))
                return useLoading;
            return false;
        }

        private bool TryGetTransitionRule(string fromId, string toId, out bool useLoading, out bool closeBeforeLoading)
        {
            useLoading = false;
            closeBeforeLoading = false;
            if (config?.transitions == null) return false;
            var ord = StringComparison.OrdinalIgnoreCase;
            bool IsFromWildcard(string id) => string.IsNullOrEmpty(id) || string.Equals(id, "*", ord) || string.Equals(id, "any", ord);
            foreach (var toEntry in config.transitions)
            {
                if (toEntry == null || toEntry.fromRules == null || toEntry.fromRules.Count == 0) continue;
                if (!string.Equals(toEntry.toId, toId, ord)) continue;
                foreach (var fr in toEntry.fromRules)
                {
                    if (fr == null) continue;
                    if (string.Equals(fr.fromId, fromId, ord)) { useLoading = fr.useLoading; closeBeforeLoading = fr.closeBeforeLoading; return true; }
                }
                foreach (var fr in toEntry.fromRules)
                {
                    if (fr == null) continue;
                    if (IsFromWildcard(fr.fromId)) { useLoading = fr.useLoading; closeBeforeLoading = fr.closeBeforeLoading; return true; }
                }
                return false;
            }
            return false;
        }

        private IEnumerator LoadSceneWithoutLoadingRoutine(string sceneNameOrPath, Action onComplete)
        {
            yield return CloseCurrentCanvasesForTransition();
            AsyncOperation op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneNameOrPath, LoadSceneMode.Single);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }
            onComplete?.Invoke();
        }

        private IEnumerator CloseCurrentCanvasesForTransition()
        {
            yield return Helper.CloseAllActiveCanvasesRoutine();
        }
    }
}
