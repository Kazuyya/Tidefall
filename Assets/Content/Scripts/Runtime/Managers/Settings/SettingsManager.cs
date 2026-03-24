using System;
using System.Globalization;
using UnityEngine;

namespace LittleHeroJourney
{
    public class SettingsManager : MonoBehaviour
    {
        public const string OpenSettingsEvent = "OpenSettings";
        public const string SetAudioVolumeByIdEvent = "SetAudioVolumeById";
        public const string RequestAudioValuesEvent = "RequestAudioSettingsValues";
        public const string AudioVolumeValueByIdEvent = "AudioVolumeValueById";

        private const string MasterKey = "settings_audio_master";
        private const string BgmKey = "settings_audio_bgm";
        private const string SfxKey = "settings_audio_sfx";
        private const string BloomEnabledKey = "settings_bloom_enabled";

        private static SettingsManager _instance;
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SettingsManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SettingsManager");
                        _instance = go.AddComponent<SettingsManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public static event Action AudioSettingsChanged;

        [Header("Audio")]
        [SerializeField] private string masterVolumeId = "master";
        [SerializeField] private string bgmVolumeId = "bgm";
        [SerializeField] private string sfxVolumeId = "sfx";
        private float masterVolume = 1f;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;

        [Header("Graphics")]
        [SerializeField] private GameObject BloomObject;
        [SerializeField] private string bloomEnabledId = "bloom";
        private bool bloomEnabled = false;

        [Header("UI")]
        [SerializeField] private string settingsCanvasId = "Settings";

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public bool BloomEnabled => bloomEnabled;

        private Action<string> _setAudioByIdHandler;
        private Action _requestValuesHandler;
        private Action _openSettingsHandler;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            PublishSettingsValues();
        }

        private void OnEnable()
        {
            _setAudioByIdHandler = OnSetAudioByIdPayload;
            GameEventSystem.SubscribeActionWithPayload(SetAudioVolumeByIdEvent, _setAudioByIdHandler);
            _requestValuesHandler = PublishSettingsValues;
            GameEventSystem.SubscribeAction(RequestAudioValuesEvent, _requestValuesHandler);
            _openSettingsHandler = OpenSettingsCanvas;
            GameEventSystem.SubscribeAction(OpenSettingsEvent, _openSettingsHandler);
        }

        private void OnDisable()
        {
            if (_setAudioByIdHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload(SetAudioVolumeByIdEvent, _setAudioByIdHandler);
            if (_requestValuesHandler != null)
                GameEventSystem.UnsubscribeAction(RequestAudioValuesEvent, _requestValuesHandler);
            if (_openSettingsHandler != null)
                GameEventSystem.UnsubscribeAction(OpenSettingsEvent, _openSettingsHandler);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            SaveSettings();
            AudioSettingsChanged?.Invoke();
            PublishSettingsValues();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            SaveSettings();
            AudioSettingsChanged?.Invoke();
            PublishSettingsValues();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            SaveSettings();
            AudioSettingsChanged?.Invoke();
            PublishSettingsValues();
        }

        public void SetBloomEnabled(bool value)
        {
            bloomEnabled = value;
            ApplyBloomSetting();
            SaveSettings();
            PublishSettingsValues();
        }

        public void LoadSettings()
        {
            masterVolume = ES3.KeyExists(MasterKey) ? ES3.Load<float>(MasterKey) : 1f;
            bgmVolume = ES3.KeyExists(BgmKey) ? ES3.Load<float>(BgmKey) : 1f;
            sfxVolume = ES3.KeyExists(SfxKey) ? ES3.Load<float>(SfxKey) : 1f;
            bloomEnabled = ES3.KeyExists(BloomEnabledKey) ? ES3.Load<bool>(BloomEnabledKey) : false;
            masterVolume = Mathf.Clamp01(masterVolume);
            bgmVolume = Mathf.Clamp01(bgmVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            ApplyBloomSetting();
            AudioSettingsChanged?.Invoke();
            PublishSettingsValues();
        }

        public void SaveSettings()
        {
            ES3.Save(MasterKey, masterVolume);
            ES3.Save(BgmKey, bgmVolume);
            ES3.Save(SfxKey, sfxVolume);
            ES3.Save(BloomEnabledKey, bloomEnabled);
        }

        private void OnSetAudioByIdPayload(string payload)
        {
            if (!TryParseIdAndRaw(payload, out string id, out string rawValue)) return;
            if (string.Equals(id, masterVolumeId, StringComparison.OrdinalIgnoreCase))
            {
                if (TryParse01(rawValue, out float value))
                    SetMasterVolume(value);
            }
            else if (string.Equals(id, bgmVolumeId, StringComparison.OrdinalIgnoreCase))
            {
                if (TryParse01(rawValue, out float value))
                    SetBgmVolume(value);
            }
            else if (string.Equals(id, sfxVolumeId, StringComparison.OrdinalIgnoreCase))
            {
                if (TryParse01(rawValue, out float value))
                    SetSfxVolume(value);
            }
            else if (string.Equals(id, bloomEnabledId, StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseBool(rawValue, out bool value))
                    SetBloomEnabled(value);
            }
        }

        private static bool TryParseIdAndRaw(string payload, out string id, out string rawValue)
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

        private static bool TryParse01(string rawValue, out float value)
        {
            bool ok = float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            value = Mathf.Clamp01(value);
            return ok;
        }

        private static bool TryParseBool(string rawValue, out bool value)
        {
            value = false;
            if (string.IsNullOrEmpty(rawValue)) return false;
            if (string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
            if (string.Equals(rawValue, "0", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
            return bool.TryParse(rawValue, out value);
        }

        private void PublishSettingsValues()
        {
            PublishValue(masterVolumeId, masterVolume);
            PublishValue(bgmVolumeId, bgmVolume);
            PublishValue(sfxVolumeId, sfxVolume);
            PublishBoolValue(bloomEnabledId, bloomEnabled);
        }

        private static void PublishValue(string id, float value)
        {
            if (string.IsNullOrEmpty(id)) return;
            string payload = id + "|" + value.ToString(CultureInfo.InvariantCulture);
            GameEventSystem.Publish(new UIActionEvent(AudioVolumeValueByIdEvent, payload));
        }

        private static void PublishBoolValue(string id, bool value)
        {
            if (string.IsNullOrEmpty(id)) return;
            string payload = id + "|" + (value ? "1" : "0");
            GameEventSystem.Publish(new UIActionEvent(AudioVolumeValueByIdEvent, payload));
        }

        private void ApplyBloomSetting()
        {
            if (BloomObject == null)
            {
                Debug.LogWarning($"[{GetType().Name}] BloomObject is not assigned. Bloom setting cannot be applied.");
                return;
            }
            BloomObject.SetActive(bloomEnabled);
        }

        private void OpenSettingsCanvas()
        {
            if (string.IsNullOrEmpty(settingsCanvasId)) return;
            if (GameManager.Instance?.CanvasManager == null) return;
            GameManager.Instance.CanvasManager.SwitchCanvas(settingsCanvasId);
        }
    }
}
