using UnityEngine;
using UnityEngine.UI;

namespace LittleHeroJourney.UI
{
    public class StatBar : MonoBehaviour
    {
        #region Fields

        [Header("Slider")]
        [SerializeField] private Slider slider;

        [Header("Value Range")]
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;

        [Header("Debug")]
        [SerializeField] protected bool showDebugLog = false;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (slider != null)
            {
                slider.minValue = minValue;
                slider.maxValue = maxValue;
            }
        }

        #endregion

        #region Update Slider Value

        /// <summary>
        /// Set slider value berdasarkan current/max
        /// Otomatis normalize ke slider min/max
        /// </summary>
        public void SetValue(float currentValue, float max)
        {
            if (slider == null) return;

            // Normalize current value ke 0-1
            float normalizedValue = max > 0 ? currentValue / max : 0f;

            // Map ke slider range
            float sliderValue = Mathf.Lerp(minValue, maxValue, normalizedValue);
            slider.value = sliderValue;

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Value: {currentValue:F1}/{max:F1} -> Slider: {sliderValue:F1}");
        }

        /// <summary>
        /// Set slider value directly (0-1 normalized)
        /// </summary>
        public void SetNormalizedValue(float normalizedValue)
        {
            if (slider == null) return;

            normalizedValue = Mathf.Clamp01(normalizedValue);
            float sliderValue = Mathf.Lerp(minValue, maxValue, normalizedValue);
            slider.value = sliderValue;

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Normalized: {normalizedValue:F2} -> Slider: {sliderValue:F1}");
        }

        #endregion

        #region Properties

        public Slider Slider => slider;
        public float MinValue { get => minValue; set => minValue = value; }
        public float MaxValue { get => maxValue; set => maxValue = value; }

        #endregion
    }
}
