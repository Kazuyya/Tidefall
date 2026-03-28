using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "AISettings", menuName = "Little Hero Journey/AI/AI Settings")]
    public class AISettingsSO : ScriptableObject
    {
        [Header("Movement Settings")]
        public float MoveSpeed = 2.5f;
        public float RotationSpeed = 720f;
        public float StoppingDistance = 1.5f;

        [Header("Detection Settings")]
        public float DetectionRange = 15f;
        public float AggroRange = 8f;
        public float AttackRange = 2f;
        public LayerMask TargetLayers;
        public float DetectionUpdateInterval = 0.5f;

        [Header("Behavior Settings")]
        public float TauntDuration = 1.5f;
        public float AggroBuildupTime = 2f;
        public float CombatCooldownTime = 10f;
        public float LoseAggroDistance = 20f;
        public float IdleMoveRadius = 5f;
        public float IdleWaitTime = 3f;

        [Header("Combat Settings")]
        public float AttackPreparationTime = 0.3f;
        public float PostAttackRepositionDistance = 3f;
        public float MovementPreparationTime = 0.5f;

        [Header("Animation Settings")]
        public string speedParameterName = "speed";

        [Header("Taunt")]
        public string tauntTriggerName = "taunt";
        public string tauntSfxEffectName = "";

        [Range(0f, 1f)]
        public float tauntSfxNormalizedTime = 0.15f;
        public AudioPlaybackChannel tauntSfxPlaybackChannel = AudioPlaybackChannel.Sfx;

        [Header("Debug Settings")]
        public bool ShowDebugGizmos = true;
        public bool ShowDebugLog = false;

        private void OnValidate()
        {
            tauntSfxNormalizedTime = Mathf.Clamp01(tauntSfxNormalizedTime);
        }
    }
}