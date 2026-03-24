using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        Single,
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

        [Tooltip("Main/first play clips (played once before optional loop section)")]
        [SerializeField]
        [FormerlySerializedAs("audioClips")]
        private List<AudioClipData> audioClips = new List<AudioClipData>();

        [Tooltip("Playback mode for first play clips")]
        public AudioPlayType playType = AudioPlayType.Random;

        [Tooltip("Spatial blend: 0 = 2D (volume same anywhere, e.g. BGM/UI), 1 = 3D (volume depends on distance, e.g. SFX world)")]
        [Range(0f, 1f)]
        public float spatialBlend = 0f;

        [Tooltip("Number of AudioSources to precreate for this effect (so it can play overlap / multiple instances)")]
        [Min(1)]
        public int poolSize = 3;

        [Tooltip("Legacy loop for first play clips (kept for backward compatibility). Prefer Loop Section below.")]
        public bool loop = false;

        [Tooltip("If true, use dedicated loop section after first play is finished")]
        [SerializeField] private bool useLoopSection = false;

        [System.Serializable]
        public class LoopSectionData
        {
            [Tooltip("Loop section clips")]
            [SerializeField] private List<AudioClipData> clips = new List<AudioClipData>();

            [Tooltip("Blend = play all clips together each cycle, Random = one random clip each cycle, Sequential = in order each cycle")]
            public AudioPlayType playType = AudioPlayType.Sequential;

            [Tooltip("Optional delay between loop cycles")]
            [Min(0f)] public float cycleDelay = 0f;

            public IReadOnlyList<AudioClipData> Clips => clips;
            public int ClipCount => clips.Count;

            public AudioClipData GetClip(int index)
            {
                if (index >= 0 && index < clips.Count) return clips[index];
                return null;
            }

            public AudioClipData GetRandomClip()
            {
                if (clips.Count == 0) return null;
                return clips[Random.Range(0, clips.Count)];
            }

            public AudioClipData GetClipSequential(int index)
            {
                if (clips.Count == 0) return null;
                return clips[index % clips.Count];
            }

            public bool IsValid => clips.Count > 0 && clips.TrueForAll(c => c != null && c.IsValid);
        }

        [SerializeField] private LoopSectionData loopSection = new LoopSectionData();

        public IReadOnlyList<AudioClipData> AudioClips => audioClips;
        public int ClipCount => audioClips.Count;
        public LoopSectionData LoopSection => loopSection;
        public bool UseLoopSection => useLoopSection;

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
        [SerializeField]
        private List<AudioEffectData> audioEffects = new List<AudioEffectData>();

        public IReadOnlyList<AudioEffectData> AudioEffects => audioEffects;

        public AudioEffectData GetAudioEffect(string effectName)
        {
            return audioEffects.Find(e => e.effectName == effectName);
        }
    }
}
