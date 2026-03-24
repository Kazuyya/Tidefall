using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleHeroJourney.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIButtonInteraction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Click Audio")]
        [SerializeField] private bool playClickSfx = true;
        [SerializeField] private string clickSfxId = string.Empty;

        [Header("Click Feedback")]
        [SerializeField] private RectTransform target;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] [Range(0.8f, 1f)] private float clickScale = 0.92f;
        [SerializeField] [Min(0.01f)] private float shrinkDuration = 0.06f;
        [SerializeField] [Min(0.01f)] private float returnDuration = 0.1f;

        private Button _button;
        private Vector3 _baseScale;
        private Tween _scaleTween;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (target == null)
                target = transform as RectTransform;

            if (target != null)
                _baseScale = target.localScale;
        }

        private void OnEnable()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(OnClick);

            if (target != null)
                target.localScale = _baseScale;
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
            KillTween();
            if (target != null)
                target.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate()) return;
            AnimateTo(_baseScale * clickScale, shrinkDuration, Ease.OutCubic);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanAnimate()) return;
            AnimateTo(_baseScale, returnDuration, Ease.OutExpo);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanAnimate()) return;
            AnimateTo(_baseScale, returnDuration, Ease.OutExpo);
        }

        private void OnClick()
        {
            if (!CanAnimate()) return;
            KillTween();
            float pulseDown = Mathf.Max(0.01f, shrinkDuration * 0.5f);
            float pulseUp = Mathf.Max(0.01f, returnDuration * 0.6f);
            _scaleTween = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .Append(target.DOScale(_baseScale * clickScale, pulseDown).SetEase(Ease.OutCubic))
                .Append(target.DOScale(_baseScale, pulseUp).SetEase(Ease.OutExpo))
                .OnComplete(() =>
                {
                    if (target != null) target.localScale = _baseScale;
                    _scaleTween = null;
                });

            if (playClickSfx && !string.IsNullOrEmpty(clickSfxId))
            {
                CharacterEffectManager.Instance?.PlayAudio(clickSfxId, Vector3.zero);
            }
        }

        private bool CanAnimate()
        {
            return _button != null && target != null && _button.interactable && isActiveAndEnabled;
        }

        private void AnimateTo(Vector3 scale, float duration, Ease ease)
        {
            KillTween();
            _scaleTween = target.DOScale(scale, duration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime);
        }

        private void KillTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        private void OnValidate()
        {
            clickScale = Mathf.Clamp(clickScale, 0.8f, 1f);
            shrinkDuration = Mathf.Max(0.01f, shrinkDuration);
            returnDuration = Mathf.Max(0.01f, returnDuration);
        }
    }

    // Backward-compat alias for existing components in scenes/prefabs.
    public class UIButtonClickFeedback : UIButtonInteraction
    {
    }
}
