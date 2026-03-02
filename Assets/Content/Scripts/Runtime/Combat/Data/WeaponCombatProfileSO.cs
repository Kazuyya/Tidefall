using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "WeaponCombatProfile", menuName = "Little Hero Journey/Combat/Weapon Combat Profile")]
    public class WeaponCombatProfileSO : ScriptableObject
    {
        [Header("Weapon Info")]
    public WeaponType weaponType;
    public string weaponName;

    [Header("Available Combo Sequences")]
    public List<ComboSequenceSO> availableCombos = new List<ComboSequenceSO>();

    [Header("Animation Settings")]
    public AnimatorOverrideController animatorOverride;

    [Header("Debug")]
    public bool showDebugInfo = false;

        // Helper methods
        public ComboSequenceSO GetComboByIndex(int index)
        {
            if (index >= 0 && index < availableCombos.Count)
                return availableCombos[index];
            return null;
        }

        public List<ComboSequenceSO> GetCombosForWeaponType()
        {
            return availableCombos.FindAll(combo => combo.requiredWeapon == weaponType);
        }

        // TODO: Implement damage calculation when combat stats are ready
        public float GetTotalDamageMultiplier(int comboIndex, int attackIndex)
        {
            // For now, return 1.0 (no multiplier)
            return 1.0f;
        }
    }
}