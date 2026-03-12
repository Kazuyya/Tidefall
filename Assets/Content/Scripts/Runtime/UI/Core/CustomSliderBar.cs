using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LittleHeroJourney.UI
{
    public class CustomSliderBar : MonoBehaviour
    {
        [Header("Fill Range")]
        [Tooltip("Fill 0% (kosong) tampil di nilai ini. 1 = mentok penuh.")]
        [SerializeField] [Range(0f, 1f)] private float fillRangeMin = 0.1f;
        [Tooltip("Fill 100% (penuh) tampil di nilai ini. 0 = mentok kosong.")]
        [SerializeField] [Range(0f, 1f)] private float fillRangeMax = 0.9f;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            if (_slider != null)
            {
                _slider.minValue = fillRangeMin;
                _slider.maxValue = fillRangeMax;
            }
        }

        public float GetTargetValue(float normalized)
        {
            return Mathf.Lerp(fillRangeMin, fillRangeMax, Mathf.Clamp01(normalized));
        }

        public float GetNormalized()
        {
            if (_slider == null) _slider = GetComponent<Slider>();
            if (_slider == null) return 0f;
            return Mathf.InverseLerp(fillRangeMin, fillRangeMax, _slider.value);
        }

        public void SetNormalized(float normalized)
        {
            if (_slider == null) _slider = GetComponent<Slider>();
            if (_slider == null) return;
            _slider.value = GetTargetValue(normalized);
        }

        public Tween TweenToNormalized(float normalized, float duration, Ease ease)
        {
            if (_slider == null) _slider = GetComponent<Slider>();
            if (_slider == null) return null;
            float target = GetTargetValue(normalized);
            return _slider.DOValue(target, duration).SetEase(ease);
        }

        public Slider Slider => _slider != null ? _slider : (_slider = GetComponent<Slider>());
        public float FillRangeMin => fillRangeMin;
        public float FillRangeMax => fillRangeMax;
    }
}
