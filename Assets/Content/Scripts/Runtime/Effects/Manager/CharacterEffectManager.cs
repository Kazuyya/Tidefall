using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Tiny;

namespace LittleHeroJourney
{
    /// <summary>
    /// Manages pooling and playback of character spawn/death effects (VFX, Audio, Particles)
    /// Works for all characters (Player, AI, Bosses, etc.)
    /// Singleton pattern for global access
    /// </summary>
    public class CharacterEffectManager : MonoBehaviour
    {
        #region Singleton

        private static CharacterEffectManager _instance;
        public static CharacterEffectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CharacterEffectManager>();
                    if (_instance == null)
                    {
                        GameObject managerObj = new GameObject("CharacterEffectManager");
                        _instance = managerObj.AddComponent<CharacterEffectManager>();
                        DontDestroyOnLoad(managerObj);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        [SerializeField]
        private AudioSetSO audioSet;

        [SerializeField]
        private VFXSetSO vfxSet;

        [SerializeField]
        private ParticleSetSO particleSet;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugLog = false;

        // Object pools
        private Dictionary<string, Queue<VisualEffect>> _vfxPool = new Dictionary<string, Queue<VisualEffect>>();
        private Dictionary<string, Queue<AudioSource>> _audioPool = new Dictionary<string, Queue<AudioSource>>();
        private Dictionary<string, Queue<ParticleSystem>> _particlePool = new Dictionary<string, Queue<ParticleSystem>>();

        // Container for pooled objects
        private Transform _poolContainer;

        // Active effects (for tracking and cleanup)
        private List<VisualEffect> _activeVFX = new List<VisualEffect>();
        private List<AudioSource> _activeAudio = new List<AudioSource>();
        private List<ParticleSystem> _activeParticles = new List<ParticleSystem>();

        // When followCharacter: sync position/rotation/scale from parent every frame (guarantees all three follow)
        private struct FollowData
        {
            public Transform Parent;
            public Vector3 PosOffset;
            public Vector3 RotEuler;
            public Vector3 Scale;
        }
        private Dictionary<Transform, FollowData> _followTransforms = new Dictionary<Transform, FollowData>();

        private Dictionary<string, Trail> _trailRegistry = new Dictionary<string, Trail>();

        private AudioSource _bgmSource;
        private AudioSource _bgmSecondarySource;
        private string _bgmEffectName;
        private Coroutine _bgmSequentialCoroutine;
        private Coroutine _bgmFadeCoroutine;
        private readonly List<AudioSource> _bgmBlendSources = new List<AudioSource>();
        private int _bgmFirstPlaySequentialIndex;
        private int _bgmLoopSequentialIndex;
        private const double BgmSeamlessPreRollSeconds = 0.2d;
        private readonly Dictionary<AudioSource, float> _audioBaseVolumes = new Dictionary<AudioSource, float>();
        private readonly HashSet<AudioSource> _bgmTrackedSources = new HashSet<AudioSource>();

        #endregion

        #region Lifecycle
        private readonly List<Transform> _followToRemove = new List<Transform>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Create pool container
            GameObject containerObj = new GameObject("EffectPool");
            containerObj.transform.SetParent(transform);
            _poolContainer = containerObj.transform;

            // Initialize pools
            InitializePools();
            _ = SettingsManager.Instance;
            ApplyAudioSettingsToAllSources();
        }

        private void OnEnable()
        {
            SettingsManager.AudioSettingsChanged += HandleAudioSettingsChanged;
        }

        private void OnDisable()
        {
            SettingsManager.AudioSettingsChanged -= HandleAudioSettingsChanged;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            SettingsManager.AudioSettingsChanged -= HandleAudioSettingsChanged;
        }

        private void LateUpdate()
        {
            if (_followTransforms.Count == 0) return;
            Vector3 poolScale = _poolContainer != null ? _poolContainer.lossyScale : Vector3.one;
            _followToRemove.Clear();
            foreach (var kv in _followTransforms)
            {
                Transform t = kv.Key;
                if (t == null || !t.gameObject.activeInHierarchy)
                {
                    _followToRemove.Add(t);
                    continue;
                }
                FollowData d = kv.Value;
                if (d.Parent == null)
                {
                    _followToRemove.Add(t);
                    continue;
                }
                t.position = d.Parent.position + d.Parent.rotation * d.PosOffset;
                t.rotation = d.Parent.rotation * Quaternion.Euler(d.RotEuler);
                float sx = (poolScale.x > 0.0001f) ? (d.Scale.x * d.Parent.lossyScale.x / poolScale.x) : d.Scale.x;
                float sy = (poolScale.y > 0.0001f) ? (d.Scale.y * d.Parent.lossyScale.y / poolScale.y) : d.Scale.y;
                float sz = (poolScale.z > 0.0001f) ? (d.Scale.z * d.Parent.lossyScale.z / poolScale.z) : d.Scale.z;
                t.localScale = new Vector3(sx, sy, sz);
            }
            for (int i = 0; i < _followToRemove.Count; i++)
                _followTransforms.Remove(_followToRemove[i]);
        }

        #endregion

        #region Initialization

        private void InitializePools()
        {
            // Initialize Audio pools (from AudioSet)
            if (audioSet != null)
            {
                foreach (var audioData in audioSet.AudioEffects)
                {
                    if (!audioData.IsValid) continue;

                    Queue<AudioSource> pool = new Queue<AudioSource>();
                    _audioPool[audioData.effectName] = pool;

                    for (int i = 0; i < audioData.poolSize; i++)
                    {
                        GameObject audioObj = new GameObject($"AudioSource_{audioData.effectName}_{i}");
                        audioObj.transform.SetParent(_poolContainer);
                        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                        audioSource.spatialBlend = audioData.spatialBlend;
                        audioObj.SetActive(false);
                        pool.Enqueue(audioSource);
                    }

                    if (showDebugLog)
                        Debug.Log($"[{GetType().Name}] Created Audio pool '{audioData.effectName}' with size {audioData.poolSize}");
                }
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] No AudioSetSO assigned!");
            }

            // Initialize VFX pools (from VFXSet)
            if (vfxSet != null)
            {
                foreach (var vfxData in vfxSet.VFXEffects)
                {
                    if (!vfxData.IsValid) continue;

                    Queue<VisualEffect> pool = new Queue<VisualEffect>();
                    _vfxPool[vfxData.effectName] = pool;

                    for (int i = 0; i < vfxData.poolSize; i++)
                    {
                        VisualEffect vfx = Instantiate(vfxData.vfxPrefab, _poolContainer);
                        vfx.gameObject.SetActive(false);
                        pool.Enqueue(vfx);
                    }

                    if (showDebugLog)
                        Debug.Log($"[{GetType().Name}] Created VFX pool '{vfxData.effectName}' with size {vfxData.poolSize}");
                }
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] No VFXSetSO assigned!");
            }

            // Initialize Particle pools (from ParticleSet)
            if (particleSet != null)
            {
                foreach (var particleData in particleSet.ParticleEffects)
                {
                    if (!particleData.IsValid) continue;

                    Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
                    _particlePool[particleData.effectName] = pool;

                    for (int i = 0; i < particleData.poolSize; i++)
                    {
                        ParticleSystem particle = Instantiate(particleData.particlePrefab, _poolContainer);
                        particle.gameObject.SetActive(false);
                        pool.Enqueue(particle);
                    }

                    if (showDebugLog)
                        Debug.Log($"[{GetType().Name}] Created Particle pool '{particleData.effectName}' with size {particleData.poolSize}");
                }
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] No ParticleSetSO assigned!");
            }
        }

        /// <summary>
        /// Initialize VFX pool for a specific effect
        /// </summary>
        public void InitializeVFXPool(string effectName, VisualEffect vfxPrefab, int poolSize)
        {
            if (_vfxPool.ContainsKey(effectName))
                return; // Already initialized

            Queue<VisualEffect> pool = new Queue<VisualEffect>();
            _vfxPool[effectName] = pool;

            for (int i = 0; i < poolSize; i++)
            {
                VisualEffect vfx = Instantiate(vfxPrefab, _poolContainer);
                vfx.gameObject.SetActive(false);
                pool.Enqueue(vfx);
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Created VFX pool '{effectName}' with size {poolSize}");
        }

        /// <summary>
        /// Initialize Particle pool for a specific effect
        /// </summary>
        public void InitializeParticlePool(string effectName, ParticleSystem particlePrefab, int poolSize)
        {
            if (_particlePool.ContainsKey(effectName))
                return; // Already initialized

            Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
            _particlePool[effectName] = pool;

            for (int i = 0; i < poolSize; i++)
            {
                ParticleSystem particle = Instantiate(particlePrefab, _poolContainer);
                particle.gameObject.SetActive(false);
                pool.Enqueue(particle);
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Created Particle pool '{effectName}' with size {poolSize}");
        }

        #endregion

        private void ApplyEffectTransform(Transform t, Vector3 position, Quaternion rotation, Vector3 positionOffset, Vector3 scale, Transform parentTransform, bool followCharacter)
        {
            t.SetParent(_poolContainer);
            Vector3 worldOffset = parentTransform != null ? parentTransform.rotation * positionOffset : positionOffset;
            t.position = position + worldOffset;
            t.rotation = rotation;
            t.localScale = followCharacter && parentTransform != null
                ? new Vector3(scale.x * parentTransform.lossyScale.x, scale.y * parentTransform.lossyScale.y, scale.z * parentTransform.lossyScale.z)
                : scale;
        }

        private void RegisterFollow(Transform effectTransform, Transform parent, Vector3 posOffset, Vector3 rotEuler, Vector3 scale)
        {
            if (effectTransform == null || parent == null) return;
            _followTransforms[effectTransform] = new FollowData
            {
                Parent = parent,
                PosOffset = posOffset,
                RotEuler = rotEuler,
                Scale = scale
            };
        }

        private void UnregisterFollow(Transform effectTransform)
        {
            if (effectTransform != null) _followTransforms.Remove(effectTransform);
        }

        #region VFX Methods

        /// <summary>
        /// Play a VFX effect at target position
        /// </summary>
        public void PlayVFX(string effectName, Vector3 position, Quaternion rotation = default, Vector3 positionOffset = default, Vector3 scale = default, Transform parentTransform = null, bool followCharacter = false, Vector3 rotationEulerForFollow = default)
        {
            if (string.IsNullOrEmpty(effectName) || vfxSet == null) return;
            if (scale == default) scale = Vector3.one;

            VFXEffectData vfxData = vfxSet.GetVFXEffect(effectName);
            if (vfxData == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] VFX effect '{effectName}' not found!");
                return;
            }

            VisualEffect vfx = GetVFXFromPool(effectName, vfxData.vfxPrefab);
            if (vfx == null) return;

            ApplyEffectTransform(vfx.transform, position, rotation, positionOffset, scale, parentTransform, followCharacter);
            if (followCharacter && parentTransform != null)
                RegisterFollow(vfx.transform, parentTransform, positionOffset, rotationEulerForFollow, scale);
            vfx.gameObject.SetActive(true);
            vfx.Reinit();
            vfx.Play();

            _activeVFX.Add(vfx);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Playing VFX '{effectName}' at {position + positionOffset} (Follow: {followCharacter})");

            StartCoroutine(ReturnVFXToPoolAfterDuration(vfx, effectName));
        }

        private VisualEffect GetVFXFromPool(string effectName, VisualEffect prefab = null)
        {
            if (!_vfxPool.ContainsKey(effectName))
            {
                if (prefab != null)
                {
                    InitializeVFXPool(effectName, prefab, 5);
                }
                else
                {
                    if (showDebugLog)
                        Debug.LogWarning($"[{GetType().Name}] VFX pool '{effectName}' doesn't exist!");
                    return null;
                }
            }

            Queue<VisualEffect> pool = _vfxPool[effectName];
            VisualEffect vfx;

            if (pool.Count > 0)
            {
                vfx = pool.Dequeue();
            }
            else
            {
                // Expand pool if needed
                if (prefab != null)
                {
                    vfx = Instantiate(prefab, _poolContainer);
                }
                else
                {
                    return null;
                }
            }

            return vfx;
        }

        private void ReturnVFXToPool(string effectName, VisualEffect vfx)
        {
            if (vfx == null) return;

            UnregisterFollow(vfx.transform);
            vfx.gameObject.SetActive(false);
            vfx.transform.SetParent(_poolContainer);
            _activeVFX.Remove(vfx);

            if (!_vfxPool.ContainsKey(effectName))
                _vfxPool[effectName] = new Queue<VisualEffect>();

            _vfxPool[effectName].Enqueue(vfx);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Returned VFX '{effectName}' to pool");
        }

        private IEnumerator ReturnVFXToPoolAfterDuration(VisualEffect vfx, string effectName)
        {
            // Wait for VFX to finish
            yield return new WaitForSeconds(2f); // Default timeout, adjust as needed

            ReturnVFXToPool(effectName, vfx);
        }

        #endregion

        private void HandleAudioSettingsChanged()
        {
            ApplyAudioSettingsToAllSources();
        }

        private float GetSfxVolume(float baseVolume)
        {
            float master = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1f;
            float sfx = SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume : 1f;
            return Mathf.Clamp01(baseVolume) * master * sfx;
        }

        private float GetBgmVolume(float baseVolume)
        {
            float master = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1f;
            float bgm = SettingsManager.Instance != null ? SettingsManager.Instance.BgmVolume : 1f;
            return Mathf.Clamp01(baseVolume) * master * bgm;
        }

        private void TrackAudioSource(AudioSource source, float baseVolume, bool isBgm)
        {
            if (source == null) return;
            _audioBaseVolumes[source] = Mathf.Clamp01(baseVolume);
            if (isBgm) _bgmTrackedSources.Add(source);
            else _bgmTrackedSources.Remove(source);
            ApplyVolumeForSource(source);
        }

        private void UntrackAudioSource(AudioSource source)
        {
            if (source == null) return;
            _audioBaseVolumes.Remove(source);
            _bgmTrackedSources.Remove(source);
        }

        private void ApplyVolumeForSource(AudioSource source)
        {
            if (source == null) return;
            float baseVolume = _audioBaseVolumes.TryGetValue(source, out float v) ? v : source.volume;
            source.volume = _bgmTrackedSources.Contains(source) ? GetBgmVolume(baseVolume) : GetSfxVolume(baseVolume);
        }

        private void ApplyAudioSettingsToAllSources()
        {
            if (_audioBaseVolumes.Count == 0) return;
            var sources = new List<AudioSource>(_audioBaseVolumes.Keys);
            for (int i = 0; i < sources.Count; i++)
                ApplyVolumeForSource(sources[i]);
        }

        #region Audio Methods

        /// <summary>
        /// Play audio effect at target position
        /// Supports Blend (all clips together) or Random (one random clip)
        /// </summary>
        public void PlayAudio(string effectName, Vector3 position)
        {
            if (string.IsNullOrEmpty(effectName) || audioSet == null) return;

            AudioEffectData audioData = audioSet.GetAudioEffect(effectName);
            if (audioData == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] Audio effect '{effectName}' not found!");
                return;
            }

            if (audioData.playType == AudioPlayType.Blend) PlayAudioBlend(audioData, position);
            else if (audioData.playType == AudioPlayType.Random) PlayAudioRandom(audioData, position);
            else if (audioData.playType == AudioPlayType.Sequential) PlayAudioSequential(audioData, position);
            else PlayAudioSingle(audioData, position);
        }

        /// <summary>
        /// Play all audio clips from the effect simultaneously (Blend mode)
        /// </summary>
        private void PlayAudioBlend(AudioEffectData audioData, Vector3 position)
        {
            float maxDuration = 0f;
            int clipCount = audioData.ClipCount;

            for (int i = 0; i < clipCount; i++)
            {
                AudioClipData clipData = audioData.GetClip(i);
                if (clipData?.clip == null) continue;

                AudioSource audioSource = GetAudioFromPool(audioData.effectName);
                if (audioSource == null) continue;

                audioSource.clip = clipData.clip;
                audioSource.loop = false;
                audioSource.transform.position = position;
                audioSource.gameObject.SetActive(true);
                TrackAudioSource(audioSource, clipData.volume, false);
                audioSource.Play();

                _activeAudio.Add(audioSource);

                float duration = clipData.clip.length;
                if (duration > maxDuration)
                    maxDuration = duration;

                if (showDebugLog)
                    Debug.Log($"[{GetType().Name}] Playing Audio (Blend) '{audioData.effectName}' - clip {i} at {position}");
            }

            // Return to pool after all clips finish
            if (maxDuration > 0f)
                StartCoroutine(ReturnAudioToPoolAfterDuration(audioData.effectName, maxDuration));
        }

        /// <summary>
        /// Play one random audio clip from the effect (Random mode)
        /// </summary>
        private void PlayAudioRandom(AudioEffectData audioData, Vector3 position)
        {
            AudioClipData clipData = audioData.GetRandomClip();
            if (clipData?.clip == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] No valid clips for random audio '{audioData.effectName}'!");
                return;
            }

            AudioSource audioSource = GetAudioFromPool(audioData.effectName);
            if (audioSource == null) return;

            audioSource.clip = clipData.clip;
            audioSource.loop = false;
            audioSource.transform.position = position;
            audioSource.gameObject.SetActive(true);
            TrackAudioSource(audioSource, clipData.volume, false);
            audioSource.Play();

            _activeAudio.Add(audioSource);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Playing Audio (Random) '{audioData.effectName}' at {position}");

            StartCoroutine(ReturnAudioToPoolAfterDuration(audioSource, audioData.effectName, clipData.clip.length));
        }

        private void PlayAudioSequential(AudioEffectData audioData, Vector3 position)
        {
            if (audioData.ClipCount == 0) return;
            AudioSource audioSource = GetAudioFromPool(audioData.effectName);
            if (audioSource == null) return;

            audioSource.transform.position = position;
            audioSource.spatialBlend = audioData.spatialBlend;
            audioSource.loop = false;
            audioSource.gameObject.SetActive(true);
            _activeAudio.Add(audioSource);

            StartCoroutine(PlaySequentialClipsThenReturn(audioData, audioSource));
        }

        private void PlayAudioSingle(AudioEffectData audioData, Vector3 position)
        {
            AudioClipData clipData = audioData.GetClip(0);
            if (clipData?.clip == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] No valid first clip for single audio '{audioData.effectName}'!");
                return;
            }

            AudioSource audioSource = GetAudioFromPool(audioData.effectName);
            if (audioSource == null) return;

            audioSource.clip = clipData.clip;
            audioSource.loop = false;
            audioSource.transform.position = position;
            audioSource.gameObject.SetActive(true);
            TrackAudioSource(audioSource, clipData.volume, false);
            audioSource.Play();

            _activeAudio.Add(audioSource);
            StartCoroutine(ReturnAudioToPoolAfterDuration(audioSource, audioData.effectName, clipData.clip.length));
        }

        private IEnumerator PlaySequentialClipsThenReturn(AudioEffectData audioData, AudioSource audioSource)
        {
            int count = audioData.ClipCount;
            float totalDuration = 0f;
            for (int i = 0; i < count; i++)
            {
                AudioClipData clipData = audioData.GetClipSequential(i);
                if (clipData?.clip == null) continue;
                audioSource.clip = clipData.clip;
                TrackAudioSource(audioSource, clipData.volume, false);
                audioSource.Play();
                totalDuration += clipData.clip.length;
                yield return new WaitForSeconds(clipData.clip.length + 0.05f);
            }
            ReturnAudioToPool(audioData.effectName, audioSource);
        }

        public void PlayBGM(string effectName)
        {
            if (string.IsNullOrEmpty(effectName) || audioSet == null) return;
            StopBGM();

            AudioEffectData audioData = audioSet.GetAudioEffect(effectName);
            if (audioData == null || !audioData.IsValid)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] BGM effect '{effectName}' not found or invalid!");
                return;
            }

            _bgmSource = GetAudioFromPool(effectName);
            if (_bgmSource == null) return;
            _bgmSecondarySource = GetAudioFromPool(effectName);

            _bgmEffectName = effectName;
            _bgmFirstPlaySequentialIndex = 0;
            _bgmLoopSequentialIndex = 0;
            _bgmSource.transform.position = Vector3.zero;
            _bgmSource.spatialBlend = audioData.spatialBlend;
            _bgmSource.gameObject.SetActive(true);
            TrackAudioSource(_bgmSource, 1f, true);
            _activeAudio.Add(_bgmSource);
            if (_bgmSecondarySource != null)
            {
                _bgmSecondarySource.transform.position = Vector3.zero;
                _bgmSecondarySource.spatialBlend = audioData.spatialBlend;
                _bgmSecondarySource.gameObject.SetActive(true);
                TrackAudioSource(_bgmSecondarySource, 1f, true);
                _activeAudio.Add(_bgmSecondarySource);
            }
            _bgmSequentialCoroutine = StartCoroutine(PlayBGMWithIntroAndLoop(audioData, effectName));
        }

        private IEnumerator PlayBGMWithIntroAndLoop(AudioEffectData audioData, string effectName)
        {
            if (_bgmSource == null) yield break;

            float introDuration = PlayFirstSection(audioData, effectName, Vector3.zero);

            if (audioData.loop && audioData.UseLoopSection && audioData.LoopSection != null && audioData.LoopSection.IsValid)
            {
                if (TryScheduleSeamlessSingleIntroToSingleLoop(audioData))
                {
                    _bgmSequentialCoroutine = null;
                    yield break;
                }
            }

            if (introDuration > 0f)
                yield return WaitForBGMSectionEnd(introDuration);

            if (audioData.loop && audioData.UseLoopSection && audioData.LoopSection != null && audioData.LoopSection.IsValid)
            {
                if (TryStartContinuousLoopSection(audioData))
                {
                    _bgmSequentialCoroutine = null;
                    yield break;
                }

                while (_bgmSource != null && _bgmSource.gameObject.activeInHierarchy)
                {
                    float cycleDuration = PlayLoopSectionCycle(audioData, effectName, Vector3.zero);
                    if (cycleDuration <= 0f) break;
                    yield return WaitForBGMSectionEnd(cycleDuration);
                    if (audioData.LoopSection.cycleDelay > 0f)
                        yield return new WaitForSeconds(audioData.LoopSection.cycleDelay);
                }
            }
            else if (audioData.loop)
            {
                if (TryStartContinuousLegacyLoop(audioData))
                {
                    _bgmSequentialCoroutine = null;
                    yield break;
                }

                while (_bgmSource != null && _bgmSource.gameObject.activeInHierarchy)
                {
                    float cycleDuration = PlayFirstSection(audioData, effectName, Vector3.zero);
                    if (cycleDuration <= 0f) break;
                    yield return WaitForBGMSectionEnd(cycleDuration);
                }
            }

            _bgmSequentialCoroutine = null;
        }

        private IEnumerator WaitForBGMSectionEnd(float fallbackDuration)
        {
            if (_bgmSource != null && _bgmSource.clip != null && _bgmSource.isPlaying)
            {
                while (_bgmSource != null && _bgmSource.isPlaying)
                    yield return null;
                yield break;
            }

            if (fallbackDuration > 0f)
                yield return new WaitForSeconds(fallbackDuration);
        }

        private bool TryStartContinuousLoopSection(AudioEffectData audioData)
        {
            var loop = audioData.LoopSection;
            if (_bgmSource == null || loop == null) return false;
            if (loop.cycleDelay > 0f) return false;
            if (loop.playType != AudioPlayType.Single) return false;

            AudioClipData clipData = loop.GetClip(0);
            if (clipData?.clip == null) return false;

            _bgmSource.clip = clipData.clip;
            _bgmSource.spatialBlend = audioData.spatialBlend;
            _bgmSource.loop = true;
            TrackAudioSource(_bgmSource, clipData.volume, true);
            _bgmSource.Play();
            return true;
        }

        private bool TryScheduleSeamlessSingleIntroToSingleLoop(AudioEffectData audioData)
        {
            if (_bgmSource == null || _bgmSecondarySource == null) return false;
            if (!audioData.loop || !audioData.UseLoopSection) return false;
            if (audioData.playType != AudioPlayType.Single) return false;
            var loop = audioData.LoopSection;
            if (loop == null || !loop.IsValid) return false;
            if (loop.playType != AudioPlayType.Single || loop.cycleDelay > 0f) return false;
            if (_bgmSource.clip == null || !_bgmSource.isPlaying) return false;

            AudioClipData loopClipData = loop.GetClip(0);
            if (loopClipData?.clip == null) return false;

            int freq = _bgmSource.clip.frequency > 0 ? _bgmSource.clip.frequency : 44100;
            double remaining = (_bgmSource.clip.samples - _bgmSource.timeSamples) / (double)freq;
            double now = AudioSettings.dspTime;
            double switchDsp = now + remaining;
            double startDsp = switchDsp - BgmSeamlessPreRollSeconds;
            if (startDsp < now)
                startDsp = now;

            _bgmSecondarySource.Stop();
            _bgmSecondarySource.clip = loopClipData.clip;
            _bgmSecondarySource.spatialBlend = audioData.spatialBlend;
            _bgmSecondarySource.loop = true;
            TrackAudioSource(_bgmSecondarySource, loopClipData.volume, true);
            _bgmSecondarySource.PlayScheduled(startDsp);

            StartCoroutine(FinishPrimaryAfterSeamlessSwitch(switchDsp));
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Scheduled seamless intro->loop (pre-roll {BgmSeamlessPreRollSeconds:0.000}s).");
            return true;
        }

        private IEnumerator FinishPrimaryAfterSeamlessSwitch(double switchDspTime)
        {
            while (AudioSettings.dspTime < switchDspTime && _bgmSource != null)
                yield return null;

            if (_bgmSource != null)
                _bgmSource.Stop();

            var oldPrimary = _bgmSource;
            _bgmSource = _bgmSecondarySource;
            _bgmSecondarySource = oldPrimary;
        }

        private bool TryStartContinuousLegacyLoop(AudioEffectData audioData)
        {
            if (_bgmSource == null) return false;
            if (audioData.playType != AudioPlayType.Single) return false;

            AudioClipData clipData = audioData.GetClip(0);
            if (clipData?.clip == null) return false;

            _bgmSource.clip = clipData.clip;
            _bgmSource.spatialBlend = audioData.spatialBlend;
            _bgmSource.loop = true;
            TrackAudioSource(_bgmSource, clipData.volume, true);
            _bgmSource.Play();
            return true;
        }

        public void StopBGM()
        {
            string bgmEffect = _bgmEffectName;
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
                _bgmFadeCoroutine = null;
            }
            if (_bgmSequentialCoroutine != null)
            {
                StopCoroutine(_bgmSequentialCoroutine);
                _bgmSequentialCoroutine = null;
            }
            if (_bgmSource != null && !string.IsNullOrEmpty(bgmEffect))
            {
                ReturnAudioToPool(bgmEffect, _bgmSource);
                _bgmSource = null;
            }
            if (_bgmSecondarySource != null && !string.IsNullOrEmpty(bgmEffect))
            {
                ReturnAudioToPool(bgmEffect, _bgmSecondarySource);
                _bgmSecondarySource = null;
            }
            for (int i = 0; i < _bgmBlendSources.Count; i++)
            {
                var src = _bgmBlendSources[i];
                if (!string.IsNullOrEmpty(bgmEffect) && src != null && _activeAudio.Contains(src))
                    ReturnAudioToPool(bgmEffect, _bgmBlendSources[i]);
            }
            _bgmBlendSources.Clear();
            _bgmEffectName = null;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] BGM stopped");
        }

        public void FadeOutBGM(float duration)
        {
            if (_bgmSource == null) return;
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(FadeOutBGMCoroutine(duration));
        }

        private IEnumerator FadeOutBGMCoroutine(float duration)
        {
            float startVolume = _bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration && _bgmSource != null)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            _bgmFadeCoroutine = null;
            StopBGM();
        }

        private float PlayFirstSection(AudioEffectData audioData, string effectName, Vector3 position)
        {
            if (audioData == null || audioData.ClipCount == 0) return 0f;
            switch (audioData.playType)
            {
                case AudioPlayType.Single:
                {
                    var clipData = audioData.GetClip(0);
                    return PlaySingleOnBGMSource(clipData, audioData, false);
                }
                case AudioPlayType.Blend:
                    return PlayClipListBlend(audioData.AudioClips, effectName, position, audioData.spatialBlend, true);
                case AudioPlayType.Sequential:
                {
                    var clipData = audioData.GetClipSequential(_bgmFirstPlaySequentialIndex++);
                    return PlaySingleOnBGMSource(clipData, audioData, false);
                }
                default:
                {
                    var clipData = audioData.GetRandomClip();
                    return PlaySingleOnBGMSource(clipData, audioData, false);
                }
            }
        }

        private float PlayLoopSectionCycle(AudioEffectData audioData, string effectName, Vector3 position)
        {
            var loop = audioData.LoopSection;
            if (loop == null || !loop.IsValid) return 0f;
            switch (loop.playType)
            {
                case AudioPlayType.Single:
                {
                    var clipData = loop.GetClip(0);
                    return PlaySingleOnBGMSource(clipData, audioData, false);
                }
                case AudioPlayType.Blend:
                    return PlayClipListBlend(loop.Clips, effectName, position, audioData.spatialBlend, true);
                case AudioPlayType.Sequential:
                {
                    var clipData = loop.GetClipSequential(_bgmLoopSequentialIndex++);
                    return PlaySingleOnBGMSource(clipData, audioData, false);
                }
                default:
                {
                    var clipData = loop.GetRandomClip();
                    return PlaySingleOnBGMSource(clipData, audioData, false);
                }
            }
        }

        private float PlaySingleOnBGMSource(AudioClipData clipData, AudioEffectData audioData, bool loop)
        {
            if (_bgmSource == null || clipData?.clip == null) return 0f;
            _bgmSource.clip = clipData.clip;
            _bgmSource.spatialBlend = audioData.spatialBlend;
            _bgmSource.loop = loop;
            TrackAudioSource(_bgmSource, clipData.volume, true);
            _bgmSource.Play();
            return clipData.clip.length;
        }

        private float PlayClipListBlend(IReadOnlyList<AudioClipData> clips, string effectName, Vector3 position, float spatialBlend, bool autoReturn)
        {
            if (clips == null || clips.Count == 0) return 0f;
            float maxDuration = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                var clipData = clips[i];
                if (clipData?.clip == null) continue;

                AudioSource src = GetAudioFromPool(effectName);
                if (src == null) continue;

                src.clip = clipData.clip;
                src.loop = false;
                src.spatialBlend = spatialBlend;
                src.transform.position = position;
                src.gameObject.SetActive(true);
                TrackAudioSource(src, clipData.volume, true);
                src.Play();
                _activeAudio.Add(src);
                _bgmBlendSources.Add(src);

                if (clipData.clip.length > maxDuration) maxDuration = clipData.clip.length;
                if (autoReturn)
                    StartCoroutine(ReturnAudioToPoolAfterDuration(src, effectName, clipData.clip.length));
            }
            return maxDuration;
        }

        private AudioSource GetAudioFromPool(string effectName)
        {
            if (!_audioPool.ContainsKey(effectName))
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] Audio pool '{effectName}' doesn't exist!");
                return null;
            }

            Queue<AudioSource> pool = _audioPool[effectName];
            AudioSource audioSource;

            if (pool.Count > 0)
            {
                audioSource = pool.Dequeue();
            }
            else
            {
                // Expand pool if needed
                AudioEffectData audioData = audioSet.GetAudioEffect(effectName);
                if (audioData != null)
                {
                    GameObject audioObj = new GameObject($"AudioSource_{effectName}");
                    audioObj.transform.SetParent(_poolContainer);
                    audioSource = audioObj.AddComponent<AudioSource>();
                    audioSource.spatialBlend = audioData.spatialBlend;
                }
                else
                {
                    return null;
                }
            }

            return audioSource;
        }

        private void ReturnAudioToPool(string effectName, AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.gameObject.SetActive(false);
            audioSource.transform.SetParent(_poolContainer);
            _activeAudio.Remove(audioSource);
            UntrackAudioSource(audioSource);

            if (!_audioPool.ContainsKey(effectName))
                _audioPool[effectName] = new Queue<AudioSource>();

            _audioPool[effectName].Enqueue(audioSource);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Returned Audio '{effectName}' to pool");
        }

        private IEnumerator ReturnAudioToPoolAfterDuration(AudioSource audioSource, string effectName, float duration)
        {
            yield return new WaitForSeconds(duration + 0.1f);
            ReturnAudioToPool(effectName, audioSource);
        }

        private IEnumerator ReturnAudioToPoolAfterDuration(string effectName, float duration)
        {
            yield return new WaitForSeconds(duration + 0.1f);

            // Return all active audio sources for this effect
            var audioSourcesSnapshot = new List<AudioSource>(_activeAudio);
            foreach (var audioSource in audioSourcesSnapshot)
            {
                if (audioSource != null && audioSource.clip != null)
                {
                    ReturnAudioToPool(effectName, audioSource);
                }
            }
        }

        #endregion

        #region Particle Methods

        /// <summary>
        /// Play particle effect at target position
        /// </summary>
        public void PlayParticle(string effectName, Vector3 position, Quaternion rotation = default, Vector3 positionOffset = default, Vector3 scale = default, Transform parentTransform = null, bool followCharacter = false, Vector3 rotationEulerForFollow = default)
        {
            if (string.IsNullOrEmpty(effectName) || particleSet == null) return;
            if (scale == default) scale = Vector3.one;

            ParticleEffectData particleData = particleSet.GetParticleEffect(effectName);
            if (particleData == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] Particle effect '{effectName}' not found!");
                return;
            }

            ParticleSystem particle = GetParticleFromPool(effectName, particleData.particlePrefab);
            if (particle == null) return;

            particle.Clear(true);
            ApplyEffectTransform(particle.transform, position, rotation, positionOffset, scale, parentTransform, followCharacter);
            if (followCharacter && parentTransform != null)
                RegisterFollow(particle.transform, parentTransform, positionOffset, rotationEulerForFollow, scale);
            var main = particle.main;
            main.simulationSpace = followCharacter ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            particle.gameObject.SetActive(true);
            particle.Play();

            _activeParticles.Add(particle);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Playing Particle '{effectName}' at {position + positionOffset} (Follow: {followCharacter})");

            float duration = particle.main.duration;
            var startLifetimeCurve = particle.main.startLifetime;
            float startLifetime = startLifetimeCurve.constantMax > 0f
                ? startLifetimeCurve.constantMax
                : (startLifetimeCurve.constant > 0f ? startLifetimeCurve.constant : 2f);
            float totalDuration = duration + startLifetime;
            StartCoroutine(ReturnParticleToPoolAfterDuration(particle, effectName, totalDuration, parentTransform));
        }

        private ParticleSystem GetParticleFromPool(string effectName, ParticleSystem prefab = null)
        {
            if (!_particlePool.ContainsKey(effectName))
            {
                if (prefab != null)
                {
                    InitializeParticlePool(effectName, prefab, 5);
                }
                else
                {
                    if (showDebugLog)
                        Debug.LogWarning($"[{GetType().Name}] Particle pool '{effectName}' doesn't exist!");
                    return null;
                }
            }

            Queue<ParticleSystem> pool = _particlePool[effectName];
            ParticleSystem particle;

            if (pool.Count > 0)
            {
                particle = pool.Dequeue();
            }
            else
            {
                // Expand pool if needed
                if (prefab != null)
                {
                    particle = Instantiate(prefab, _poolContainer);
                }
                else
                {
                    return null;
                }
            }

            return particle;
        }

        private void ReturnParticleToPool(string effectName, ParticleSystem particle)
        {
            if (particle == null) return;

            UnregisterFollow(particle.transform);
            particle.Stop();
            particle.gameObject.SetActive(false);
            particle.transform.SetParent(_poolContainer);
            _activeParticles.Remove(particle);

            if (!_particlePool.ContainsKey(effectName))
                _particlePool[effectName] = new Queue<ParticleSystem>();

            _particlePool[effectName].Enqueue(particle);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Returned Particle '{effectName}' to pool");
        }

        private IEnumerator ReturnParticleToPoolAfterDuration(ParticleSystem particle, string effectName, float duration, Transform parentTransform = null)
        {
            yield return new WaitForSeconds(duration + 0.1f);
            
            // Unparent if it was parented to a character
            if (particle != null && parentTransform != null)
            {
                particle.transform.SetParent(_poolContainer);
            }
            
            ReturnParticleToPool(effectName, particle);
        }

        #endregion

        #region Trail Methods

        public void RegisterTrail(string effectName, Trail trail)
        {
            if (string.IsNullOrEmpty(effectName) || trail == null) return;
            _trailRegistry[effectName] = trail;
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Registered trail '{effectName}'");
        }

        public void UnregisterTrail(string effectName)
        {
            if (string.IsNullOrEmpty(effectName)) return;
            _trailRegistry.Remove(effectName);
        }

        public void PlayTrailStart(string effectName, WeaponTrailStopMode stopMode, float frozenTrailLifetime)
        {
            if (string.IsNullOrEmpty(effectName)) return;
            var trail = GetOrFindTrail(effectName);
            if (trail == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] Trail '{effectName}' not found in scene!");
                return;
            }
            trail.StopMode = stopMode == WeaponTrailStopMode.FreezeAndFadeOut ? TrailStopMode.FreezeAndFadeOut : TrailStopMode.ShrinkThenHide;
            trail.FrozenTrailLifetime = frozenTrailLifetime;
            trail.StartTrail();
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Trail start '{effectName}'");
        }

        public void PlayTrailStop(string effectName)
        {
            if (string.IsNullOrEmpty(effectName)) return;
            var trail = GetOrFindTrail(effectName);
            if (trail == null) return;
            trail.StopTrail();
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Trail stop '{effectName}'");
        }

        private Trail GetOrFindTrail(string effectName)
        {
            if (_trailRegistry.TryGetValue(effectName, out var cached) && cached != null)
                return cached;
            var all = FindObjectsOfType<Trail>(true);
            foreach (var t in all)
            {
                if (t == null || t.TrailId != effectName) continue;
                RegisterTrail(effectName, t);
                return t;
            }
            return null;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clear all pools and reset manager (call when changing scenes)
        /// </summary>
        public void ClearAllPools()
        {
            // Deactivate all active effects
            foreach (var vfx in _activeVFX)
            {
                if (vfx != null)
                    vfx.gameObject.SetActive(false);
            }
            _activeVFX.Clear();

            foreach (var audio in _activeAudio)
            {
                if (audio != null)
                    audio.gameObject.SetActive(false);
            }
            _activeAudio.Clear();

            foreach (var particle in _activeParticles)
            {
                if (particle != null)
                    particle.gameObject.SetActive(false);
            }
            _activeParticles.Clear();

            // Clear pools and trail registry
            _vfxPool.Clear();
            _audioPool.Clear();
            _particlePool.Clear();
            _trailRegistry.Clear();

            // Destroy all pool objects
            if (_poolContainer != null)
            {
                foreach (Transform child in _poolContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] All pools cleared and reset");
        }

        #endregion
    }
}
