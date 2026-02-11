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
        private Dictionary<string, string> _idToPath = new Dictionary<string, string>();

        public static SceneManager Instance { get; private set; }
        public string CurrentId => _currentId;
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
        public void GoToId(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            string path = ResolvePathById(targetId);
            if (string.IsNullOrEmpty(path)) return;

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
        }

        public void StartLevel(LevelSO level)
        {
            if (level == null || string.IsNullOrEmpty(level.SceneName)) return;
            string targetId = string.IsNullOrEmpty(level.LoadTargetId) ? "level" : level.LoadTargetId;
            bool useLoading = ShouldUseLoading(_currentId, targetId, out bool closeBeforeLoading);
            if (useLoading && GameManager.Instance != null && GameManager.Instance.LoadingManager != null)
            {
                GameManager.Instance.LoadingManager.LoadSceneWithLoading(level.SceneName, LoadSceneMode.Single, -1f, null, closeBeforeLoading);
            }
            else
            {
                StartCoroutine(LoadSceneWithoutLoadingRoutine(level.SceneName, null));
            }
            _currentId = targetId;
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

        // SplashScreenManager handles transition to MainMenu after sequence completes

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
            
            // Fallback kuat: dari Splash ke MainMenu pakai loading jika useSplash aktif
            if (config.useSplash 
                && !string.IsNullOrEmpty(config.splashId) 
                && !string.IsNullOrEmpty(config.mainMenuId))
            {
                if (string.Equals(fromId, config.splashId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(toId, config.mainMenuId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            // Cek aturan transition yang dikonfigurasi
            if (config.transitions == null) return false;
            for (int i = 0; i < config.transitions.Count; i++)
            {
                var toEntry = config.transitions[i];
                if (toEntry == null) continue;
                if (!string.Equals(toEntry.toId, toId, StringComparison.OrdinalIgnoreCase)) continue;
                if (toEntry.fromRules == null || toEntry.fromRules.Count == 0) return false;
                
                // 1) Cocokkan aturan spesifik
                for (int j = 0; j < toEntry.fromRules.Count; j++)
                {
                    var fr = toEntry.fromRules[j];
                    if (fr == null) continue;
                    if (string.Equals(fr.fromId, fromId, StringComparison.OrdinalIgnoreCase))
                    {
                        closeBeforeLoading = fr.closeBeforeLoading;
                        return fr.useLoading;
                    }
                }
                
                // 2) Wildcard: fromId kosong/"*"/"any" dianggap berlaku untuk semua sumber
                for (int j = 0; j < toEntry.fromRules.Count; j++)
                {
                    var fr = toEntry.fromRules[j];
                    if (fr == null) continue;
                    bool isWildcard = string.IsNullOrEmpty(fr.fromId) ||
                                      string.Equals(fr.fromId, "*", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(fr.fromId, "any", StringComparison.OrdinalIgnoreCase);
                    if (isWildcard)
                    {
                        closeBeforeLoading = fr.closeBeforeLoading;
                        return fr.useLoading;
                    }
                }
                
                // Tidak menemukan aturan yang cocok
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
            var canvasManager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
            if (canvasManager != null)
            {
                yield return canvasManager.CloseAllCanvasesRoutine();
                yield break;
            }

            var canvases = FindObjectsOfType<GameCanvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null) continue;
                if (!canvas.gameObject.activeInHierarchy) continue;
                yield return canvas.CloseForTransitionRoutine();
            }
        }
    }
}
