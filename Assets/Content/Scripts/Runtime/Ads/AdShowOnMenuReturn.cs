using UnityEngine;
using LittleHeroJourney.UI;
using System.Collections;

namespace LittleHeroJourney
{
    public class AdShowOnMenuReturn : MonoBehaviour
    {
        private static bool _bootstrapped;
        private AdsSettingsSO _settings;
        private System.Action<string> _loadingFinishedHandler;
        private System.Action<string> _canvasOpenCompleteHandler;
        private Coroutine _delayedShowRoutine;
        private float _lastShowAttemptTime;
        private bool _pendingShowAfterMainMenuOpen;
        private bool DebugLogsEnabled => _settings == null || _settings.enableDebugLogs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_bootstrapped) return;
            if (Object.FindObjectOfType<AdShowOnMenuReturn>() != null)
            {
                _bootstrapped = true;
                return;
            }
            var go = new GameObject("AdShowOnMenuReturn");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AdShowOnMenuReturn>();
            _bootstrapped = true;
        }

        private void OnEnable()
        {
            _settings = Resources.Load<AdsSettingsSO>("AdsSettings");
            _loadingFinishedHandler = HandleLoadingFinished;
            _canvasOpenCompleteHandler = HandleCanvasOpenComplete;
            GameEventSystem.SubscribeActionWithPayload("LoadingFinished", _loadingFinishedHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompleteHandler);
            Log("OnEnable");
        }

        private void OnDisable()
        {
            if (_loadingFinishedHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("LoadingFinished", _loadingFinishedHandler);
            if (_canvasOpenCompleteHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompleteHandler);
            if (_delayedShowRoutine != null)
            {
                StopCoroutine(_delayedShowRoutine);
                _delayedShowRoutine = null;
            }
        }

        private static bool IsGameplayId(string id)
        {
            var sm = SceneManager.Instance;
            if (sm != null && sm.Config != null && sm.Config.scenes != null)
            {
                for (int i = 0; i < sm.Config.scenes.Count; i++)
                {
                    var e = sm.Config.scenes[i];
                    if (e != null && string.Equals(e.id, id, System.StringComparison.OrdinalIgnoreCase))
                        return e.kind == SceneFlowConfigSO.SceneKind.Gameplay;
                }
            }
            return false;
        }

        private void HandleLoadingFinished(string loadedSceneName)
        {
            _pendingShowAfterMainMenuOpen = false;
            Log("LoadingFinished scene=" + loadedSceneName);
            if (!ShouldTriggerOnCurrentFlow(loadedSceneName))
            {
                Log("LoadingFinished ignored by flow rules");
                return;
            }
            _pendingShowAfterMainMenuOpen = true;
            Log("LoadingFinished accepted, waiting CanvasOpenComplete");
        }

        private void HandleCanvasOpenComplete(string canvasId)
        {
            Log("CanvasOpenComplete canvasId=" + canvasId + " pending=" + _pendingShowAfterMainMenuOpen);
            if (!_pendingShowAfterMainMenuOpen) return;
            if (_delayedShowRoutine != null)
            {
                StopCoroutine(_delayedShowRoutine);
                _delayedShowRoutine = null;
            }
            _pendingShowAfterMainMenuOpen = false;
            _delayedShowRoutine = StartCoroutine(ShowAfterDelayRoutine());
        }

        private bool ShouldTriggerOnCurrentFlow(string loadedSceneName)
        {
            var sm = SceneManager.Instance;
            if (sm == null) return false;
            var cfg = sm.Config;
            string mainMenuId = cfg != null ? cfg.mainMenuId : null;
            if (string.IsNullOrEmpty(mainMenuId))
                mainMenuId = _settings != null ? _settings.mainMenuSceneId : "MainMenu";
            if (!string.Equals(sm.CurrentId, mainMenuId, System.StringComparison.OrdinalIgnoreCase)) return false;
            string prev = sm.PreviousId;
            if (string.IsNullOrEmpty(prev)) return false;
            bool fromGameplay = IsGameplayId(prev);
            if (!fromGameplay)
            {
                string fallbackGameplay = _settings != null ? _settings.gameplaySceneId : "Level";
                if (!string.Equals(prev, fallbackGameplay, System.StringComparison.OrdinalIgnoreCase)) return false;
            }
            string path = sm.ResolvePathById(mainMenuId);
            string expectedName = path;
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                expectedName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(expectedName) &&
                !string.Equals(loadedSceneName, expectedName, System.StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private IEnumerator ShowAfterDelayRoutine()
        {
            float delay = _settings != null ? Mathf.Max(0f, _settings.showDelayAfterLoadingSeconds) : 1.5f;
            if (delay > 0f)
            {
                Log("Delay before show=" + delay);
                yield return new WaitForSecondsRealtime(delay);
            }

            var sm = SceneManager.Instance;
            if (sm == null)
            {
                _delayedShowRoutine = null;
                yield break;
            }
            var cfg = sm.Config;
            string mainMenuId = cfg != null ? cfg.mainMenuId : null;
            if (string.IsNullOrEmpty(mainMenuId))
                mainMenuId = _settings != null ? _settings.mainMenuSceneId : "MainMenu";
            if (!string.Equals(sm.CurrentId, mainMenuId, System.StringComparison.OrdinalIgnoreCase))
            {
                _delayedShowRoutine = null;
                yield break;
            }
            if (Time.unscaledTime - _lastShowAttemptTime < 1f)
            {
                Log("Show skipped by throttle");
                _delayedShowRoutine = null;
                yield break;
            }
            _lastShowAttemptTime = Time.unscaledTime;
            if (AdsManager.CanShowInterstitial())
            {
                Log("Show attempt ready=true");
                AdsManager.ShowInterstitial();
            }
            else
            {
                Log("Show attempt ready=false, reload");
                AdsManager.LoadInterstitial();
            }
            _delayedShowRoutine = null;
        }

        private void Log(string message)
        {
            if (!DebugLogsEnabled) return;
            Debug.Log("[AdShowOnMenuReturn] " + message);
        }
    }
}
