using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using LittleHeroJourney.UI;

namespace LittleHeroJourney
{
    public class CanvasManager : MonoBehaviour
    {
        #region Fields

        [SerializeField] private bool showDebugLog = false;
        [SerializeField] private bool disableEventSystemDuringTransition = true;
        private EventSystem _eventSystem;
        
        public System.Action<GameCanvas> OnOpenStart;
        public System.Action<GameCanvas> OnOpenComplete;
        public System.Action<GameCanvas> OnCloseStart;
        public System.Action<GameCanvas> OnCloseComplete;

        [Header("Transition Rules")]
        [SerializeField] private CanvasFlowConfigSO config;

        private Dictionary<string, GameCanvas> registeredCanvases = new Dictionary<string, GameCanvas>();

        private GameCanvas currentActiveCanvas;
        private bool isTransitioning = false;
        private List<string> _canvasHistory = new List<string>();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _eventSystem = EventSystem.current != null ? EventSystem.current : FindObjectOfType<EventSystem>();
        }

        #endregion

        #region Registry

        public void RegisterCanvas(GameCanvas canvas)
        {
            if (canvas == null) return;
            if (string.IsNullOrEmpty(canvas.ID))
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Canvas has empty ID! Ignoring registration.");
                return;
            }

            if (registeredCanvases.ContainsKey(canvas.ID))
            {
                GameCanvas oldCanvas = registeredCanvases[canvas.ID];
                // Only replace if it's a different instance
                if (oldCanvas != canvas)
                {
                    if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Duplicate canvas ID registered: {canvas.ID}. Replacing old instance.");
                    UnsubscribeCanvasEvents(oldCanvas);
                }
                else
                {
                    // Same canvas being registered multiple times - just skip
                    return;
                }
            }

            registeredCanvases[canvas.ID] = canvas;
            SubscribeCanvasEvents(canvas);
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Registered Canvas: {canvas.ID}");
        }

        public void UnregisterCanvas(GameCanvas canvas)
        {
            if (canvas == null || string.IsNullOrEmpty(canvas.ID)) return;

            if (registeredCanvases.ContainsKey(canvas.ID) && registeredCanvases[canvas.ID] == canvas)
            {
                registeredCanvases.Remove(canvas.ID);
                UnsubscribeCanvasEvents(canvas);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Unregistered Canvas: {canvas.ID}");
            }
        }

        public GameCanvas GetCanvas(string canvasID)
        {
            if (registeredCanvases.TryGetValue(canvasID, out var canvas))
            {
                return canvas;
            }
            return null;
        }

        #endregion

        #region Public Methods

       public void SwitchCanvas(string nextCanvasID, bool addCurrentToHistory = true)
        {
            if (string.IsNullOrEmpty(nextCanvasID)) return;

            // Auto-discover canvas if not registered
            if (!registeredCanvases.ContainsKey(nextCanvasID))
            {
                var allCanvases = FindObjectsOfType<GameCanvas>(true); // true = include inactive
                foreach (var c in allCanvases)
                {
                    if (c.ID == nextCanvasID)
                    {
                        RegisterCanvas(c);
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] Auto-discovered and registered canvas: {nextCanvasID}");
                    }
                }
            }

            if (!registeredCanvases.TryGetValue(nextCanvasID, out GameCanvas targetCanvas))
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Canvas not found for ID: {nextCanvasID}");
                return;
            }

            // Prevent opening same canvas or during transition
            if (isTransitioning)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Already transitioning, ignoring SwitchCanvas({nextCanvasID})");
                return;
            }

            // Determine transition rule
            string fromId = currentActiveCanvas != null ? currentActiveCanvas.ID : null;
            var rule = GetTransitionRule(fromId, nextCanvasID);
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] SwitchCanvas: from='{fromId ?? "null"}' -> to='{nextCanvasID}', Mode={rule.mode}, currentActive={(currentActiveCanvas != null ? currentActiveCanvas.ID : "null")}");

            // If no active canvas (first time), just show and track
            if (currentActiveCanvas == null)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] No active canvas, direct open: {nextCanvasID}");
                targetCanvas.Show();
                currentActiveCanvas = targetCanvas;
                return;
            }

            // Same canvas, ignore
            if (currentActiveCanvas == targetCanvas)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Target is already current, ignoring.");
                return;
            }

            if (addCurrentToHistory && currentActiveCanvas != null)
                _canvasHistory.Add(currentActiveCanvas.ID);

            // Execute transition based on rule
            switch (rule.mode)
            {
                case CanvasFlowConfigSO.TransitionMode.Direct:
                    if (rule.closeAfterInComplete)
                        StartCoroutine(RunTransitionRoutine(rule.mode, currentActiveCanvas, targetCanvas, true));
                    else
                    {
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] Direct: just show {nextCanvasID}");
                        targetCanvas.Show();
                        currentActiveCanvas = targetCanvas;
                    }
                    break;

                case CanvasFlowConfigSO.TransitionMode.SnapOutThenIn:
                    StartCoroutine(RunTransitionRoutine(rule.mode, currentActiveCanvas, targetCanvas, rule.closeAfterInComplete));
                    break;

                case CanvasFlowConfigSO.TransitionMode.SnapSwitch:
                    if (rule.closeAfterInComplete)
                        StartCoroutine(RunTransitionRoutine(rule.mode, currentActiveCanvas, targetCanvas, true));
                    else
                    {
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] SnapSwitch: {currentActiveCanvas.ID} -> {nextCanvasID}");
                        currentActiveCanvas.Hide();
                        targetCanvas.Show();
                        currentActiveCanvas = targetCanvas;
                    }
                    break;

                case CanvasFlowConfigSO.TransitionMode.WaitOutThenIn:
                case CanvasFlowConfigSO.TransitionMode.WaitOutThenSnapIn:
                    StartCoroutine(RunTransitionRoutine(rule.mode, currentActiveCanvas, targetCanvas, rule.closeAfterInComplete));
                    break;

                case CanvasFlowConfigSO.TransitionMode.ParallelInOut:
                case CanvasFlowConfigSO.TransitionMode.ParallelOutSnapIn:
                    StartCoroutine(RunTransitionRoutine(rule.mode, currentActiveCanvas, targetCanvas, rule.closeAfterInComplete));
                    break;
            }
        }

        public void ReturnToPreviousCanvas()
        {
            if (_canvasHistory.Count == 0)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No previous canvas in history.");
                return;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetTimeScale(1f);
                GameManager.Instance.ResetGameState();
            }
            string targetId = _canvasHistory[_canvasHistory.Count - 1];
            _canvasHistory.RemoveAt(_canvasHistory.Count - 1);
            SwitchCanvas(targetId, addCurrentToHistory: false);
        }

        public void CloseCanvas(string canvasID)
        {
            if (string.IsNullOrEmpty(canvasID)) return;

            if (registeredCanvases.TryGetValue(canvasID, out GameCanvas canvas))
            {
                canvas.Hide();
                if (currentActiveCanvas == canvas) currentActiveCanvas = null;
            }
        }

        public void CloseAllCanvases()
        {
            foreach (var canvas in registeredCanvases.Values)
            {
                canvas.Hide();
            }
            currentActiveCanvas = null;
        }
        
        public void CloseAllCanvasesImmediate()
        {
            foreach (var canvas in registeredCanvases.Values)
            {
                if (canvas == null) continue;
                canvas.HideImmediate();
            }
            currentActiveCanvas = null;
        }

        public System.Collections.IEnumerator CloseAllCanvasesRoutine()
        {
            var list = new List<GameCanvas>(registeredCanvases.Values);
            for (int i = 0; i < list.Count; i++)
            {
                var canvas = list[i];
                if (canvas == null) continue;
                if (!canvas.gameObject.activeInHierarchy) continue;
                yield return canvas.CloseForTransitionRoutine();
            }
            currentActiveCanvas = null;
        }

        #endregion
        
        #region Internal
        
        private void SubscribeCanvasEvents(GameCanvas canvas)
        {
            if (canvas == null) return;
            canvas.OnOpenStart += HandleCanvasOpenStart;
            canvas.OnOpenComplete += HandleCanvasOpenComplete;
            canvas.OnCloseStart += HandleCanvasCloseStart;
            canvas.OnCloseComplete += HandleCanvasCloseComplete;
        }
        
        private void UnsubscribeCanvasEvents(GameCanvas canvas)
        {
            if (canvas == null) return;
            canvas.OnOpenStart -= HandleCanvasOpenStart;
            canvas.OnOpenComplete -= HandleCanvasOpenComplete;
            canvas.OnCloseStart -= HandleCanvasCloseStart;
            canvas.OnCloseComplete -= HandleCanvasCloseComplete;
        }
        
        private void HandleCanvasOpenStart(GameCanvas canvas)
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Open start: {canvas?.ID}");
            if (disableEventSystemDuringTransition) SetEventSystemEnabled(false);
            OnOpenStart?.Invoke(canvas);
        }
        
        private void HandleCanvasOpenComplete(GameCanvas canvas)
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Open complete: {canvas?.ID}");
            if (disableEventSystemDuringTransition) SetEventSystemEnabled(true);
            OnOpenComplete?.Invoke(canvas);
        }
        
        private void HandleCanvasCloseStart(GameCanvas canvas)
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Close start: {canvas?.ID}");
            if (disableEventSystemDuringTransition) SetEventSystemEnabled(false);
            OnCloseStart?.Invoke(canvas);
        }
        
        private void HandleCanvasCloseComplete(GameCanvas canvas)
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Close complete: {canvas?.ID}");
            if (disableEventSystemDuringTransition) SetEventSystemEnabled(true);
            OnCloseComplete?.Invoke(canvas);
        }
        
        private void SetEventSystemEnabled(bool enabled)
        {
            if (_eventSystem == null)
            {
                _eventSystem = EventSystem.current != null ? EventSystem.current : FindObjectOfType<EventSystem>();
            }
            if (_eventSystem != null)
            {
                _eventSystem.enabled = enabled;
            }
        }
        
        #endregion


        #region Transition Helpers

        private static bool IsCanvasIdWildcard(string id)
        {
            return string.IsNullOrEmpty(id) ||
                   string.Equals(id, "*", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "any", System.StringComparison.OrdinalIgnoreCase);
        }

        private (CanvasFlowConfigSO.TransitionMode mode, bool closeAfterInComplete) GetTransitionRule(string fromId, string toId)
        {
            if (config == null || config.transitionRules == null) return GetDefaultRule();
            var ord = System.StringComparison.OrdinalIgnoreCase;
            foreach (var group in config.transitionRules)
            {
                bool toMatch = string.Equals(group.toCanvasId, toId, ord) || IsCanvasIdWildcard(group.toCanvasId);
                if (!toMatch) continue;
                foreach (var r in group.fromRules)
                    if (string.Equals(r.fromCanvasId, fromId, ord)) return (r.mode, r.closeAfterInComplete);
                foreach (var r in group.fromRules)
                    if (IsCanvasIdWildcard(r.fromCanvasId)) return (r.mode, r.closeAfterInComplete);
            }
            return GetDefaultRule();
        }

        private (CanvasFlowConfigSO.TransitionMode mode, bool closeAfterInComplete) GetDefaultRule()
        {
            return (CanvasFlowConfigSO.TransitionMode.WaitOutThenIn, true);
        }

        private System.Collections.IEnumerator RunTransitionRoutine(CanvasFlowConfigSO.TransitionMode mode, GameCanvas oldCanvas, GameCanvas newCanvas, bool closeAfterInComplete)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            bool oldActive = oldCanvas != null && oldCanvas.gameObject.activeInHierarchy;

            switch (mode)
            {
                case CanvasFlowConfigSO.TransitionMode.WaitOutThenIn:
                    if (oldActive) { if (showDebugLog) Debug.Log($"[CanvasManager] Close: {oldCanvas.ID}"); yield return oldCanvas.CloseForTransitionRoutine(); }
                    if (showDebugLog) Debug.Log($"[CanvasManager] ShowAndWait: {newCanvas.ID}");
                    yield return newCanvas.ShowAndWait();
                    break;
                case CanvasFlowConfigSO.TransitionMode.WaitOutThenSnapIn:
                    if (oldActive) { if (showDebugLog) Debug.Log($"[CanvasManager] Close: {oldCanvas.ID}"); yield return oldCanvas.CloseForTransitionRoutine(); }
                    if (showDebugLog) Debug.Log($"[CanvasManager] Snap: {newCanvas.ID}");
                    newCanvas.Show();
                    break;
                case CanvasFlowConfigSO.TransitionMode.Direct:
                case CanvasFlowConfigSO.TransitionMode.SnapSwitch:
                    yield return newCanvas.ShowAndWait();
                    if (closeAfterInComplete && oldActive) yield return oldCanvas.CloseForTransitionRoutine();
                    break;
                case CanvasFlowConfigSO.TransitionMode.SnapOutThenIn:
                    if (oldActive) oldCanvas.HideImmediate();
                    yield return newCanvas.ShowAndWait();
                    break;
                case CanvasFlowConfigSO.TransitionMode.ParallelInOut:
                    bool inDone = false, outDone = false;
                    StartCoroutine(ParallelRunner(newCanvas.ShowAndWait(), () => inDone = true));
                    if (oldActive) StartCoroutine(ParallelRunner(oldCanvas.CloseForTransitionRoutine(), () => outDone = true));
                    else outDone = true;
                    while (!inDone || !outDone) yield return null;
                    break;
                case CanvasFlowConfigSO.TransitionMode.ParallelOutSnapIn:
                    newCanvas.Show();
                    bool outDoneSnap = false;
                    if (oldActive) StartCoroutine(ParallelRunner(oldCanvas.CloseForTransitionRoutine(), () => outDoneSnap = true));
                    else outDoneSnap = true;
                    while (!outDoneSnap) yield return null;
                    break;
                default:
                    if (oldActive) yield return oldCanvas.CloseForTransitionRoutine();
                    yield return newCanvas.ShowAndWait();
                    break;
            }

            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }

        private System.Collections.IEnumerator ParallelRunner(System.Collections.IEnumerator routine, System.Action onComplete)
        {
            yield return routine;
            onComplete?.Invoke();
        }

        #endregion
    }
}
