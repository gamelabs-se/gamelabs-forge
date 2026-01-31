namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Optional interface for ScriptableObjects to implement custom validation logic.
    /// If a template implements this interface, FORGE will call ValidateForgeItem after generation
    /// and allow the AI to retry if validation fails.
    /// </summary>
    public interface IForgeValidatable
    {
        /// <summary>
        /// Validates the generated item. Return null if valid, or an error message string if invalid.
        /// The error message will be sent back to the AI for retry attempts.
        /// </summary>
        /// <returns>Null if valid, error message string if validation fails.</returns>
        string ValidateForgeItem();
    }
}
