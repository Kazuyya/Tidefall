using System;
using UnityEngine;

namespace LittleHeroJourney
{
    [Serializable]
    public struct MovementEffectChannelSettings
    {
        public float minSpeed;

        public float groundRayLength;

        public float emitInterval;
    }
}
