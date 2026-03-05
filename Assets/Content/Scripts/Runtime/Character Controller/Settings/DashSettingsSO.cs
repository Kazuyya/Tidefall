using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "DashSettings", menuName = "Little Hero Journey/Player/Dash Settings")]
    public class DashSettingsSO : ScriptableObject
    {
        [Header("Dash Settings")]
        public float DashDistance;
        public float DashSpeed;
        public float DashCooldown;
        public float DashAccelerationSharpness;

        [Header("Ground Following Settings")]
        public bool EnableGroundFollowing = true;
        public float MinGroundSmoothFactor;
        public float MaxGroundSmoothFactor;
        public float GroundSmoothScaleThreshold;

        [Header("Animation Settings")]
        [Tooltip("Trigger parameter dash di Animator. Pastikan ada transisi Attack → Dash (condition: trigger ini) supaya cancel attack langsung ke animasi dash.")]
        public string dashParameterName;
        [Tooltip("True = bisa dash saat attack (override), trigger dash dipanggil. False = tidak bisa dash selama attack.")]
        public bool dashOverridesAttack = true;

        [Header("Debug Settings")]
        public bool ShowDebugGizmos;
        public bool ShowDebugLog;
    }
}