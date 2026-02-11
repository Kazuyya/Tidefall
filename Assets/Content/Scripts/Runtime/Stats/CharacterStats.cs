using UnityEngine;

namespace LittleHeroJourney
{
    public class CharacterStats : MonoBehaviour
    {
        [Header("Stats")]
        [Min(0)] public float ATK = 50f;
        [Min(0)] public float DEF = 20f;
        [Min(0)] public float Impact = 50f;

        [Header("Knockback")]
        [Min(0)] public float knockbackResistance = 20f;
        [Min(0)] public float knockbackDistance = 2f;

        public float CalculateDamageWithDefense(float incomingDamage)
        {
            return Helper.ApplyPercentReduction(incomingDamage, DEF, 1f);
        }
    }
}
