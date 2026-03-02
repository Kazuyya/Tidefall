using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KinematicCharacterController;
using UnityEngine.InputSystem;
using Terresquall;
using LittleHeroJourney.InputSystem;

// Suppress obsolete warnings from KinematicCharacterController plugin
#pragma warning disable CS0618

namespace LittleHeroJourney
{
    public enum PlayerState
    {
        Default,
        Dashing,
    }

    public enum OrientationMethod
    {
        TowardsCamera,
        TowardsMovement,
    }

    /// <summary>
    /// Tracks which system is currently locking player movement (if any)
    /// Prevents race conditions between Knockback, DamageFeedback, Stun, etc
    /// </summary>
    public enum MovementLockReason
    {
        None,              // Movement is free
        Knockback,         // Currently being knocked back
        DamageFeedback,    // Currently playing damage feedback animation
        Stun,              // Currently stunned
    }

    public struct PlayerMovementInputs
    {
        public Vector2 MoveAxis;
        public Vector2 LookAxis;
        public Quaternion CameraRotation;
        public bool DashDown;
    }

    public class PlayerMovementController : MonoBehaviour, ICharacterController, IKnockbackable
    {
        #region Fields

        [Header("Movement Settings")]
        public MovementSettingsSO movementSettings;

        [Header("Dash Settings")]
        public DashSettingsSO dashSettings;

        [Header("Input Settings")]
        public InputActionAsset inputActionAsset;
        public Camera playerCamera;
        public int virtualJoystickId;

        // Core Components
        private KinematicCharacterMotor _motor;
        private PlayerCombat _playerCombat;
        private Health _health;
        private AutoAimController _autoAimController;
        private EyeframeManager _eyeframeManager;
        private LevelableStats _levelable;

        /// <summary>Move speed from LevelableStats if present and &gt; 0, else from MovementSettings.</summary>
        public float EffectiveMaxStableMoveSpeed => _levelable != null && _levelable.MoveSpeed > 0f ? _levelable.MoveSpeed : (movementSettings != null ? movementSettings.MaxStableMoveSpeed : 0f);
        /// <summary>Air move speed from LevelableStats if present and &gt; 0, else from MovementSettings.</summary>
        public float EffectiveMaxAirMoveSpeed => _levelable != null && _levelable.MoveSpeed > 0f ? _levelable.MoveSpeed : (movementSettings != null ? movementSettings.MaxAirMoveSpeed : 0f);

        // Cached values for performance
        private bool _hasValidAnimatorSetup = false;
        private int _cachedSpeedParameterHash;
        private Animator _playerAnimator;

        // Movement Vectors
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private Vector3 _internalVelocityAdd = Vector3.zero;

        // State Management
        public PlayerState CurrentPlayerState { get; private set; }
        private PlayerMovementState _currentState;
        private DefaultMovementState _defaultState;
        private DashingMovementState _dashingState;

        // Combat State - Single source of truth for movement locks
        private MovementLockReason _movementLockReason = MovementLockReason.None;
        public bool IsMovementDisabled => _movementLockReason != MovementLockReason.None || CurrentPlayerState == PlayerState.Dashing;

        private bool _ignorePlayerMovementInput = false;
        public bool IsPlayerMovementInputIgnored => _ignorePlayerMovementInput;

        // Encounter Zone Reference - Set when player enters encounter zone
        private EncounterZone _currentEncounterZone;
        public EncounterZone CurrentEncounterZone => _currentEncounterZone;
        
        [Header("Encounter Settings")]
        [SerializeField] private float boundaryClampOffset = 0.5f;
        [SerializeField] private bool showEncounterBoundaryLog = false;
        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        // Dash Properties
        private bool _dashRequested = false;
        private float _dashCooldownTimer = 0f;
        private Vector3 _dashDirection;
        private Vector3 _dashVelocity;
        private float _dashTimeRemaining = 0f;
        private float _dashDistanceRemaining = 0f;
        private Vector3 _dashStartPosition;
        private float _dashMaxDistance;
        private Vector3 _dashTargetPosition;
        private float _currentDashSpeed = 0f;

        // Input Actions
        private InputAction moveAction;
        private InputAction dashAction;

        // Knockback
        private Coroutine _knockbackCoroutine;
        private bool _isKnockedBack = false;

        // Forced Rotation (for damage feedback)
        private Transform _forcedRotationTarget;
        private bool _isForcedRotating = false;
        private float _forcedRotationSpeed = 720f;
        private float _forcedRotationThreshold = 5f;

        [Header("Debug")]
        [SerializeField] private bool showKnockbackLog;

        #endregion

        #region Properties

        public KinematicCharacterMotor Motor => _motor;
        public Animator PlayerAnimator { get => _playerAnimator; set => _playerAnimator = value; }
        public Vector3 MoveInputVector { get => _moveInputVector; set => _moveInputVector = value; }
        public Vector3 LookInputVector { get => _lookInputVector; set => _lookInputVector = value; }
        public Vector3 InternalVelocityAdd { get => _internalVelocityAdd; set => _internalVelocityAdd = value; }
        public bool IsKnockedBack => _isKnockedBack;

        // Dash Properties
        public bool DashRequested { get => _dashRequested; set => _dashRequested = value; }
        public float DashCooldownTimer { get => _dashCooldownTimer; set => _dashCooldownTimer = value; }

        public bool IsDashing => CurrentPlayerState == PlayerState.Dashing;
        public AutoAimController AutoAimController => _autoAimController;
        public EyeframeManager EyeframeManager => _eyeframeManager;
        public Vector3 DashDirection { get => _dashDirection; set => _dashDirection = value; }
        public Vector3 DashVelocity { get => _dashVelocity; set => _dashVelocity = value; }
        public float DashTimeRemaining { get => _dashTimeRemaining; set => _dashTimeRemaining = value; }
        public float DashDistanceRemaining { get => _dashDistanceRemaining; set => _dashDistanceRemaining = value; }
        public Vector3 DashStartPosition { get => _dashStartPosition; set => _dashStartPosition = value; }
        public float DashMaxDistance { get => _dashMaxDistance; set => _dashMaxDistance = value; }
        public Vector3 DashTargetPosition { get => _dashTargetPosition; set => _dashTargetPosition = value; }
        public float CurrentDashSpeed { get => _currentDashSpeed; set => _currentDashSpeed = value; }

        #endregion

        #region Initialization

        private void OnEnable()
        {
            GameInputEvents.OnDash += TriggerDash;
        }

        private void OnDisable()
        {
            GameInputEvents.OnDash -= TriggerDash;
        }

        private void Awake()
        {
            // Validate settings
            if (movementSettings == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] MovementSettingsSO is not assigned!");
                return;
            }

            if (dashSettings == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] DashSettingsSO is not assigned!");
                return;
            }


            _motor = GetComponent<KinematicCharacterMotor>();
            if (_motor == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] KinematicCharacterMotor component not found on the same GameObject!");
                return;
            }

            _playerCombat = GetComponent<PlayerCombat>();
            _health = GetComponent<Health>();
            _autoAimController = GetComponent<AutoAimController>();
            _eyeframeManager = GetComponent<EyeframeManager>();
            _levelable = GetComponent<LevelableStats>();

            _playerAnimator = GetComponentInChildren<Animator>();
            if (_playerAnimator == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Animator component not found on the same GameObject!");
                return;
            }

            // Initialize state objects
            _defaultState = new DefaultMovementState(this);
            _dashingState = new DashingMovementState(this);

            TransitionToState(PlayerState.Default);
            Motor.CharacterController = this;
            SetupInputActions();
            SetupAnimator();

            if (_health != null)
            {
                _health.OnDeath += HandleDeath;
            }
        }

        private void OnDestroy()
        {
            if (moveAction != null) moveAction.Disable();
            if (dashAction != null) dashAction.Disable();
            if (inputActionAsset != null) inputActionAsset.Disable();

            if (_health != null)
            {
                _health.OnDeath -= HandleDeath;
            }
        }

        private void SetupInputActions()
        {
            if (inputActionAsset == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] InputActionAsset is not assigned!");
                return;
            }

            try
            {
                moveAction = inputActionAsset.FindAction("Move");
                dashAction = inputActionAsset.FindAction("Dash");

                if (moveAction != null) moveAction.Enable();
                if (dashAction != null) dashAction.Enable();

                inputActionAsset.Enable();
            }
            catch (System.Exception e)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Failed to setup input actions: {e.Message}");
            }
        }

        private void SetupAnimator()
        {
            if (_playerAnimator == null)
            {
                _playerAnimator = GetComponentInChildren<Animator>();
            }

            Animator animator = _playerAnimator;
            ValidateAnimatorParameters(animator);
        }

        private void ValidateAnimatorParameters(Animator animator)
        {
            if (animator == null) return;

            // Validate speed parameter
            if (!string.IsNullOrEmpty(movementSettings?.speedParameterName))
            {
                bool speedParamExists = Helper.ValidateAnimatorParameter(animator,
                    movementSettings.speedParameterName, AnimatorControllerParameterType.Float);

                if (!speedParamExists && movementSettings.ShowDebugLog)
                {
                    if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Speed parameter '{movementSettings.speedParameterName}' not found in Animator!");
                }
            }

            // Validate dash parameter
            if (!string.IsNullOrEmpty(dashSettings?.dashParameterName))
            {
                bool dashParamExists = Helper.HasAnimatorParameter(animator, dashSettings.dashParameterName);

                if (!dashParamExists && movementSettings.ShowDebugLog)
                {
                    if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Dash parameter '{dashSettings.dashParameterName}' not found in Animator!");
                }
            }
        }

        #endregion

        #region State Management

        public void TransitionToState(PlayerState newState)
        {
            PlayerState tmpInitialState = CurrentPlayerState;

            // Exit current state
            if (_currentState != null)
            {
                _currentState.OnStateExit(newState);
            }

            CurrentPlayerState = newState;

            // Set new state object
            switch (newState)
            {
                case PlayerState.Default:
                    _currentState = _defaultState;
                    break;
                case PlayerState.Dashing:
                    _currentState = _dashingState;
                    break;
            }

            // Enter new state
            if (_currentState != null)
            {
                _currentState.OnStateEnter(tmpInitialState);
            }
        }

        #endregion

        #region Input Handling

        public void SetInputs(ref PlayerMovementInputs inputs)
        {
            if (_currentState != null)
            {
                _currentState.SetInputs(ref inputs);
            }
        }

        private void Update()
        {
            HandleDashInput();
            HandleMovementInput();
            HandleDashRequest();
            UpdateStateAnimation();
        }

        private void HandleMovementInput()
        {
            Vector2 moveInput = _ignorePlayerMovementInput ? Vector2.zero : GetCombinedMoveInput();

            Quaternion cameraRotation = playerCamera != null ? playerCamera.transform.rotation : Quaternion.identity;

            PlayerMovementInputs inputs = new PlayerMovementInputs
            {
                MoveAxis = moveInput,
                LookAxis = Vector2.zero,
                CameraRotation = cameraRotation,
                DashDown = false
            };

            SetInputs(ref inputs);
        }

        private void HandleDashInput()
        {
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }
        }

        private Vector2 GetCombinedMoveInput()
        {
            Vector2 joystickInput = GetJoystickInput();
            Vector2 keyboardInput = GetKeyboardInput();

            if (joystickInput.sqrMagnitude > 0.01f) return joystickInput;
            if (keyboardInput.sqrMagnitude > 0.01f) return keyboardInput;

            return Vector2.zero;
        }

        private Vector2 GetJoystickInput()
        {
            if (VirtualJoystick.CountActiveInstances() == 0) return Vector2.zero;
            VirtualJoystick joystick = VirtualJoystick.GetInstance(virtualJoystickId);
            return (joystick != null && joystick.isActiveAndEnabled) ? joystick.GetAxis() : Vector2.zero;
        }

        private Vector2 GetKeyboardInput()
        {
            if (moveAction == null) return Vector2.zero;
            return moveAction.ReadValue<Vector2>();
        }

        private void HandleDashRequest()
        {
            if (_dashRequested && CurrentPlayerState == PlayerState.Default)
            {
                if (_playerCombat != null && _playerCombat.IsAttacking)
                {
                    _playerCombat.OnInterrupted();
                }

                if (_playerAnimator != null && !string.IsNullOrEmpty(dashSettings?.dashParameterName))
                {
                    _playerAnimator.SetTrigger(dashSettings.dashParameterName);
                }
                TransitionToState(PlayerState.Dashing);
                _dashRequested = false;
            }
        }

        private void UpdateStateAnimation()
        {
            if (_currentState != null)
            {
                _currentState.UpdateAnimation(Time.deltaTime);
            }
        }

        #endregion

        #region Character Controller Interface

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (_currentState != null)
            {
                _currentState.BeforeCharacterUpdate(deltaTime);
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // If movement is disabled (disable window), don't update rotation at all
            if (IsMovementDisabled)
            {
                return;
            }

            // Priority 1: Forced rotation (damage feedback to damager)
            if (_isForcedRotating && _forcedRotationTarget != null)
            {
                currentRotation = ApplyForcedRotation(currentRotation, deltaTime);
            }
            // Priority 2: Normal state rotation
            else if (_currentState != null)
            {
                _currentState.UpdateRotation(ref currentRotation, deltaTime);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_currentState != null)
            {
                _currentState.UpdateVelocity(ref currentVelocity, deltaTime);
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Triggered via GameInputEvents.OnDash
        /// </summary>
        private void TriggerDash()
        {
            if (_health != null && !_health.IsAlive) return;

            bool isCombatInterruptible = _playerCombat != null && _playerCombat.IsAttacking && _playerCombat.IsInterruptible;
            bool isMovementAllowed = _movementLockReason == MovementLockReason.None || isCombatInterruptible;
            
            // Allow dash if cooldown is ready, not already requested, movement is allowed (or interruptible), and grounded
            if (_dashCooldownTimer <= 0f && !_dashRequested && isMovementAllowed && CurrentPlayerState == PlayerState.Default)
            {
                if (_playerCombat != null && _playerCombat.IsAttacking && !_playerCombat.IsInterruptible)
                {
                    // Combat is blocking and NOT interruptible
                    return;
                }

                if (Motor != null && Motor.GroundingStatus.IsStableOnGround)
                {
                    _dashRequested = true;
                    HandleDashRequest(); // Execute immediately to avoid 1-frame delay
                }
            }
        }

        public void AddVelocity(Vector3 velocity)
        {
            if (CurrentPlayerState == PlayerState.Default)
            {
                InternalVelocityAdd += velocity;
            }
        }

        #region IKnockbackable

        public void ApplyKnockback(Vector3 direction, float distance)
        {
            if (CurrentPlayerState != PlayerState.Default) return;
            if (_isKnockedBack && _knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);

            Vector3 startPos = transform.position;
            _knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction, distance, startPos));
        }

        private IEnumerator KnockbackRoutine(Vector3 direction, float distance, Vector3 startPosition)
        {
            _isKnockedBack = true;
            const float duration = 0.3f;
            Helper.GetKnockbackTarget(startPosition, direction, distance, out Vector3 targetPosition, out bool hitObstacle);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float curve = Helper.EaseOutCubic(elapsed / duration);
                Motor.SetPosition(Vector3.Lerp(startPosition, targetPosition, curve));
                yield return null;
            }

            Motor.SetPosition(targetPosition);
            
            // Validate and clamp position to stay within encounter zone if active
            if (_currentEncounterZone != null)
            {
                _currentEncounterZone.ValidateAndClampPosition(transform, boundaryClampOffset);
            }
            
            _isKnockedBack = false;
            _knockbackCoroutine = null;

            if (showKnockbackLog)
            {
                float actual = Vector3.Distance(startPosition, targetPosition);
                string extra = hitObstacle ? " (obstacle)" : "";
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Knockback{extra}: {startPosition} -> {targetPosition}, {actual:F2}m");
            }
        }

        #endregion

        #region Encounter Zone Reference

        /// <summary>
        /// Set the encounter zone reference when player enters an encounter
        /// Called by EncounterZone.OnTriggerEnter
        /// </summary>
        public void SetEncounterZone(EncounterZone encounterZone)
        {
            _currentEncounterZone = encounterZone;
            if (showEncounterBoundaryLog)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter zone reference set: {(encounterZone != null ? encounterZone.gameObject.name : "null")}");
            }
        }

        /// <summary>
        /// Clear the encounter zone reference when encounter ends or player leaves
        /// </summary>
        public void ClearEncounterZone()
        {
            if (showEncounterBoundaryLog && _currentEncounterZone != null)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter zone reference cleared");
            }
            _currentEncounterZone = null;
        }

        #endregion

        public void UpdateAnimationSpeed(float normalizedSpeed)
        {
            if (!_hasValidAnimatorSetup)
            {
                if (_playerAnimator != null && movementSettings != null && !string.IsNullOrEmpty(movementSettings.speedParameterName))
                {
                    _hasValidAnimatorSetup = true;
                    _cachedSpeedParameterHash = Animator.StringToHash(movementSettings.speedParameterName);
                }
                else
                {
                    return;
                }
            }

            _playerAnimator.SetFloat(_cachedSpeedParameterHash, normalizedSpeed);
        }

        public void SetIgnorePlayerMovementInput(bool ignore)
        {
            _ignorePlayerMovementInput = ignore;
        }

        public void SetMovementDisabled(bool disabled)
        {
            _movementLockReason = disabled ? MovementLockReason.DamageFeedback : MovementLockReason.None;
        }

        public void SetMovementLockReason(MovementLockReason reason)
        {
            _movementLockReason = reason;
        }

        public void SetDashingDisabled(bool disabled)
        {
            if (disabled)
            {
                _movementLockReason = MovementLockReason.DamageFeedback;
                if (CurrentPlayerState == PlayerState.Dashing)
                {
                    TransitionToState(PlayerState.Default);
                }
            }
            else
            {
                // Only clear if we set it
                if (_movementLockReason == MovementLockReason.DamageFeedback)
                {
                    _movementLockReason = MovementLockReason.None;
                }
            }
        }

        public void KeepPlayerGroundedDuringDash()
        {
            MovementHelper.KeepPlayerGroundedDuringDash(this);
        }

        public Vector3 CalculateGroundFollowingTarget(Vector3 startPosition, Vector3 dashDirection, float maxDistance)
        {
            return MovementHelper.CalculateGroundFollowingTarget(startPosition, dashDirection, maxDistance, Motor);
        }

        public float CheckDashObstacle(Vector3 dashDirection, float maxDistance)
        {
            return MovementHelper.CheckDashObstacle(dashDirection, maxDistance, Motor, movementSettings.ShowDebugLog, GetType().Name);
        }

        private void HandleDeath()
        {
            SetMovementDisabled(true);
            SetDashingDisabled(true);

            if (_motor != null)
            {
                _motor.enabled = false;
            }

            if (_playerCombat != null)
            {
                _playerCombat.SetAttackDisabled(true);
            }

            if (GameplayManager.Instance != null)
            {
                GameplayManager.Instance.TriggerGameOver();
            }

            if (movementSettings.ShowDebugLog)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Player died - all controls disabled");
            }
        }

        #endregion

        #region Debug Gizmos


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!movementSettings.ShowDebugGizmos || Motor == null) return;

            DrawPlayerDirectionGizmo();
            DrawMovementInputGizmo();
            DrawDashPathGizmo();
        }

        private void DrawPlayerDirectionGizmo()
        {
            Vector3 currentPos = transform.position;

            // Player forward direction (transparan)
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f); // Semi-transparent green
            Gizmos.DrawLine(currentPos, currentPos + Motor.CharacterForward * 2f);
            Gizmos.DrawSphere(currentPos + Motor.CharacterForward * 2f, 0.08f);

            // Player right direction
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Semi-transparent blue
            Gizmos.DrawLine(currentPos, currentPos + Motor.CharacterRight * 1.5f);
            Gizmos.DrawSphere(currentPos + Motor.CharacterRight * 1.5f, 0.06f);

            // Player up direction
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
            Gizmos.DrawLine(currentPos, currentPos + Motor.CharacterUp * 1.2f);
            Gizmos.DrawSphere(currentPos + Motor.CharacterUp * 1.2f, 0.05f);
        }

        private void DrawMovementInputGizmo()
        {
            if (_moveInputVector.sqrMagnitude > 0.01f)
            {
                Vector3 currentPos = transform.position;
                Vector3 inputDirection = _moveInputVector.normalized;

                // Movement input direction (cyan, more visible)
                Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
                Gizmos.DrawLine(currentPos, currentPos + inputDirection * 1.8f);
                Gizmos.DrawWireCube(currentPos + inputDirection * 1.8f, Vector3.one * 0.1f);
            }
        }

        private void DrawDashPathGizmo()
        {
            if (!dashSettings.ShowDebugGizmos) return;

            if (CurrentPlayerState == PlayerState.Dashing && _dashTargetPosition != Vector3.zero)
            {
                Vector3 dashDirection = (_dashTargetPosition - _dashStartPosition).normalized;
                float dashDistance = Vector3.Distance(_dashStartPosition, _dashTargetPosition);

                const int gizmoSegments = 12;
                Vector3 previousPoint = _dashStartPosition;

                // Dash path (semi-transparent red)
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                for (int i = 1; i <= gizmoSegments; i++)
                {
                    float t = (float)i / gizmoSegments;
                    Vector3 samplePoint = _dashStartPosition + (dashDirection * dashDistance * t);

                    RaycastHit groundHit;
                    Vector3 rayStart = samplePoint + (Motor.CharacterUp * 2f);

                    if (Physics.Raycast(rayStart, -Motor.CharacterUp, out groundHit, 5f, Motor.CollidableLayers, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 groundPoint = new Vector3(samplePoint.x, groundHit.point.y, samplePoint.z);
                        Gizmos.DrawLine(previousPoint, groundPoint);
                        Gizmos.DrawCube(groundPoint, Vector3.one * 0.05f);
                        previousPoint = groundPoint;
                    }
                    else
                    {
                        Gizmos.DrawLine(previousPoint, samplePoint);
                        Gizmos.DrawWireSphere(samplePoint, 0.03f);
                        previousPoint = samplePoint;
                    }
                }

                // Dash target (more visible)
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
                Gizmos.DrawWireSphere(_dashTargetPosition, 0.25f);
                Gizmos.DrawSphere(_dashTargetPosition, 0.15f);

                // Current dash position (yellow)
                Vector3 currentPos = Motor.TransientPosition;
                Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
                Gizmos.DrawSphere(currentPos, 0.12f);
                Gizmos.DrawWireCube(currentPos, Vector3.one * 0.2f);
            }
        }

#endif

        /// <summary>
        /// Set forced rotation target (for damage feedback)
        /// </summary>
        public void SetForcedRotationTarget(Transform target, float rotationSpeed = 720f, float rotationThreshold = 5f)
        {
            _forcedRotationTarget = target;
            _isForcedRotating = target != null;
            _forcedRotationSpeed = rotationSpeed;
            _forcedRotationThreshold = rotationThreshold;
        }

        /// <summary>
        /// Get current forced rotation state
        /// </summary>
        public bool IsForcedRotating => _isForcedRotating;

        /// <summary>
        /// Apply forced rotation (smooth rotate to target)
        /// </summary>
        public Quaternion ApplyForcedRotation(Quaternion currentRotation, float deltaTime)
        {
            if (!_isForcedRotating || _forcedRotationTarget == null)
            {
                _isForcedRotating = false;
                return currentRotation;
            }

            Vector3 directionToTarget = (_forcedRotationTarget.position - transform.position).normalized;
            directionToTarget.y = 0f;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                float step = _forcedRotationSpeed * deltaTime;
                Quaternion newRotation = Quaternion.RotateTowards(currentRotation, targetRotation, step);

                // Stop rotating when close enough
                if (Quaternion.Angle(newRotation, targetRotation) < _forcedRotationThreshold)
                {
                    _isForcedRotating = false;
                }

                return newRotation;
            }

            return currentRotation;
        }

        #endregion
    }
}
