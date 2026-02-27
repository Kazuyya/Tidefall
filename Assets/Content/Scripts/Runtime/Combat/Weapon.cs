using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [RequireComponent(typeof(BoxCollider))]
    public class Weapon : MonoBehaviour
    {
        #region Fields

        [Header("Components")]
        private BoxCollider weaponCollider;
        private PlayerCombat playerCombat;
        private DamageDealer damageDealer;
        private Animator _animator;

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
            playerCombat = GetComponentInParent<PlayerCombat>();
            damageDealer = GetComponent<DamageDealer>();
            _animator = Helper.GetAndCacheAnimator(this, searchInChildren: true, showDebugLog: false, ignoreLayerName: "UI");

            if (weaponCollider == null)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No BoxCollider found on weapon!");
            }
            else
            {
                weaponCollider.enabled = false;
            }

            if (damageDealer == null)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No DamageDealer found on weapon! Adding default one.");
                damageDealer = gameObject.AddComponent<DamageDealer>();
            }
        }

        #endregion

        #region Hit Detection

        void OnTriggerEnter(Collider other)
        {
            if (playerCombat == null || !playerCombat.IsAttacking) return;

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

        public void ResetForNewAttack()
        {
            hitEnemies.Clear();
        }

        #endregion

        #region Weapon Control

        /// <summary>
        /// Set weapon active state. When activated, also clears hit history to prevent double-hits.
        /// </summary>
        public void SetWeaponActive(bool active)
        {
            if (weaponCollider == null) return;

            if (active && playerCombat != null)
            {
                if (!playerCombat.IsAttacking) return;
                if (_animator != null && !IsInAttackClip()) return;
            }

            weaponCollider.enabled = active;
            if (active)
            {
                hitEnemies.Clear();
            }
        }

        // Legacy methods for backward compatibility
        public void EnableWeapon() => SetWeaponActive(true);
        public void DisableWeapon() => SetWeaponActive(false);
        public void EnableWeaponCollider() => SetWeaponActive(true);
        public void DisableWeaponCollider() => SetWeaponActive(false);

        #endregion

        #region Properties

        public BoxCollider WeaponCollider => weaponCollider;
        public DamageDealer DamageDealer => damageDealer;

        public void UpdateDamageData(DamageData newDamageData)
        {
            if (damageDealer == null || newDamageData == null) return;
            damageDealer.SetDamageData(newDamageData);
            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Damage data: base {newDamageData.baseDamage}");
        }

        private bool ShowDebugLog => showDebugLog || (playerCombat != null && playerCombat.ShowDebugLog);
        
        private bool IsInAttackClip()
        {
            return Helper.IsInAttackState(_animator);
        }

        #endregion
    }
}
