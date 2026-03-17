using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cinemachine;
using LittleHeroJourney;

namespace DM
{
    public class FreeLookCameraControl : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        #region Inspector Settings
        private CinemachineFreeLook freeLookCamera;

        [Header("Sensitivity Settings")]
        [Tooltip("Horizontal rotation speed multiplier")]
        [SerializeField] private float horizontalSpeed;

        [Tooltip("Vertical rotation speed multiplier")]
        [SerializeField] private float verticalSpeed;

        [Header("Input Separation")]
        [Tooltip("Angle threshold for axis separation. Lower = stricter separation, Higher = more diagonal movement")]
        [Range(10f, 35f)]
        [SerializeField] private float axisLockAngle;

        [Tooltip("Minimum pixel movement to register as input")]
        [SerializeField] private float inputThreshold;

        [Tooltip("If drag delta exceeds this (e.g. after canvas switch), treat as fresh start to avoid sensitivity spike. Pixels.")]
        [SerializeField] private float maxDeltaForValidDrag = 200f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;
        #endregion

        #region Private Variables
        private Vector2 lastPointerPos;
        private bool isDragging;
        private Canvas parentCanvas;

        // External input override for target lock
        private bool useExternalInput = false;
        private float externalMouseX = 0f;
        private float externalMouseY = 0f;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            GameEventSystem.SubscribeAction("CameraInitialized", OnCameraInitializedEvent);
            GameEventSystem.SubscribeAction("GameplayReset", OnGameplayResetEvent);
            if (GameplayManager.Instance != null && GameplayManager.Instance.CurrentCamera != null)
                HandleCameraInitialized(GameplayManager.Instance.CurrentCamera);
        }

        private void OnDisable()
        {
            GameEventSystem.UnsubscribeAction("CameraInitialized", OnCameraInitializedEvent);
            GameEventSystem.UnsubscribeAction("GameplayReset", OnGameplayResetEvent);
        }

        private void OnGameplayResetEvent()
        {
            if (GameplayManager.Instance != null)
                HandleCameraInitialized(GameplayManager.Instance.CurrentCamera);
        }

        private void OnCameraInitializedEvent()
        {
            if (GameplayManager.Instance != null)
                HandleCameraInitialized(GameplayManager.Instance.CurrentCamera);
        }

        private void Update()
        {
            if (useExternalInput && freeLookCamera != null)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = externalMouseX;
                freeLookCamera.m_YAxis.m_InputAxisValue = externalMouseY;
            }
        }

        #endregion

        #region Initialization
        private void Initialize()
        {
            parentCanvas = GetComponentInParent<Canvas>();

            if (showDebugLog)
            {
                Debug.Log($"[{GetType().Name}] Initialized");
            }
        }

        private void HandleCameraInitialized(CinemachineFreeLook cam)
        {
            freeLookCamera = cam;
            isDragging = false;
            StopCameraInput();
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Received Camera Reference from GameplayManager (drag state reset).");
        }
        #endregion

        #region Touch Input Handlers
        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            lastPointerPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || freeLookCamera == null) return;

            if (useExternalInput)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = externalMouseX;
                freeLookCamera.m_YAxis.m_InputAxisValue = externalMouseY;
                return;
            }

            Vector2 rawDelta = eventData.position - lastPointerPos;
            lastPointerPos = eventData.position;

            if (rawDelta.magnitude > maxDeltaForValidDrag)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = 0f;
                freeLookCamera.m_YAxis.m_InputAxisValue = 0f;
                return;
            }

            if (rawDelta.magnitude < inputThreshold)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = 0f;
                freeLookCamera.m_YAxis.m_InputAxisValue = 0f;
                return;
            }

            float angle = Mathf.Atan2(Mathf.Abs(rawDelta.y), Mathf.Abs(rawDelta.x)) * Mathf.Rad2Deg;

            float inputX = (rawDelta.x / Screen.width) * horizontalSpeed;
            float inputY = -(rawDelta.y / Screen.height) * verticalSpeed;

            if (angle > (90f - axisLockAngle))
            {
                inputX *= 0.1f;
            }
            else if (angle < axisLockAngle)
            {
                inputY *= 0.2f;
            }

            const float maxInputPerFrame = 1.5f;
            inputX = Mathf.Clamp(inputX, -maxInputPerFrame, maxInputPerFrame);
            inputY = Mathf.Clamp(inputY, -maxInputPerFrame, maxInputPerFrame);

            freeLookCamera.m_XAxis.m_InputAxisValue = inputX;
            freeLookCamera.m_YAxis.m_InputAxisValue = inputY;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            StopCameraInput();
            isDragging = false;
        }

        private void StopCameraInput()
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = 0f;
                freeLookCamera.m_YAxis.m_InputAxisValue = 0f;
            }
        }
        #endregion

        #region Public Methods
        public void ResetCamera()
        {
            if (freeLookCamera == null) return;

            StopCameraInput();
            isDragging = false;
            freeLookCamera.m_XAxis.Value = 0f;
            freeLookCamera.m_YAxis.Value = 0.5f;
        }

        public void SetVerticalPosition(float value)
        {
            if (freeLookCamera == null) return;
            freeLookCamera.m_YAxis.Value = Mathf.Clamp01(value);
        }

        public void SetHorizontalRotation(float degrees)
        {
            if (freeLookCamera == null) return;
            freeLookCamera.m_XAxis.Value = degrees;
        }

        public void EnableExternalInput()
        {
            useExternalInput = true;
        }

        public void DisableExternalInput()
        {
            useExternalInput = false;
            externalMouseX = 0f;
            externalMouseY = 0f;
            
            if (freeLookCamera != null)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = 0f;
                freeLookCamera.m_YAxis.m_InputAxisValue = 0f;
            }
        }

        public void SetExternalInput(float mouseX, float mouseY)
        {
            externalMouseX = mouseX;
            externalMouseY = mouseY;

            if (useExternalInput && freeLookCamera != null)
            {
                freeLookCamera.m_XAxis.m_InputAxisValue = externalMouseX;
                freeLookCamera.m_YAxis.m_InputAxisValue = externalMouseY;
            }
        }
        #endregion

    }
}
