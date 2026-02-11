using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [System.Serializable]
    public class WeaponTiming
    {
        [Tooltip("Name of the weapon (matches PlayerCombat.availableWeapons)")]
        public string weaponName;

        [Tooltip("Time window when this weapon's collider is active (start-end in animation 0-1)")]
        public Vector2 colliderTriggerWindow = new Vector2(0.3f, 0.7f);
    }


    [CreateAssetMenu(fileName = "AttackData", menuName = "Combat/Attack Data")]
    public class AttackDataSO : ScriptableObject
    {
    public enum AttackType
    {
        Light,    // Fast, low damage, quick recovery
        Heavy,    // Slow, high damage, long recovery
        Special,  // Unique effects, balanced stats
        Ultimate  // Powerful finisher, long cooldown
    }

    public string attackName;
    public AttackType attackType;

    public AnimationClip attackAnimation;
    public string animationTriggerName = "Attack";

    [Header("Weapon Configuration")]
    [Tooltip("List of weapons with their individual timing windows")]
    public List<WeaponTiming> weaponTimings = new List<WeaponTiming>();

    [Header("Damage Configuration")]
    [Tooltip("Damage data for this attack (base damage, critical chance, etc.)")]
    public DamageData attackDamageData;

    [Header("Timing Windows (Normalized 0-1)")]
    [Tooltip("Time window for next combo input (start-end in animation 0-1)")]
    public Vector2 inputWindow = new Vector2(0.4f, 0.8f);

    [Tooltip("Time window when attack can be interrupted (start-end in animation 0-1)")]
    public Vector2 interruptibleWindow = new Vector2(0.0f, 0.7f);

    [Tooltip("Time window when movement input is disabled (start-end in animation 0-1)")]
    public Vector2 movementDisableWindow = new Vector2(0.0f, 0.8f);

    [Tooltip("Reset combo if no input during animation?")]
    public bool resetComboOnAnimationEnd = true;

    [Header("Effects (String-based references to SO Sets)")]
    [Tooltip("Particle effects triggered during animation")]
    public List<ParticleEffectTiming> particleEffects = new List<ParticleEffectTiming>();

    [Tooltip("VFX effects triggered during animation")]
    public List<VFXEffectTiming> vfxEffects = new List<VFXEffectTiming>();

    [Tooltip("Audio effects triggered during animation")]
    public List<AudioEffectTiming> audioEffects = new List<AudioEffectTiming>();

    [Header("AI Combo Settings")]
    [Tooltip("Probability for AI to continue combo after this attack (0-1). Only used by AI, player uses inputWindow instead")]
    [Range(0f, 1f)]
    public float aiComboContinueChance = 1.0f;
    }
}