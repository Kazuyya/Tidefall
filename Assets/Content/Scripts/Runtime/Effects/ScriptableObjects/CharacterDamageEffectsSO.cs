using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "CharacterDamageEffects", menuName = "Little Hero Journey/Effects/Character Damage Effects", order = 2)]
    public class CharacterDamageEffectsSO : ScriptableObject
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
