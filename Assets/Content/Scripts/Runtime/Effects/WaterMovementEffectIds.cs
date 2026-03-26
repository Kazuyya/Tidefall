using System;

namespace LittleHeroJourney
{
    [Serializable]
    public struct WaterMovementEffectIds
    {
        public string particleEffectId;

        public string vfxEffectId;

        public string audioEffectId;

        public bool HasAny() =>
            !string.IsNullOrEmpty(particleEffectId) ||
            !string.IsNullOrEmpty(vfxEffectId) ||
            !string.IsNullOrEmpty(audioEffectId);
    }
}
