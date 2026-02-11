using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "DamageData", menuName = "Little Hero Journey/Combat/Damage Data", order = 1)]
    public class DamageData : ScriptableObject
    {
        [Header("Damage")]
        [Min(0)] public float baseDamage = 10f;
        public DamageType damageType = DamageType.Physical;
        [Range(0f, 1f)] public float criticalChance = 0.1f;
        [Min(1f)] public float criticalMultiplier = 2f;
        [Min(0)] public float damageVariance = 2f;

        [Header("Effects")]
        public float knockbackForce;
        [Min(0)] public float stunDamage;

        [Header("Targeting")]
        public Faction canDamageFactions = Faction.AI;

        public float MinDamage => Mathf.Max(0, baseDamage - damageVariance);
        public float MaxDamage => baseDamage + damageVariance;

        public float CalculateDamage()
        {
            float damage = baseDamage + Random.Range(-damageVariance, damageVariance);

            bool isCritical = Random.value < criticalChance;
            if (isCritical)
            {
                damage *= criticalMultiplier;
            }

            return Mathf.Max(0, damage);
        }

        public DamageResult CreateDamageResult()
        {
            float d = CalculateDamage();
            bool crit = d > (baseDamage + damageVariance) * 0.9f;
            return new DamageResult { Damage = d, DamageType = damageType, IsCritical = crit, KnockbackForce = knockbackForce, StunDamage = stunDamage };
        }
    }

    #region Supporting Types

    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Fire,
        Ice,
        Poison
    }

    public struct DamageResult
    {
        public float Damage;
        public DamageType DamageType;
        public bool IsCritical;
        public float KnockbackForce;
        public float StunDamage;

        public override string ToString()
        {
            return $"Damage: {Damage:F1}, Type: {DamageType}, Critical: {IsCritical}, StunDamage: {StunDamage:F1}";
        }
    }

    #endregion
}