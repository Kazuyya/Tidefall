using UnityEngine;
using DG.Tweening;

namespace LittleHeroJourney.UI
{
    public enum BarAnimationType
    {
        None,
        Shake,
        PunchScale
    }

    public enum FillAnimationType
    {
        None,
        Lerp
    }

    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private CustomSliderBar mainSliderBar;

        [SerializeField] private FillAnimationType fillOnIncrease = FillAnimationType.None;
        [SerializeField] private FillAnimationType fillOnDecrease = FillAnimationType.None;
        [SerializeField] private float fillDurationIncrease = 0.25f;
        [SerializeField] private float fillDurationDecrease = 0.4f;

        [SerializeField] private RectTransform parentAnimTarget;
        [SerializeField] private BarAnimationType animOnIncrease = BarAnimationType.None;
        [SerializeField] private float animIncreaseDuration = 0.2f;
        [SerializeField] private float animIncreaseStrength = 8f;
        [SerializeField] private BarAnimationType animOnDecrease = BarAnimationType.Shake;
        [SerializeField] private float animDecreaseDuration = 0.2f;
        [SerializeField] private float animDecreaseStrength = 8f;

        [SerializeField] private bool useGhost;
        [SerializeField] private bool useDifferentGhostForIncreaseDecrease;
        [SerializeField] private CustomSliderBar ghostBar;
        [SerializeField] private float ghostDuration = 0.4f;
        [SerializeField] private CustomSliderBar ghostBarIncrease;
        [SerializeField] private CustomSliderBar ghostBarDecrease;
        [SerializeField] private float ghostDurationIncrease = 0.25f;
        [SerializeField] private float ghostDurationDecrease = 0.4f;

        [SerializeField] private bool showDebugLog;

        private Health _targetHealth;
        private float _lastNormalized = 1f;
        private Tween _ghostTween;
        private Tween _mainBarTween;
        private Tween _animTween;

        private void Awake()
        {
            if (mainSliderBar == null)
                mainSliderBar = GetComponent<CustomSliderBar>();
            if (parentAnimTarget == null && TryGetComponent<RectTransform>(out var rt))
                parentAnimTarget = rt;
            SyncGhostToMain();
        }

        private void SyncGhostToMain()
        {
            if (mainSliderBar == null) return;
            float n = mainSliderBar.Slider != null
                ? Mathf.InverseLerp(mainSliderBar.FillRangeMin, mainSliderBar.FillRangeMax, mainSliderBar.Slider.value)
                : 1f;
            if (ghostBar != null) ghostBar.SetNormalized(n);
            if (ghostBarIncrease != null) ghostBarIncrease.SetNormalized(n);
            if (ghostBarDecrease != null) ghostBarDecrease.SetNormalized(n);
        }

        public void SetupForTarget(Health target)
        {
            if (target == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Target health is null!");
                return;
            }

            Cleanup();
            _targetHealth = target;
            _targetHealth.OnHealthChanged += OnHealthChanged;
            OnHealthChanged(_targetHealth.CurrentHealth);
        }

        public void Cleanup()
        {
            if (_targetHealth != null)
                _targetHealth.OnHealthChanged -= OnHealthChanged;
            _targetHealth = null;
        }

        private void OnHealthChanged(float currentHealth)
        {
            if (_targetHealth == null) return;
            SetValue(currentHealth, _targetHealth.MaxHealth);
        }

        public void SetValue(float currentValue, float max)
        {
            if (mainSliderBar == null) return;

            float normalized = max > 0 ? Mathf.Clamp01(currentValue / max) : 0f;
            bool isIncrease = normalized > _lastNormalized;

            _mainBarTween?.Kill();
            if (isIncrease && fillOnIncrease == FillAnimationType.Lerp)
                _mainBarTween = mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad);
            else if (!isIncrease && fillOnDecrease == FillAnimationType.Lerp)
                _mainBarTween = mainSliderBar.TweenToNormalized(normalized, fillDurationDecrease, Ease.OutQuad);
            else
                mainSliderBar.SetNormalized(normalized);

            if (useGhost)
            {
                _ghostTween?.Kill();
                CustomSliderBar ghost = null;
                float dur = ghostDuration;
                if (useDifferentGhostForIncreaseDecrease)
                {
                    if (isIncrease && ghostBarIncrease != null) { ghost = ghostBarIncrease; dur = ghostDurationIncrease; }
                    else if (!isIncrease && ghostBarDecrease != null) { ghost = ghostBarDecrease; dur = ghostDurationDecrease; }
                }
                else if (ghostBar != null)
                {
                    ghost = ghostBar;
                }
                if (ghost != null)
                    _ghostTween = ghost.TweenToNormalized(normalized, dur, Ease.OutQuad);
            }

            if (parentAnimTarget != null)
            {
                if (isIncrease && animOnIncrease != BarAnimationType.None)
                    PlayBarAnim(parentAnimTarget, animOnIncrease, animIncreaseDuration, animIncreaseStrength);
                else if (!isIncrease && animOnDecrease != BarAnimationType.None)
                    PlayBarAnim(parentAnimTarget, animOnDecrease, animDecreaseDuration, animDecreaseStrength);
            }

            _lastNormalized = normalized;

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Value: {currentValue:F1}/{max:F1} -> normalized: {normalized:F2}");
        }

        public Health TargetHealth => _targetHealth;
        public CustomSliderBar MainSliderBar => mainSliderBar;
        public CustomSliderBar GhostBar => ghostBar;

        private void PlayBarAnim(RectTransform target, BarAnimationType type, float duration, float strength)
        {
            _animTween?.Kill();
            switch (type)
            {
                case BarAnimationType.Shake:
                    _animTween = target.DOShakeAnchorPos(duration, strength, 30);
                    break;
                case BarAnimationType.PunchScale:
                    _animTween = target.DOPunchScale(Vector3.one * (strength * 0.1f), duration, 8);
                    break;
            }
        }

        private void OnDestroy()
        {
            _ghostTween?.Kill();
            _mainBarTween?.Kill();
            _animTween?.Kill();
            Cleanup();
        }
    }
}
