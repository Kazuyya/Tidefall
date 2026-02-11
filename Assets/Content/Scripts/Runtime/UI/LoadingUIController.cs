using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace LittleHeroJourney.UI
{
    public enum OverrideSelectionMode { Random, Ordered }
    public enum LoadingDisplayMode { ProgressBar, Spinner, ImageFade }

    [System.Serializable]
    public class LoadingOverride
    {
        public string title;
        public string body;
        public Sprite background;
    }

    public class LoadingUIController : MonoBehaviour
    {
        [SerializeField] private bool showDebugLog = false;
        [SerializeField] private LoadingDisplayMode displayMode = LoadingDisplayMode.ProgressBar;
        [SerializeField] private OverrideSelectionMode selectionMode = OverrideSelectionMode.Random;

        public enum SequencePlayMode { Sequential, Parallel }
        [Header("Open (set 0 → tween to 1)")]
        [SerializeField] private List<DOTweenAnimation> openAnimations = new List<DOTweenAnimation>();
        [SerializeField] private SequencePlayMode openPlayMode = SequencePlayMode.Parallel;
        [SerializeField] private bool openUseCustomDuration = false;
        [SerializeField] private float openCustomDuration = 1f;

        [Header("Close (tween to 0)")]
        [SerializeField] private List<DOTweenAnimation> closeAnimations = new List<DOTweenAnimation>();
        [SerializeField] private SequencePlayMode closePlayMode = SequencePlayMode.Parallel;
        [SerializeField] private bool closeUseCustomDuration = false;
        [SerializeField] private float closeCustomDuration = 1f;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private string progressTextFormat = "Loading... {0}%";
        [SerializeField] private string loadingStaticText = "Loading...";
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image spinnerImage;
        [SerializeField] private float spinnerDuration = 1f;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private List<LoadingOverride> overrides = new List<LoadingOverride>();

        [Header("Image Fade mode")]
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private Ease fadeEase = Ease.InOutSine;
        [SerializeField] private bool iconFadeEnabled = false;

        private Tween _spinnerTween;
        private Tween _fadeTween;
        private Sequence _sequenceRunner;
        private static int _lastOverrideIndex = -1;

        private void OnEnable()
        {
            ApplyOverride();
            if (backgroundImage != null && backgroundImage.sprite == null) backgroundImage.sprite = backgroundSprite;
            if (progressText != null && !string.IsNullOrEmpty(loadingStaticText)) progressText.text = loadingStaticText;
            StartDisplayMode();
        }

        private void OnDisable()
        {
            if (_spinnerTween != null && _spinnerTween.IsActive()) _spinnerTween.Kill();
            if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
            if (_sequenceRunner != null && _sequenceRunner.IsActive()) _sequenceRunner.Kill();
        }

        public void UpdateProgress(float progress)
        {
            float p = Mathf.Clamp01(progress);
            if (progressBar != null) progressBar.value = p;
            if (progressText != null) progressText.text = string.Format(progressTextFormat, Mathf.RoundToInt(p * 100f));
        }

        public void ApplyBackground()
        {
            if (backgroundImage != null && backgroundImage.sprite == null) backgroundImage.sprite = backgroundSprite;
        }

        /// <summary>Set open targets to 0 so next PlayOpen animates 0→1. Call before showing (e.g. from LoadingManager).</summary>
        public void PrepareForOpenImmediate()
        {
            SetFadeTargetsToZero(openAnimations);
            for (int i = 0; i < (openAnimations?.Count ?? 0); i++)
            {
                if (openAnimations[i] != null && !openAnimations[i].gameObject.activeSelf)
                    openAnimations[i].gameObject.SetActive(true);
            }
        }

        public IEnumerator PlayOpenSequenceRoutine()
        {
            yield return PlaySequenceRoutine(openAnimations, openPlayMode, openUseCustomDuration, openCustomDuration);
        }

        public IEnumerator PlayCloseSequenceRoutine()
        {
            yield return PlaySequenceRoutine(closeAnimations, closePlayMode, closeUseCustomDuration, closeCustomDuration);
            SetFadeTargetsToZero(openAnimations);
        }

        public void SetDisplayMode(LoadingDisplayMode mode)
        {
            displayMode = mode;
            StartDisplayMode();
        }

        private void StartDisplayMode()
        {
            if (_spinnerTween != null && _spinnerTween.IsActive()) _spinnerTween.Kill();
            if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();

            if (progressBar != null) progressBar.gameObject.SetActive(displayMode == LoadingDisplayMode.ProgressBar);
            if (spinnerImage != null) spinnerImage.gameObject.SetActive(displayMode == LoadingDisplayMode.Spinner);
            if (iconImage != null) iconImage.gameObject.SetActive(displayMode == LoadingDisplayMode.ImageFade && iconFadeEnabled);

            if (displayMode == LoadingDisplayMode.Spinner && spinnerImage != null)
                _spinnerTween = spinnerImage.rectTransform.DORotate(new Vector3(0, 0, 360f), spinnerDuration, RotateMode.FastBeyond360).SetLoops(-1, LoopType.Restart);
            else if (displayMode == LoadingDisplayMode.ImageFade && iconFadeEnabled && iconImage != null)
            {
                var c = iconImage.color;
                c.a = 0f;
                iconImage.color = c;
                _fadeTween = iconImage.DOFade(1f, fadeDuration).SetEase(fadeEase).SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void ApplyOverride()
        {
            if (overrides == null || overrides.Count == 0) return;
            int idx = selectionMode == OverrideSelectionMode.Random ? Random.Range(0, overrides.Count) : (_lastOverrideIndex = (_lastOverrideIndex + 1) % overrides.Count);
            var ov = overrides[idx];
            if (ov == null) return;
            if (titleText != null && !string.IsNullOrEmpty(ov.title)) titleText.text = ov.title;
            if (bodyText != null && !string.IsNullOrEmpty(ov.body)) bodyText.text = ov.body;
            if (backgroundImage != null && ov.background != null) backgroundImage.sprite = ov.background;
        }

        private void SetFadeTargetsToZero(List<DOTweenAnimation> list)
        {
            if (list == null) return;
            foreach (var anim in list)
            {
                if (anim == null || anim.target == null || anim.animationType != DOTweenAnimation.AnimationType.Fade) continue;
                var cg = anim.target as CanvasGroup;
                if (cg != null) { cg.alpha = 0f; continue; }
                var g = anim.target as Graphic;
                if (g != null) { var c = g.color; c.a = 0f; g.color = c; }
            }
        }

        private IEnumerator PlaySequenceRoutine(List<DOTweenAnimation> animations, SequencePlayMode mode, bool useCustomDuration, float customDuration)
        {
            if (animations == null || animations.Count == 0) yield break;
            if (_sequenceRunner != null && _sequenceRunner.IsActive()) _sequenceRunner.Kill();
            _sequenceRunner = DOTween.Sequence();

            if (mode == SequencePlayMode.Sequential)
            {
                foreach (var anim in animations)
                {
                    if (anim == null) continue;
                    float dur = LittleHeroJourney.Helper.GetTweenEffectiveDuration(anim, useCustomDuration, customDuration);
                    var a = anim;
                    _sequenceRunner.AppendCallback(() => { if (a != null) a.RewindThenRecreateTweenAndPlay(); });
                    if (dur > 0f) _sequenceRunner.AppendInterval(dur);
                }
            }
            else
            {
                float maxDur = LittleHeroJourney.Helper.GetSequenceTotalDuration(animations, false, useCustomDuration, customDuration);
                _sequenceRunner.AppendCallback(() =>
                {
                    foreach (var anim in animations)
                        if (anim != null) anim.RewindThenRecreateTweenAndPlay();
                });
                if (maxDur > 0f) _sequenceRunner.AppendInterval(maxDur);
            }

            _sequenceRunner.Play();
            yield return _sequenceRunner.WaitForCompletion();
        }
    }
}
