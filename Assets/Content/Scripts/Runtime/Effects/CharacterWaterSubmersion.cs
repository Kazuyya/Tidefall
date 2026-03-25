using UnityEngine;

namespace LittleHeroJourney
{
    public enum WaterSubmersionLevel
    {
        None = 0,
        Shallow = 1,
        Deep = 2,
    }

    public class CharacterWaterSubmersion : MonoBehaviour
    {
        [Header("Water")]
        [SerializeField] private LayerMask waterLayers;

        [Tooltip("Added to raycast hit Y. Use when the collider is taller than the visible water surface — this is the playable water height for submersion.")]
        [SerializeField] private float trueWaterSurfaceOffset = -0.5f;

        [Header("Submersion depth")]
        [SerializeField] private float deepThreshold = 0.5f;

        [Header("Body check")]
        [SerializeField] private Vector3 bodyCheckOffset = new Vector3(0f, 1f, 0f);

        [SerializeField] private float bodyCheckRadius = 0.5f;

        [SerializeField] private float bodyCheckHeight = 1.8f;

        [Header("Debug gizmo")]
        [SerializeField] private bool showGizmos = true;

        public bool IsSubmerged { get; private set; }

        public WaterVolumeKind? WaterKind { get; private set; }

        public WaterSubmersionLevel SubmersionLevel { get; private set; }

        public float CurrentDepth { get; private set; }

        private Transform _root;

        private void Awake()
        {
            _root = transform.root;
        }

        private void LateUpdate()
        {
            if (waterLayers.value == 0)
            {
                IsSubmerged = false;
                WaterKind = null;
                SubmersionLevel = WaterSubmersionLevel.None;
                CurrentDepth = 0f;
                return;
            }

            if (!TryGetWaterSurface(out float surfaceY, out RaycastHit hit))
            {
                IsSubmerged = false;
                WaterKind = null;
                SubmersionLevel = WaterSubmersionLevel.None;
                CurrentDepth = 0f;
                return;
            }

            WaterKind = WaterVolume.ResolveKind(hit.collider);

            Vector3 bottom = GetBodyCheckBottomWorld();
            CurrentDepth = Mathf.Max(0f, surfaceY - bottom.y);
            IsSubmerged = CurrentDepth > 0f;

            if (!IsSubmerged)
            {
                SubmersionLevel = WaterSubmersionLevel.None;
                return;
            }

            float threshold = Mathf.Max(0f, deepThreshold);
            SubmersionLevel = CurrentDepth < threshold
                ? WaterSubmersionLevel.Shallow
                : WaterSubmersionLevel.Deep;
        }

        private static float BodyCheckHalfHeight(float height)
        {
            return Mathf.Max(0.01f, height) * 0.5f;
        }

        private Vector3 GetBodyCheckWorldCenter()
        {
            Transform root = _root != null ? _root : transform.root;
            return root.position + bodyCheckOffset;
        }

        private Vector3 GetBodyCheckBottomWorld()
        {
            Vector3 center = GetBodyCheckWorldCenter();
            float half = BodyCheckHalfHeight(bodyCheckHeight);
            return new Vector3(center.x, center.y - half, center.z);
        }

        private Vector3 GetBodyCheckTopWorld()
        {
            Vector3 center = GetBodyCheckWorldCenter();
            float half = BodyCheckHalfHeight(bodyCheckHeight);
            return new Vector3(center.x, center.y + half, center.z);
        }

        private float GetWaterRaycastMaxDistance()
        {
            return Mathf.Max(0.01f, bodyCheckHeight);
        }

        private bool TryGetWaterSurface(out float surfaceY, out RaycastHit hit)
        {
            surfaceY = 0f;
            hit = default;

            Vector3 origin = GetBodyCheckTopWorld();
            if (!Physics.Raycast(origin, Vector3.down, out hit, GetWaterRaycastMaxDistance(), waterLayers, QueryTriggerInteraction.Collide))
                return false;

            surfaceY = hit.point.y + trueWaterSurfaceOffset;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos)
                return;

            DrawBodyCheckCylinderGizmo();
            DrawDeepThresholdGizmo();
            DrawWaterSurfaceDebugGizmo();
        }

        private void DrawBodyCheckCylinderGizmo()
        {
            Transform root = Application.isPlaying && _root != null ? _root : transform.root;
            Vector3 center = root.position + bodyCheckOffset;
            float r = Mathf.Max(0f, bodyCheckRadius);
            float h = bodyCheckHeight;

            Gizmos.color = new Color(0.75f, 0.75f, 0.78f, 0.65f);
            DrawWireCylinderGizmo(center, r, h, Vector3.up);
        }

        private void DrawWaterSurfaceDebugGizmo()
        {
            if (waterLayers.value == 0)
                return;

            if (!TryGetWaterSurface(out float trueSurfaceY, out RaycastHit hit))
                return;

            Vector3 top = GetBodyCheckTopWorld();
            Vector3 bottom = GetBodyCheckBottomWorld();
            Vector3 hitPoint = hit.point;
            Vector3 trueWaterPoint = new Vector3(hit.point.x, trueSurfaceY, hit.point.z);
            const float rHit = 0.055f;
            const float rTrue = 0.065f;
            const float rEnd = 0.055f;

            Gizmos.color = new Color(0.45f, 0.95f, 0.35f, 0.95f);
            Gizmos.DrawWireSphere(top, rEnd);

            Gizmos.color = new Color(1f, 0.82f, 0.2f, 0.95f);
            Gizmos.DrawWireSphere(bottom, rEnd);

            Gizmos.color = new Color(0.25f, 0.88f, 1f, 0.95f);
            Gizmos.DrawWireSphere(hitPoint, rHit);

            Gizmos.color = new Color(0.55f, 0.45f, 0.95f, 0.95f);
            Gizmos.DrawWireSphere(trueWaterPoint, rTrue);

            Gizmos.color = new Color(0.45f, 0.45f, 0.5f, 0.75f);
            Gizmos.DrawLine(top, hitPoint);

            if (Mathf.Abs(trueWaterSurfaceOffset) > 1e-4f)
            {
                Gizmos.color = new Color(0.35f, 0.35f, 0.4f, 0.65f);
                Gizmos.DrawLine(hitPoint, trueWaterPoint);
            }
        }

        private void DrawDeepThresholdGizmo()
        {
            Vector3 bottom = GetBodyCheckBottomWorld();
            float threshold = Mathf.Max(0f, deepThreshold);
            Vector3 deepBoundaryPoint = new Vector3(bottom.x, bottom.y + threshold, bottom.z);
            float r = Mathf.Max(0.02f, bodyCheckRadius);

            Gizmos.color = new Color(0.95f, 0.35f, 0.95f, 0.95f);
            DrawCircle3D(deepBoundaryPoint, Vector3.right, Vector3.forward, r);
            Gizmos.DrawWireSphere(deepBoundaryPoint, 0.03f);
            Gizmos.DrawLine(bottom, deepBoundaryPoint);
        }

        private static void DrawWireCylinderGizmo(Vector3 center, float radius, float height, Vector3 up)
        {
            up = up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
            float r = Mathf.Max(0f, radius);
            float halfH = BodyCheckHalfHeight(height);

            Vector3 bottomCenter = center - up * halfH;
            Vector3 topCenter = center + up * halfH;

            Vector3 right = Vector3.Cross(up, Vector3.forward);
            if (right.sqrMagnitude < 1e-6f)
                right = Vector3.Cross(up, Vector3.right);
            right.Normalize();
            Vector3 forward = Vector3.Cross(right, up).normalized;

            DrawCircle3D(bottomCenter, right, forward, r);
            DrawCircle3D(topCenter, right, forward, r);

            for (int i = 0; i < 4; i++)
            {
                float ang = i * Mathf.PI * 0.5f;
                Vector3 offset = right * Mathf.Cos(ang) * r + forward * Mathf.Sin(ang) * r;
                Gizmos.DrawLine(bottomCenter + offset, topCenter + offset);
            }
        }

        private static void DrawCircle3D(Vector3 center, Vector3 right, Vector3 forward, float radius, int segments = 24)
        {
            Vector3 prev = center + right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                Vector3 next = center + right * Mathf.Cos(t) * radius + forward * Mathf.Sin(t) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
