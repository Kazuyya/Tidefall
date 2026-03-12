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

        [SerializeField] private FillAnimationType fillOnIncrease = FillAnimationType.Lerp;
        [SerializeField] private FillAnimationType fillOnDecrease = FillAnimationType.None;
        [SerializeField] private float fillDurationIncrease = 0.4f;
        [SerializeField] private float fillDelayIncrease = 0.5f;
        [SerializeField] private float fillDurationDecrease = 0.4f;
        [SerializeField] private float fillDelayDecrease;

        [SerializeField] private RectTransform parentAnimTarget;
        [SerializeField] private BarAnimationType animOnIncrease = BarAnimationType.None;
        [SerializeField] private float animIncreaseDuration = 0.2f;
        [SerializeField] private float animIncreaseStrength = 8f;
        [SerializeField] private BarAnimationType animOnDecrease = BarAnimationType.Shake;
        [SerializeField] private float animDecreaseDuration = 0.2f;
        [SerializeField] private float animDecreaseStrength = 8f;

        [SerializeField] private bool useGhost;
        [SerializeField] private bool useDifferentGhostForIncreaseDecrease;
        [SerializeField] private FillAnimationType ghostAnimOnIncrease = FillAnimationType.None;
        [SerializeField] private FillAnimationType ghostAnimOnDecrease = FillAnimationType.Lerp;
        [SerializeField] private CustomSliderBar ghostBar;
        [SerializeField] private float ghostDuration = 0.4f;
        [SerializeField] private float ghostIncreaseDelay;
        [SerializeField] private float ghostDecreaseDelay = 0.5f;
        [SerializeField] private CustomSliderBar ghostBarIncrease;
        [SerializeField] private CustomSliderBar ghostBarDecrease;
        [SerializeField] private float ghostDurationIncrease = 0.25f;
        [SerializeField] private float ghostDurationDecrease = 0.4f;
        [SerializeField] private float ghostIncreaseDelayDual;
        [SerializeField] private float ghostDecreaseDelayDual = 0.5f;

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
            _ghostTween?.Kill();

            if (isIncrease)
            {
                Sequence mainSeq = null;
                if (fillOnIncrease == FillAnimationType.Lerp)
                    mainSeq = DOTween.Sequence();

                if (useGhost)
                {
                    if (useDifferentGhostForIncreaseDecrease)
                    {
                        if (ghostBarDecrease != null) ghostBarDecrease.gameObject.SetActive(false);
                        if (ghostBarIncrease != null)
                        {
                            ghostBarIncrease.gameObject.SetActive(true);
                            ghostBarIncrease.SetNormalized(_lastNormalized);
                            if (ghostAnimOnIncrease == FillAnimationType.Lerp)
                            {
                                float incDelay = ghostIncreaseDelayDual;
                                var ghostSeq = DOTween.Sequence();
                                ghostSeq.AppendInterval(incDelay);
                                ghostSeq.Append(ghostBarIncrease.TweenToNormalized(normalized, ghostDurationIncrease, Ease.OutQuad));
                                _ghostTween = ghostSeq;
                                if (mainSeq != null)
                                    mainSeq.Append(ghostSeq).AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                            }
                            else
                            {
                                ghostBarIncrease.SetNormalized(normalized);
                                if (mainSeq != null)
                                    mainSeq.AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                            }
                        }
                        else if (mainSeq != null)
                            mainSeq.AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                    }
                    else if (ghostBar != null)
                    {
                        ghostBar.SetNormalized(_lastNormalized);
                        if (ghostAnimOnIncrease == FillAnimationType.Lerp)
                        {
                            float incDelay = ghostIncreaseDelay;
                            var ghostSeq = DOTween.Sequence();
                            ghostSeq.AppendInterval(incDelay);
                            ghostSeq.Append(ghostBar.TweenToNormalized(normalized, ghostDuration, Ease.OutQuad));
                            _ghostTween = ghostSeq;
                            if (mainSeq != null)
                                mainSeq.Append(ghostSeq).AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                        }
                        else
                        {
                            ghostBar.SetNormalized(normalized);
                            if (mainSeq != null)
                                mainSeq.AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                        }
                    }
                    else if (mainSeq != null)
                        mainSeq.AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                }
                else if (mainSeq != null)
                {
                    mainSeq.AppendInterval(fillDelayIncrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationIncrease, Ease.OutQuad));
                }

                if (mainSeq != null)
                    _mainBarTween = mainSeq;
                else if (fillOnIncrease != FillAnimationType.Lerp)
                    mainSliderBar.SetNormalized(normalized);
            }
            else
            {
                if (useGhost)
                    mainSliderBar.SetNormalized(normalized);
                else if (fillOnDecrease == FillAnimationType.None)
                    mainSliderBar.SetNormalized(normalized);
                else
                {
                    var decSeq = DOTween.Sequence();
                    decSeq.AppendInterval(fillDelayDecrease).Append(mainSliderBar.TweenToNormalized(normalized, fillDurationDecrease, Ease.OutQuad));
                    _mainBarTween = decSeq;
                }

                if (useGhost)
                {
                    float delay = useDifferentGhostForIncreaseDecrease ? ghostDecreaseDelayDual : ghostDecreaseDelay;
                    float dur = useDifferentGhostForIncreaseDecrease ? ghostDurationDecrease : ghostDuration;
                    CustomSliderBar ghostDec = useDifferentGhostForIncreaseDecrease ? ghostBarDecrease : ghostBar;

                    if (useDifferentGhostForIncreaseDecrease)
                    {
                        if (ghostBarIncrease != null) ghostBarIncrease.gameObject.SetActive(false);
                        if (ghostBarDecrease != null)
                        {
                            ghostBarDecrease.SetNormalized(_lastNormalized);
                            ghostBarDecrease.gameObject.SetActive(true);
                        }
                    }

                    if (ghostAnimOnDecrease == FillAnimationType.Lerp && ghostDec != null)
                    {
                        float ghostStart = ghostDec.GetNormalized();
                        ghostStart = Mathf.Max(ghostStart, normalized);
                        ghostDec.SetNormalized(ghostStart);
                        var seq = DOTween.Sequence();
                        seq.AppendInterval(delay);
                        seq.Append(ghostDec.TweenToNormalized(normalized, dur, Ease.OutQuad));
                        _ghostTween = seq;
                    }
                    else if (ghostDec != null)
                    {
                        ghostDec.SetNormalized(normalized);
                    }
                }
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

        [ContextMenu("Debug: Set 100%")]
        public void DebugSetFull()
        {
            _lastNormalized = 0f;
            SetValue(100f, 100f);
        }

        [ContextMenu("Debug: Increase 10%")]
        public void DebugIncrease10()
        {
            float next = Mathf.Min(1f, _lastNormalized + 0.1f);
            SetValue(next * 100f, 100f);
        }

        [ContextMenu("Debug: Decrease 10%")]
        public void DebugDecrease10()
        {
            float next = Mathf.Max(0f, _lastNormalized - 0.1f);
            SetValue(next * 100f, 100f);
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
