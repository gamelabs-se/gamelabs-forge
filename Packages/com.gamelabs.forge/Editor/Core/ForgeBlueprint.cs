using UnityEngine;
using System.Collections.Generic;

namespace GameLabs.Forge.Editor
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

        [HideInInspector]
        [SerializeField]
        private bool _overrideDuplicateStrategy = false;

        [SerializeField]
        private ForgeDuplicateStrategy _duplicateStrategy = ForgeDuplicateStrategy.Ignore;

        [SerializeField]
        private string _discoveryPathOverride = ""; // Empty = use global default

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
        /// Whether this blueprint overrides the global duplicate strategy.
        /// </summary>
        public bool OverrideDuplicateStrategy
        {
            get => _overrideDuplicateStrategy;
            set => _overrideDuplicateStrategy = value;
        }

        /// <summary>
        /// Strategy for handling duplicate prevention in generation requests.
        /// Only used if OverrideDuplicateStrategy is true.
        /// </summary>
        public ForgeDuplicateStrategy DuplicateStrategy
        {
            get => _duplicateStrategy;
            set => _duplicateStrategy = value;
        }

        /// <summary>
        /// Gets the effective duplicate strategy (override if set, otherwise global default).
        /// </summary>
        public ForgeDuplicateStrategy GetEffectiveDuplicateStrategy()
        {
            var result = _overrideDuplicateStrategy ? _duplicateStrategy : (ForgeConfig.GetGeneratorSettings()?.duplicateStrategy ?? ForgeDuplicateStrategy.Ignore);
            ForgeLogger.DebugLog($"GetEffectiveDuplicateStrategy: override={_overrideDuplicateStrategy}, blueprintStrat={_duplicateStrategy}, effective={result}");
            return result;
        }

        /// <summary>
        /// Discovery path override for this blueprint (empty = use global default).
        /// </summary>
        public string DiscoveryPathOverride
        {
            get => _discoveryPathOverride;
            set => _discoveryPathOverride = value;
        }

        /// <summary>
        /// Gets the effective discovery path (override if set, otherwise global default).
        /// </summary>
        public string GetEffectiveDiscoveryPath()
        {
            if (!string.IsNullOrEmpty(_discoveryPathOverride))
                return _discoveryPathOverride;

            var config = ForgeConfig.GetGeneratorSettings();
            return config?.existingAssetsSearchPath ?? "Assets";
        }

        /// <summary>
        /// Gets the display name for this blueprint (uses asset name if available).
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(name) ? "Unnamed Blueprint" : name;
    }
}
