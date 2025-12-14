using UnityEngine;
using System.Collections.Generic;

namespace GameLabs.Forge
{
    /// <summary>
    /// Defines how duplicate detection is handled during item generation.
    /// </summary>
    public enum ForgeDuplicateStrategy
    {
        /// <summary>Don't send any existing items to the API (lowest cost, default).</summary>
        Ignore,
        
        /// <summary>Send only item names with instruction to avoid them (low cost).</summary>
        NamesOnly,
        
        /// <summary>Send full item data with instruction to avoid identical compositions (higher cost, highest quality).</summary>
        FullComposition
    }

    /// <summary>
    /// A blueprint defines generation settings, instructions, and duplicate handling for a specific item type.
    /// Blueprints allow saving and reusing generation profiles across multiple generation runs.
    /// </summary>
    public class ForgeBlueprint : ScriptableObject
    {
        [SerializeField]
        private ScriptableObject _template;

        [TextArea(2, 5)]
        [SerializeField]
        private string _instructions = "";

        [SerializeField]
        private ForgeDuplicateStrategy _duplicateStrategy = ForgeDuplicateStrategy.Ignore;

        [SerializeField]
        private List<ScriptableObject> _existingItems = new List<ScriptableObject>();

        /// <summary>
        /// The template ScriptableObject that defines the item schema.
        /// </summary>
        public ScriptableObject Template
        {
            get => _template;
            set => _template = value;
        }

        /// <summary>
        /// Custom generation instructions to append to the schema prompt.
        /// </summary>
        public string Instructions
        {
            get => _instructions;
            set => _instructions = value;
        }

        /// <summary>
        /// Strategy for handling duplicate prevention in generation requests.
        /// </summary>
        public ForgeDuplicateStrategy DuplicateStrategy
        {
            get => _duplicateStrategy;
            set => _duplicateStrategy = value;
        }

        /// <summary>
        /// List of existing items to check/avoid during generation.
        /// </summary>
        public List<ScriptableObject> ExistingItems
        {
            get => _existingItems;
        }

        /// <summary>
        /// Adds an item to the existing items list (used after successful generation).
        /// </summary>
        public void RegisterExistingItem(ScriptableObject item)
        {
            if (item != null && !_existingItems.Contains(item))
            {
                _existingItems.Add(item);
            }
        }

        /// <summary>
        /// Clears the existing items list for fresh generation.
        /// </summary>
        public void ClearExistingItems()
        {
            _existingItems.Clear();
        }

        /// <summary>
        /// Gets the display name for this blueprint (uses asset name if available).
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(name) ? "Unnamed Blueprint" : name;
    }
}
