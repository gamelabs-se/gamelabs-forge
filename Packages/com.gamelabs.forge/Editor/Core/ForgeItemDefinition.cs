using System;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Base class for all forgeable items. Inherit from this to create custom item types.
    /// The Forge system uses reflection to extract the schema from derived classes.
    /// </summary>
    [Serializable]
    public abstract class ForgeItemDefinition
    {
        /// <summary>Unique identifier for the generated item.</summary>
        public string id;
        
        /// <summary>Display name of the item.</summary>
        public string name;
        
        /// <summary>Description of the item.</summary>
        public string description;
        
        /// <summary>
        /// Called after deserialization to allow post-processing.
        /// Override to add custom validation or initialization.
        /// </summary>
        public virtual void OnDeserialized()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        
        /// <summary>
        /// Validates the item data. Override to add custom validation.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public virtual bool Validate()
        {
            return !string.IsNullOrEmpty(name);
        }
    }
}
