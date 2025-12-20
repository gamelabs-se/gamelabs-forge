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
        public ForgeAIModel model = ForgeAIModel.GPT4o;
        
        [Header("Item Context")]
        [Tooltip("Additional context or rules for item generation.")]
        [TextArea(2, 4)]
        public string additionalRules = "";
        
        [Header("Asset Paths")]
        [Tooltip("Base path to search for existing assets (relative to Assets folder). Default: 'Assets'")]
        public string existingAssetsSearchPath = "Assets";
        
        [Tooltip("Base path for generated assets (relative to Assets folder). Default: 'Resources/Generated'")]
        public string generatedAssetsBasePath = "Resources/Generated";
        
        [Header("Existing Items Context")]
        [Tooltip("If true, automatically looks for existing assets of the same type and adds them to context.")]
        public bool autoLoadExistingAssets = true;
        
        [Tooltip("How to use existing items in generation")]
        public ExistingItemsIntent intent = ExistingItemsIntent.PreventDuplicatesAndRefineNaming;
        
        /// <summary>
        /// Serialized list of existing items as JSON strings.
        /// Used to provide context to the AI about what items already exist.
        /// </summary>
        [HideInInspector]
        public List<string> existingItemsJson = new List<string>();
        
        // Internal HashSet for efficient duplicate checking
        [NonSerialized]
        private System.Collections.Generic.HashSet<string> _existingItemsSet = null;
        
        /// <summary>
        /// Gets or initializes the internal HashSet for efficient operations.
        /// </summary>
        private System.Collections.Generic.HashSet<string> GetExistingItemsSet()
        {
            if (_existingItemsSet == null)
            {
                _existingItemsSet = new System.Collections.Generic.HashSet<string>(existingItemsJson);
            }
            return _existingItemsSet;
        }
        
        /// <summary>
        /// Adds an existing item to the context.
        /// Uses HashSet internally for O(1) duplicate checking.
        /// </summary>
        public void AddExistingItem<T>(T item) where T : class
        {
            var json = JsonUtility.ToJson(item);
            var itemSet = GetExistingItemsSet();
            
            if (itemSet.Add(json))
            {
                existingItemsJson.Add(json);
            }
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
            _existingItemsSet = null;
        }
        
        /// <summary>
        /// Gets the existing items context as a formatted string.
        /// </summary>
        public string GetExistingItemsContext()
        {
            if (existingItemsJson == null || existingItemsJson.Count == 0)
                return "No existing items provided.";
            
            string intentInstruction = GetIntentInstruction();
            return $"Existing items ({existingItemsJson.Count} total) - {intentInstruction}:\n[\n{string.Join(",\n", existingItemsJson)}\n]";
        }
        
        /// <summary>
        /// Gets the instruction text for the AI based on the intent setting.
        /// </summary>
        private string GetIntentInstruction()
        {
            return intent switch
            {
                ExistingItemsIntent.PreventDuplicates => "Generate UNIQUE items that don't duplicate existing ones",
                ExistingItemsIntent.RefineNaming => "Use these items as examples to match naming conventions and style",
                ExistingItemsIntent.PreventDuplicatesAndRefineNaming => "Generate UNIQUE items while following the naming conventions and style of existing items",
                _ => "Use for reference"
            };
        }
    }
    
    /// <summary>
    /// Defines how existing items should be used during generation.
    /// </summary>
    [Serializable]
    public enum ExistingItemsIntent
    {
        [Tooltip("Generate unique items that don't duplicate existing ones")]
        PreventDuplicates,
        
        [Tooltip("Use existing items as examples to refine naming accuracy and style")]
        RefineNaming,
        
        [Tooltip("Both prevent duplicates AND use existing items to guide naming conventions")]
        PreventDuplicatesAndRefineNaming
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
            // Use GPT-4o pricing as default (will be recalculated with correct model in result)
            return ForgeAIModelHelper.CalculateCost(ForgeAIModel.GPT4o, prompt, completion);
        }
    }
}
