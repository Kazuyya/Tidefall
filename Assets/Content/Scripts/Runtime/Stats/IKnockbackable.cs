using UnityEngine;

namespace LittleHeroJourney
{
    public interface IKnockbackable
    {
        void ApplyKnockback(Vector3 direction, float distance);
    }
}
