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

        // Trail registry (Trail by id; registered by Weapon when enabled)
        private Dictionary<string, Trail> _trailRegistry = new Dictionary<string, Trail>();

        #endregion

        #region Lifecycle

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
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
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

        private void ApplyEffectTransform(Transform t, Vector3 position, Quaternion rotation, Vector3 positionOffset, Transform parentTransform, bool followCharacter)
        {
            if (followCharacter && parentTransform != null)
            {
                t.SetParent(parentTransform);
                t.localPosition = positionOffset;
                t.localRotation = rotation;
            }
            else
            {
                t.SetParent(_poolContainer);
                t.position = position + positionOffset;
                t.rotation = rotation;
            }
        }

        #region VFX Methods

        /// <summary>
        /// Play a VFX effect at target position
        /// </summary>
        public void PlayVFX(string effectName, Vector3 position, Quaternion rotation = default, Vector3 positionOffset = default, Transform parentTransform = null, bool followCharacter = false)
        {
            if (string.IsNullOrEmpty(effectName) || vfxSet == null) return;

            VFXEffectData vfxData = vfxSet.GetVFXEffect(effectName);
            if (vfxData == null)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[{GetType().Name}] VFX effect '{effectName}' not found!");
                return;
            }

            VisualEffect vfx = GetVFXFromPool(effectName, vfxData.vfxPrefab);
            if (vfx == null) return;

            ApplyEffectTransform(vfx.transform, position, rotation, positionOffset, parentTransform, followCharacter);
            vfx.gameObject.SetActive(true);
            vfx.Reinit();
            vfx.Play();

            _activeVFX.Add(vfx);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Playing VFX '{effectName}' at {position + positionOffset} (Follow: {followCharacter})");

            // Return to pool after duration
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

            if (audioData.playType == AudioPlayType.Blend)
            {
                PlayAudioBlend(audioData, position);
            }
            else // Random
            {
                PlayAudioRandom(audioData, position);
            }
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
                audioSource.volume = clipData.volume;
                audioSource.transform.position = position;
                audioSource.gameObject.SetActive(true);
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
            audioSource.volume = clipData.volume;
            audioSource.transform.position = position;
            audioSource.gameObject.SetActive(true);
            audioSource.Play();

            _activeAudio.Add(audioSource);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Playing Audio (Random) '{audioData.effectName}' at {position}");

            // Return to pool after audio finishes
            StartCoroutine(ReturnAudioToPoolAfterDuration(audioSource, audioData.effectName, clipData.clip.length));
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
        public void PlayParticle(string effectName, Vector3 position, Quaternion rotation = default, Vector3 positionOffset = default, Transform parentTransform = null, bool followCharacter = false)
        {
            if (string.IsNullOrEmpty(effectName) || particleSet == null) return;

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
            ApplyEffectTransform(particle.transform, position, rotation, positionOffset, parentTransform, followCharacter);
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
