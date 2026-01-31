using System.Collections.Generic;

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
        /// Validates the generated item. Add error messages to the list for any validation failures.
        /// Leave the list empty if the item is valid.
        /// The error messages will be sent back to the AI for retry attempts.
        /// </summary>
        /// <param name="validationErrors">List to add error messages to. Empty list = valid item.</param>
        void ValidateForgeItem(List<string> validationErrors);
    }
}
