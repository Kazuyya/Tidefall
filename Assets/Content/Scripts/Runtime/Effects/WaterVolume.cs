using UnityEngine;

namespace LittleHeroJourney
{
    public enum WaterVolumeKind
    {
        Normal = 0,
        Murky = 1,
    }

    public class WaterVolume : MonoBehaviour
    {
        [SerializeField] private WaterVolumeKind kind = WaterVolumeKind.Normal;

        public WaterVolumeKind Kind => kind;

        public static WaterVolumeKind ResolveKind(Collider collider)
        {
            if (collider == null)
                return WaterVolumeKind.Normal;

            WaterVolume volume = collider.GetComponent<WaterVolume>();
            if (volume == null)
                volume = collider.GetComponentInParent<WaterVolume>();

            return volume != null ? volume.Kind : WaterVolumeKind.Normal;
        }
    }
}
