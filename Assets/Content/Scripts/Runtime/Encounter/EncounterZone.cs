using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.AI.Navigation;

namespace LittleHeroJourney
{
    /// <summary>
    /// Data untuk satu enemy spawn
    /// </summary>
    [System.Serializable]
    public class EnemySpawnData
    {
        [Tooltip("Enemy prefab yang akan di-spawn")]
        public GameObject enemyPrefab;
        
        [Tooltip("Posisi spawn untuk enemy ini")]
        public Transform spawnPoint;
        
        [Tooltip("Delay sebelum spawn enemy ini (seconds dari encounter start)")]
        public float spawnDelay = 0f;
    }
    
    [RequireComponent(typeof(BoxCollider))]
    public class EncounterZone : MonoBehaviour
    {
        #region Enums
        
        public enum EncounterState
        {
            Idle,           // Belum triggered
            Active,         // Player sudah masuk, enemy spawned
            Completed       // Semua enemy mati
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Enemy Spawning")]
        [Tooltip("List data spawn enemy. Setiap entry = 1 enemy dengan prefab, spawn point, dan delay masing-masing.")]
        [SerializeField] private List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();
        
        [Header("Barrier Walls")]
        [Tooltip("List GameObject barrier/wall yang akan diaktifkan saat encounter. Bisa 1, 2, 3, atau berapapun.")]
        [SerializeField] private List<GameObject> barrierWalls = new List<GameObject>();
        

        [Header("Barrier Walls Activation Delay")]
        [Tooltip("Delay sebelum barrier walls diaktifkan setelah encounter started (seconds)")]
        [SerializeField] private float barrierActivateDelay = 0.5f;
        
        [Tooltip("Delay sebelum barrier walls nonaktif setelah semua enemy mati (seconds)")]
        [SerializeField] private float barrierDeactivateDelay = 1.0f;
        
        [Header("Player Detection")]
        [Tooltip("Tag untuk mendeteksi player")]
        [SerializeField] private string playerTag = "Player";
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
        
        #endregion
        
        #region Private Fields
        
        private BoxCollider _triggerCollider;
        private EncounterState _currentState = EncounterState.Idle;
        private List<GameObject> _spawnedEnemies = new List<GameObject>();
        private int _aliveEnemyCount = 0;
        
        #endregion
        
        #region Properties
        
        public EncounterState CurrentState => _currentState;
        public int AliveEnemyCount => _aliveEnemyCount;
        public bool IsCompleted => _currentState == EncounterState.Completed;
        
        #endregion
        
        #region Events
        
        public event Action OnEncounterStarted;
        public event Action OnEncounterCompleted;
        public event Action<GameObject> OnEnemySpawned;
        public event Action<GameObject> OnEnemyDied;
        
        [Header("Unity Events")]
        [SerializeField] public UnityEvent OnPlayerEntered;
        [SerializeField] public UnityEvent OnEncounterStartedEvent;
        [SerializeField] public UnityEvent OnAllEnemiesDefeated;
        [SerializeField] public UnityEvent OnBarriersActivated;
        [SerializeField] public UnityEvent OnBarriersDeactivated;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
            
            // Pastikan barrier walls nonaktif di awal
            SetBarrierWallsActive(false);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (_currentState != EncounterState.Idle) return;
            
            if (other.CompareTag(playerTag))
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Player entered encounter zone!");
                
                // Set encounter zone reference for player
                PlayerMovementController playerMovement = other.GetComponent<PlayerMovementController>();
                if (playerMovement != null)
                {
                    playerMovement.SetEncounterZone(this);
                }
                
                OnPlayerEntered?.Invoke();
                StartEncounter();
            }
        }
        
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
        
        #endregion
        
        #region Encounter Logic
        
        private void StartEncounter()
        {
            _currentState = EncounterState.Active;
            
            StartCoroutine(ActivateBarriersCoroutine());
            
            // Spawn enemies dengan delay masing-masing
            StartCoroutine(SpawnEnemiesCoroutine());
            
            OnEncounterStarted?.Invoke();
            OnEncounterStartedEvent?.Invoke();
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter STARTED! Barriers will activate in {barrierActivateDelay} seconds.");
        }
        
        private IEnumerator SpawnEnemiesCoroutine()
        {
            if (enemySpawns.Count == 0)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No enemy spawns assigned!");
                yield break;
            }
            
            // Sort by spawn delay untuk spawn berurutan
            List<EnemySpawnData> sortedSpawns = new List<EnemySpawnData>(enemySpawns);
            sortedSpawns.Sort((a, b) => a.spawnDelay.CompareTo(b.spawnDelay));
            
            float lastDelay = 0f;
            
            foreach (var spawnData in sortedSpawns)
            {
                if (spawnData.enemyPrefab == null) continue;
                
                // Hitung delay relatif dari spawn sebelumnya
                float waitTime = spawnData.spawnDelay - lastDelay;
                if (waitTime > 0)
                {
                    yield return new WaitForSeconds(waitTime);
                }
                lastDelay = spawnData.spawnDelay;
                
                // Spawn enemy
                SpawnEnemy(spawnData);
            }
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] All enemies spawned! Total: {_aliveEnemyCount}");
        }
        
        private void SpawnEnemy(EnemySpawnData spawnData)
        {
            // Tentukan posisi spawn
            Vector3 spawnPos = spawnData.spawnPoint != null 
                ? spawnData.spawnPoint.position 
                : transform.position;
            
            Quaternion spawnRot = spawnData.spawnPoint != null 
                ? spawnData.spawnPoint.rotation 
                : Quaternion.identity;
            
            // Spawn enemy
            GameObject enemy = Instantiate(spawnData.enemyPrefab, spawnPos, spawnRot);
            _spawnedEnemies.Add(enemy);
            _aliveEnemyCount++;
            
            // Set encounter zone reference for spawned enemy
            AIKnockbackHandler knockbackHandler = enemy.GetComponent<AIKnockbackHandler>();
            if (knockbackHandler != null)
            {
                knockbackHandler.SetEncounterZone(this);
            }
            
            // Subscribe ke event kematian
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
            
            // Clear encounter zone reference from dead enemy
            AIKnockbackHandler knockbackHandler = enemy.GetComponent<AIKnockbackHandler>();
            if (knockbackHandler != null)
            {
                knockbackHandler.ClearEncounterZone();
            }
            
            OnEnemyDied?.Invoke(enemy);
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Enemy died! Remaining: {_aliveEnemyCount}");
            
            // Check jika semua enemy mati
            if (_aliveEnemyCount <= 0)
            {
                CompleteEncounter();
            }
        }
        
        private void CompleteEncounter()
        {
            _currentState = EncounterState.Completed;
            
            // Clear encounter zone reference from player
            if (_triggerCollider != null)
            {
                Collider[] colliders = Physics.OverlapBox(_triggerCollider.bounds.center, _triggerCollider.bounds.extents);
                foreach (var col in colliders)
                {
                    PlayerMovementController playerMovement = col.GetComponent<PlayerMovementController>();
                    if (playerMovement != null)
                    {
                        playerMovement.ClearEncounterZone();
                    }
                }
            }
            
            // Nonaktifkan barrier walls dengan delay
            StartCoroutine(DeactivateBarriersCoroutine());
        
            OnEncounterCompleted?.Invoke();
            OnAllEnemiesDefeated?.Invoke();
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter COMPLETED! All enemies defeated.");
        }
        
        private IEnumerator ActivateBarriersCoroutine()
        {
            yield return new WaitForSeconds(barrierActivateDelay);
            
            SetBarrierWallsActive(true);
            OnBarriersActivated?.Invoke();
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Barrier walls activated.");
        }
        
        private IEnumerator DeactivateBarriersCoroutine()
        {
            yield return new WaitForSeconds(barrierDeactivateDelay);
            
            SetBarrierWallsActive(false);
            OnBarriersDeactivated?.Invoke();
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Barrier walls deactivated.");
        }
        
        #endregion
        
        #region Barrier Walls
        
        private void SetBarrierWallsActive(bool active)
        {
            foreach (var wall in barrierWalls)
            {
                if (wall != null)
                {
                    wall.SetActive(active);
                }
            }
        }
        
        #endregion

        #region Boundary Validation

        /// <summary>
        /// Check if position is inside encounter zone bounds
        /// </summary>
        public bool IsPositionInZone(Vector3 position)
        {
            if (_triggerCollider == null) return false;
            Bounds bounds = _triggerCollider.bounds;
            return bounds.Contains(position);
        }

        /// <summary>
        /// Get the clamped position if it's outside zone bounds
        /// Returns the closest point on the boundary if outside, otherwise returns original position
        /// </summary>
        public Vector3 GetClampedPosition(Vector3 position, float offsetFromBoundary = 0.5f)
        {
            if (_triggerCollider == null) return position;
            
            Bounds bounds = _triggerCollider.bounds;
            Vector3 clampedPos = bounds.ClosestPoint(position);
            
            // If position was outside, move it slightly inward from the boundary
            if (!bounds.Contains(position))
            {
                Vector3 directionInward = (bounds.center - clampedPos).normalized;
                clampedPos += directionInward * offsetFromBoundary;
            }
            
            return clampedPos;
        }

        /// <summary>
        /// Validate and clamp character position to stay within encounter zone
        /// Called after knockback to ensure character doesn't escape
        /// Only clamps if encounter is still active
        /// </summary>
        public void ValidateAndClampPosition(Transform characterTransform, float offsetFromBoundary = 0.5f)
        {
            if (characterTransform == null || _currentState != EncounterState.Active)
                return;

            Vector3 currentPos = characterTransform.position;
            if (!IsPositionInZone(currentPos))
            {
                Vector3 clampedPos = GetClampedPosition(currentPos, offsetFromBoundary);
                characterTransform.position = clampedPos;
                
                if (showDebugLog)
                    Debug.Log($"[{GetType().Name}] Clamped {characterTransform.gameObject.name} position back into zone: {currentPos} -> {clampedPos}");
            }
        }

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force start encounter (untuk testing atau scripted events)
        /// </summary>
        public void ForceStartEncounter()
        {
            if (_currentState == EncounterState.Idle)
            {
                StartEncounter();
            }
        }
        
        /// <summary>
        /// Force complete encounter (untuk testing atau skip)
        /// </summary>
        public void ForceCompleteEncounter()
        {
            if (_currentState == EncounterState.Active)
            {
                // Kill semua enemy
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
        
        /// <summary>
        /// Reset encounter ke state Idle (untuk replay)
        /// </summary>
        public void ResetEncounter()
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
            _aliveEnemyCount = 0;
            
            // Reset state
            _currentState = EncounterState.Idle;
            
            // Nonaktifkan barriers
            SetBarrierWallsActive(false);
            
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Encounter RESET to Idle state.");
        }
        
        #endregion
        
        #region Editor Gizmos
        
        private void OnDrawGizmos()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.color = gizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                
                // Wireframe
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn points dari enemySpawns list
            Gizmos.color = Color.green;
            foreach (var spawnData in enemySpawns)
            {
                if (spawnData != null && spawnData.spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnData.spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(transform.position, spawnData.spawnPoint.position);
                    
                    // Draw label dengan delay info
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(spawnData.spawnPoint.position + Vector3.up * 0.7f, 
                        $"Delay: {spawnData.spawnDelay}s");
                    #endif
                }
            }
            
            // Draw barrier walls
            Gizmos.color = Color.red;
            foreach (var wall in barrierWalls)
            {
                if (wall != null)
                {
                    Collider wallCol = wall.GetComponent<Collider>();
                    if (wallCol != null)
                    {
                        Gizmos.DrawWireCube(wallCol.bounds.center, wallCol.bounds.size);
                    }
                    else
                    {
                        Gizmos.DrawWireCube(wall.transform.position, Vector3.one);
                    }
                }
            }
        }
        
        #endregion
    }
}
