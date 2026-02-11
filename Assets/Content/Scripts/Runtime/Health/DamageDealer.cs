using UnityEngine;

namespace LittleHeroJourney
{
    public class DamageDealer : MonoBehaviour
    {
        #region Fields

        [Header("Damage Configuration")]
        [SerializeField] private DamageData damageData;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;

        public event System.Action<Health, DamageResult> OnDamageDealt;

        private Transform _attackerRoot;
        private CharacterStats _attackerStats;

        #endregion

        #region Properties

        public DamageData DamageData => damageData;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _attackerStats = Helper.GetComponentWithFallback<CharacterStats>(this);
            
            Health health = Helper.GetComponentWithFallback<Health>(this);
            if (health != null) 
            {
                _attackerRoot = health.transform;
            }
            else
            {
                AIAgent aiAgent = Helper.GetComponentWithFallback<AIAgent>(this);
                if (aiAgent != null)
                {
                    _attackerRoot = aiAgent.transform;
                }
                else
                {
                    PlayerCombat playerCombat = Helper.GetComponentWithFallback<PlayerCombat>(this);
                    _attackerRoot = playerCombat != null ? playerCombat.transform : transform.root;
                }
            }
        }

        #endregion

        #region Damage Dealing

        public void DealDamage(Health target)
        {
            if (target == null || damageData == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] DealDamage: target or damageData null.");
                return;
            }
            if (!target.IsAlive)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Target {target.gameObject.name} already dead.");
                return;
            }
            if ((damageData.canDamageFactions & target.ObjectFaction) == 0)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Cannot damage {target.ObjectFaction}.");
                return;
            }

            float atk = _attackerStats != null ? _attackerStats.ATK : 100f;
            float impact = _attackerStats != null ? _attackerStats.Impact : 50f;

            DamageResult result = CreateDamageResultWithATK(atk);
            float healthBefore = target.CurrentHealth;
            target.TakeDamage(result.Damage, transform);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Dealt {result.Damage:F1} ({result.DamageType}, Crit:{result.IsCritical}) to {target.gameObject.name}. Health {healthBefore:F0} -> {target.CurrentHealth:F0}");
            OnDamageDealt?.Invoke(target, result);

            HandleSpecialEffects(target, result, impact);
        }

        public void DealDamage(GameObject targetObject)
        {
            if (targetObject == null) return;

            Health health = targetObject.GetComponent<Health>();
            if (health != null)
            {
                DealDamage(health);
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] Target {targetObject.name} has no Health component");
            }
        }

        public void DealDamage(Component targetComponent)
        {
            if (targetComponent == null) return;
            DealDamage(targetComponent.gameObject);
        }

        #endregion

        #region Damage Calculation

        private DamageResult CreateDamageResultWithATK(float atk)
        {
            float baseVal = damageData.baseDamage + Random.Range(-damageData.damageVariance, damageData.damageVariance);
            float scaled = baseVal * (atk / 100f);
            bool crit = Random.value < damageData.criticalChance;
            if (crit) scaled *= damageData.criticalMultiplier;
            scaled = Mathf.Max(0, scaled);
            return new DamageResult
            {
                Damage = scaled,
                DamageType = damageData.damageType,
                IsCritical = crit,
                KnockbackForce = damageData.knockbackForce,
                StunDamage = damageData.stunDamage
            };
        }

        #endregion

        #region Special Effects

        private void HandleSpecialEffects(Health target, DamageResult result, float attackerImpact)
        {
            if (target == null || !target.IsAlive) return;
            if (result.KnockbackForce > 0) ApplyKnockback(target, result.KnockbackForce);
            if (result.StunDamage > 0) ApplyStunDamage(target, result.StunDamage, attackerImpact);
        }

        private void ApplyKnockback(Health target, float force)
        {
            Vector3 attackerPos = _attackerRoot != null ? _attackerRoot.position : transform.position;

            CharacterStats s = target.GetComponent<CharacterStats>();
            if (s == null)
            {
                var rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = target.transform.position - attackerPos;
                    dir.y = 0;
                    dir.Normalize();
                    rb.AddForce(dir * force, ForceMode.Impulse);
                }
                return;
            }
            if (force <= s.knockbackResistance)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Knockback blocked: force {force} <= res {s.knockbackResistance}.");
                return;
            }

            Vector3 dir2 = target.transform.position - attackerPos;
            if (dir2.sqrMagnitude < 0.1f)
                dir2 = _attackerRoot != null ? _attackerRoot.forward : transform.forward;
            dir2.y = 0;
            dir2.Normalize();

            var kb = target.GetComponent<IKnockbackable>();
            if (kb != null)
                kb.ApplyKnockback(dir2, s.knockbackDistance);
            else if (showDebugLog)
                Debug.LogWarning($"[{GetType().Name}] No IKnockbackable on {target.gameObject.name}.");
        }

        private void ApplyStunDamage(Health target, float stunDamage, float attackerImpact)
        {
            var sm = target.GetComponent<StunManager>();
            if (sm == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No StunManager on {target.gameObject.name}.");
                return;
            }
            sm.AddStunDamage(stunDamage, attackerImpact);
        }

        #endregion

        #region Utility Methods

        public void SetDamageData(DamageData newDamageData)
        {
            damageData = newDamageData;
        }

        #endregion
    }
}