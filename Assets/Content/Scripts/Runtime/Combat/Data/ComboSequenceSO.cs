using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "ComboSequence", menuName = "Combat/Combo Sequence")]
    public class ComboSequenceSO : ScriptableObject
    {
        [Header("Sequence Info")]
    public string sequenceName;
    public WeaponType requiredWeapon;

    [Header("Attack Chain")]
    public List<AttackDataSO> attackSequence = new List<AttackDataSO>();

    [Header("AI Combo Loop Settings")]
    [Tooltip("Allow AI to loop combo sequence from last attack back to first?")]
    public bool allowComboLoop = false;

    [Tooltip("Maximum number of times combo can loop (0 = infinite, only if allowComboLoop = true)")]
    public int maxComboLoops = 0;

    [Header("Debug")]
    public bool showDebugInfo = false;

        // Helper properties
        public int SequenceLength => attackSequence.Count;
        public AttackDataSO GetAttackAtIndex(int index)
        {
            if (index >= 0 && index < attackSequence.Count)
                return attackSequence[index];
            return null;
        }

        public bool IsValidSequence()
        {
            return attackSequence != null && attackSequence.Count > 0;
        }
    }
}