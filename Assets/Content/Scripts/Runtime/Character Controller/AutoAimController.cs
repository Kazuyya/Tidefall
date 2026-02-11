using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LittleHeroJourney
{
    public class AutoAimController : MonoBehaviour
    {
        #region Fields

        [Header("Behavior")]
        [SerializeField] private bool enableAutoAim = true;
        [SerializeField] private float rotationSpeed = 720f;
        
        [Header("Detection Range")]
        [SerializeField] private float detectionDistance = 20f;
        [SerializeField] private float minDetectionDistance = 2f;
        [SerializeField] private LayerMask detectionLayers = ~0;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private float minDistanceThreshold = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;
        [SerializeField] private bool showDebugGizmos = false;

        // Core Components
        private PlayerMovementController _movementController;
        private PlayerCombat _playerCombat;
        private Transform _playerTransform;
        private TargetLockCameraController _targetLockController;

        // Auto Aim State
        private bool _isAutoAiming = false;
        private Transform _currentAutoAimTarget;
        private Transform _currentLockedTarget;  // Track locked target separately to maintain priority
        private Transform _stickyAimTarget;      // Stick to target to prevent flickering (only changes if invalid or out of range)

        #endregion

        #region Initialization

        private void Awake()
        {
            InitializeComponents();
            TargetLockCameraController.OnLockedTargetChanged += HandleLockedTargetChanged;
        }

        private void InitializeComponents()
        {
            _movementController = GetComponent<PlayerMovementController>();
            _playerCombat = GetComponent<PlayerCombat>();
            _playerTransform = transform;
            _targetLockController = FindObjectOfType<TargetLockCameraController>();

            if (_movementController == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] PlayerMovementController not found!");
                enabled = false;
                return;
            }
        }

        #endregion

        #region Update Logic

        private void Update()
        {
            if (_targetLockController == null)
            {
                _targetLockController = FindObjectOfType<TargetLockCameraController>();
            }
            
            UpdateLockedTarget();
            FindOrUpdateAutoAimTarget();
            UpdateAutoAimStateAndInput();
        }

        private void LateUpdate()
        {
            // Apply movement override in LateUpdate to ensure it runs AFTER player input is processed
            ApplyAutoAimMovement();
        }

        private void OnDisable()
        {
            TargetLockCameraController.OnLockedTargetChanged -= HandleLockedTargetChanged;
        }

        private void OnDestroy()
        {
            TargetLockCameraController.OnLockedTargetChanged -= HandleLockedTargetChanged;
        }

        /// <summary>
        /// Update locked target from camera controller
        /// </summary>
        private void UpdateLockedTarget()
        {
            if (_targetLockController == null || !_targetLockController.IsTargeting)
            {
                _currentLockedTarget = null;
                return;
            }

            _currentLockedTarget = _targetLockController.CurrentTarget;
        }

        /// <summary>
        /// Find or maintain current auto aim target with persistence
        /// Priority 1: Locked target (absolute, never switch)
        /// Priority 2: Maintain sticky target (don't switch unless invalid/out of range)
        /// Priority 3: Find nearest enemy (fallback)
        /// </summary>
        private void FindOrUpdateAutoAimTarget()
        {
            // Priority 1: If locked target exists and valid, ALWAYS use it (never switch)
            if (_currentLockedTarget != null && IsValidTarget(_currentLockedTarget))
            {
                _currentAutoAimTarget = _currentLockedTarget;
                _stickyAimTarget = _currentLockedTarget;
                return;
            }

            // Priority 2: If using sticky target, check if still valid
            if (_stickyAimTarget != null && IsValidTarget(_stickyAimTarget))
            {
                _currentAutoAimTarget = _stickyAimTarget;
                return;  // Keep using current sticky target (prevents flickering!)
            }

            // Priority 3: Sticky target is invalid, find new nearest enemy
            Transform nearestEnemy = FindNearestValidEnemy();
            if (nearestEnemy != null)
            {
                _stickyAimTarget = nearestEnemy;  // Set as new sticky target
                _currentAutoAimTarget = nearestEnemy;
            }
            else
            {
                // No valid target found
                _stickyAimTarget = null;
                _currentAutoAimTarget = null;
            }
        }

        /// <summary>
        /// Update auto aim state - active when attacking AND have valid target
        /// Rotation is applied automatically during active state
        /// Attack data will handle movement disable in its own window
        /// </summary>
        private void UpdateAutoAimStateAndInput()
        {
            if (!enableAutoAim || _playerCombat == null)
            {
                _isAutoAiming = false;
                if (_movementController != null)
                {
                    _movementController.SetForcedRotationTarget(null);
                }
                return;
            }

            // Auto aim active when attacking AND have valid target
            bool isAttacking = _playerCombat.IsAttacking;
            bool hasValidTarget = _currentAutoAimTarget != null;
            _isAutoAiming = isAttacking && hasValidTarget;

            if (_movementController != null)
            {
                // Apply forced rotation during auto aim (smooth tracking)
                _movementController.SetForcedRotationTarget(_isAutoAiming ? _currentAutoAimTarget : null, rotationSpeed, 2f);
            }

            if (showDebugLog)
            {
                if (_isAutoAiming)
                    Debug.Log($"[aim] Auto aiming at: {_currentAutoAimTarget.name}");
                else if (isAttacking && !hasValidTarget)
                    Debug.Log($"[aim] Attacking but no valid target in area");
            }
        }

        #endregion

        #region Enemy Detection

        private Transform FindNearestValidEnemy()
        {
            if (_playerTransform == null) return null;
            Transform nearestEnemy = null;
            float nearestDistance = detectionDistance;
            Vector3 playerPosition = _playerTransform.position;

            Collider[] hits = Physics.OverlapSphere(playerPosition, detectionDistance, detectionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null) continue;
                Health enemyHealth = c.GetComponentInParent<Health>();
                if (enemyHealth == null || !enemyHealth.gameObject.activeInHierarchy) continue;
                if (enemyHealth.IsDead) continue;
                if (enemyHealth.transform == _playerTransform) continue;

                Transform enemyTransform = enemyHealth.transform;
                Vector3 enemyPosition = enemyTransform.position;
                float distance = Vector3.Distance(playerPosition, enemyPosition);
                if (distance < minDetectionDistance) continue;

                if (requireLineOfSight && !HasLineOfSight(playerPosition, enemyPosition)) continue;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemyTransform;
                }
            }
            return nearestEnemy;
        }

        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);

            // Raycast to check for obstacles
            if (Physics.Raycast(from, direction, out RaycastHit hit, distance, ~LayerMask.GetMask("Player", "Enemy")))
            {
                // Check if the hit object is on the path
                return false; // Obstacle in the way
            }

            return true;
        }

        #endregion

        #region Public Methods

        public void StopAutoAim()
        {
            _isAutoAiming = false;
            _currentAutoAimTarget = null;
            _stickyAimTarget = null;

            if (_movementController != null)
            {
                _movementController.SetForcedRotationTarget(null);
            }
            if (showDebugLog)
            {
                Debug.Log("[aim] StopAutoAim");
            }
        }

        /// <summary>
        /// Check if a transform is a valid auto aim target
        /// </summary>
        public bool IsValidTarget(Transform target)
        {
            if (target == null || _playerTransform == null) return false;

            if (!Helper.IsValidTarget(target)) return false;

            float distance = Vector3.Distance(_playerTransform.position, target.position);
            if (distance > detectionDistance || distance < minDistanceThreshold)
                return false;

            return true;
        }

        /// <summary>
        /// Get current auto aim target
        /// </summary>
        public Transform CurrentAutoAimTarget => _currentAutoAimTarget;

        /// <summary>
        /// Check if currently auto aiming
        /// </summary>
        public bool IsAutoAiming => _isAutoAiming;

        #endregion

        private void ApplyAutoAimMovement()
        {
            if (!_isAutoAiming || _currentAutoAimTarget == null || _movementController == null)
                return;

            Vector3 targetPosition = _currentAutoAimTarget.position;
            ILockOnTarget lockOnPoint = _currentAutoAimTarget.GetComponent<ILockOnTarget>();
            if (lockOnPoint != null)
            {
                Transform lockOnTransform = lockOnPoint.GetLockOnTransform();
                if (lockOnTransform != null)
                    targetPosition = lockOnTransform.position;
            }

            Vector3 directionToTarget = (targetPosition - _playerTransform.position).normalized;
            directionToTarget.y = 0f;
            if (directionToTarget != Vector3.zero)
            {
                _movementController.MoveInputVector = directionToTarget;
            }
        }

        #region Debug Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || _playerTransform == null) return;

            // Draw detection sphere
            Gizmos.color = _isAutoAiming ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(_playerTransform.position, detectionDistance);

            // Draw line to current auto aim target
            if (_currentAutoAimTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_playerTransform.position, _currentAutoAimTarget.position);
                Gizmos.DrawWireSphere(_currentAutoAimTarget.position, 0.5f);
            }

            // Draw line to locked target if different
            if (_currentLockedTarget != null && _currentLockedTarget != _currentAutoAimTarget)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_playerTransform.position, _currentLockedTarget.position);
                Gizmos.DrawWireSphere(_currentLockedTarget.position, 0.3f);
            }

            // Draw sticky target indicator if different from current
            if (_stickyAimTarget != null && _stickyAimTarget != _currentAutoAimTarget)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(_playerTransform.position, _stickyAimTarget.position);
            }
        }
#endif

        #endregion

        private void HandleLockedTargetChanged(Transform t)
        {
            _currentLockedTarget = t;
            
            // If lockedtarget changed, reset sticky to allow immediate switch to new locked target
            if (t != null)
            {
                _stickyAimTarget = null;  // Reset sticky so we immediately use locked target
            }
            
            if (showDebugLog)
            {
                if (t != null)
                    Debug.Log($"[aim] Locked target changed to: {t.name}");
                else
                    Debug.Log($"[aim] Lock-on released");
            }
        }
    }
}
