using System;
using UnityEngine;

namespace LittleHeroJourney
{
    public class SceneReadyAnchor : MonoBehaviour, ISceneLoadProgress
    {
        [SerializeField] private string sceneIdOverride;
        
        private int _requiredCount = 0;
        private int _readyCount = 0;
        private bool _explicitReady = false;
        
        public string SceneId => string.IsNullOrEmpty(sceneIdOverride) && GameManager.Instance != null && GameManager.Instance.SceneManager != null
            ? GameManager.Instance.SceneManager.CurrentId
            : sceneIdOverride;
        
        public bool IsReady => _explicitReady || (_requiredCount > 0 && _readyCount >= _requiredCount);
        public float Progress => _requiredCount == 0 ? (_explicitReady ? 1f : 0f) : Mathf.Clamp01((float)_readyCount / _requiredCount);
        public event Action OnReady;
        
        public void Register()
        {
            _requiredCount++;
        }
        
        public void MarkReady()
        {
            _readyCount++;
            if (IsReady) OnReady?.Invoke();
        }
        
        public void SetReady()
        {
            _explicitReady = true;
            OnReady?.Invoke();
        }
    }
}
