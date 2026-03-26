using KinematicCharacterController;
using UnityEngine;
using UnityEngine.AI;

namespace LittleHeroJourney
{
    public class CharacterMovementEffect : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers;

        [SerializeField] private MovementEffectChannelSettings globalSettings = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.35f,
            emitInterval = 0.06f,
        };

        [SerializeField] private string particleEffectId;

        [SerializeField] private bool particleUseCustomSettings;

        [SerializeField] private MovementEffectChannelSettings particleCustom = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.1f,
            emitInterval = 0.06f,
        };

        [SerializeField] private string vfxEffectId;

        [SerializeField] private bool vfxUseCustomSettings;

        [SerializeField] private MovementEffectChannelSettings vfxCustom = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.1f,
            emitInterval = 0.06f,
        };

        [SerializeField] private string audioEffectId;

        [SerializeField] private bool audioUseCustomSettings;

        [SerializeField] private MovementEffectChannelSettings audioCustom = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.1f,
            emitInterval = 0.06f,
        };

        [SerializeField] private bool showGizmos = true;

        [Header("Water tuning (submerged)")]
        [SerializeField] private MovementEffectChannelSettings waterGlobalSettings = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.35f,
            emitInterval = 0.06f,
        };

        [SerializeField] private bool waterParticleUseCustomSettings;
        [SerializeField] private MovementEffectChannelSettings waterParticleCustom = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.35f,
            emitInterval = 0.06f,
        };

        [SerializeField] private bool waterVfxUseCustomSettings;
        [SerializeField] private MovementEffectChannelSettings waterVfxCustom = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.0f,
            emitInterval = 0.06f,
        };

        [SerializeField] private bool waterAudioUseCustomSettings;
        [SerializeField] private MovementEffectChannelSettings waterAudioCustom = new MovementEffectChannelSettings
        {
            minSpeed = 0.2f,
            groundRayLength = 0.35f,
            emitInterval = 0.06f,
        };

        [Header("Water movement (submerged)")]
        [SerializeField] private WaterMovementEffectIds waterNormalShallow;
        [SerializeField] private WaterMovementEffectIds waterMurkyShallow;
        [SerializeField] private WaterMovementEffectIds waterNormalDeep;
        [SerializeField] private WaterMovementEffectIds waterMurkyDeep;

        [Tooltip("World Y added to true water surface for all water effects (particle/audio/VFX).")]
        [SerializeField] private float waterSurfaceEffectOffset = 0.08f;

        [Tooltip("Jika true: saat Shallow, spawn particle/VFX/audio mengikuti tinggi permukaan air + offset. Jika false: Y pakai posisi CharacterMovementEffect itu sendiri (tanpa offset).")]
        [SerializeField] private bool shallowUseSurfaceY = true;

        private NavMeshAgent _agent;
        private KinematicCharacterMotor _motor;
        private CharacterWaterSubmersion _waterSubmersion;
        private Vector3 _prevHorizontalPos;
        private float _emitTimerParticle;
        private float _emitTimerVfx;
        private float _emitTimerAudio;
        private static RaycastHit[] _rayHits = new RaycastHit[8];
        private bool _wasBelowWaterSurface;
        private float _deepMovedSinceLastParticle;
        private float _deepMovedSinceLastVfx;
        private float _deepMovedSinceLastAudio;

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            _motor = GetComponentInParent<KinematicCharacterMotor>();
            _waterSubmersion = GetComponentInParent<CharacterWaterSubmersion>();

            var p = transform.position;
            _prevHorizontalPos = new Vector3(p.x, 0f, p.z);
        }

        private void OnEnable()
        {
            if (_waterSubmersion == null)
                _waterSubmersion = GetComponentInParent<CharacterWaterSubmersion>();

            if (_waterSubmersion != null)
            {
                _waterSubmersion.SubmergedStateChanged += HandleSubmergedStateChanged;
                _waterSubmersion.RegisterMovementEffect(this);
            }
        }

        private void OnDisable()
        {
            if (_waterSubmersion != null)
            {
                _waterSubmersion.SubmergedStateChanged -= HandleSubmergedStateChanged;
                _waterSubmersion.UnregisterMovementEffect(this);
            }
        }

        private void HandleSubmergedStateChanged(bool submerged)
        {
            // Reset timers to avoid "carry-over" emits right after entering/exiting water.
            _emitTimerParticle = 0f;
            _emitTimerVfx = 0f;
            _emitTimerAudio = 0f;
            _deepMovedSinceLastParticle = 0f;
            _deepMovedSinceLastVfx = 0f;
            _deepMovedSinceLastAudio = 0f;
            _wasBelowWaterSurface = false;
        }

        private void LateUpdate()
        {
            if (!HasAnyEffectId())
                return;

            Vector3 horizontalVel = GetHorizontalVelocity();
            float speedSq = horizontalVel.sqrMagnitude;

            if (_waterSubmersion != null && _waterSubmersion.IsSubmerged)
            {
                if (!HasWaterEffectsConfigured())
                {
                    _emitTimerParticle = 0f;
                    _emitTimerVfx = 0f;
                    _emitTimerAudio = 0f;
                    return;
                }

                ProcessWaterMovementEffects(speedSq);
                return;
            }

            float maxRay = 0f;
            bool needParticle = HasAnyId(particleEffectId);
            bool needVfx = HasAnyId(vfxEffectId);
            bool needAudio = HasAnyId(audioEffectId);
            MovementEffectChannelSettings tuneP = GetParticleTune();
            MovementEffectChannelSettings tuneV = GetVfxTune();
            MovementEffectChannelSettings tuneA = GetAudioTune();
            if (needParticle) maxRay = Mathf.Max(maxRay, tuneP.groundRayLength);
            if (needVfx) maxRay = Mathf.Max(maxRay, tuneV.groundRayLength);
            if (needAudio) maxRay = Mathf.Max(maxRay, tuneA.groundRayLength);
            bool groundedHit = false;
            float hitDistance = 0f;
            if (maxRay > 0f)
            {
                Vector3 origin = transform.position + Vector3.up * 0.05f;
                int mask = groundLayers.value != 0 ? groundLayers.value : Physics.DefaultRaycastLayers;
                groundedHit = TryFootRayHitGroundNonAlloc(origin, Mathf.Max(0.05f, maxRay), mask, out RaycastHit h);
                if (groundedHit) hitDistance = h.distance;
            }
            bool motorOk = GetMotorOrAgentGroundOk(_motor, _agent);
            bool gateP = false;
            if (needParticle)
            {
                bool speedOk = speedSq >= tuneP.minSpeed * tuneP.minSpeed;
                bool groundOk = groundedHit && motorOk && hitDistance <= Mathf.Max(0.05f, tuneP.groundRayLength);
                if (speedOk && groundOk)
                {
                    _emitTimerParticle += Time.deltaTime;
                    if (_emitTimerParticle >= tuneP.emitInterval)
                    {
                        gateP = true;
                        _emitTimerParticle = 0f;
                    }
                }
                else
                {
                    _emitTimerParticle = 0f;
                }
            }
            bool gateV = false;
            if (needVfx)
            {
                bool speedOk = speedSq >= tuneV.minSpeed * tuneV.minSpeed;
                bool groundOk = groundedHit && motorOk && hitDistance <= Mathf.Max(0.05f, tuneV.groundRayLength);
                if (speedOk && groundOk)
                {
                    _emitTimerVfx += Time.deltaTime;
                    if (_emitTimerVfx >= tuneV.emitInterval)
                    {
                        gateV = true;
                        _emitTimerVfx = 0f;
                    }
                }
                else
                {
                    _emitTimerVfx = 0f;
                }
            }
            bool gateA = false;
            if (needAudio)
            {
                bool speedOk = speedSq >= tuneA.minSpeed * tuneA.minSpeed;
                bool groundOk = groundedHit && motorOk && hitDistance <= Mathf.Max(0.05f, tuneA.groundRayLength);
                if (speedOk && groundOk)
                {
                    _emitTimerAudio += Time.deltaTime;
                    if (_emitTimerAudio >= tuneA.emitInterval)
                    {
                        gateA = true;
                        _emitTimerAudio = 0f;
                    }
                }
                else
                {
                    _emitTimerAudio = 0f;
                }
            }

            if (!gateP && !gateV && !gateA)
                return;

            PlayEffects(gateP, gateV, gateA);
        }

        private MovementEffectChannelSettings GetParticleTune() =>
            particleUseCustomSettings ? particleCustom : globalSettings;

        private MovementEffectChannelSettings GetVfxTune() =>
            vfxUseCustomSettings ? vfxCustom : globalSettings;

        private MovementEffectChannelSettings GetAudioTune() =>
            audioUseCustomSettings ? audioCustom : globalSettings;

        private static bool HasAnyId(string id) => !string.IsNullOrEmpty(id);

        private bool HasAnyEffectId() =>
            HasAnyId(particleEffectId) ||
            HasAnyId(vfxEffectId) ||
            HasAnyId(audioEffectId) ||
            HasWaterEffectsConfigured();

        private bool HasWaterEffectsConfigured() =>
            waterNormalShallow.HasAny() ||
            waterMurkyShallow.HasAny() ||
            waterNormalDeep.HasAny() ||
            waterMurkyDeep.HasAny();

        private WaterMovementEffectIds ResolveWaterEffectIds()
        {
            WaterVolumeKind kind = _waterSubmersion != null && _waterSubmersion.WaterKind.HasValue
                ? _waterSubmersion.WaterKind.Value
                : WaterVolumeKind.Normal;

            bool shallow = _waterSubmersion != null &&
                _waterSubmersion.SubmersionLevel == WaterSubmersionLevel.Shallow;

            if (shallow)
                return kind == WaterVolumeKind.Murky ? waterMurkyShallow : waterNormalShallow;

            return kind == WaterVolumeKind.Murky ? waterMurkyDeep : waterNormalDeep;
        }

        private void ProcessWaterMovementEffects(float speedSq)
        {
            bool shallow = _waterSubmersion != null &&
                _waterSubmersion.SubmersionLevel == WaterSubmersionLevel.Shallow;

            // If character has multiple emitters, only ONE is allowed to emit in deep water.
            if (!shallow && _waterSubmersion != null && !_waterSubmersion.CanEmitDeep(this))
            {
                _emitTimerParticle = 0f;
                _emitTimerVfx = 0f;
                _emitTimerAudio = 0f;
                _deepMovedSinceLastParticle = 0f;
                _deepMovedSinceLastVfx = 0f;
                _deepMovedSinceLastAudio = 0f;
                return;
            }

            WaterMovementEffectIds ids = ResolveWaterEffectIds();
            MovementEffectChannelSettings particleTune = GetWaterParticleTune();
            MovementEffectChannelSettings vfxTune = GetWaterVfxTune();
            MovementEffectChannelSettings audioTune = GetWaterAudioTune();

            bool playParticle = false;
            bool playVfx = false;
            bool playAudio = false;

            float waterSurfaceY = _waterSubmersion.TrueWaterSurfaceWorldY;
            if (shallow)
            {
                bool belowNow = transform.position.y <= waterSurfaceY + 0.01f;
                bool crossing = belowNow && !_wasBelowWaterSurface;
                float dt = Time.deltaTime;

                if (belowNow)
                {
                    if (HasAnyId(ids.particleEffectId))
                    {
                        bool moving = speedSq >= particleTune.minSpeed * particleTune.minSpeed;
                        if (moving)
                        {
                            _emitTimerParticle += dt;
                            if (_emitTimerParticle >= particleTune.emitInterval)
                            {
                                playParticle = true;
                                _emitTimerParticle = 0f;
                            }
                        }
                        else
                        {
                            _emitTimerParticle = 0f;
                        }
                    }

                    if (HasAnyId(ids.vfxEffectId))
                    {
                        bool moving = speedSq >= vfxTune.minSpeed * vfxTune.minSpeed;
                        if (moving)
                        {
                            _emitTimerVfx += dt;
                            if (_emitTimerVfx >= vfxTune.emitInterval)
                            {
                                playVfx = true;
                                _emitTimerVfx = 0f;
                            }
                        }
                        else
                        {
                            _emitTimerVfx = 0f;
                        }
                    }

                    if (HasAnyId(ids.audioEffectId))
                    {
                        bool moving = speedSq >= audioTune.minSpeed * audioTune.minSpeed;
                        if (moving)
                        {
                            _emitTimerAudio += dt;
                            if (_emitTimerAudio >= audioTune.emitInterval)
                            {
                                playAudio = true;
                                _emitTimerAudio = 0f;
                            }
                        }
                        else
                        {
                            _emitTimerAudio = 0f;
                        }
                    }

                    if (crossing)
                    {
                        if (HasAnyId(ids.particleEffectId) && speedSq >= particleTune.minSpeed * particleTune.minSpeed)
                        {
                            playParticle = true;
                            _emitTimerParticle = 0f;
                        }
                        if (HasAnyId(ids.vfxEffectId) && speedSq >= vfxTune.minSpeed * vfxTune.minSpeed)
                        {
                            playVfx = true;
                            _emitTimerVfx = 0f;
                        }
                        if (HasAnyId(ids.audioEffectId) && speedSq >= audioTune.minSpeed * audioTune.minSpeed)
                        {
                            playAudio = true;
                            _emitTimerAudio = 0f;
                        }
                    }
                }
                else
                {
                    _emitTimerParticle = 0f;
                    _emitTimerVfx = 0f;
                    _emitTimerAudio = 0f;
                }

                _wasBelowWaterSurface = belowNow;
            }
            else
            {
                float dist = GetHorizontalVelocity().magnitude * Time.deltaTime;
                if (HasAnyId(ids.particleEffectId))
                {
                    float threshold = Mathf.Max(0.01f, particleTune.minSpeed * Mathf.Max(0.01f, particleTune.emitInterval));
                    if (speedSq >= particleTune.minSpeed * particleTune.minSpeed)
                    {
                        _deepMovedSinceLastParticle += dist;
                        if (_deepMovedSinceLastParticle >= threshold)
                        {
                            playParticle = true;
                            _deepMovedSinceLastParticle = 0f;
                        }
                    }
                    else _deepMovedSinceLastParticle = 0f;
                }
                if (HasAnyId(ids.vfxEffectId))
                {
                    float threshold = Mathf.Max(0.01f, vfxTune.minSpeed * Mathf.Max(0.01f, vfxTune.emitInterval));
                    if (speedSq >= vfxTune.minSpeed * vfxTune.minSpeed)
                    {
                        _deepMovedSinceLastVfx += dist;
                        if (_deepMovedSinceLastVfx >= threshold)
                        {
                            playVfx = true;
                            _deepMovedSinceLastVfx = 0f;
                        }
                    }
                    else _deepMovedSinceLastVfx = 0f;
                }
                if (HasAnyId(ids.audioEffectId))
                {
                    float threshold = Mathf.Max(0.01f, audioTune.minSpeed * Mathf.Max(0.01f, audioTune.emitInterval));
                    if (speedSq >= audioTune.minSpeed * audioTune.minSpeed)
                    {
                        _deepMovedSinceLastAudio += dist;
                        if (_deepMovedSinceLastAudio >= threshold)
                        {
                            playAudio = true;
                            _deepMovedSinceLastAudio = 0f;
                        }
                    }
                    else _deepMovedSinceLastAudio = 0f;
                }
            }

            if (!playParticle && !playVfx && !playAudio)
                return;

            if (CharacterEffectManager.Instance == null)
                return;

            PlayWaterEffects(playParticle, playVfx, playAudio, ids, shallow, waterSurfaceY, transform.rotation);
        }

        private MovementEffectChannelSettings GetWaterParticleTune() =>
            waterParticleUseCustomSettings ? waterParticleCustom : waterGlobalSettings;

        private MovementEffectChannelSettings GetWaterVfxTune() =>
            waterVfxUseCustomSettings ? waterVfxCustom : waterGlobalSettings;

        private MovementEffectChannelSettings GetWaterAudioTune() =>
            waterAudioUseCustomSettings ? waterAudioCustom : waterGlobalSettings;

        private void PlayWaterEffects(
            bool playParticle,
            bool playVfx,
            bool playAudio,
            WaterMovementEffectIds ids,
            bool shallow,
            float surfaceY,
            Quaternion rot)
        {
            CharacterEffectManager mgr = CharacterEffectManager.Instance;
            if (mgr == null)
                return;

            float deepY = surfaceY + waterSurfaceEffectOffset;
            float shallowY = shallowUseSurfaceY ? deepY : transform.position.y;

            float yParticleAudio = shallow ? shallowY : deepY;
            float yVfx = shallow ? shallowY : deepY;

            Vector3 deepAnchor = _waterSubmersion.GetBodyCheckCenterWorld();

            Vector3 shallowAnchorParticleAudio = new Vector3(transform.position.x, yParticleAudio, transform.position.z);
            Vector3 shallowAnchorVfx = new Vector3(transform.position.x, yVfx, transform.position.z);

            void PlayParticleOne()
            {
                if (!playParticle || !HasAnyId(ids.particleEffectId))
                    return;

                if (shallow)
                {
                    mgr.PlayParticle(ids.particleEffectId, shallowAnchorParticleAudio, rot);
                    return;
                }

                mgr.PlayParticle(ids.particleEffectId, new Vector3(deepAnchor.x, yParticleAudio, deepAnchor.z), rot);
            }

            void PlayVfxOne()
            {
                if (!playVfx || !HasAnyId(ids.vfxEffectId))
                    return;

                if (shallow)
                {
                    mgr.PlayVFX(ids.vfxEffectId, shallowAnchorVfx, rot);
                    return;
                }

                mgr.PlayVFX(ids.vfxEffectId, new Vector3(deepAnchor.x, yVfx, deepAnchor.z), rot);
            }

            void PlayAudioOne()
            {
                if (!playAudio || !HasAnyId(ids.audioEffectId))
                    return;

                if (shallow)
                {
                    mgr.PlayAudio(ids.audioEffectId, shallowAnchorParticleAudio);
                    return;
                }

                mgr.PlayAudio(ids.audioEffectId, new Vector3(deepAnchor.x, yParticleAudio, deepAnchor.z));
            }

            PlayParticleOne();
            PlayVfxOne();
            PlayAudioOne();
        }

        private void PlayEffects(bool playParticle, bool playVfx, bool playAudio)
        {
            CharacterEffectManager mgr = CharacterEffectManager.Instance;
            if (mgr == null)
                return;

            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (playParticle && HasAnyId(particleEffectId))
                mgr.PlayParticle(particleEffectId, pos, rot);

            if (playVfx && HasAnyId(vfxEffectId))
                mgr.PlayVFX(vfxEffectId, pos, rot);

            if (playAudio && HasAnyId(audioEffectId))
                mgr.PlayAudio(audioEffectId, pos);
        }

        private Vector3 GetHorizontalVelocity()
        {
            if (_motor != null)
            {
                Vector3 v = _motor.Velocity;
                v.y = 0f;
                return v;
            }

            if (_agent != null)
            {
                Vector3 v = _agent.velocity;
                v.y = 0f;
                return v;
            }

            Vector3 pos = transform.position;
            Vector3 cur = new Vector3(pos.x, 0f, pos.z);
            Vector3 delta = cur - _prevHorizontalPos;
            _prevHorizontalPos = cur;
            return delta / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        private bool IsGrounded(float rayLength)
        {
            Vector3 origin = transform.position + Vector3.up * 0.05f;
            int mask = groundLayers.value != 0 ? groundLayers.value : Physics.DefaultRaycastLayers;
            float maxDist = Mathf.Max(0.05f, rayLength);
            if (!TryFootRayHitGroundNonAlloc(origin, maxDist, mask, out _))
                return false;

            if (_motor != null)
                return _motor.GroundingStatus.IsStableOnGround;

            if (_agent != null)
                return _agent.isOnNavMesh;

            return true;
        }

        private bool IsGroundedForWaterShallow(float rayLength)
        {
            Vector3 origin = transform.position + Vector3.up * 0.05f;
            int mask = groundLayers.value != 0 ? groundLayers.value : Physics.DefaultRaycastLayers;
            float effective =
                rayLength > 1e-4f ? rayLength : globalSettings.groundRayLength;
            float maxDist = Mathf.Max(0.05f, effective);
            return TryFootRayHitGroundNonAlloc(origin, maxDist, mask, out _);
        }

        private bool TryFootRayHitGroundNonAlloc(Vector3 origin, float maxDist, int layerMask, out RaycastHit groundHit)
        {
            groundHit = default;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, _rayHits, maxDist, layerMask, QueryTriggerInteraction.Ignore);
            if (count <= 0)
                return false;

            Transform root = transform.root;

            float best = float.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < count; i++)
            {
                Collider c = _rayHits[i].collider;
                if (c == null) continue;
                if (c.transform == root || c.transform.IsChildOf(root))
                    continue;
                float d = _rayHits[i].distance;
                if (d < best)
                {
                    best = d;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                groundHit = _rayHits[bestIndex];
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 origin = transform.position + Vector3.up * 0.05f;
            int mask = groundLayers.value != 0 ? groundLayers.value : Physics.DefaultRaycastLayers;

            var motor = GetComponentInParent<KinematicCharacterMotor>();
            var agent = GetComponentInParent<NavMeshAgent>();

            float globalDist = Mathf.Max(0.05f, globalSettings.groundRayLength);
            bool footGlobal = TryFootRayHitGroundNonAlloc(origin, globalDist, mask, out RaycastHit hitGlobal);
            bool motorOk = GetMotorOrAgentGroundOk(motor, agent);
            bool groundedGlobal = footGlobal && motorOk;

            Gizmos.color = groundedGlobal
                ? new Color(0.82f, 0.82f, 0.82f, 0.75f)
                : new Color(0.85f, 0.45f, 0.45f, 0.7f);
            Gizmos.DrawLine(origin, origin + Vector3.down * globalDist);
            Gizmos.color = new Color(0.92f, 0.92f, 0.92f, 0.55f);
            Gizmos.DrawWireSphere(origin, 0.016f);
            if (footGlobal)
            {
                Gizmos.color = new Color(0.72f, 0.72f, 0.78f, 0.85f);
                Gizmos.DrawWireSphere(hitGlobal.point, 0.028f);
                Gizmos.DrawLine(origin, hitGlobal.point);
            }

            if (HasAnyId(particleEffectId))
            {
                MovementEffectChannelSettings t = GetParticleTune();
                float maxDist = Mathf.Max(0.05f, t.groundRayLength);
                bool footRayOk = TryFootRayHitGroundNonAlloc(origin, maxDist, mask, out RaycastHit groundHitPv);
                bool motorOrAgentOk = GetMotorOrAgentGroundOk(motor, agent);
                bool groundedGate = footRayOk && motorOrAgentOk;

                Gizmos.color = groundedGate
                    ? new Color(0.25f, 0.95f, 0.35f, 0.9f)
                    : new Color(0.95f, 0.35f, 0.25f, 0.9f);
                Gizmos.DrawLine(origin, origin + Vector3.down * maxDist);

                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                Gizmos.DrawWireSphere(origin, 0.02f);

                if (footRayOk)
                {
                    Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.95f);
                    Gizmos.DrawWireSphere(groundHitPv.point, 0.035f);
                    Gizmos.DrawLine(origin, groundHitPv.point);
                }
            }

            if (HasAnyId(vfxEffectId))
            {
                Vector3 originV = origin + new Vector3(0.03f, 0f, 0f);
                MovementEffectChannelSettings t = GetVfxTune();
                float maxDist = Mathf.Max(0.05f, t.groundRayLength);
                bool footRayOk = TryFootRayHitGroundNonAlloc(originV, maxDist, mask, out RaycastHit groundHitV);
                bool motorOrAgentOk = GetMotorOrAgentGroundOk(motor, agent);
                bool groundedGate = footRayOk && motorOrAgentOk;

                Gizmos.color = groundedGate
                    ? new Color(0.25f, 0.85f, 0.45f, 0.85f)
                    : new Color(0.95f, 0.35f, 0.25f, 0.85f);
                Gizmos.DrawLine(originV, originV + Vector3.down * maxDist);

                Gizmos.color = new Color(0.95f, 0.9f, 0.55f, 0.95f);
                Gizmos.DrawWireSphere(originV, 0.018f);

                if (footRayOk)
                {
                    Gizmos.color = new Color(0.35f, 0.95f, 0.85f, 0.95f);
                    Gizmos.DrawWireSphere(groundHitV.point, 0.032f);
                    Gizmos.DrawLine(originV, groundHitV.point);
                }
            }

            if (HasAnyId(audioEffectId))
            {
                Vector3 originA = origin + new Vector3(0.06f, 0f, 0f);
                MovementEffectChannelSettings t = GetAudioTune();
                float maxDist = Mathf.Max(0.05f, t.groundRayLength);
                bool footRayOkA = TryFootRayHitGroundNonAlloc(originA, maxDist, mask, out RaycastHit groundHitA);
                bool motorOrAgentOkA = GetMotorOrAgentGroundOk(motor, agent);
                bool groundedGateA = footRayOkA && motorOrAgentOkA;

                Gizmos.color = groundedGateA
                    ? new Color(0.35f, 0.55f, 1f, 0.85f)
                    : new Color(1f, 0.45f, 0.35f, 0.85f);
                Gizmos.DrawLine(originA, originA + Vector3.down * maxDist);

                Gizmos.color = new Color(0.95f, 0.75f, 0.15f, 0.95f);
                Gizmos.DrawWireSphere(originA, 0.018f);

                if (footRayOkA)
                {
                    Gizmos.color = new Color(0.5f, 0.65f, 1f, 0.95f);
                    Gizmos.DrawWireSphere(groundHitA.point, 0.03f);
                    Gizmos.DrawLine(originA, groundHitA.point);
                }
            }
        }

        private static bool GetMotorOrAgentGroundOk(KinematicCharacterMotor motor, NavMeshAgent agent)
        {
            if (!Application.isPlaying)
                return true;
            if (motor != null)
                return motor.GroundingStatus.IsStableOnGround;
            if (agent != null)
                return agent.isOnNavMesh;
            return true;
        }
    }
}
