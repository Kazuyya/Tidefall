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

        private NavMeshAgent _agent;
        private KinematicCharacterMotor _motor;
        private CharacterWaterSubmersion _waterSubmersion;
        private Vector3 _prevHorizontalPos;
        private float _emitTimerParticle;
        private float _emitTimerVfx;
        private float _emitTimerAudio;

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            _motor = GetComponentInParent<KinematicCharacterMotor>();
            _waterSubmersion = GetComponentInParent<CharacterWaterSubmersion>();

            var p = transform.position;
            _prevHorizontalPos = new Vector3(p.x, 0f, p.z);
        }

        private void LateUpdate()
        {
            if (!HasAnyEffectId())
                return;

            Vector3 horizontalVel = GetHorizontalVelocity();
            float speedSq = horizontalVel.sqrMagnitude;

            if (_waterSubmersion != null && _waterSubmersion.IsSubmerged)
            {
                _emitTimerParticle = 0f;
                _emitTimerVfx = 0f;
                _emitTimerAudio = 0f;
                return;
            }

            bool playParticle = false;
            if (HasAnyId(particleEffectId))
            {
                MovementEffectChannelSettings t = GetParticleTune();
                if (speedSq >= t.minSpeed * t.minSpeed && IsGrounded(t.groundRayLength))
                {
                    _emitTimerParticle += Time.deltaTime;
                    if (_emitTimerParticle >= t.emitInterval)
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

            bool playVfx = false;
            if (HasAnyId(vfxEffectId))
            {
                MovementEffectChannelSettings t = GetVfxTune();
                if (speedSq >= t.minSpeed * t.minSpeed && IsGrounded(t.groundRayLength))
                {
                    _emitTimerVfx += Time.deltaTime;
                    if (_emitTimerVfx >= t.emitInterval)
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

            bool playAudio = false;
            if (HasAnyId(audioEffectId))
            {
                MovementEffectChannelSettings t = GetAudioTune();
                if (speedSq >= t.minSpeed * t.minSpeed && IsGrounded(t.groundRayLength))
                {
                    _emitTimerAudio += Time.deltaTime;
                    if (_emitTimerAudio >= t.emitInterval)
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

            if (!playParticle && !playVfx && !playAudio)
                return;

            PlayEffects(playParticle, playVfx, playAudio);
        }

        private MovementEffectChannelSettings GetParticleTune() =>
            particleUseCustomSettings ? particleCustom : globalSettings;

        private MovementEffectChannelSettings GetVfxTune() =>
            vfxUseCustomSettings ? vfxCustom : globalSettings;

        private MovementEffectChannelSettings GetAudioTune() =>
            audioUseCustomSettings ? audioCustom : globalSettings;

        private static bool HasAnyId(string id) => !string.IsNullOrEmpty(id);

        private bool HasAnyEffectId() =>
            HasAnyId(particleEffectId) || HasAnyId(vfxEffectId) || HasAnyId(audioEffectId);

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
            if (!TryFootRayHitGround(origin, maxDist, mask, out _))
                return false;

            if (_motor != null)
                return _motor.GroundingStatus.IsStableOnGround;

            if (_agent != null)
                return _agent.isOnNavMesh;

            return true;
        }

        private bool TryFootRayHitGround(Vector3 origin, float maxDist, int layerMask, out RaycastHit groundHit)
        {
            groundHit = default;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDist, layerMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Transform root = transform.root;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null) continue;
                if (c.transform == root || c.transform.IsChildOf(root))
                    continue;

                groundHit = hits[i];
                return true;
            }

            return false;
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
            bool footGlobal = TryFootRayHitGround(origin, globalDist, mask, out RaycastHit hitGlobal);
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
                bool footRayOk = TryFootRayHitGround(origin, maxDist, mask, out RaycastHit groundHitPv);
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
                bool footRayOk = TryFootRayHitGround(originV, maxDist, mask, out RaycastHit groundHitV);
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
                bool footRayOkA = TryFootRayHitGround(originA, maxDist, mask, out RaycastHit groundHitA);
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
