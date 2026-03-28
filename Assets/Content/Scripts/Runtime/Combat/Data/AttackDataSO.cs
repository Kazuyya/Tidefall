using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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


    [CreateAssetMenu(fileName = "AttackData", menuName = "Little Hero Journey/Combat/Attack Data")]
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

    [Tooltip("Time windows when attack can be interrupted (start-end in animation 0-1).")]
    public List<Vector2> interruptibleWindows = new List<Vector2>();

    [Tooltip("Time windows when movement input is disabled (start-end in animation 0-1).")]
    public List<Vector2> movementDisableWindows = new List<Vector2>();

    [FormerlySerializedAs("interruptibleWindow")]
    [SerializeField, HideInInspector] private Vector2 _legacyInterruptibleWindow = new Vector2(0.0f, 0.7f);
    [FormerlySerializedAs("movementDisableWindow")]
    [SerializeField, HideInInspector] private Vector2 _legacyMovementDisableWindow = new Vector2(0.0f, 0.8f);

    [Tooltip("Reset combo if no input during animation?")]
    public bool resetComboOnAnimationEnd = true;

    [Header("Effects (String-based references to SO Sets)")]
    [Tooltip("Particle effects triggered during animation")]
    public List<ParticleEffectTiming> particleEffects = new List<ParticleEffectTiming>();

    [Tooltip("VFX effects triggered during animation")]
    public List<VFXEffectTiming> vfxEffects = new List<VFXEffectTiming>();

    [Tooltip("Audio effects triggered during animation")]
    public List<AudioEffectTiming> audioEffects = new List<AudioEffectTiming>();

    [Tooltip("Trail effects (enable/disable window; id = Trail.trailId on weapon)")]
    public List<TrailEffectTiming> trailEffects = new List<TrailEffectTiming>();

    [Header("AI Combo Settings")]
    [Tooltip("Probability for AI to continue combo after this attack (0-1). Only used by AI, player uses inputWindow instead")]
    [Range(0f, 1f)]
    public float aiComboContinueChance = 1.0f;

    public bool IsInterruptibleAt(float normalizedTime, float epsilon = 0.001f) =>
        IsWithinAnyWindow(normalizedTime, interruptibleWindows, epsilon);

    public bool IsMovementDisabledAt(float normalizedTime, float epsilon = 0.001f) =>
        IsWithinAnyWindow(normalizedTime, movementDisableWindows, epsilon);

    private static bool IsWithinAnyWindow(float t, List<Vector2> windows, float epsilon)
    {
        if (windows == null || windows.Count == 0) return false;
        for (int i = 0; i < windows.Count; i++)
        {
            float a = Mathf.Min(windows[i].x, windows[i].y);
            float b = Mathf.Max(windows[i].x, windows[i].y);
            if (t >= (a - epsilon) && t <= (b + epsilon))
                return true;
        }
        return false;
    }

    private void OnValidate()
    {
        if ((interruptibleWindows == null || interruptibleWindows.Count == 0) && _legacyInterruptibleWindow != Vector2.zero)
        {
            if (interruptibleWindows == null) interruptibleWindows = new List<Vector2>();
            interruptibleWindows.Add(_legacyInterruptibleWindow);
        }
        if ((movementDisableWindows == null || movementDisableWindows.Count == 0) && _legacyMovementDisableWindow != Vector2.zero)
        {
            if (movementDisableWindows == null) movementDisableWindows = new List<Vector2>();
            movementDisableWindows.Add(_legacyMovementDisableWindow);
        }
        if (weaponTimings != null)
        {
            for (int i = 0; i < weaponTimings.Count; i++)
            {
                Vector2 w = weaponTimings[i].colliderTriggerWindow;
                float a = Mathf.Clamp01(w.x);
                float b = Mathf.Clamp01(w.y);
                if (a > b) { float t = a; a = b; b = t; }
                weaponTimings[i].colliderTriggerWindow = new Vector2(a, b);
            }
        }
        if (trailEffects != null)
        {
            for (int i = 0; i < trailEffects.Count; i++)
            {
                Vector2 w = trailEffects[i].triggerWindow;
                float a = Mathf.Clamp01(w.x);
                float b = Mathf.Clamp01(w.y);
                if (a > b) { float t = a; a = b; b = t; }
                trailEffects[i].triggerWindow = new Vector2(a, b);
            }
        }
    }
    }
}
