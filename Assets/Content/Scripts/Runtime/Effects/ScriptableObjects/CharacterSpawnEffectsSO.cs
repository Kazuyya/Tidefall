using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Character spawn effects - triggered immediately with list of timed effects per type
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSpawnEffects", menuName = "Little Hero Journey/Effects/Character Spawn Effects", order = 2)]
    public class CharacterSpawnEffectsSO : ScriptableObject
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
