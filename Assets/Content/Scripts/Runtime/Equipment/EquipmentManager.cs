using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    public class EquipmentManager : MonoBehaviour
    {
        #region Fields

        [Header("Current Equipment")]
        public WeaponDataSO currentWeapon;

        [Header("Available Weapons (Player Inventory)")]
        [Tooltip("Weapons available to the player")]
        public List<WeaponDataSO> availableWeapons;

        // Private references (auto-assigned)
        private PlayerMovementController _movementController;
        private PlayerCombat _playerCombat;

        // Events
        public event System.Action<WeaponDataSO> OnWeaponEquipped;

        [Tooltip("Enable number keys for direct weapon selection (1,2,3)")]
        public bool enableNumberKeys = true;

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugLog = false;
        public bool ShowDebugLog => showDebugLog;

        // Performance optimization - cache input checks    
        private float _inputCacheTimer = 0f;
        private const float INPUT_CACHE_INTERVAL = 0.05f;

        #endregion

        #region Initialization

        private void Start()
        {
            // Get component references
            _movementController = GetComponent<PlayerMovementController>();
            _playerCombat = GetComponent<PlayerCombat>();

            // Apply current weapon if set in Inspector
            if (currentWeapon != null)
            {
                // Validate that currentWeapon is in availableWeapons
                if (availableWeapons.Contains(currentWeapon))
                {
                    EquipWeapon(currentWeapon);
                }
                else
                {
                    if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Current weapon '{currentWeapon.weaponName}' not found in available weapons! Adding to list.");
                    availableWeapons.Add(currentWeapon);
                    EquipWeapon(currentWeapon);
                }
            }
        }

        private void Update()
        {
            if (!enabled) return;

            _inputCacheTimer += Time.deltaTime;
            if (_inputCacheTimer >= INPUT_CACHE_INTERVAL)
            {
                _inputCacheTimer = 0f;

                if (enableNumberKeys)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1) && availableWeapons.Count >= 1)
                    {
                        EquipWeapon(availableWeapons[0]);
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha2) && availableWeapons.Count >= 2)
                    {
                        EquipWeapon(availableWeapons[1]);
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha3) && availableWeapons.Count >= 3)
                    {
                        EquipWeapon(availableWeapons[2]);
                    }
                }
            }
        }

        #endregion

        #region Equipment Management

        public void EquipWeapon(WeaponDataSO weaponData)
        {
            if (weaponData == null) return;

            currentWeapon = weaponData;

            if (_movementController != null && weaponData.animatorOverride != null)
            {
                _movementController.PlayerAnimator.runtimeAnimatorController = weaponData.animatorOverride;
            }
            else
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Cannot apply animator override - missing references or animatorOverride");
            }

            if (_movementController != null && weaponData.customMovementSettings != null)
            {
                _movementController.movementSettings = weaponData.customMovementSettings;
            }

            if (_movementController != null && weaponData.customDashSettings != null)
            {
                _movementController.dashSettings = weaponData.customDashSettings;
            }

            // Update PlayerCombat with new combat profile and weapon
            if (_playerCombat != null)
            {
                _playerCombat.currentCombatProfile = weaponData.combatProfile;

                // Find and set the corresponding Weapon component by weapon name
                PlayerCombat.WeaponEntry weaponEntry = _playerCombat.availableWeapons.Find(w =>
                    w != null && w.weaponName == weaponData.weaponName);

                if (weaponEntry != null && weaponEntry.weaponComponent != null)
                {
                    _playerCombat.currentWeapon = weaponEntry.weaponComponent;
                }
                else
                {
                    if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Weapon '{weaponData.weaponName}' not found in available weapons!");
                }

            }

            OnWeaponEquipped?.Invoke(weaponData);

        }

        public void EquipNextWeapon()
        {
            if (availableWeapons.Count == 0) return;

            int currentIndex = availableWeapons.IndexOf(currentWeapon);
            int nextIndex = (currentIndex + 1) % availableWeapons.Count;

            EquipWeapon(availableWeapons[nextIndex]);
        }

        public void EquipPreviousWeapon()
        {
            if (availableWeapons.Count == 0) return;

            int currentIndex = availableWeapons.IndexOf(currentWeapon);
            int prevIndex = currentIndex - 1;
            if (prevIndex < 0) prevIndex = availableWeapons.Count - 1;

            EquipWeapon(availableWeapons[prevIndex]);
        }

        public void EquipWeaponByType(WeaponType weaponType)
        {
            WeaponDataSO weapon = availableWeapons.Find(w => w.weaponType == weaponType);
            if (weapon != null)
            {
                EquipWeapon(weapon);
            }
        }

        #endregion
    }
}
