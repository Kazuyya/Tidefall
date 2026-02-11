using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// VFX effect timing - references name from VFXSetSO
    /// </summary>
    [System.Serializable]
    public class VFXEffectTiming
    {
        [Tooltip("VFX effect name (must exist in VFXSetSO)")]
        public string effectName;

        [Range(0f, 1f)]
        [Tooltip("When to trigger this effect (normalized time 0-1)")]
        public float triggerTime = 0.5f;

        [Tooltip("Position offset from character (x, y, z)")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("If true, effect follows character. If false, stays at spawn position")]
        public bool followCharacter = false;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && triggerTime >= 0f && triggerTime <= 1f;
    }

    /// <summary>
    /// Audio effect timing - references name from AudioSetSO
    /// </summary>
    [System.Serializable]
    public class AudioEffectTiming
    {
        [Tooltip("Audio effect name (must exist in AudioSetSO)")]
        public string effectName;

        [Range(0f, 1f)]
        [Tooltip("When to trigger this effect (normalized time 0-1)")]
        public float triggerTime = 0.5f;

        [Tooltip("Position offset from character (x, y, z)")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("If true, effect follows character. If false, stays at spawn position")]
        public bool followCharacter = false;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && triggerTime >= 0f && triggerTime <= 1f;
    }

    /// <summary>
    /// Particle effect timing - references name from ParticleSetSO
    /// </summary>
    [System.Serializable]
    public class ParticleEffectTiming
    {
        [Tooltip("Particle effect name (must exist in ParticleSetSO)")]
        public string effectName;

        [Range(0f, 1f)]
        [Tooltip("When to trigger this effect (normalized time 0-1)")]
        public float triggerTime = 0.5f;

        [Tooltip("Position offset from character (x, y, z)")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("If true, effect follows character. If false, stays at spawn position")]
        public bool followCharacter = false;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && triggerTime >= 0f && triggerTime <= 1f;
    }
}
