using UnityEngine;
using KinematicCharacterController;

namespace LittleHeroJourney
{
    public class AnimatorMatcherKCC : MonoBehaviour
    {
        #region Fields

        [Header("Settings")]
        [Tooltip("Multiplier for root motion velocity (like original AnimatorMatcher)")]
        public float rootMotionMultiplier = 3f;

        [Header("Debug")]
        public bool showDebugLog = false;

        private Animator _animator;
        private KinematicCharacterMotor _motor;
        private PlayerCombat _playerCombat;
        private PlayerMovementController _movementController;
        private Vector3 _rootMotionDelta;
        private bool _hasRootMotionThisFrame;

        private bool _wasAttackingLastFrame = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnAnimatorMove()
        {
            if (_animator == null || _motor == null)
                return;

            bool shouldApplyRootMotion = ShouldApplyRootMotion();

            if (!shouldApplyRootMotion)
            {
                ResetChildPosition();
                return;
            }

            ProcessRootMotion();
        }

        #endregion

        #region Helper Methods

        private void InitializeComponents()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Animator component not found on {gameObject.name}!");
                enabled = false;
                return;
            }

            _motor = GetComponentInParent<KinematicCharacterMotor>();
            if (_motor == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] KinematicCharacterMotor not found on parent!");
                enabled = false;
                return;
            }

            _playerCombat = GetComponentInParent<PlayerCombat>();
            if (_playerCombat == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] PlayerCombat not found. Root motion will always be applied.");
            }

            _movementController = GetComponentInParent<PlayerMovementController>();
            if (_movementController == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] PlayerMovementController not found. Root motion velocity approach disabled.");
            }

            if (showDebugLog)
            {
                Debug.Log($"[{GetType().Name}] Initialized successfully");
            }
        }

        private void ProcessRootMotion()
        {
            Vector3 deltaPosition = _animator.deltaPosition;
            deltaPosition.y = 0f;

            _rootMotionDelta = deltaPosition;
            _hasRootMotionThisFrame = deltaPosition.sqrMagnitude > 0.0001f;

            if (_hasRootMotionThisFrame)
            {
                ApplyRootMotion(deltaPosition);
                ResetChildPosition();
            }
            else
            {
                ResetChildPosition();
            }
        }

        private void ApplyRootMotion(Vector3 deltaPosition)
        {
            if (_movementController != null)
            {
                Vector3 rootMotionVelocity = (deltaPosition * rootMotionMultiplier) / Time.fixedDeltaTime;
                _movementController.AddVelocity(rootMotionVelocity);

                if (showDebugLog && deltaPosition.sqrMagnitude > 0.01f)
                {
                    Debug.Log($"[{GetType().Name}] Root motion applied");
                }
            }
            else
            {
                _motor.MoveCharacter(transform.parent.position + deltaPosition);

            }
        }

        private bool ShouldApplyRootMotion()
        {
            if (_playerCombat != null)
            {
                _wasAttackingLastFrame = _playerCombat.IsAttacking;
                return _wasAttackingLastFrame;
            }

            return true;
        }

        private void ResetChildPosition()
        {
            transform.localPosition = Vector3.zero;
        }

        #endregion
    }
}
