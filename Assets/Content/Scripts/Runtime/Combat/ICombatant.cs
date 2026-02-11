namespace LittleHeroJourney
{
    /// <summary>
    /// Interface untuk mengakses combat state dari berbagai entity (Player, AI, etc)
    /// Menggantikan reflection pattern dengan type-safe interface
    /// </summary>
    public interface ICombatant
    {
        bool IsAttacking { get; }
        
        /// <summary>
        /// Returns true jika entity bisa di-interrupt (trigger damaged animation).
        /// Returns false jika entity punya Super Armor (tetap kena damage tapi tidak interrupt attack).
        /// </summary>
        bool IsInterruptible { get; }
        
        /// <summary>
        /// Called ketika entity di-interrupt oleh damage.
        /// Harus reset attack state, disable weapon colliders, dll.
        /// </summary>
        void OnInterrupted();
    }
}
