using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "MovementSettings", menuName = "Player/Movement Settings")]
    public class MovementSettingsSO : ScriptableObject
    {
        [Header("Stable Movement")]
        public float MaxStableMoveSpeed;
        public float StableMovementSharpness;
        public float OrientationSharpness;
        public OrientationMethod OrientationMethod;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed;
        public float AirAccelerationSpeed;
        public float Drag;

        [Header("Gravity Settings")]
        public Vector3 Gravity;

        [Header("Animation Settings")]
        [Tooltip("Parameter name for speed in animation blend tree")]
        public string speedParameterName;

        [Header("Debug Settings")]
        public bool ShowDebugGizmos;
        public bool ShowDebugLog;
    }
}