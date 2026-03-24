using UnityEngine;

namespace LittleHeroJourney
{
    public enum AudioPlaybackChannel
    {
        Sfx = 0,
        Bgm = 1
    }

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

        [Tooltip("Rotation in euler angles (degrees)")]
        public Vector3 rotationEuler = Vector3.zero;

        [Tooltip("Scale (1,1,1) = default size")]
        public Vector3 scale = Vector3.one;

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
        
        [Tooltip("Where this audio should be routed. Default: SFX")]
        public AudioPlaybackChannel playbackChannel = AudioPlaybackChannel.Sfx;

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

        [Tooltip("Rotation in euler angles (degrees)")]
        public Vector3 rotationEuler = Vector3.zero;

        [Tooltip("Scale (1,1,1) = default size")]
        public Vector3 scale = Vector3.one;

        [Tooltip("If true, effect follows character. If false, stays at spawn position")]
        public bool followCharacter = false;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && triggerTime >= 0f && triggerTime <= 1f;
    }

    /// <summary>
    /// When trail stops: shrink tail toward freeze position then hide, or freeze and fade out.
    /// </summary>
    public enum WeaponTrailStopMode
    {
        ShrinkThenHide,
        FreezeAndFadeOut
    }

    [System.Serializable]
    public class TrailEffectTiming
    {
        [Tooltip("Trail effect id (must match Trail.trailId on the weapon)")]
        public string effectName;

        [Tooltip("Normalized time window: x = when to start trail, y = when to stop trail (0-1)")]
        public Vector2 triggerWindow = new Vector2(0f, 0.8f);

        [Tooltip("When trail stops: shrink then hide, or freeze and fade out")]
        public WeaponTrailStopMode stopMode = WeaponTrailStopMode.ShrinkThenHide;

        [Tooltip("How long the frozen trail stays visible after stop (seconds). 0 = never auto-destroy.")]
        public float frozenTrailLifetime = 2f;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && triggerWindow.x >= 0f && triggerWindow.x <= 1f && triggerWindow.y >= 0f && triggerWindow.y <= 1f && triggerWindow.y >= triggerWindow.x;
    }
}
