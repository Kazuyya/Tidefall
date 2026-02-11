using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Particle system effect configuration with pooling support
    /// </summary>
    [System.Serializable]
    public class ParticleEffectData
    {
        [Tooltip("Unique identifier for this particle effect")]
        public string effectName;

        [Tooltip("Particle system prefab to instantiate")]
        public ParticleSystem particlePrefab;

        [Tooltip("Pool size for this effect")]
        [Min(1)]
        public int poolSize = 5;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && particlePrefab != null;
    }

    /// <summary>
    /// Scriptable Object for storing Particle effect configurations
    /// </summary>
    [CreateAssetMenu(fileName = "ParticleSet", menuName = "Little Hero Journey/Particle/Particle Set", order = 1)]
    public class ParticleSetSO : ScriptableObject
    {
        [Header("Particle Effects")]
        [SerializeField]
        private List<ParticleEffectData> particleEffects = new List<ParticleEffectData>();

        public IReadOnlyList<ParticleEffectData> ParticleEffects => particleEffects;

        public ParticleEffectData GetParticleEffect(string effectName)
        {
            return particleEffects.Find(e => e.effectName == effectName);
        }
    }
}
