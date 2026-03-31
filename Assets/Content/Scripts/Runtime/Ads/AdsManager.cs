using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;

namespace LittleHeroJourney
{
    public static class AdsManager
    {
        private static InterstitialAd _interstitial;
        private static bool _initialized;
        private static AdsSettingsSO _settings;
        public static bool IsShowing { get; private set; }
        private static bool DebugLogsEnabled => _settings == null || _settings.enableDebugLogs;
        private static bool _isLoadInProgress;
        private static Coroutine _retryLoadCoroutine;
        private static AdsRuntimeRunner _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            _settings = LoadSettings();
            EnsureRunner();
            Log("Init start");
            MobileAds.Initialize(status =>
            {
                Log(status == null ? "MobileAds.Initialize status=NULL" : "MobileAds.Initialize done");
            });
            LoadInterstitial();
        }

        private static AdsSettingsSO LoadSettings()
        {
            return Resources.Load<AdsSettingsSO>("AdsSettings");
        }

        private static string GetInterstitialUnitId()
        {
#if UNITY_ANDROID
            if (_settings != null && _settings.useTestInterstitial)
                return "ca-app-pub-3940256099942544/1033173712";
            if (_settings != null && !string.IsNullOrEmpty(_settings.androidInterstitialUnitId))
                return _settings.androidInterstitialUnitId;
            return "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
            if (_settings != null && _settings.useTestInterstitial)
                return "ca-app-pub-3940256099942544/4411468910";
            if (_settings != null && !string.IsNullOrEmpty(_settings.iosInterstitialUnitId))
                return _settings.iosInterstitialUnitId;
            return "ca-app-pub-3940256099942544/4411468910";
#else
            return "unused";
#endif
        }

        public static void LoadInterstitial()
        {
            if (_isLoadInProgress)
            {
                Log("LoadInterstitial skipped load in progress");
                return;
            }
            string unitId = GetInterstitialUnitId();
            if (string.IsNullOrEmpty(unitId) || unitId == "unused")
            {
                Log("LoadInterstitial skipped unitId invalid");
                return;
            }
            Log("LoadInterstitial request unitId=" + unitId);
            _isLoadInProgress = true;
            var request = new AdRequest();
            InterstitialAd.Load(unitId, request, (ad, error) =>
            {
                _isLoadInProgress = false;
                if (error != null)
                {
                    Log("LoadInterstitial failed code=" + error.GetCode() + " domain=" + error.GetDomain() + " message=" + error.GetMessage());
                    ScheduleRetryLoad(5f);
                    return;
                }
                if (ad == null)
                {
                    Log("LoadInterstitial callback ad=NULL");
                    ScheduleRetryLoad(5f);
                    return;
                }
                _interstitial = ad;
                CancelRetryLoad();
                Log("LoadInterstitial success");
                _interstitial.OnAdFullScreenContentOpened += OnAdOpened;
                _interstitial.OnAdFullScreenContentClosed += OnAdClosed;
                _interstitial.OnAdFullScreenContentFailed += OnAdFailedToShow;
                _interstitial.OnAdPaid += OnAdPaid;
                _interstitial.OnAdImpressionRecorded += OnAdImpressionRecorded;
                _interstitial.OnAdClicked += OnAdClicked;
            });
        }

        public static bool CanShowInterstitial()
        {
            return _interstitial != null && _interstitial.CanShowAd();
        }

        public static bool ShowInterstitial()
        {
            if (_interstitial != null && _interstitial.CanShowAd())
            {
                Log("ShowInterstitial show");
                _interstitial.Show();
                return true;
            }
            Log("ShowInterstitial skipped not ready");
            return false;
        }

        private static void OnAdOpened()
        {
            AudioListener.pause = true;
            IsShowing = true;
            Log("OnAdOpened");
        }

        private static void OnAdClosed()
        {
            AudioListener.pause = false;
            IsShowing = false;
            Log("OnAdClosed");
            LoadInterstitial();
        }

        private static void OnAdFailedToShow(AdError error)
        {
            AudioListener.pause = false;
            IsShowing = false;
            Log("OnAdFailedToShow error=" + error);
            LoadInterstitial();
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("AdsRuntimeRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<AdsRuntimeRunner>();
        }

        private static void ScheduleRetryLoad(float seconds)
        {
            EnsureRunner();
            if (_retryLoadCoroutine != null) return;
            _retryLoadCoroutine = _runner.StartCoroutine(RetryLoadRoutine(seconds));
            Log("Retry scheduled in " + seconds + "s");
        }

        private static IEnumerator RetryLoadRoutine(float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
            _retryLoadCoroutine = null;
            LoadInterstitial();
        }

        private static void CancelRetryLoad()
        {
            if (_runner == null || _retryLoadCoroutine == null) return;
            _runner.StopCoroutine(_retryLoadCoroutine);
            _retryLoadCoroutine = null;
        }

        private static void OnAdPaid(AdValue adValue)
        {
            if (adValue == null)
            {
                Log("OnAdPaid value=NULL");
                return;
            }
            Log("OnAdPaid micros=" + adValue.Value + " currency=" + adValue.CurrencyCode + " precision=" + adValue.Precision);
        }

        private static void OnAdImpressionRecorded()
        {
            Log("OnAdImpressionRecorded");
        }

        private static void OnAdClicked()
        {
            Log("OnAdClicked");
        }

        private static void Log(string message)
        {
            if (!DebugLogsEnabled) return;
            Debug.Log("[AdsManager] " + message);
        }

        private sealed class AdsRuntimeRunner : MonoBehaviour
        {
        }
    }
}
