using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Base stats + scale per level. Final = base * GetScale(level).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelStatsConfig", menuName = "Little Hero Journey/Progression/Level Stats Config", order = 0)]
    public class LevelStatsConfigSO : ScriptableObject
    {
        [Header("Base Stats (Level 1)")]
        [Min(0)] public float baseMaxHealth = 100f;
        [Min(0)] public float baseATK = 50f;
        [Min(0)] public float baseDEF = 20f;
        [Min(0)] public float baseImpact = 50f;
        [Min(0)] public float baseKnockbackResistance = 20f;
        [Min(0)] public float baseKnockbackDistance = 2f;

        [Header("Base Movement / Combat (no level scale)")]
        [Tooltip("Base move speed. 0 = not used (use from Movement/AI Settings).")]
        [Min(0)] public float baseMoveSpeed = 0f;
        [Tooltip("Attack animation speed multiplier. 1 = normal. Used by AI.")]
        [Min(0.1f)] public float baseAttackSpeed = 1f;
        [Tooltip("Cooldown between attacks (seconds). 0 = use from AI Settings.")]
        [Min(0)] public float baseAttackCooldown = 0f;

        [Header("Scale Per Level")]
        [Tooltip("Scale = 1 + (level-1) * ini. Contoh: 0.1 → level 1=1, level 2=1.1, level 5=1.4")]
        [Range(0f, 0.5f)]
        [SerializeField] private float scalePerLevel = 0.1f;

        /// <summary>Multiplier for given level. Scale = 1 + (level-1) * scalePerLevel.</summary>
        public float GetScale(int level)
        {
            if (level < 1) level = 1;
            return 1f + (level - 1) * scalePerLevel;
        }

        /// <summary>Base stats scaled for level. Move/attack speed and cooldown are unscaled.</summary>
        public StatsSnapshot GetStats(int level)
        {
            float scale = GetScale(level);
            return new StatsSnapshot
            {
                maxHealth = baseMaxHealth * scale,
                ATK = baseATK * scale,
                DEF = baseDEF * scale,
                Impact = baseImpact * scale,
                knockbackResistance = baseKnockbackResistance * scale,
                knockbackDistance = baseKnockbackDistance * scale,
                moveSpeed = baseMoveSpeed,
                attackSpeed = baseAttackSpeed,
                attackCooldown = baseAttackCooldown
            };
        }
    }

    /// <summary>Runtime snapshot of stats to apply to Health and CharacterStats. Move/attack are unscaled.</summary>
    public struct StatsSnapshot
    {
        public float maxHealth;
        public float ATK;
        public float DEF;
        public float Impact;
        public float knockbackResistance;
        public float knockbackDistance;
        public float moveSpeed;
        public float attackSpeed;
        public float attackCooldown;
    }

    /// <summary>Override which base stats to use per character. Only base damage is scaled by level; rest use override or config base.</summary>
    [System.Serializable]
    public struct StatsOverride
    {
        [Header("Combat (base only, scale applies)")]
        [Tooltip("Override base damage (ATK = this * scale). Player & AI.")]
        public bool overrideBaseDamage;
        [Min(0)] public float baseDamage;

        [Tooltip("Override DEF base (DEF = this * scale). Player & AI.")]
        public bool overrideDEF;
        [Min(0)] public float def;

        [Header("Movement (no scale)")]
        [Tooltip("Override move speed. Player & AI.")]
        public bool overrideMoveSpeed;
        [Min(0)] public float moveSpeed;

        [Header("AI only (no scale)")]
        [Tooltip("Override attack animation speed multiplier.")]
        public bool overrideAttackSpeed;
        [Min(0.1f)] public float attackSpeed;
        [Tooltip("Override cooldown between attacks (seconds).")]
        public bool overrideAttackCooldown;
        [Min(0)] public float attackCooldown;
    }
}
