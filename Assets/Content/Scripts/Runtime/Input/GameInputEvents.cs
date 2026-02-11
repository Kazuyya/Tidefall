using System;
using UnityEngine;

namespace LittleHeroJourney.InputSystem
{
    /// <summary>
    /// Central event bus for player inputs.
    /// Decouples UI buttons from Player scripts.
    /// </summary>
    public static class GameInputEvents
    {
        // Events
        public static event Action OnAttack;
        public static event Action OnDash;
        
        // Methods to trigger events (called by UI Buttons)
        public static void TriggerAttack() => OnAttack?.Invoke();
        public static void TriggerDash() => OnDash?.Invoke();

        /// <summary>
        /// Clears all subscribers. Call this on Scene Unload/GameManager Reset 
        /// to prevent memory leaks if static events persist across scenes.
        /// </summary>
        public static void ClearListeners()
        {
            OnAttack = null;
            OnDash = null;
        }
    }
}