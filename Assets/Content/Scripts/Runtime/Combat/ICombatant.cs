namespace LittleHeroJourney
{
    /// <summary>
    /// Interface to access combat state from any entity (Player, AI, etc).
    /// Replaces reflection with a type-safe interface.
    /// </summary>
    public interface ICombatant
    {
        bool IsAttacking { get; }
        
        /// <summary>
        /// True if entity can be interrupted (trigger damaged animation).
        /// False if entity has Super Armor (still takes damage but attack is not interrupted).
        /// </summary>
        bool IsInterruptible { get; }
        
        /// <summary>
        /// Called when entity is interrupted by damage.
        /// Must reset attack state, disable weapon colliders, etc.
        /// </summary>
        void OnInterrupted();
    }
}
