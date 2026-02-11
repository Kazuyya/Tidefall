using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace LittleHeroJourney
{
    /// <summary>
    /// VFX effect configuration with pooling support
    /// </summary>
    [System.Serializable]
    public class VFXEffectData
    {
        [Tooltip("Unique identifier for this VFX effect")]
        public string effectName;

        [Tooltip("Visual Effect prefab to instantiate")]
        public VisualEffect vfxPrefab;

        [Tooltip("Pool size for this effect")]
        [Min(1)]
        public int poolSize = 5;

        public bool IsValid => !string.IsNullOrEmpty(effectName) && vfxPrefab != null;
    }

    /// <summary>
    /// Scriptable Object for storing VFX effect configurations
    /// </summary>
    [CreateAssetMenu(fileName = "VFXSet", menuName = "Little Hero Journey/VFX/VFX Set", order = 1)]
    public class VFXSetSO : ScriptableObject
    {
        [Header("VFX Effects")]
        [SerializeField]
        private List<VFXEffectData> vfxEffects = new List<VFXEffectData>();

        public IReadOnlyList<VFXEffectData> VFXEffects => vfxEffects;

        public VFXEffectData GetVFXEffect(string effectName)
        {
            return vfxEffects.Find(e => e.effectName == effectName);
        }
    }
}
