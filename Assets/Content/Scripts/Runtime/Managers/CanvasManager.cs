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

        public void SwitchCanvas(string nextCanvasID)
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

            // Execute transition based on rule
            switch (rule.mode)
            {
                case CanvasFlowConfigSO.TransitionMode.Direct:
                    if (rule.closeAfterInComplete)
                    {
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] Direct+CloseAfterIN: show {nextCanvasID} then close {currentActiveCanvas.ID}");
                        StartCoroutine(DirectOpenThenCloseRoutine(currentActiveCanvas, targetCanvas));
                    }
                    else
                    {
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] Direct: just show {nextCanvasID} tanpa closing {currentActiveCanvas.ID}");
                        targetCanvas.Show();
                    }
                    break;

                case CanvasFlowConfigSO.TransitionMode.SnapOutThenIn:
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] SnapOutThenIn: snap close {currentActiveCanvas.ID}, then IN {nextCanvasID}");
                    StartCoroutine(SnapOutThenInRoutine(currentActiveCanvas, targetCanvas));
                    break;

                case CanvasFlowConfigSO.TransitionMode.SnapSwitch:
                    if (rule.closeAfterInComplete)
                    {
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] SnapSwitch+CloseAfterIN: show {nextCanvasID} then close {currentActiveCanvas.ID}");
                        StartCoroutine(SnapSwitchOpenThenCloseRoutine(currentActiveCanvas, targetCanvas));
                    }
                    else
                    {
                        if (showDebugLog) Debug.Log($"[{GetType().Name}] SnapSwitch: hiding {currentActiveCanvas.ID}, showing {nextCanvasID}");
                        currentActiveCanvas.Hide();
                        targetCanvas.Show();
                        currentActiveCanvas = targetCanvas;
                    }
                    break;

                case CanvasFlowConfigSO.TransitionMode.WaitOutThenIn:
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] WaitOutThenIn: starting transition from {currentActiveCanvas.ID} to {nextCanvasID}");
                    StartCoroutine(WaitOutThenInRoutine(currentActiveCanvas, targetCanvas, rule.closeAfterInComplete));
                    break;

                case CanvasFlowConfigSO.TransitionMode.WaitOutThenSnapIn:
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] WaitOutThenSnapIn: starting transition from {currentActiveCanvas.ID} to {nextCanvasID}");
                    StartCoroutine(WaitOutThenSnapInRoutine(currentActiveCanvas, targetCanvas, rule.closeAfterInComplete));
                    break;
                
                case CanvasFlowConfigSO.TransitionMode.ParallelInOut:
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] ParallelInOut: starting parallel transition {currentActiveCanvas.ID} -> {nextCanvasID}");
                    StartCoroutine(ParallelInOutRoutine(currentActiveCanvas, targetCanvas));
                    break;
                
                case CanvasFlowConfigSO.TransitionMode.ParallelOutSnapIn:
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] ParallelOutSnapIn: snap {nextCanvasID} while OUT {currentActiveCanvas.ID}");
                    StartCoroutine(ParallelOutSnapInRoutine(currentActiveCanvas, targetCanvas));
                    break;
            }
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

        private (CanvasFlowConfigSO.TransitionMode mode, bool closeAfterInComplete) GetTransitionRule(string fromId, string toId)
        {
            if (config == null || config.transitionRules == null)
                return GetDefaultRule();
            
            // 1. Exact match: To group then From rule exact
            foreach (var group in config.transitionRules)
            {
                if (string.Equals(group.toCanvasId, toId, System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var r in group.fromRules)
                    {
                        if (string.Equals(r.fromCanvasId, fromId, System.StringComparison.OrdinalIgnoreCase))
                            return (r.mode, r.closeAfterInComplete);
                    }
                }
            }
            
            // 2. Wildcard match (specific TO, wildcard FROM)
            foreach (var group in config.transitionRules)
            {
                if (string.Equals(group.toCanvasId, toId, System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var r in group.fromRules)
                    {
                        bool isWildcard = string.IsNullOrEmpty(r.fromCanvasId) ||
                                          string.Equals(r.fromCanvasId, "*", System.StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(r.fromCanvasId, "any", System.StringComparison.OrdinalIgnoreCase);
                        if (isWildcard)
                            return (r.mode, r.closeAfterInComplete);
                    }
                }
            }
            
            // 3. Global wildcard (wildcard TO, wildcard FROM)
            foreach (var group in config.transitionRules)
            {
                bool isToWildcard = string.IsNullOrEmpty(group.toCanvasId) ||
                                    string.Equals(group.toCanvasId, "*", System.StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(group.toCanvasId, "any", System.StringComparison.OrdinalIgnoreCase);
                if (isToWildcard)
                {
                    foreach (var r in group.fromRules)
                    {
                        bool isFromWildcard = string.IsNullOrEmpty(r.fromCanvasId) ||
                                              string.Equals(r.fromCanvasId, "*", System.StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(r.fromCanvasId, "any", System.StringComparison.OrdinalIgnoreCase);
                        if (isFromWildcard)
                            return (r.mode, r.closeAfterInComplete);
                    }
                }
            }
            
            return GetDefaultRule();
        }

        private (CanvasFlowConfigSO.TransitionMode mode, bool closeAfterInComplete) GetDefaultRule()
        {
            return (CanvasFlowConfigSO.TransitionMode.WaitOutThenIn, true);
        }

        private System.Collections.IEnumerator WaitOutThenInRoutine(GameCanvas oldCanvas, GameCanvas newCanvas, bool closeAfterIn)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
            {
                if (showDebugLog) Debug.Log($"[CanvasManager] Playing full close sequence for: {oldCanvas.ID}");
                yield return oldCanvas.CloseForTransitionRoutine();
            }
            if (showDebugLog) Debug.Log($"[CanvasManager] Playing IN: {newCanvas.ID}");
            yield return newCanvas.ShowAndWait();
            
            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }

        private System.Collections.IEnumerator WaitOutThenSnapInRoutine(GameCanvas oldCanvas, GameCanvas newCanvas, bool closeAfterIn)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
            {
                if (showDebugLog) Debug.Log($"[CanvasManager] Playing full close sequence for: {oldCanvas.ID}");
                yield return oldCanvas.CloseForTransitionRoutine();
            }
            if (showDebugLog) Debug.Log($"[CanvasManager] Snapping IN: {newCanvas.ID}");
            newCanvas.Show();
            
            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }
        
        private System.Collections.IEnumerator DirectOpenThenCloseRoutine(GameCanvas oldCanvas, GameCanvas newCanvas)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            yield return newCanvas.ShowAndWait();
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
                yield return oldCanvas.CloseForTransitionRoutine();
            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }
        
        private System.Collections.IEnumerator SnapOutThenInRoutine(GameCanvas oldCanvas, GameCanvas newCanvas)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
                oldCanvas.HideImmediate();
            yield return newCanvas.ShowAndWait();
            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }
        
        private System.Collections.IEnumerator SnapSwitchOpenThenCloseRoutine(GameCanvas oldCanvas, GameCanvas newCanvas)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            yield return newCanvas.ShowAndWait();
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
                yield return oldCanvas.CloseForTransitionRoutine();
            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }
        
        private System.Collections.IEnumerator ParallelInOutRoutine(GameCanvas oldCanvas, GameCanvas newCanvas)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            bool inDone = false;
            bool outDone = false;
            System.Action markIn = () => inDone = true;
            System.Action markOut = () => outDone = true;
            
            StartCoroutine(ParallelRunner(newCanvas.ShowAndWait(), markIn));
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
                StartCoroutine(ParallelRunner(oldCanvas.CloseForTransitionRoutine(), markOut));
            else
                outDone = true;
            
            while (!inDone || !outDone) yield return null;
            currentActiveCanvas = newCanvas;
            isTransitioning = false;
        }
        
        private System.Collections.IEnumerator ParallelOutSnapInRoutine(GameCanvas oldCanvas, GameCanvas newCanvas)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;
            newCanvas.Show();
            bool outDone = false;
            System.Action markOut = () => outDone = true;
            if (oldCanvas != null && oldCanvas.gameObject.activeInHierarchy)
                StartCoroutine(ParallelRunner(oldCanvas.CloseForTransitionRoutine(), markOut));
            else
                outDone = true;
            while (!outDone) yield return null;
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
