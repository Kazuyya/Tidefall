using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

namespace LittleHeroJourney.UI
{
    [RequireComponent(typeof(Canvas))]
    public class GameCanvas : MonoBehaviour
    {
        [Tooltip("Identifier for this canvas (e.g., 'Pause', 'Win', 'MainMenu')")]
        [SerializeField] private string canvasID;
        
        public enum SequencePlayMode { Sequential, Parallel }
        
        [System.Serializable]
        public class SequenceItem
        {
            [Tooltip("Parent Transform untuk animasi di step ini. Jika di-set, bisa dipakai untuk disable saat canvas tertutup.")]
            public Transform targetParent;
            [Tooltip("Saat canvas close/prepare, target parent ini di-nonaktifkan. Centang per step yang mau ikut hide.")]
            public bool disableTargetParentWhenHidden = true;
            public List<DOTweenAnimation> animations = new List<DOTweenAnimation>();
            public SequencePlayMode playMode = SequencePlayMode.Parallel;
            public bool useCustomDuration = false;
            public float customDuration = 1f;
        }
        
        [Header("Open Sequence")]
        [SerializeField] private List<SequenceItem> openSequence = new List<SequenceItem>();
        [Header("Close Sequence")]
        [SerializeField] private List<SequenceItem> closeSequence = new List<SequenceItem>();
        
        [SerializeField] private bool showDebugLog = false;
        [SerializeField] private bool disableGameObjectOnClose = true;

        [SerializeField] private bool openOnLoadingFinished = false;
        [SerializeField] private string openOnlyInScene = "";
        [SerializeField] private bool allowOpenWhenActive = true;
        
        private Sequence sequenceRunner;
        
        public event System.Action<GameCanvas> OnOpenStart;
        public event System.Action<GameCanvas> OnOpenComplete;
        public event System.Action<GameCanvas> OnCloseStart;
        public event System.Action<GameCanvas> OnCloseComplete;

        public string ID => canvasID;

        private void Awake()
        {
            SetTargetParentsActive(true);
            SetOpenTargetsToFromState();
            SetTargetParentsActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null)
            {
                GameManager.Instance.CanvasManager.RegisterCanvas(this);
            }
            else
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] GameManager or CanvasManager is null during OnEnable for {canvasID}");
            }

            LoadingManager.OnLoadingFinished += HandleLoadingFinished;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null)
            {
                GameManager.Instance.CanvasManager.RegisterCanvas(this);
            }
        }

        private void OnDisable()
        {
            LoadingManager.OnLoadingFinished -= HandleLoadingFinished;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null)
            {
                GameManager.Instance.CanvasManager.UnregisterCanvas(this);
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnOpenStart?.Invoke(this);
            StartCoroutine(ShowRoutine());
        }

        public System.Collections.IEnumerator ShowAndWait()
        {
            gameObject.SetActive(true);
            OnOpenStart?.Invoke(this);
            yield return ShowRoutine();
        }
        
        private System.Collections.IEnumerator ShowRoutine()
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] ShowRoutine: Starting for {canvasID}");
            SetTargetParentsActive(true);
            yield return PlaySequenceRoutine(openSequence);
            if (showDebugLog) Debug.Log($"[{GetType().Name}] ShowRoutine: Complete for {canvasID}");
            OnOpenComplete?.Invoke(this);
        }

        public virtual void Hide()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(HideRoutine());
        }
        
        public void HideImmediate()
        {
            if (!gameObject.activeInHierarchy) return;
            SetOpenTargetsToFromState();
            if (disableGameObjectOnClose) gameObject.SetActive(false);
            SetTargetParentsActive(false);
        }
        
        private System.Collections.IEnumerator HideRoutine()
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] HideRoutine: Starting for {canvasID}");
            SetTargetParentsActive(true);
            OnCloseStart?.Invoke(this);
            yield return PlaySequenceRoutine(closeSequence);
            SetOpenTargetsToFromState();
            if (disableGameObjectOnClose)
            {
                gameObject.SetActive(false);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] HideRoutine: Canvas deactivated {canvasID}");
            }
            SetTargetParentsActive(false);
            OnCloseComplete?.Invoke(this);
        }

        public void SwitchCanvasById(string targetCanvasId)
        {
            var manager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
            if (manager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] CanvasManager not found for SwitchCanvasById: {targetCanvasId}");
                return;
            }

            manager.SwitchCanvas(targetCanvasId);
        }

        public void CloseCanvasById(string targetCanvasId)
        {
            var manager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
            if (manager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] CanvasManager not found for CloseCanvasById: {targetCanvasId}");
                return;
            }

            manager.CloseCanvas(targetCanvasId);
        }

        public void ReturnToMainMenu()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] GameManager not found for ReturnToMainMenu");
                return;
            }

            manager.ReturnToMainMenu();
        }

        public void ExitGame()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] GameManager not found for ExitGame");
                return;
            }

            manager.ExitGame();
        }

        public void PrepareForOpen()
        {
            SetTargetParentsActive(false);
        }

        private System.Collections.IEnumerator PlaySequenceRoutine(List<SequenceItem> steps)
        {
            if (steps == null || steps.Count == 0) yield break;
            if (sequenceRunner != null && sequenceRunner.IsActive()) sequenceRunner.Kill();
            sequenceRunner = DOTween.Sequence();
            
            for (int i = 0; i < steps.Count; i++)
            {
                var item = steps[i];
                if (item == null) continue;
                AppendItem(sequenceRunner, item);
            }
            
            sequenceRunner.Play();
            yield return sequenceRunner.WaitForCompletion();
        }
        
        private void AppendItem(Sequence seq, SequenceItem item)
        {
            if (item == null || item.animations == null || item.animations.Count == 0) return;
            
            if (item.playMode == SequencePlayMode.Sequential)
            {
                foreach (var anim in item.animations)
                {
                    if (anim == null) continue;
                    float duration = LittleHeroJourney.Helper.GetTweenEffectiveDuration(anim, item.useCustomDuration, item.customDuration);
                    var a = anim;
                    seq.AppendCallback(() => { if (a != null) a.RewindThenRecreateTweenAndPlay(); });
                    if (duration > 0f) seq.AppendInterval(duration);
                }
            }
            else
            {
                float maxDuration = LittleHeroJourney.Helper.GetSequenceTotalDuration(item.animations, false, item.useCustomDuration, item.customDuration);
                seq.AppendCallback(() =>
                {
                    foreach (var anim in item.animations)
                    {
                        if (anim == null) continue;
                        anim.RewindThenRecreateTweenAndPlay();
                    }
                });
                if (maxDuration > 0f) seq.AppendInterval(maxDuration);
            }
        }

        private void HandleLoadingFinished(string loadedSceneName)
        {
            TriggerOpenForScene(loadedSceneName);
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (LoadingManager.Instance != null && LoadingManager.Instance.IsLoading) return;
            if (scene != gameObject.scene) return;
            TriggerOpenForScene(scene.name);
        }

        private void TriggerOpenForScene(string sceneName)
        {
            if (!openOnLoadingFinished) return;
            if (string.IsNullOrEmpty(canvasID)) return;

            if (!string.IsNullOrEmpty(openOnlyInScene))
            {
                if (!string.Equals(sceneName, openOnlyInScene, System.StringComparison.OrdinalIgnoreCase)) return;
            }

            if (!allowOpenWhenActive && gameObject.activeInHierarchy) return;

            var manager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
            if (manager != null)
            {
                manager.SwitchCanvas(canvasID);
            }
            else
            {
                Show();
            }
        }

        public void PrepareForLoadingOpen(string loadedSceneName)
        {
            if (!openOnLoadingFinished) return;
            if (!string.IsNullOrEmpty(openOnlyInScene) && !string.Equals(loadedSceneName, openOnlyInScene, System.StringComparison.OrdinalIgnoreCase)) return;
            SetTargetParentsActive(false);
            if (!allowOpenWhenActive && gameObject.activeSelf) gameObject.SetActive(false);
        }

        public System.Collections.IEnumerator CloseForTransitionRoutine()
        {
            if (!gameObject.activeInHierarchy) yield break;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] CloseForTransitionRoutine: Starting for {canvasID}");
            
            SetTargetParentsActive(true);
            OnCloseStart?.Invoke(this);
            
            // Play close sequence (DOTween animations)
            yield return PlaySequenceRoutine(closeSequence);
            SetOpenTargetsToFromState();
            if (disableGameObjectOnClose) gameObject.SetActive(false);
            SetTargetParentsActive(false);
            OnCloseComplete?.Invoke(this);
        }

        private void SetOpenTargetsToFromState()
        {
            if (openSequence == null || openSequence.Count == 0) return;
            foreach (var item in openSequence)
            {
                if (item?.animations == null) continue;
                foreach (var anim in item.animations)
                {
                    if (anim == null || anim.target == null) continue;
                    if (anim.animationType != DOTweenAnimation.AnimationType.Fade) continue;
                    var cg = anim.target as CanvasGroup;
                    if (cg != null) { cg.alpha = 0f; continue; }
                    var graphic = anim.target as Graphic;
                    if (graphic != null) { var c = graphic.color; c.a = 0f; graphic.color = c; }
                }
            }
        }

        private void SetTargetParentsActive(bool active)
        {
            if (active)
            {
                if (openSequence != null)
                    for (int i = 0; i < openSequence.Count; i++)
                    {
                        var item = openSequence[i];
                        if (item?.targetParent != null) item.targetParent.gameObject.SetActive(true);
                    }
                if (closeSequence != null)
                    for (int i = 0; i < closeSequence.Count; i++)
                    {
                        var item = closeSequence[i];
                        if (item?.targetParent != null) item.targetParent.gameObject.SetActive(true);
                    }
                return;
            }
            if (openSequence != null)
            {
                for (int i = 0; i < openSequence.Count; i++)
                {
                    var item = openSequence[i];
                    if (item == null || item.targetParent == null || !item.disableTargetParentWhenHidden) continue;
                    item.targetParent.gameObject.SetActive(false);
                }
            }
            if (closeSequence != null)
            {
                for (int i = 0; i < closeSequence.Count; i++)
                {
                    var item = closeSequence[i];
                    if (item == null || item.targetParent == null || !item.disableTargetParentWhenHidden) continue;
                    item.targetParent.gameObject.SetActive(false);
                }
            }
        }
        
    }
}
