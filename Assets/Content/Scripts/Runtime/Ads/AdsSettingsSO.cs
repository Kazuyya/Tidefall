using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "Little Hero Journey/Ads/Ads Settings")]
    public class AdsSettingsSO : ScriptableObject
    {
        [Header("Interstitial")]
        public string androidInterstitialUnitId;
        public string iosInterstitialUnitId;
        public float showDelayAfterLoadingSeconds = 1f;
        public bool enableDebugLogs = true;

        [Header("Main Menu Scene Id")]
        public string mainMenuSceneId = "MainMenu";

        [Header("Gameplay Scene Id")]
        public string gameplaySceneId = "Level";
    }
}
