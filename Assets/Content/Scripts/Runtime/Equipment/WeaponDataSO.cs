using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Little Hero Journey/Equipment/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public WeaponType weaponType;
        public string weaponName;

        [Header("Animation")]
        [Tooltip("Animator Override Controller for this weapon's attack animations")]
        public AnimatorOverrideController animatorOverride;

        [Header("Combat Profile (New System)")]
        [Tooltip("Weapon combat profile with advanced combo system")]
        public WeaponCombatProfileSO combatProfile;

        [Header("Optional: Custom Movement Settings")]
        [Tooltip("Leave empty to use default movement settings")]
        public MovementSettingsSO customMovementSettings;

        [Header("Optional: Custom Dash Settings")]
        [Tooltip("Leave empty to use default dash settings")]
        public DashSettingsSO customDashSettings;
    }
}