using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace LittleHeroJourney
{
    public class TargetLockCameraController : MonoBehaviour
    {
        public static System.Action<Transform> OnLockedTargetChanged;
        #region Inspector Settings
        [Header("UI")]
        [SerializeField] private Image aimIcon;

        [Header("Settings")]
        [Tooltip("Layer name for enemies (used for Physics detection)")]
        [SerializeField] private string enemyLayerName = "Enemy";
        [Tooltip("Max distance to detect enemies (uses Physics.OverlapSphere)")]
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private Vector2 targetLockOffset;
        [SerializeField] private float maxAngle = 90f;
        
        [Header("Lock Behavior")]
        [SerializeField] private bool enableLockOn = true;
        [SerializeField] private float centerDeadZone = 0.02f;
        [SerializeField] private float lockGain = 2.5f;
        [SerializeField] private float playerSearchInterval = 0.5f;
        [SerializeField] private float searchGracePeriod = 5f;
        
        [Header("Vertical Rig")]
        [SerializeField] private float lowRigY = 0.25f;
        [SerializeField] private float midRigY = 0.5f;
        [SerializeField] private float highRigY = 0.75f;
        [SerializeField] private float verticalSmoothTime = 0.2f;
        [SerializeField] private float rigHysteresis = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;
        [SerializeField] private bool showDebugGizmo = false;
        #endregion

        #region Private Variables
        private bool isTargeting;
        private Transform currentTarget;
        private Camera _mainCamera;
        private CinemachineFreeLook _freeLookCamera;
        private enum VerticalRigState { Low, Mid, High }
        private VerticalRigState _rigState = VerticalRigState.Mid;
        private float _verticalVelocity = 0f;

        /// <summary>
        /// Get the currently locked target transform
        /// </summary>
        public Transform CurrentTarget => currentTarget;

        /// <summary>
        /// Check if currently targeting an enemy
        /// </summary>
        public bool IsTargeting => isTargeting;
        public float MaxDistance => maxDistance;
        public float MinDistance => minDistance;
        public float MaxAngle => maxAngle;
        public List<Health> CachedEnemies => _cachedEnemies;
        public float CacheDuration => CACHE_DURATION;
        private float mouseX;
        private float mouseY;
        private Transform _playerTransform;
        private PlayerMovementController _playerMovement;
        private DM.FreeLookCameraControl _freeLookCameraControl;
        private float _lastPlayerSearchTime = 0f;
        private float _lastCameraSearchTime = 0f;
        private float _sceneLoadTime = 0f;
        private GameplayManager _gameplayManager;
        private List<Health> _cachedEnemies = new List<Health>();
        private const float CACHE_DURATION = 0.5f;
        private float _cacheTime = 0f;
        private Health _currentTargetHealth;
        private bool _targetHealthCached = false;
        private ILockOnTarget _currentTargetLockOnPoint;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            InitializeComponents();
            SetupFreeLookCamera();
        }

        void OnEnable()
        {
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
            GameplayManager.OnCameraInitialized += HandleCameraInitialized;
            GameplayManager.OnPlayerInitialized += HandlePlayerInitialized;
            GameManager.OnGamePaused += HandleGamePaused;
            GameManager.OnGameResumed += HandleGameResumed;
            GameManager.OnGameOver += HandleGameOver;
            GameManager.OnGameWin += HandleGameWin;
            _gameplayManager = GameplayManager.Instance;
            if (_gameplayManager != null)
            {
                _gameplayManager.OnReady += HandleGameplayReady;
                if (_gameplayManager.CurrentCamera != null) HandleCameraInitialized(_gameplayManager.CurrentCamera);
                if (_gameplayManager.PlayerController != null) HandlePlayerInitialized(_gameplayManager.PlayerController);
            }
        }

        void OnDisable()
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
            GameplayManager.OnCameraInitialized -= HandleCameraInitialized;
            GameplayManager.OnPlayerInitialized -= HandlePlayerInitialized;
            GameManager.OnGamePaused -= HandleGamePaused;
            GameManager.OnGameResumed -= HandleGameResumed;
            GameManager.OnGameOver -= HandleGameOver;
            GameManager.OnGameWin -= HandleGameWin;
            if (_gameplayManager != null) _gameplayManager.OnReady -= HandleGameplayReady;
        }

        private void SetupFreeLookCamera()
        {
            _freeLookCamera = null;
            if (GameplayManager.Instance != null)
            {
                _freeLookCamera = GameplayManager.Instance.CurrentCamera;
            }
            else
            {
                _freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
            }

            if (_freeLookCamera != null)
            {
                _freeLookCamera.m_XAxis.m_InputAxisName = "";
                _freeLookCamera.m_YAxis.m_InputAxisName = "";
                // Do not force initial Y to prevent startup 'snap'
            }
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                if (isTargeting) UnlockTarget();
                return;
            }
            UpdateEnemyCache(); // Always update cache for AutoAim fallback
            UpdateCameraInput();
            UpdateVerticalRig();
            UpdateUI();
            UpdatePlayerFacing();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            if (GameplayManager.Instance != null)
            {
                _mainCamera = GameplayManager.Instance.MainCamera;
            }
            else
            {
                _mainCamera = Camera.main;
            }

            // Get freeLookCamera dari GameplayManager
            CinemachineFreeLook freeLookCamera = null;
            if (GameplayManager.Instance != null)
            {
                freeLookCamera = GameplayManager.Instance.CurrentCamera;
            }
            else
            {
                freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
            }

            _freeLookCameraControl = FindObjectOfType<DM.FreeLookCameraControl>();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerMovement = player.GetComponent<PlayerMovementController>();
            }
            else
            {
                PlayerMovementController playerController = FindObjectOfType<PlayerMovementController>();
                if (playerController != null)
                {
                    _playerTransform = playerController.transform;
                    _playerMovement = playerController;
                }
                else
                {
                    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.GetComponent<Health>() != null && obj.GetComponent<Animator>() != null && obj.activeInHierarchy)
                        {
                            _playerTransform = obj.transform;
                            _playerMovement = obj.GetComponent<PlayerMovementController>();
                            break;
                        }
                    }
                }
            }

            if (showDebugLog)
            {
                Debug.Log($"{GetType().Name}: Initialized - Player: {_playerTransform != null}, Camera: {_mainCamera != null}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _lastPlayerSearchTime = 0f;
            _lastCameraSearchTime = 0f;
            _sceneLoadTime = Time.time;
            isTargeting = false;
            currentTarget = null;
            mouseX = 0f;
            mouseY = 0f;
            if (aimIcon) aimIcon.enabled = false;

            InitializeComponents();
            SetupFreeLookCamera();
        }

        private void HandleGameplayReady()
        {
            InitializeComponents();
            SetupFreeLookCamera();
        }

        private void HandleCameraInitialized(CinemachineFreeLook cam)
        {
            _freeLookCamera = cam;
            if (_freeLookCamera != null)
            {
                _freeLookCamera.m_XAxis.m_InputAxisName = "";
                _freeLookCamera.m_YAxis.m_InputAxisName = "";
            }
        }

        private void HandlePlayerInitialized(PlayerMovementController player)
        {
            _playerMovement = player;
            _playerTransform = player != null ? player.transform : null;
        }
        #endregion

        #region Input Handling
        private void AssignTarget()
        {
            EnsurePlayerReference(true);
            EnsureCameraReferences(true);

            if (isTargeting)
            {
                isTargeting = false;
                currentTarget = null;

                // CRITICAL FIX: Reset camera input when unlocking
                mouseX = 0f;
                mouseY = 0f;

                ResetPlayerOrientation();
                DisableExternalInput();

                if (showDebugLog) Debug.Log($"{GetType().Name}: Target unlocked - Camera input reset to 0");
            }
            else
            {
                GameObject closest = ClosestTarget();
                if (closest != null)
                {
                    currentTarget = closest.transform;
                    isTargeting = true;

                    _currentTargetHealth = currentTarget.GetComponentInParent<Health>();
                    _targetHealthCached = _currentTargetHealth != null;
                    if (_currentTargetHealth == null)
                    {
                        UnlockTarget();
                        if (showDebugLog) Debug.LogWarning($"{GetType().Name}: Target has no Health, auto unlocking");
                        return;
                    }

                    EnableExternalInput();

                    if (showDebugLog) Debug.Log($"{GetType().Name}: Locked to {closest.name}");
                }
                else if (showDebugLog)
                {
                    Debug.Log($"{GetType().Name}: No target found");
                }
            }
        }

        private void ResetPlayerOrientation()
        {
            if (_playerTransform != null && _mainCamera != null)
            {
                _playerTransform.rotation = Quaternion.Euler(0f, _mainCamera.transform.eulerAngles.y, 0f);
            }
        }

        private void EnableExternalInput()
        {
            if (_freeLookCameraControl != null)
            {
                _freeLookCameraControl.EnableExternalInput();
            }
        }

        private void DisableExternalInput()
        {
            if (_freeLookCameraControl != null)
            {
                _freeLookCameraControl.DisableExternalInput();
            }
        }

        private void EnsurePlayerReference(bool force = false)
        {
            if (_playerTransform != null) return;
            if (!force && (Time.time - _sceneLoadTime) > searchGracePeriod) return;
            if (Time.time - _lastPlayerSearchTime < playerSearchInterval) return;
            _lastPlayerSearchTime = Time.time;

            if (GameplayManager.Instance != null && GameplayManager.Instance.PlayerController != null)
            {
                _playerMovement = GameplayManager.Instance.PlayerController;
                _playerTransform = _playerMovement.transform;
                return;
            }

            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                    _playerMovement = player.GetComponent<PlayerMovementController>();
                }
            }
        }

        private void EnsureCameraReferences(bool force = false)
        {
            if (!force && _playerTransform == null) return;
            if (Time.time - _lastCameraSearchTime < playerSearchInterval) return;
            _lastCameraSearchTime = Time.time;

            if (_mainCamera == null)
            {
                _mainCamera = GameplayManager.Instance != null ? GameplayManager.Instance.MainCamera : Camera.main;
                if (_mainCamera == null)
                {
                    var camObj = GameObject.FindGameObjectWithTag("MainCamera");
                    if (camObj != null) _mainCamera = camObj.GetComponent<Camera>();
                }
            }

            if (_freeLookCamera == null)
            {
                _freeLookCamera = GameplayManager.Instance != null ? GameplayManager.Instance.CurrentCamera : FindObjectOfType<CinemachineFreeLook>();
                if (_freeLookCamera != null)
                {
                    _freeLookCamera.m_XAxis.m_InputAxisName = "";
                    _freeLookCamera.m_YAxis.m_InputAxisName = "";
                }
            }

            if (_freeLookCameraControl == null)
            {
                _freeLookCameraControl = FindObjectOfType<DM.FreeLookCameraControl>();
            }
        }
        #endregion

        #region Camera Control
        private void UpdateCameraInput()
        {
            EnsureCameraReferences();
            if (!isTargeting || IsTargetInvalid())
            {
                mouseX = 0f;
                mouseY = 0f;
                if (isTargeting && IsTargetInvalid())
                {
                    UnlockTarget();
                }
            }
            else
            {
                NewInputTarget(currentTarget);
            }

            if (_freeLookCameraControl != null)
            {
                _freeLookCameraControl.SetExternalInput(mouseX, 0f);
            }
        }

        private Vector3 GetLockOnTargetPosition(Transform fallbackTransform)
        {
            return _currentTargetLockOnPoint != null
                ? _currentTargetLockOnPoint.GetLockOnTransform().position
                : fallbackTransform.position;
        }

        private void NewInputTarget(Transform target)
        {
            if (!currentTarget || !_mainCamera) return;

            Vector3 targetPosition = GetLockOnTargetPosition(target);

            Vector3 viewPos = _mainCamera.WorldToViewportPoint(targetPosition);

            if (aimIcon)
                aimIcon.transform.position = _mainCamera.WorldToScreenPoint(targetPosition);

            if (_playerTransform && (targetPosition - _playerTransform.position).magnitude < minDistance)
            {
                if (showDebugLog) Debug.Log($"{GetType().Name}: Target too close, resetting camera input to 0");
                mouseX = 0f; // CRITICAL FIX: Reset input when target too close
                mouseY = 0f;
                return;
            }

            float dx = (viewPos.x - 0.5f + targetLockOffset.x);
            float dy = (viewPos.y - 0.5f + targetLockOffset.y);
            if (Mathf.Abs(dx) < centerDeadZone) dx = 0f;
            if (Mathf.Abs(dy) < centerDeadZone) dy = 0f;
            mouseX = dx * lockGain;
            mouseY = dy * lockGain;
        }
        #endregion

        #region UI
        private void UpdateUI()
        {
            if (aimIcon)
            {
                bool shouldShow = isTargeting && !IsTargetInvalid();
                aimIcon.gameObject.SetActive(shouldShow);
            }
        }
        #endregion

        #region Player Facing
        private void UpdatePlayerFacing()
        {
            if (!isTargeting || !currentTarget || _playerTransform == null) return;

            if (IsTargetInvalid())
            {
                UnlockTarget();
                if (showDebugLog) Debug.Log($"{GetType().Name}: Target died, auto unlocking");
                return;
            }

            float distToTarget = Vector3.Distance(_playerTransform.position, currentTarget.position);
            if (distToTarget > maxDistance + rigHysteresis)
            {
                UnlockTarget();
                if (showDebugLog) Debug.Log($"{GetType().Name}: Target out of maxDistance, unlocking & canceling auto aim");
                return;
            }

            Vector3 targetPosition = GetLockOnTargetPosition(currentTarget);

            Vector3 directionToTarget = (targetPosition - _playerTransform.position).normalized;
            directionToTarget.y = 0f;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                float rotationSpeed = Time.deltaTime * 10f;
                _playerTransform.rotation = Quaternion.Slerp(_playerTransform.rotation, targetRotation, rotationSpeed);
            }
        }

        private bool IsTargetInvalid()
        {
            if (currentTarget == null) return true;
            if (_targetHealthCached && _currentTargetHealth == null) return true;
            return _currentTargetHealth != null && _currentTargetHealth.IsDead;
        }
        #endregion

        #region Vertical Rig Control
        private void UpdateVerticalRig()
        {
            if (_freeLookCamera == null || !isTargeting || currentTarget == null || _playerTransform == null) return;
            Vector3 targetPosition = GetLockOnTargetPosition(currentTarget);
            float dist = Vector3.Distance(_playerTransform.position, targetPosition);

            float lowThreshold = Mathf.Max(0.1f, minDistance);
            float highThreshold = Mathf.Max(lowThreshold + rigHysteresis, maxDistance);

            switch (_rigState)
            {
                case VerticalRigState.Mid:
                    if (dist <= lowThreshold - rigHysteresis) _rigState = VerticalRigState.Low;
                    else if (dist >= highThreshold + rigHysteresis) _rigState = VerticalRigState.High;
                    break;
                case VerticalRigState.Low:
                    if (dist >= lowThreshold + rigHysteresis) _rigState = VerticalRigState.Mid;
                    break;
                case VerticalRigState.High:
                    if (dist <= highThreshold - rigHysteresis) _rigState = VerticalRigState.Mid;
                    break;
            }

            float targetY = _rigState == VerticalRigState.Low ? lowRigY :
                            _rigState == VerticalRigState.High ? highRigY : midRigY;
            float currentY = _freeLookCamera.m_YAxis.Value;
            float newY = Mathf.SmoothDamp(currentY, targetY, ref _verticalVelocity, verticalSmoothTime);
            if (_freeLookCameraControl != null)
            {
                _freeLookCameraControl.SetVerticalPosition(newY);
            }
            else
            {
                _freeLookCamera.m_YAxis.Value = newY;
            }
        }
        #endregion

        #region Target Detection
        private void UpdateEnemyCache()
        {
            EnsurePlayerReference();
            EnsureCameraReferences();
            if (_playerTransform == null) return;

            // Update enemy cache every CACHE_DURATION seconds using Physics OverlapSphere
            if (Time.time - _cacheTime > CACHE_DURATION)
            {
                int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
                if (enemyLayer != -1)
                {
                    int layerMask = (1 << enemyLayer);
                    Helper.FindAliveTargets(_playerTransform.position, maxDistance, layerMask, _cachedEnemies);
                }
                else
                {
                    _cachedEnemies.Clear();
                }

                if (_cachedEnemies.Count == 0)
                {
                    var allHealth = FindObjectsOfType<Health>(true);
                    _cachedEnemies.Clear();
                    for (int i = 0; i < allHealth.Length; i++)
                    {
                        var h = allHealth[i];
                        if (h == null || !h.IsAlive) continue;
                        if (!h.gameObject.activeInHierarchy) continue;
                        if (h.ObjectFaction != Faction.AI) continue;
                        if (h.transform == _playerTransform) continue;
                        if (Vector3.Distance(_playerTransform.position, h.transform.position) > maxDistance) continue;
                        _cachedEnemies.Add(h);
                    }
                }
                
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Enemy cache updated: {_cachedEnemies.Count}");
                _cacheTime = Time.time;
            }
        }

        private GameObject ClosestTarget()
        {
            EnsurePlayerReference();
            if (_playerTransform == null) return null;

            // Cache is already updated in UpdateEnemyCache() called from Update()
            // Just find the closest valid target from cached enemies

            Health closest = null;
            ILockOnTarget closestLockOnPoint = null;
            float closestDistance = maxDistance;
            Vector3 position = _playerTransform.position;

            foreach (Health enemy in _cachedEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                // Health adalah anchor (parent dengan layer "Enemy")
                // Cari LockOnPoint di child-nya (any layer)
                ILockOnTarget lockOnPoint = enemy.GetComponentInChildren<ILockOnTarget>();
                
                // Tentukan posisi yang dipakai untuk perhitungan jarak & angle
                Transform targetTransform = lockOnPoint != null ? lockOnPoint.GetLockOnTransform() : enemy.transform;

                Vector3 diff = targetTransform.position - position;
                float curDistance = diff.magnitude;

                if (curDistance < closestDistance)
                {
                    if (_mainCamera && Vector3.Angle(diff.normalized, _mainCamera.transform.forward) < maxAngle)
                    {
                        closest = enemy;
                        closestLockOnPoint = lockOnPoint;
                        closestDistance = curDistance;
                    }
                }
            }

            // Cache lock-on point untuk dipakai di UpdateCurrentTarget()
            _currentTargetLockOnPoint = closestLockOnPoint;
            
            return closest?.gameObject;
        }
        #endregion

        #region Public Methods
        public void ToggleLockTarget()
        {
            if (!enableLockOn) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            AssignTarget();
        }

        public void LockToTarget(Transform target)
        {
            if (target == null) return;

            currentTarget = target;
            isTargeting = true;

            if (_freeLookCameraControl != null)
            {
                _freeLookCameraControl.EnableExternalInput();
            }
            OnLockedTargetChanged?.Invoke(currentTarget);
        }

        public void UnlockTarget()
        {
            isTargeting = false;
            currentTarget = null;

            _currentTargetHealth = null;
            _targetHealthCached = false;
            _currentTargetLockOnPoint = null;

            if (_freeLookCameraControl != null)
            {
                _freeLookCameraControl.DisableExternalInput();
            }
            OnLockedTargetChanged?.Invoke(null);
        }
        #endregion

        private void HandleGamePaused()
        {
            UnlockTarget();
        }

        private void HandleGameResumed()
        {
        }

        private void HandleGameOver()
        {
            UnlockTarget();
        }

        private void HandleGameWin()
        {
            UnlockTarget();
        }

        #region Debug Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmo) return;

            // Draw detection sphere at player position
            if (_playerTransform != null)
            {
                Gizmos.color = isTargeting ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(_playerTransform.position, maxDistance);

                // Draw current target indicator
                if (currentTarget != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(_playerTransform.position, currentTarget.position);
                    Gizmos.DrawSphere(currentTarget.position, 0.5f);
                }
            }
        }
#endif

        #endregion

    }
}
