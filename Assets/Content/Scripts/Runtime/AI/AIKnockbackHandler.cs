using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace LittleHeroJourney
{
    [RequireComponent(typeof(AIAgent))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIKnockbackHandler : MonoBehaviour, IKnockbackable
    {
        private AIAgent _aiAgent;
        private NavMeshAgent _navMeshAgent;
        private bool _isKnockedBack;
        private Coroutine _knockbackCoroutine;
        
        private StoryEncounterSpawner _currentEncounterSpawner;

        [Header("Knockback")]
        [Min(0.1f)]
        public float knockbackDuration = 0.3f;

        [Header("Encounter Settings")]
        [SerializeField] private float boundaryClampOffset = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;
        [SerializeField] private bool showEncounterBoundaryLog;

        private void Awake()
        {
            _aiAgent = GetComponent<AIAgent>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            if (_aiAgent == null) Debug.LogWarning($"[{GetType().Name}] No AIAgent.");
            if (_navMeshAgent == null) Debug.LogWarning($"[{GetType().Name}] No NavMeshAgent.");
        }

        public void ApplyKnockback(Vector3 direction, float distance)
        {
            if (_navMeshAgent == null || _aiAgent == null) return;
            if (_isKnockedBack) return;
            if (direction.sqrMagnitude < 0.01f)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Knockback skipped: direction too small.");
                return;
            }

            if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);

            Vector3 startPos = transform.position;
            _knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction, distance, startPos));
        }

        private IEnumerator KnockbackRoutine(Vector3 direction, float distance, Vector3 startPosition)
        {
            _isKnockedBack = true;
            bool wasEnabled = _navMeshAgent.enabled;
            _navMeshAgent.enabled = false;

            Helper.GetKnockbackTarget(startPosition, direction, distance, out Vector3 targetPosition, out bool hitObstacle);

            float elapsed = 0f;
            while (elapsed < knockbackDuration)
            {
                elapsed += Time.deltaTime;
                float curve = Helper.EaseOutCubic(elapsed / knockbackDuration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, curve);
                yield return null;
            }

            transform.position = targetPosition;
            
            if (_currentEncounterSpawner != null)
            {
            }

            if (wasEnabled)
            {
                _navMeshAgent.enabled = true;
                _navMeshAgent.Warp(transform.position);
            }

            _isKnockedBack = false;
            _knockbackCoroutine = null;

            if (showDebugLog)
            {
                float actual = Vector3.Distance(startPosition, targetPosition);
                string extra = hitObstacle ? " (obstacle)" : "";
                Debug.Log($"[{GetType().Name}] Knockback{extra}: {startPosition} -> {targetPosition}, {actual:F2}m");
            }
        }

        public void SetEncounterZone(StoryEncounterSpawner encounterSpawner)
        {
            _currentEncounterSpawner = encounterSpawner;
            if (showEncounterBoundaryLog)
            {
                Debug.Log($"[{GetType().Name}] Encounter spawner reference set: {(encounterSpawner != null ? encounterSpawner.gameObject.name : "null")}");
            }
        }

        public void ClearEncounterZone()
        {
            if (showEncounterBoundaryLog && _currentEncounterSpawner != null)
            {
                Debug.Log($"[{GetType().Name}] Encounter spawner reference cleared");
            }
            _currentEncounterSpawner = null;
        }
    }
}
