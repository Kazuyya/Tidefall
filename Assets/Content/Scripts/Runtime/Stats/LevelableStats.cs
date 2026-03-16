using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Applies level-based stats from LevelStatsConfigSO to Health and CharacterStats.
    /// Use StatsOverride to override per character: Player = move speed + base damage (and DEF). AI = move speed, base damage, DEF, attack speed, attack cooldown.
    /// Final combat stats = base * scale(level). Move/attack speed and cooldown are unscaled (override or config base).
    /// </summary>
    public class LevelableStats : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private LevelStatsConfigSO config;
        [SerializeField] private int level = 1;

        [Header("Override (optional)")]
        [Tooltip("Override which base stats to use. Scale(level) still applies to ATK/DEF/HP/Impact/knockback.")]
        [SerializeField] private StatsOverride overrides;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;
        [Tooltip("Editor only: set level (e.g. 3) to see preview stats below.")]
        [SerializeField] private int debugPreviewLevel;

        private Health _health;
        private CharacterStats _characterStats;

        public int Level => level;
        /// <summary>Effective move speed (override or config). 0 = use from Movement/AI Settings.</summary>
        public float MoveSpeed { get; private set; }
        /// <summary>Attack animation speed multiplier. Used by AI.</summary>
        public float AttackSpeed { get; private set; }
        /// <summary>Cooldown between attacks (seconds). 0 = use from AI Settings.</summary>
        public float AttackCooldown { get; private set; }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _characterStats = GetComponent<CharacterStats>();
        }

        private void Start()
        {
            ApplyStatsForLevel(level, healToFull: true);
        }

        /// <summary>Apply stats for level. Use healToFull=true on first init or level-up.</summary>
        public void ApplyStatsForLevel(int newLevel, bool healToFull = false)
        {
            level = Mathf.Max(1, newLevel);
            StatsSnapshot snapshot = BuildSnapshot(level);

            if (_health != null)
            {
                float newMax = snapshot.maxHealth;
                float current = healToFull ? newMax : Mathf.Min(_health.CurrentHealth, newMax);
                _health.Initialize(newMax, current);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Applied maxHealth={newMax:F0} (level {level})");
            }

            if (_characterStats != null)
            {
                _characterStats.ATK = snapshot.ATK;
                _characterStats.DEF = snapshot.DEF;
                _characterStats.Impact = snapshot.Impact;
                _characterStats.knockbackResistance = snapshot.knockbackResistance;
                _characterStats.knockbackDistance = snapshot.knockbackDistance;
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Applied ATK={snapshot.ATK:F0} DEF={snapshot.DEF:F0} (level {level})");
            }

            MoveSpeed = snapshot.moveSpeed;
            AttackSpeed = snapshot.attackSpeed;
            AttackCooldown = snapshot.attackCooldown;
        }

        /// <summary>Set level and apply stats. Use for level-up (optionally heal to full).</summary>
        public void SetLevel(int newLevel, bool healToFull = true)
        {
            ApplyStatsForLevel(newLevel, healToFull);
        }

        /// <summary>Preview stats for a level (e.g. for editor debug).</summary>
        public StatsSnapshot GetStatsForLevel(int forLevel) => BuildSnapshot(Mathf.Max(1, forLevel));

        private StatsSnapshot BuildSnapshot(int forLevel)
        {
            float scale = config != null ? config.GetScale(forLevel) : 1f;
            float baseATK = config != null ? config.baseATK : 50f;
            float baseDEF = config != null ? config.baseDEF : 20f;
            float baseMove = config != null ? config.baseMoveSpeed : 0f;
            float baseAtkSpd = config != null ? config.baseAttackSpeed : 1f;
            float baseAtkCD = config != null ? config.baseAttackCooldown : 0f;

            if (overrides.overrideBaseDamage) baseATK = overrides.baseDamage;
            if (overrides.overrideDEF) baseDEF = overrides.def;
            if (overrides.overrideMoveSpeed) baseMove = overrides.moveSpeed;
            if (overrides.overrideAttackSpeed) baseAtkSpd = overrides.attackSpeed;
            if (overrides.overrideAttackCooldown) baseAtkCD = overrides.attackCooldown;

            if (config == null)
                return new StatsSnapshot { maxHealth = 100f, ATK = baseATK * scale, DEF = baseDEF * scale, Impact = 50f * scale, knockbackResistance = 20f, knockbackDistance = 2f, moveSpeed = baseMove, attackSpeed = baseAtkSpd, attackCooldown = baseAtkCD };

            return new StatsSnapshot
            {
                maxHealth = config.baseMaxHealth * scale,
                ATK = baseATK * scale,
                DEF = baseDEF * scale,
                Impact = config.baseImpact * scale,
                knockbackResistance = config.baseKnockbackResistance * scale,
                knockbackDistance = config.baseKnockbackDistance * scale,
                moveSpeed = baseMove,
                attackSpeed = baseAtkSpd,
                attackCooldown = baseAtkCD
            };
        }
    }
}
