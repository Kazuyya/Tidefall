using UnityEngine;
using DG.Tweening;

namespace LittleHeroJourney.UI
{
    public class StunBar : MonoBehaviour
    {
        [Header("Main Bar")]
        [SerializeField] private CustomSliderBar mainSliderBar;

        [Header("Animation")]
        [SerializeField] private bool shakeOnDecrease = true;
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeStrength = 8f;
        [SerializeField] private RectTransform shakeTarget;

        [Header("Ghost")]
        [SerializeField] private bool useGhost;
        [SerializeField] private CustomSliderBar ghostBar;
        [SerializeField] private float ghostDuration = 0.4f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;

        private StunManager _targetStunManager;
        private float _lastNormalized = 1f;
        private Tween _ghostTween;
        private Tween _shakeTween;

        private void Awake()
        {
            if (mainSliderBar == null)
                mainSliderBar = GetComponent<CustomSliderBar>();
            if (shakeTarget == null && TryGetComponent<RectTransform>(out var rt))
                shakeTarget = rt;
            SyncGhostToMain();
        }

        private void OnDestroy()
        {
            _ghostTween?.Kill();
            _shakeTween?.Kill();
            Cleanup();
        }

        private void SyncGhostToMain()
        {
            if (mainSliderBar == null || ghostBar == null) return;
            float n = mainSliderBar.Slider != null
                ? Mathf.InverseLerp(mainSliderBar.FillRangeMin, mainSliderBar.FillRangeMax, mainSliderBar.Slider.value)
                : 1f;
            ghostBar.SetNormalized(n);
        }

        public void SetupForTarget(StunManager target)
        {
            if (target == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Target stun manager is null!");
                return;
            }

            Cleanup();
            _targetStunManager = target;
            _targetStunManager.OnStunHealthChanged += OnStunChanged;
            OnStunChanged(_targetStunManager.StunHealthPercentage);
        }

        public void Cleanup()
        {
            if (_targetStunManager != null)
                _targetStunManager.OnStunHealthChanged -= OnStunChanged;
            _targetStunManager = null;
        }

        private void OnStunChanged(float stunPercentage)
        {
            if (_targetStunManager == null) return;
            SetValue(_targetStunManager.CurrentStunHealth, _targetStunManager.MaxStunHealth);
        }

        public void SetValue(float currentValue, float max)
        {
            if (mainSliderBar == null) return;

            float normalized = max > 0 ? Mathf.Clamp01(currentValue / max) : 0f;

            mainSliderBar.SetNormalized(normalized);

            if (useGhost && ghostBar != null)
            {
                _ghostTween?.Kill();
                _ghostTween = ghostBar.TweenToNormalized(normalized, ghostDuration, Ease.OutQuad);
            }

            if (shakeOnDecrease && normalized < _lastNormalized && shakeTarget != null)
            {
                _shakeTween?.Kill();
                _shakeTween = shakeTarget.DOShakeAnchorPos(shakeDuration, shakeStrength, 30);
            }

            _lastNormalized = normalized;
        }

        public StunManager TargetStunManager => _targetStunManager;
    }
}
