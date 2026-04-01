using System;
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
        [SerializeField] private float eventSystemEnableDelay = 0.5f;
        private readonly List<EventSystem> _eventSystems = new List<EventSystem>();
        private Coroutine _delayedEnableEventSystemRoutine;
        
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

        [Header("UIActionEvent: open canvas by id")]
        [Tooltip("E.g. Settings. If event ActionId matches one of these, open that canvas. Empty = do not handle event.")]
        [SerializeField] private List<string> canvasActionIds = new List<string>();
        [Header("UIActionEvent: back action id")]
        [SerializeField] private string backActionId = "Back";

        private Dictionary<string, Action> _canvasActionHandlers = new Dictionary<string, Action>();
        private Action<string> _canvasOpenStartHandler;
        private Action<string> _canvasOpenCompleteHandler;
        private Action<string> _canvasCloseStartHandler;
        private Action<string> _canvasCloseCompleteHandler;
        private Action _backActionHandler;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            RefreshEventSystemsCache();
            _canvasOpenStartHandler = HandleCanvasOpenStartByPayload;
            _canvasOpenCompleteHandler = HandleCanvasOpenCompleteByPayload;
            _canvasCloseStartHandler = HandleCanvasCloseStartByPayload;
            _canvasCloseCompleteHandler = HandleCanvasCloseCompleteByPayload;
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenStart", _canvasOpenStartHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompleteHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasCloseStart", _canvasCloseStartHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasCloseComplete", _canvasCloseCompleteHandler);
            foreach (var id in canvasActionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var capturedId = id;
                var handler = new Action(() => SwitchCanvas(capturedId));
                _canvasActionHandlers[capturedId] = handler;
                GameEventSystem.SubscribeAction(capturedId, handler);
            }
            if (!string.IsNullOrEmpty(backActionId))
            {
                _backActionHandler = ReturnToPreviousCanvas;
                GameEventSystem.SubscribeAction(backActionId, _backActionHandler);
            }
        }

        private void OnDisable()
        {
            if (_canvasOpenStartHandler != null) GameEventSystem.UnsubscribeActionWithPayload("CanvasOpenStart", _canvasOpenStartHandler);
            if (_canvasOpenCompleteHandler != null) GameEventSystem.UnsubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompleteHandler);
            if (_canvasCloseStartHandler != null) GameEventSystem.UnsubscribeActionWithPayload("CanvasCloseStart", _canvasCloseStartHandler);
            if (_canvasCloseCompleteHandler != null) GameEventSystem.UnsubscribeActionWithPayload("CanvasCloseComplete", _canvasCloseCompleteHandler);
            foreach (var kv in _canvasActionHandlers)
                GameEventSystem.UnsubscribeAction(kv.Key, kv.Value);
            _canvasActionHandlers.Clear();
            if (_backActionHandler != null && !string.IsNullOrEmpty(backActionId))
            {
                GameEventSystem.UnsubscribeAction(backActionId, _backActionHandler);
                _backActionHandler = null;
            }
            CancelDelayedEventSystemEnable();
            if (disableEventSystemDuringTransition) SetEventSystemEnabled(true);
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
                }
                else
                {
                    // Same canvas being registered multiple times - just skip
                    return;
                }
            }

            registeredCanvases[canvas.ID] = canvas;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Registered Canvas: {canvas.ID}");
        }

        public void UnregisterCanvas(GameCanvas canvas)
        {
            if (canvas == null || string.IsNullOrEmpty(canvas.ID)) return;

            if (registeredCanvases.ContainsKey(canvas.ID) && registeredCanvases[canvas.ID] == canvas)
            {
                registeredCanvases.Remove(canvas.ID);
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
            ForEachRegisteredCanvas(canvas => canvas.Hide());
        }
        
        public void CloseAllCanvasesImmediate()
        {
            ForEachRegisteredCanvas(canvas =>
            {
                if (canvas == null) return;
                canvas.HideImmediate();
            });
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
        
        private void HandleCanvasOpenStartByPayload(string canvasId)
        {
            var canvas = GetCanvas(canvasId);
            if (canvas == null) return;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Open start: {canvasId}");
            if (disableEventSystemDuringTransition)
            {
                CancelDelayedEventSystemEnable();
                SetEventSystemEnabled(false);
            }
            OnOpenStart?.Invoke(canvas);
        }
        
        private void HandleCanvasOpenCompleteByPayload(string canvasId)
        {
            var canvas = GetCanvas(canvasId);
            if (canvas == null) return;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Open complete: {canvasId}");
            if (disableEventSystemDuringTransition) ScheduleDelayedEventSystemEnable();
            OnOpenComplete?.Invoke(canvas);
        }
        
        private void HandleCanvasCloseStartByPayload(string canvasId)
        {
            var canvas = GetCanvas(canvasId);
            if (canvas == null) return;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Close start: {canvasId}");
            if (disableEventSystemDuringTransition)
            {
                CancelDelayedEventSystemEnable();
                SetEventSystemEnabled(false);
            }
            OnCloseStart?.Invoke(canvas);
        }
        
        private void HandleCanvasCloseCompleteByPayload(string canvasId)
        {
            var canvas = GetCanvas(canvasId);
            if (canvas == null) return;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Close complete: {canvasId}");
            if (disableEventSystemDuringTransition) ScheduleDelayedEventSystemEnable();
            OnCloseComplete?.Invoke(canvas);
        }
        
        private void SetEventSystemEnabled(bool enabled)
        {
            RefreshEventSystemsCache();
            int affected = 0;
            for (int i = 0; i < _eventSystems.Count; i++)
            {
                var es = _eventSystems[i];
                if (es == null) continue;
                if (es.enabled == enabled) continue;
                es.enabled = enabled;
                affected++;
            }
            if (showDebugLog) Debug.Log($"[{GetType().Name}] EventSystem set enabled={enabled}, affected={affected}, totalCached={_eventSystems.Count}");
        }

        private void RefreshEventSystemsCache()
        {
            _eventSystems.Clear();
            var systems = FindObjectsOfType<EventSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var es = systems[i];
                if (es == null) continue;
                _eventSystems.Add(es);
            }
        }

        private void ScheduleDelayedEventSystemEnable()
        {
            CancelDelayedEventSystemEnable();
            _delayedEnableEventSystemRoutine = StartCoroutine(DelayedEventSystemEnableRoutine(Mathf.Max(0f, eventSystemEnableDelay)));
        }

        private void CancelDelayedEventSystemEnable()
        {
            if (_delayedEnableEventSystemRoutine == null) return;
            StopCoroutine(_delayedEnableEventSystemRoutine);
            _delayedEnableEventSystemRoutine = null;
        }

        private System.Collections.IEnumerator DelayedEventSystemEnableRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            SetEventSystemEnabled(true);
            _delayedEnableEventSystemRoutine = null;
        }
        
        #endregion


        #region Transition Helpers

        private void ForEachRegisteredCanvas(Action<GameCanvas> action)
        {
            if (action == null) return;
            foreach (var canvas in registeredCanvases.Values)
            {
                if (canvas == null) continue;
                action(canvas);
            }
            currentActiveCanvas = null;
        }

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
