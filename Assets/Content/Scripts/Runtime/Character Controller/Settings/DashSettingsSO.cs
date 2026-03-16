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
        [Tooltip("Dash trigger parameter in Animator. Ensure Attack → Dash transition exists (condition: this trigger) so attack cancel goes to dash animation.")]
        public string dashParameterName;
        [Tooltip("True = can dash during attack (override), dash trigger is fired. False = cannot dash during attack.")]
        public bool dashOverridesAttack = true;

        [Header("Debug Settings")]
        public bool ShowDebugGizmos;
        public bool ShowDebugLog;
    }
}