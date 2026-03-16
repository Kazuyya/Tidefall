using System.Collections;
using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Animated barrier wall effect untuk encounter zone
    /// </summary>
    public class WallEncounterFX : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Scale animation duration (seconds)")]
        [SerializeField] public float animationDuration = 0.5f;
        
        [Tooltip("Ease type for animation")]
        [SerializeField] public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Coroutine _currentAnimation;
        private Vector3 _originalScale;
        
        private void Awake()
        {
            _originalScale = transform.localScale;
            
            // Set initial scale ke 0 untuk Y
            Vector3 startScale = _originalScale;
            startScale.y = 0f;
            transform.localScale = startScale;
        }
        
        /// <summary>
        /// Enable wall dengan animasi scale Y dari 0 ke 1
        /// </summary>
        public void Enable()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            
            _currentAnimation = StartCoroutine(EnableCoroutine());
        }
        
        /// <summary>
        /// Disable wall dengan animasi scale Y dari 1 ke 0
        /// </summary>
        public void Disable()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            
            _currentAnimation = StartCoroutine(DisableCoroutine());
        }
        
        private IEnumerator EnableCoroutine()
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = _originalScale;
            targetScale.y = _originalScale.y;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / animationDuration);
                float easedT = easeCurve.Evaluate(t);
                
                Vector3 newScale = transform.localScale;
                newScale.y = Mathf.Lerp(0f, _originalScale.y, easedT);
                transform.localScale = newScale;
                
                yield return null;
            }
            
            // Ensure final scale
            Vector3 finalScale = transform.localScale;
            finalScale.y = _originalScale.y;
            transform.localScale = finalScale;
        }
        
        private IEnumerator DisableCoroutine()
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / animationDuration);
                float easedT = easeCurve.Evaluate(t);
                
                Vector3 newScale = transform.localScale;
                newScale.y = Mathf.Lerp(_originalScale.y, 0f, easedT);
                transform.localScale = newScale;
                
                yield return null;
            }
            
            // Ensure final scale
            Vector3 finalScale = transform.localScale;
            finalScale.y = 0f;
            transform.localScale = finalScale;
        }
    }
}
