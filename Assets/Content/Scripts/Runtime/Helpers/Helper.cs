using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace LittleHeroJourney
{
    public static class Helper
    {
        #region Combat / Stats

        public static float ApplyPercentReduction(float value, float stat, float minResult = 0f)
        {
            if (stat <= 0f) return value;
            float r = stat / (stat + 100f);
            return Mathf.Max(minResult, value * (1f - r));
        }

        public static float ClampAdd(float current, float delta, float minVal, float maxVal)
        {
            return Mathf.Clamp(current + delta, minVal, maxVal);
        }

        public static float ClampSub(float current, float delta, float minVal = 0f)
        {
            return Mathf.Max(minVal, current - delta);
        }

        public static Health TryGetEnemyHealth(Collider c, Transform weaponRoot, DamageData damageData)
        {
            if (c == null || weaponRoot == null) return null;
            if (c.gameObject == weaponRoot.gameObject) return null;
            Health h = c.GetComponent<Health>();
            if (h == null) return null;
            if (damageData != null && (damageData.canDamageFactions & h.ObjectFaction) == 0) return null;
            return h;
        }

        #endregion

        #region DOTween

        public static float GetTweenEffectiveDuration(DOTweenAnimation anim, bool useCustomDuration, float customDuration)
        {
            if (anim == null) return 0f;
            float duration = useCustomDuration ? customDuration : anim.duration;
            float delay = anim.delay;
            if (delay < 0f) delay = 0f;
            return Mathf.Max(0f, duration + delay);
        }

        public static float GetSequenceTotalDuration(IList<DOTweenAnimation> animations, bool sequential, bool useCustomDuration, float customDuration)
        {
            if (animations == null) return 0f;
            float total = 0f;
            float max = 0f;
            for (int i = 0; i < animations.Count; i++)
            {
                float dur = GetTweenEffectiveDuration(animations[i], useCustomDuration, customDuration);
                if (sequential)
                {
                    total += dur;
                }
                else
                {
                    if (dur > max) max = dur;
                }
            }
            return sequential ? total : max;
        }

        #endregion

        #region Curves

        public static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        }

        #endregion

        #region Knockback

        public static void GetKnockbackTarget(Vector3 start, Vector3 direction, float distance, out Vector3 target, out bool hitObstacle, int layerMask = ~0)
        {
            target = start + direction * distance;
            hitObstacle = false;
            RaycastHit hit;
            if (Physics.Raycast(start, direction, out hit, distance, layerMask, QueryTriggerInteraction.Ignore))
            {
                target = hit.point - direction * 0.5f;
                hitObstacle = true;
            }
        }

        #endregion

        #region Target Finding (Zero-Allocation)

        private static Collider[] _cachedColliders = new Collider[32];

        private static int GetOverlapHits(Vector3 position, float range, int layers)
        {
            return Physics.OverlapSphereNonAlloc(position, range, _cachedColliders, layers);
        }

        /// <summary>
        /// Find nearest target from position within range (zero-allocation)
        /// </summary>
        public static Transform FindNearestTarget(Vector3 searchPosition,
            float detectionRange, int targetLayers, out float distance)
        {
            int hits = GetOverlapHits(searchPosition, detectionRange, targetLayers);
            Transform nearest = null;
            distance = float.MaxValue;
            for (int i = 0; i < hits; i++)
            {
                float d = Vector3.Distance(searchPosition, _cachedColliders[i].transform.position);
                if (d < distance) { distance = d; nearest = _cachedColliders[i].transform; }
            }
            return nearest;
        }

        /// <summary>
        /// Find nearest alive target (checks Health.IsAlive)
        /// </summary>
        public static Health FindNearestAliveTarget(Vector3 searchPosition,
            float detectionRange, int targetLayers, out float distance)
        {
            int hits = GetOverlapHits(searchPosition, detectionRange, targetLayers);
            Health nearestHealth = null;
            distance = float.MaxValue;
            for (int i = 0; i < hits; i++)
            {
                Health h = _cachedColliders[i].GetComponent<Health>();
                if (h == null || !h.IsAlive) continue;
                float d = Vector3.Distance(searchPosition, _cachedColliders[i].transform.position);
                if (d < distance) { distance = d; nearestHealth = h; }
            }
            return nearestHealth;
        }

        /// <summary>
        /// Find all targets within range (populates list, no new allocation)
        /// </summary>
        public static int FindAllTargets(Vector3 searchPosition,
            float detectionRange, int targetLayers, List<Transform> resultList)
        {
            int hits = GetOverlapHits(searchPosition, detectionRange, targetLayers);
            resultList.Clear();
            for (int i = 0; i < hits; i++) resultList.Add(_cachedColliders[i].transform);
            return hits;
        }

        /// <summary>
        /// Find all targets with Health component that are alive
        /// </summary>
        public static int FindAliveTargets(Vector3 searchPosition,
            float detectionRange, int targetLayers, List<Health> resultList)
        {
            int hits = GetOverlapHits(searchPosition, detectionRange, targetLayers);
            resultList.Clear();
            for (int i = 0; i < hits; i++)
            {
                Health h = _cachedColliders[i].GetComponent<Health>();
                if (h != null && h.IsAlive) resultList.Add(h);
            }
            return hits;
        }

        /// <summary>
        /// Check if target exists and is valid (not null, has Health, is alive)
        /// </summary>
        public static bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            Health health = target.GetComponent<Health>();
            return health != null && health.IsAlive;
        }

        #endregion

        #region Animator Helpers

        /// <summary>
        /// Safely get and cache Animator component
        /// Tries: current GameObject → children → parent
        /// Can optionally filter out animators on specific layers
        /// </summary>
        public static Animator GetAndCacheAnimator(MonoBehaviour component,
            bool searchInChildren = true, bool showDebugLog = false, string ignoreLayerName = "UI")
        {
            if (component == null)
            {
                if (showDebugLog) Debug.LogWarning("[Helper] Component source is null!");
                return null;
            }

            // Try current GameObject first
            Animator animator = component.GetComponent<Animator>();
            if (animator != null && ShouldIgnoreAnimator(animator, ignoreLayerName))
            {
                animator = null;
            }
            
            // If not found, try children (most common: animator on child)
            if (animator == null && searchInChildren)
            {
                Animator[] animators = component.GetComponentsInChildren<Animator>();
                foreach (Animator anim in animators)
                {
                    if (!ShouldIgnoreAnimator(anim, ignoreLayerName))
                    {
                        animator = anim;
                        break;
                    }
                }
            }

            // If still not found, try parent (fallback)
            if (animator == null)
            {
                Animator[] animators = component.GetComponentsInParent<Animator>();
                foreach (Animator anim in animators)
                {
                    if (!ShouldIgnoreAnimator(anim, ignoreLayerName))
                    {
                        animator = anim;
                        break;
                    }
                }
            }

            if (animator == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{component.GetType().Name}] Animator not found on GameObject, children, or parent (ignoring layer '{ignoreLayerName}')!");
                return null;
            }

            return animator;
        }

        /// <summary>
        /// Check if animator should be ignored based on layer name
        /// Returns true if should be ignored, false if should be used
        /// </summary>
        private static bool ShouldIgnoreAnimator(Animator animator, string ignoreLayerName)
        {
            if (animator == null) return true;
            if (string.IsNullOrEmpty(ignoreLayerName)) return false;
            
            return animator.gameObject.layer == LayerMask.NameToLayer(ignoreLayerName);
        }

        /// <summary>
        /// Generic helper to get component with fallback search: current → children → parent
        /// Useful for when components are on different GameObjects in hierarchy
        /// </summary>
        public static T GetComponentWithFallback<T>(MonoBehaviour component, 
            bool searchChildren = true, bool showDebugLog = false) where T : Component
        {
            if (component == null)
            {
                if (showDebugLog) Debug.LogWarning("[Helper] Component source is null!");
                return null;
            }

            // Try current GameObject first
            T result = component.GetComponent<T>();
            
            // If not found, try children
            if (result == null && searchChildren)
            {
                result = component.GetComponentInChildren<T>();
            }

            // If still not found, try parent (fallback)
            if (result == null)
            {
                result = component.GetComponentInParent<T>();
            }

            if (result == null && showDebugLog)
            {
                Debug.LogWarning($"[{component.GetType().Name}] {typeof(T).Name} not found on GameObject, children, or parent!");
            }

            return result;
        }

        /// <summary>
        /// Validate animator has a specific parameter with correct type
        /// </summary>
        public static bool ValidateAnimatorParameter(Animator animator,
            string parameterName, AnimatorControllerParameterType expectedType)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
                return false;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == parameterName && param.type == expectedType)
                    return true;
            }

            Debug.LogWarning($"[Helper] Parameter '{parameterName}' ({expectedType}) not found in {animator.gameObject.name}");
            return false;
        }

        /// <summary>
        /// Batch validate multiple parameters
        /// </summary>
        public static bool ValidateAnimatorParameters(Animator animator,
            params (string name, AnimatorControllerParameterType type)[] parameters)
        {
            if (animator == null) return false;

            bool allValid = true;
            foreach (var param in parameters)
            {
                if (!ValidateAnimatorParameter(animator, param.name, param.type))
                    allValid = false;
            }

            return allValid;
        }

        /// <summary>
        /// Check if animator has a parameter (any type)
        /// </summary>
        public static bool HasAnimatorParameter(Animator animator, string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
                return false;

            return animator.parameters.Any(p => p.name == parameterName);
        }

        /// <summary>
        /// Check if animator is currently in an attack state (by clip name or state name).
        /// Shared by PlayerCombat, AICombat, and Weapon to avoid duplication.
        /// </summary>
        public static bool IsInAttackState(Animator animator, int layerIndex = 0)
        {
            if (animator == null) return false;
            try
            {
                var clipInfo = animator.GetCurrentAnimatorClipInfo(layerIndex);
                if (clipInfo != null && clipInfo.Length > 0)
                {
                    string clipName = clipInfo[0].clip.name;
                    return clipName.Contains("attack", System.StringComparison.OrdinalIgnoreCase) ||
                           clipName.Contains("swing", System.StringComparison.OrdinalIgnoreCase) ||
                           clipName.Contains("slash", System.StringComparison.OrdinalIgnoreCase);
                }
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                return stateInfo.IsName("Attack") || stateInfo.IsName("Attack1") || stateInfo.IsName("Attack2");
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNextStateAttack(Animator animator, int layerIndex = 0)
        {
            if (animator == null || !animator.IsInTransition(layerIndex)) return false;
            try
            {
                var clipInfo = animator.GetNextAnimatorClipInfo(layerIndex);
                if (clipInfo != null && clipInfo.Length > 0)
                {
                    string clipName = clipInfo[0].clip.name;
                    return clipName.Contains("attack", System.StringComparison.OrdinalIgnoreCase) ||
                           clipName.Contains("swing", System.StringComparison.OrdinalIgnoreCase) ||
                           clipName.Contains("slash", System.StringComparison.OrdinalIgnoreCase);
                }
                var stateInfo = animator.GetNextAnimatorStateInfo(layerIndex);
                return stateInfo.IsName("Attack") || stateInfo.IsName("Attack1") || stateInfo.IsName("Attack2");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current animation state length
        /// </summary>
        public static float GetCurrentAnimationLength(Animator animator, int layerIndex = 0)
        {
            if (animator == null) return 0.5f;

            try
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                float remainingTime = stateInfo.length * (1f - stateInfo.normalizedTime);
                return Mathf.Max(0.1f, remainingTime);
            }
            catch
            {
                return 0.5f;
            }
        }

        #endregion

        #region Component Caching

        /// <summary>
        /// Get and cache a component with safety checks
        /// </summary>
        public static T GetAndCacheComponent<T>(MonoBehaviour component, bool searchInParent = false) where T : MonoBehaviour
        {
            if (component == null)
            {
                Debug.LogWarning("[Helper] Component source is null!");
                return null;
            }

            T cachedComponent = searchInParent
                ? component.GetComponentInParent<T>()
                : component.GetComponent<T>();

            if (cachedComponent == null)
            {
                Debug.LogWarning($"[{component.GetType().Name}] {typeof(T).Name} not found!");
            }

            return cachedComponent;
        }

        /// <summary>
        /// Cache 2 companion components (on same object)
        /// </summary>
        public static void CacheCompanionComponents<T1, T2>(MonoBehaviour source,
            out T1 comp1, out T2 comp2)
            where T1 : MonoBehaviour
            where T2 : MonoBehaviour
        {
            comp1 = source.GetComponent<T1>();
            comp2 = source.GetComponent<T2>();

            if (comp1 == null) Debug.LogWarning($"[{source.GetType().Name}] {typeof(T1).Name} not found!");
            if (comp2 == null) Debug.LogWarning($"[{source.GetType().Name}] {typeof(T2).Name} not found!");
        }

        /// <summary>
        /// Cache 3 companion components (Health uses this pattern)
        /// </summary>
        public static void CacheCompanionComponents<T1, T2, T3>(MonoBehaviour source,
            out T1 comp1, out T2 comp2, out T3 comp3)
            where T1 : MonoBehaviour
            where T2 : MonoBehaviour
            where T3 : MonoBehaviour
        {
            comp1 = source.GetComponent<T1>();
            comp2 = source.GetComponent<T2>();
            comp3 = source.GetComponent<T3>();

            if (comp1 == null) Debug.LogWarning($"[{source.GetType().Name}] {typeof(T1).Name} not found!");
            if (comp2 == null) Debug.LogWarning($"[{source.GetType().Name}] {typeof(T2).Name} not found!");
            if (comp3 == null) Debug.LogWarning($"[{source.GetType().Name}] {typeof(T3).Name} not found!");
        }

        /// <summary>
        /// Cache 3 companion components silently (no warnings - useful for optional components)
        /// </summary>
        public static void CacheCompanionComponentsSilent<T1, T2, T3>(MonoBehaviour source,
            out T1 comp1, out T2 comp2, out T3 comp3)
            where T1 : MonoBehaviour
            where T2 : MonoBehaviour
            where T3 : MonoBehaviour
        {
            comp1 = source.GetComponent<T1>();
            comp2 = source.GetComponent<T2>();
            comp3 = source.GetComponent<T3>();
        }

        #endregion
    }
}
