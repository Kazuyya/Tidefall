namespace LittleHeroJourney
{
    /// <summary>
    /// Interface for lock-on point definition
    /// Attach LockOnPoint component to a child GameObject (e.g., head)
    /// This allows flexible lock-on positioning instead of targeting root transform
    /// </summary>
    public interface ILockOnTarget
    {
        /// <summary>
        /// Get the transform to lock-on to (e.g., head position)
        /// </summary>
        UnityEngine.Transform GetLockOnTransform();

        /// <summary>
        /// Get the Health component of this target
        /// </summary>
        Health GetHealth();
    }
}
