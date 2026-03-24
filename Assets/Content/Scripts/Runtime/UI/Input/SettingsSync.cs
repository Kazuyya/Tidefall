using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UISwitcher;

namespace LittleHeroJourney.UI
{
    public class SettingsSync : MonoBehaviour
    {
        [SerializeField] private string settingId = "master";
        private Slider _targetSlider;
        private Toggle _targetToggle;
        private UISwitcher.UISwitcher _targetUiSwitcher;

        private void Awake()
        {
            CacheTargets();
        }

        private void OnEnable()
        {
            CacheTargets();
            GameEventSystem.SubscribeActionWithPayload(SettingsManager.AudioVolumeValueByIdEvent, OnValuePayload);

            if (_targetSlider != null)
                _targetSlider.onValueChanged.AddListener(OnSliderChanged);
            if (_targetToggle != null)
                _targetToggle.onValueChanged.AddListener(OnToggleChanged);
            if (_targetUiSwitcher != null)
                _targetUiSwitcher.onValueChanged.AddListener(OnUiSwitcherChanged);

            GameEventSystem.Publish(new UIActionEvent(SettingsManager.RequestAudioValuesEvent));
        }

        private void OnDisable()
        {
            GameEventSystem.UnsubscribeActionWithPayload(SettingsManager.AudioVolumeValueByIdEvent, OnValuePayload);

            if (_targetSlider != null)
                _targetSlider.onValueChanged.RemoveListener(OnSliderChanged);
            if (_targetToggle != null)
                _targetToggle.onValueChanged.RemoveListener(OnToggleChanged);
            if (_targetUiSwitcher != null)
                _targetUiSwitcher.onValueChanged.RemoveListener(OnUiSwitcherChanged);
        }

        private void OnValuePayload(string payload)
        {
            if (!TryParsePayload(payload, out string id, out string rawValue)) return;
            if (!string.Equals(id, settingId, StringComparison.OrdinalIgnoreCase)) return;

            if (_targetSlider != null)
            {
                if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return;
                _targetSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
            }
            else if (_targetToggle != null)
            {
                if (!TryParseBool(rawValue, out bool value)) return;
                _targetToggle.SetIsOnWithoutNotify(value);
            }
            else if (_targetUiSwitcher != null)
            {
                if (!TryParseBool(rawValue, out bool value)) return;
                _targetUiSwitcher.SetWithoutNotify(value);
            }
        }

        private void OnSliderChanged(float value)
        {
            if (string.IsNullOrEmpty(settingId)) return;
            string payload = settingId + "|" + Mathf.Clamp01(value).ToString(CultureInfo.InvariantCulture);
            GameEventSystem.Publish(new UIActionEvent(SettingsManager.SetAudioVolumeByIdEvent, payload));
        }

        private void OnToggleChanged(bool value)
        {
            if (string.IsNullOrEmpty(settingId)) return;
            string payload = settingId + "|" + (value ? "1" : "0");
            GameEventSystem.Publish(new UIActionEvent(SettingsManager.SetAudioVolumeByIdEvent, payload));
        }

        private void OnUiSwitcherChanged(bool value)
        {
            if (string.IsNullOrEmpty(settingId)) return;
            string payload = settingId + "|" + (value ? "1" : "0");
            GameEventSystem.Publish(new UIActionEvent(SettingsManager.SetAudioVolumeByIdEvent, payload));
        }

        private static bool TryParsePayload(string payload, out string id, out string rawValue)
        {
            id = string.Empty;
            rawValue = string.Empty;
            if (string.IsNullOrEmpty(payload)) return false;

            int sep = payload.IndexOf('|');
            if (sep <= 0 || sep >= payload.Length - 1) return false;

            id = payload.Substring(0, sep);
            rawValue = payload.Substring(sep + 1);
            return true;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrEmpty(raw)) return false;
            if (string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
            if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
            return bool.TryParse(raw, out value);
        }

        private void CacheTargets()
        {
            _targetSlider = GetComponent<Slider>();
            _targetToggle = GetComponent<Toggle>();
            _targetUiSwitcher = GetComponent<UISwitcher.UISwitcher>();

            if (_targetUiSwitcher != null && _targetSlider == null && _targetToggle == null &&
                string.Equals(settingId, "master", StringComparison.OrdinalIgnoreCase))
            {
                settingId = "bloom";
            }
        }
    }
}
