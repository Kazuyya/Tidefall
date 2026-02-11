using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Character death effects - triggered with list of timed effects synced to animation, per type
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDeathEffects", menuName = "Little Hero Journey/Effects/Character Death Effects", order = 3)]
    public class CharacterDeathEffectsSO : ScriptableObject
    {
        [SerializeField]
        private List<VFXEffectTiming> vfxEffects = new List<VFXEffectTiming>();

        [SerializeField]
        private List<AudioEffectTiming> audioEffects = new List<AudioEffectTiming>();

        [SerializeField]
        private List<ParticleEffectTiming> particleEffects = new List<ParticleEffectTiming>();

        public IReadOnlyList<VFXEffectTiming> VFXEffects => vfxEffects;
        public IReadOnlyList<AudioEffectTiming> AudioEffects => audioEffects;
        public IReadOnlyList<ParticleEffectTiming> ParticleEffects => particleEffects;
    }
}
