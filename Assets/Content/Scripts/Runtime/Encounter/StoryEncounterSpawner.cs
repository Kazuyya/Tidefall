using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LittleHeroJourney
{
    [Serializable]
    public class EnemySpawnData
    {
        [Tooltip("Enemy prefab to spawn")]
        public GameObject enemyPrefab;

        [Tooltip("Spawn position for this enemy")]
        public Transform spawnPoint;

        [Tooltip("Delay before spawning this enemy (seconds from encounter start)")]
        public float spawnDelay = 0f;
    }

    public class StoryEncounterSpawner : MonoBehaviour
    {
        public enum EncounterState
        {
            Idle,           // Not started yet
            Active,         // Encounter is running
            Completed       // All enemies defeated
        }

        [Header("Encounter Id")]
        [Tooltip("Unique id used to connect this spawner with JourneyData or other systems.")]
        [SerializeField] private string encounterId = "";

        [Header("Player Spawn")]
        [Tooltip("Spawn position for the player when this encounter starts.")]
        [SerializeField] private Transform playerSpawnPoint;

        [Tooltip("Optional player prefab to spawn for this encounter. If null, existing player is reused.")]
        [SerializeField] private GameObject playerPrefab;

        [Header("Enemy Spawning")]
        [Tooltip("Optional delay before any enemies start spawning after encounter begins.")]
        [SerializeField] private float initialEnemySpawnDelay = 1.0f;
        [Tooltip("Enemy spawn data list. Each entry = 1 enemy with prefab, spawn point, and delay.")]
        [SerializeField] private List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;

        private EncounterState _currentState = EncounterState.Idle;
        private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        private int _aliveEnemyCount;

        public EncounterState CurrentState => _currentState;
        public int AliveEnemyCount => _aliveEnemyCount;
        public bool IsCompleted => _currentState == EncounterState.Completed;
        public string EncounterId => encounterId;
        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public GameObject PlayerPrefab => playerPrefab;

        public event Action OnEncounterStarted;
        public event Action OnEncounterCompleted;
        public event Action<GameObject> OnEnemySpawned;
        public event Action<GameObject> OnEnemyDied;

        [Header("Unity Events")]
        [SerializeField] public UnityEvent OnEncounterStartedEvent;
        [SerializeField] public UnityEvent OnAllEnemiesDefeated;

        private void OnDestroy()
        {
            // Cleanup spawned enemies
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            _spawnedEnemies.Clear();
        }

        public void StartEncounter()
        {
            if (_currentState != EncounterState.Idle) return;

            _currentState = EncounterState.Active;
            StartCoroutine(SpawnEnemiesCoroutine());

            OnEncounterStarted?.Invoke();
            OnEncounterStartedEvent?.Invoke();
            GameEventSystem.Publish(new UIActionEvent("EncounterZoneStarted", encounterId));

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter STARTED!");
        }

        private IEnumerator SpawnEnemiesCoroutine()
        {
            if (enemySpawns == null || enemySpawns.Count == 0)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No enemy spawns assigned!");
                yield break;
            }

            // Global delay before first enemy so player + camera + canvas have time to settle
            if (initialEnemySpawnDelay > 0f)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Waiting {initialEnemySpawnDelay:0.##}s before spawning enemies.");
                yield return new WaitForSeconds(initialEnemySpawnDelay);
            }

            List<EnemySpawnData> sortedSpawns = new List<EnemySpawnData>(enemySpawns);
            sortedSpawns.Sort((a, b) => a.spawnDelay.CompareTo(b.spawnDelay));

            float lastDelay = 0f;

            foreach (var spawnData in sortedSpawns)
            {
                if (spawnData.enemyPrefab == null) continue;

                float waitTime = spawnData.spawnDelay - lastDelay;
                if (waitTime > 0)
                {
                    yield return new WaitForSeconds(waitTime);
                }
                lastDelay = spawnData.spawnDelay;

                SpawnEnemy(spawnData);
            }

            if (showDebugLog) Debug.Log($"[{GetType().Name}] All enemies spawned! Total: {_aliveEnemyCount}");
        }

        private void SpawnEnemy(EnemySpawnData spawnData)
        {
            Vector3 spawnPos = spawnData.spawnPoint != null
                ? spawnData.spawnPoint.position
                : transform.position;

            Quaternion spawnRot = spawnData.spawnPoint != null
                ? spawnData.spawnPoint.rotation
                : Quaternion.identity;

            GameObject enemy = Instantiate(spawnData.enemyPrefab, spawnPos, spawnRot);
            _spawnedEnemies.Add(enemy);
            _aliveEnemyCount++;

            AIKnockbackHandler knockbackHandler = enemy.GetComponent<AIKnockbackHandler>();
            if (knockbackHandler != null)
            {
                knockbackHandler.SetEncounterZone(this);
            }

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath += () => OnEnemyDeath(enemy);
            }

            OnEnemySpawned?.Invoke(enemy);

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Spawned enemy: {spawnData.enemyPrefab.name} at {spawnPos}");
        }

        private void OnEnemyDeath(GameObject enemy)
        {
            _aliveEnemyCount--;

            AIKnockbackHandler knockbackHandler = enemy.GetComponent<AIKnockbackHandler>();
            if (knockbackHandler != null)
            {
                knockbackHandler.ClearEncounterZone();
            }

            OnEnemyDied?.Invoke(enemy);

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Enemy died! Remaining: {_aliveEnemyCount}");

            if (_aliveEnemyCount <= 0)
            {
                CompleteEncounter();
            }
        }

        private void CompleteEncounter()
        {
            _currentState = EncounterState.Completed;

            OnEncounterCompleted?.Invoke();
            OnAllEnemiesDefeated?.Invoke();
            GameEventSystem.Publish(new UIActionEvent("EncounterZoneCompleted", encounterId));

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter COMPLETED! All enemies defeated.");
        }

        public void ForceStartEncounter()
        {
            if (_currentState == EncounterState.Idle)
            {
                StartEncounter();
            }
        }

        public void ForceCompleteEncounter()
        {
            if (_currentState == EncounterState.Active)
            {
                foreach (var enemy in _spawnedEnemies)
                {
                    if (enemy != null)
                    {
                        Health health = enemy.GetComponent<Health>();
                        if (health != null && health.IsAlive)
                        {
                            health.TakeDamage(9999f, transform);
                        }
                    }
                }
            }
        }

        public void ResetEncounter()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            _spawnedEnemies.Clear();
            _aliveEnemyCount = 0;
            _currentState = EncounterState.Idle;

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter RESET to Idle state.");
        }

        private void OnDrawGizmos()
        {
            if (enemySpawns == null) return;

            Gizmos.color = Color.green;
            foreach (var spawnData in enemySpawns)
            {
                if (spawnData == null || spawnData.spawnPoint == null) continue;

                Vector3 pos = spawnData.spawnPoint.position;
                Gizmos.DrawWireSphere(pos, 0.5f);
                Gizmos.DrawLine(transform.position, pos);

                Vector3 forward = spawnData.spawnPoint.forward.normalized;
                if (forward.sqrMagnitude > 0.001f)
                {
                    float dirLength = 1.3f;
                    Vector3 end = pos + forward * dirLength;
                    Gizmos.DrawLine(pos, end);

                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    float headSize = 0.35f;
                    Vector3 headLeft = end - forward * headSize + right * headSize;
                    Vector3 headRight = end - forward * headSize - right * headSize;
                    Gizmos.DrawLine(end, headLeft);
                    Gizmos.DrawLine(end, headRight);
                }

#if UNITY_EDITOR
                if (spawnData.enemyPrefab != null)
                {
                    string enemyName = spawnData.enemyPrefab.name;
                    UnityEditor.Handles.Label(
                        pos + Vector3.up * 0.9f,
                        $"{enemyName}\nDelay: {spawnData.spawnDelay:0.##}s");
                }
#endif
            }

            if (playerSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 p = playerSpawnPoint.position;
                Gizmos.DrawWireSphere(p, 0.6f);
                Gizmos.DrawLine(transform.position, p);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(p + Vector3.up * 0.7f, "Player Spawn");
#endif

                Vector3 forward = playerSpawnPoint.forward.normalized;
                if (forward.sqrMagnitude > 0.001f)
                {
                    float dirLength = 1.3f;
                    Vector3 end = p + forward * dirLength;
                    Gizmos.DrawLine(p, end);

                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    float headSize = 0.35f;
                    Vector3 headLeft = end - forward * headSize + right * headSize;
                    Vector3 headRight = end - forward * headSize - right * headSize;
                    Gizmos.DrawLine(end, headLeft);
                    Gizmos.DrawLine(end, headRight);
                }
            }
        }
    }
}

