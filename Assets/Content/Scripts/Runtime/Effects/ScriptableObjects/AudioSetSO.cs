using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Single audio clip data with volume control
    /// </summary>
    [System.Serializable]
    public class AudioClipData
    {
        [Tooltip("Audio clip")]
        public AudioClip clip;

        [Tooltip("Volume for this specific clip (0-1)")]
        [Range(0f, 1f)]
        public float volume = 1f;

        public bool IsValid => clip != null;
    }

    /// <summary>
    /// How to play multiple audio clips
    /// </summary>
    public enum AudioPlayType
    {
        Blend,
        Random,
        Sequential 
    }

    /// <summary>
    /// Audio effect configuration with multiple clips and playback modes
    /// </summary>
    [System.Serializable]
    public class AudioEffectData
    {
        [Tooltip("Unique identifier for this audio effect")]
        public string effectName;

        [Tooltip("Audio clips to play")]
        [SerializeField]
        private List<AudioClipData> audioClips = new List<AudioClipData>();

        [Tooltip("Blend = play all clips simultaneously, Random = play one random clip, Sequential = play clips in order from first to last")]
        public AudioPlayType playType = AudioPlayType.Random;

        [Tooltip("Spatial blend: 0 = 2D (volume same anywhere, e.g. BGM/UI), 1 = 3D (volume depends on distance, e.g. SFX world)")]
        [Range(0f, 1f)]
        public float spatialBlend = 0f;

        [Tooltip("Number of AudioSources to precreate for this effect (so it can play overlap / multiple instances)")]
        [Min(1)]
        public int poolSize = 3;

        [Tooltip("Loop: Random = loop 1 clip that is chosen, Sequential = after last clip, loop from first clip. For BGM, turn on.")]
        public bool loop = false;

        public IReadOnlyList<AudioClipData> AudioClips => audioClips;
        public int ClipCount => audioClips.Count;

        public AudioClipData GetClip(int index)
        {
            if (index >= 0 && index < audioClips.Count)
                return audioClips[index];
            return null;
        }

        public AudioClipData GetRandomClip()
        {
            if (audioClips.Count == 0) return null;
            return audioClips[Random.Range(0, audioClips.Count)];
        }

        public AudioClipData GetClipSequential(int index)
        {
            if (audioClips.Count == 0) return null;
            return audioClips[index % audioClips.Count];
        }

        public bool IsValid => !string.IsNullOrEmpty(effectName) && audioClips.Count > 0 && audioClips.TrueForAll(c => c.IsValid);
    }

    /// <summary>
    /// Scriptable Object for storing advanced audio configurations
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSet", menuName = "Little Hero Journey/Audio/Audio Set", order = 1)]
    public class AudioSetSO : ScriptableObject
    {
        [Header("Audio Effects")]
        [SerializeField]
        private List<AudioEffectData> audioEffects = new List<AudioEffectData>();

        public IReadOnlyList<AudioEffectData> AudioEffects => audioEffects;

        public AudioEffectData GetAudioEffect(string effectName)
        {
            return audioEffects.Find(e => e.effectName == effectName);
        }
    }
}
