using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [RequireComponent(typeof(BoxCollider))]
    public class AIWeapon : MonoBehaviour
    {
        #region Fields

        [Header("Components")]
        private BoxCollider weaponCollider;
        private AIAgent aiAgent;
        private DamageDealer damageDealer;

        [Header("Hit Detection")]
        private HashSet<Collider> hitEnemies = new HashSet<Collider>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        // Events
        public event System.Action<Collider> OnHitEnemy;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            weaponCollider = GetComponent<BoxCollider>();
            aiAgent = GetComponentInParent<AIAgent>();
            damageDealer = GetComponent<DamageDealer>();

            if (weaponCollider == null)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No BoxCollider found on AI weapon!");
            }

            if (aiAgent == null)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No AIAgent found in parent hierarchy!");
            }

            if (damageDealer == null)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No DamageDealer found on AI weapon! Adding default one.");
                damageDealer = gameObject.AddComponent<DamageDealer>();
            }

            // Start disabled
            DisableWeaponCollider();
        }

        #endregion

        #region Hit Detection

        void OnTriggerEnter(Collider other)
        {
            if (aiAgent == null || !aiAgent.IsAttacking) return;

            Health enemyHealth = Helper.TryGetEnemyHealth(other, transform.root, damageDealer?.DamageData);
            if (enemyHealth != null && !hitEnemies.Contains(other))
            {
                hitEnemies.Add(other);
                ProcessHit(enemyHealth, other);
            }
        }

        private void ProcessHit(Health enemyHealth, Collider enemyCollider)
        {
            damageDealer?.DealDamage(enemyHealth);
            OnHitEnemy?.Invoke(enemyCollider);
        }

        #endregion

        #region Weapon Control

        public void EnableWeapon()
        {
            if (weaponCollider != null && !weaponCollider.enabled)
            {
                weaponCollider.enabled = true;
                hitEnemies.Clear();
            }
        }

        public void DisableWeapon()
        {
            if (weaponCollider != null)
            {
                weaponCollider.enabled = false;
            }
        }

        public void DisableWeaponCollider()
        {
            DisableWeapon();
        }

        public void EnableWeaponCollider()
        {
            EnableWeapon();
        }

        public void ResetForNewAttack()
        {
            hitEnemies.Clear();
        }

        #endregion

        #region Properties

        public BoxCollider WeaponCollider => weaponCollider;
        public AIAgent AIAgent => aiAgent;
        public DamageDealer DamageDealer => damageDealer;

        public void UpdateDamageData(DamageData newDamageData)
        {
            if (damageDealer == null || newDamageData == null) return;
            damageDealer.SetDamageData(newDamageData);
            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Damage data: base {newDamageData.baseDamage}");
        }

        private bool ShowDebugLog => showDebugLog || (aiAgent != null && aiAgent.Settings != null && aiAgent.Settings.ShowDebugLog);

        #endregion
    }
}
