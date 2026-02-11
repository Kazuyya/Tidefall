using System.Collections;
using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Handles spawn (scale 0→1) and death (scale 1→0) animations for character GameObject
    /// </summary>
    public class CharacterSpawnDeathAnimator : MonoBehaviour
    {
        #region Fields

        [Header("Spawn Scale Animation")]
        [SerializeField] private float spawnScaleAnimDuration = 0.5f;
        [SerializeField] private AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Death Scale Animation")]
        [SerializeField] private float deathScaleAnimDuration = 0.4f;
        [SerializeField] private AnimationCurve deathScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float deathScaleDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        private Health _health;
        private Coroutine _spawnAnimCoroutine;
        private Coroutine _deathAnimCoroutine;
        private bool _hasPlayedSpawnAnimation = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _health = GetComponent<Health>();

            // Subscribe to death event
            if (_health != null)
            {
                _health.OnDeath += PlayDeathAnimation;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDeath -= PlayDeathAnimation;
            }

            // Stop any running animations
            if (_spawnAnimCoroutine != null)
                StopCoroutine(_spawnAnimCoroutine);
            if (_deathAnimCoroutine != null)
                StopCoroutine(_deathAnimCoroutine);
        }

        #endregion

        #region Spawn/Death Animations

        /// <summary>
        /// Play spawn animation: scale 0→1 with bouncy curve
        /// Call this from AIAgent.Awake() or similar
        /// </summary>
        public void PlaySpawnAnimation()
        {
            if (_hasPlayedSpawnAnimation)
                return; // Only play once

            if (_spawnAnimCoroutine != null)
                StopCoroutine(_spawnAnimCoroutine);

            _spawnAnimCoroutine = StartCoroutine(SpawnAnimationSequence());
        }

        private IEnumerator SpawnAnimationSequence()
        {
            // Set initial state - character starts at scale 0
            transform.localScale = Vector3.zero;

            // Scale up animation with bounce curve
            float elapsedTime = 0f;
            while (elapsedTime < spawnScaleAnimDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / spawnScaleAnimDuration);
                float scale = spawnScaleCurve.Evaluate(progress);
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            transform.localScale = Vector3.one;
            _hasPlayedSpawnAnimation = true;

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Spawn animation complete!");
        }

        /// <summary>
        /// Play death animation: delay then scale 1→0
        /// Automatically called on Health.OnDeath event
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (_deathAnimCoroutine != null)
                StopCoroutine(_deathAnimCoroutine);

            _deathAnimCoroutine = StartCoroutine(DeathAnimationSequence());
        }

        private IEnumerator DeathAnimationSequence()
        {
            // Wait before scale down starts
            yield return new WaitForSeconds(deathScaleDelay);

            // Ensure we start at full scale
            transform.localScale = Vector3.one;

            // Scale down animation
            float elapsedTime = 0f;
            while (elapsedTime < deathScaleAnimDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / deathScaleAnimDuration);
                float scale = deathScaleCurve.Evaluate(progress);
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            transform.localScale = Vector3.zero;

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Death animation complete!");
        }

        #endregion
    }
}
