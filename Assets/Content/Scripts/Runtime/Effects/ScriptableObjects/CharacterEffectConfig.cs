using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Character effect configuration - spawn and death effects (works for player and enemies)
    /// </summary>
    [System.Serializable]
    public class CharacterEffectConfig
    {
        [Header("Spawn Effects")]
        [Tooltip("VFX effect name for spawn (from VFXSet)")]
        public string spawnVFXName = "";

        [Tooltip("Audio effect name for spawn (from AudioSet)")]
        public string spawnAudioName = "";

        [Tooltip("Particle effect name for spawn (from ParticleSet)")]
        public string spawnParticleName = "";

        [Header("Death Effects")]
        [Tooltip("VFX effect name for death (from VFXSet)")]
        public string deathVFXName = "";

        [Tooltip("Audio effect name for death (from AudioSet)")]
        public string deathAudioName = "";

        [Tooltip("Particle effect name for death (from ParticleSet)")]
        public string deathParticleName = "";

        [Header("Effect Delay")]
        [Tooltip("Delay before spawning effects (in seconds)")]
        [Min(0f)]
        public float spawnEffectDelay = 0f;

        [Tooltip("Delay before death effects (in seconds)")]
        [Min(0f)]
        public float deathEffectDelay = 0f;
    }
}
