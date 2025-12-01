using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Settings for the item generator. Stores game context, existing items,
    /// and generation constraints.
    /// </summary>
    [Serializable]
    public class ForgeGeneratorSettings
    {
        [Header("Game Context")]
        [Tooltip("The name of your game.")]
        public string gameName = "My Game";
        
        [Tooltip("Describe your game's setting, theme, and style.")]
        [TextArea(3, 6)]
        public string gameDescription = "A fantasy RPG with dark themes.";
        
        [Tooltip("Target audience or tone (e.g., casual, hardcore, kids, mature).")]
        public string targetAudience = "General";
        
        [Header("Generation Settings")]
        [Tooltip("Default number of items to generate in batch mode.")]
        [Min(1)]
        public int defaultBatchSize = 5;
        
        [Tooltip("Maximum items per request (to stay within token limits).")]
        [Min(1)]
        public int maxBatchSize = 20;
        
        [Tooltip("AI temperature (0 = deterministic, 2 = creative).")]
        [Range(0f, 2f)]
        public float temperature = 0.8f;
        
        [Tooltip("AI model to use.")]
        public string model = "gpt-4o-mini";
        
        [Header("Item Context")]
        [Tooltip("Additional context or rules for item generation.")]
        [TextArea(2, 4)]
        public string additionalRules = "";
        
        /// <summary>
        /// Serialized list of existing items as JSON strings.
        /// Used to provide context to the AI about what items already exist.
        /// </summary>
        [HideInInspector]
        public List<string> existingItemsJson = new List<string>();
        
        /// <summary>
        /// Adds an existing item to the context.
        /// </summary>
        public void AddExistingItem<T>(T item) where T : class
        {
            var json = JsonUtility.ToJson(item);
            if (!existingItemsJson.Contains(json))
                existingItemsJson.Add(json);
        }
        
        /// <summary>
        /// Adds multiple existing items to the context.
        /// </summary>
        public void AddExistingItems<T>(IEnumerable<T> items) where T : class
        {
            foreach (var item in items)
                AddExistingItem(item);
        }
        
        /// <summary>
        /// Clears all existing items from the context.
        /// </summary>
        public void ClearExistingItems()
        {
            existingItemsJson.Clear();
        }
        
        /// <summary>
        /// Gets the existing items context as a formatted string.
        /// </summary>
        public string GetExistingItemsContext()
        {
            if (existingItemsJson == null || existingItemsJson.Count == 0)
                return "No existing items provided.";
                
            return $"Existing items ({existingItemsJson.Count} total):\n[\n{string.Join(",\n", existingItemsJson)}\n]";
        }
    }
    
    /// <summary>
    /// Field override for customizing generation constraints at runtime.
    /// </summary>
    [Serializable]
    public class ForgeFieldOverride
    {
        public string fieldName;
        public string customInstruction;
        public float? minValue;
        public float? maxValue;
        public string[] allowedValues;
    }
    
    /// <summary>
    /// Request object for item generation.
    /// </summary>
    [Serializable]
    public class ForgeGenerationRequest
    {
        /// <summary>Type of item to generate.</summary>
        public Type itemType;
        
        /// <summary>Number of items to generate.</summary>
        public int count = 1;
        
        /// <summary>Additional prompt context for this specific request.</summary>
        public string additionalContext = "";
        
        /// <summary>Field-specific overrides for this request.</summary>
        public List<ForgeFieldOverride> fieldOverrides = new List<ForgeFieldOverride>();
        
        /// <summary>
        /// Creates a request for a single item.
        /// </summary>
        public static ForgeGenerationRequest Single<T>(string context = "") where T : class
        {
            return new ForgeGenerationRequest
            {
                itemType = typeof(T),
                count = 1,
                additionalContext = context
            };
        }
        
        /// <summary>
        /// Creates a request for a batch of items.
        /// </summary>
        public static ForgeGenerationRequest Batch<T>(int count, string context = "") where T : class
        {
            return new ForgeGenerationRequest
            {
                itemType = typeof(T),
                count = count,
                additionalContext = context
            };
        }
    }
    
    /// <summary>
    /// Result of an item generation request.
    /// </summary>
    [Serializable]
    public class ForgeGenerationResult<T> where T : class
    {
        public bool success;
        public string errorMessage;
        public List<T> items = new List<T>();
        public int promptTokens;
        public int completionTokens;
        public float estimatedCost;
        
        public static ForgeGenerationResult<T> Error(string message)
        {
            return new ForgeGenerationResult<T>
            {
                success = false,
                errorMessage = message
            };
        }
        
        public static ForgeGenerationResult<T> Success(List<T> items, int promptTokens = 0, int completionTokens = 0)
        {
            return new ForgeGenerationResult<T>
            {
                success = true,
                items = items,
                promptTokens = promptTokens,
                completionTokens = completionTokens,
                estimatedCost = CalculateCost(promptTokens, completionTokens)
            };
        }
        
        private static float CalculateCost(int prompt, int completion)
        {
            // GPT-4o-mini pricing (as of late 2024): $0.15/1M input, $0.60/1M output
            return (prompt * 0.00000015f) + (completion * 0.0000006f);
        }
    }
}
