using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using GameLabs.Forge.Integration.OpenAI;

namespace GameLabs.Forge
{
    /// <summary>
    /// Main orchestrator for dynamic item generation.
    /// Supports generating any item type that inherits from ForgeItemDefinition
    /// or any plain C# class.
    /// </summary>
    [ExecuteAlways]
    public class ForgeItemGenerator : MonoBehaviour
    {
        [Header("Generator Settings")]
        [SerializeField] private ForgeGeneratorSettings settings = new ForgeGeneratorSettings();
        
        /// <summary>Current settings for generation.</summary>
        public ForgeGeneratorSettings Settings => settings;
        
        private static ForgeItemGenerator _instance;
        
        /// <summary>Singleton instance of the generator.</summary>
        public static ForgeItemGenerator Instance
        {
            get
            {
                if (_instance != null) return _instance;
                
                _instance = FindFirstObjectByType<ForgeItemGenerator>();
                if (_instance != null) return _instance;
                
#if UNITY_EDITOR
                var go = new GameObject("~ForgeItemGenerator");
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.AddComponent<ForgeItemGenerator>();
#else
                var go = new GameObject("ForgeItemGenerator");
                _instance = go.AddComponent<ForgeItemGenerator>();
                DontDestroyOnLoad(go);
#endif
                return _instance;
            }
        }
        
        private void OnEnable()
        {
            // Load settings from config file if available
            var configSettings = ForgeConfig.GetGeneratorSettings();
            if (configSettings != null && !string.IsNullOrEmpty(configSettings.gameName))
            {
                settings = configSettings;
            }
        }
        
        /// <summary>
        /// Generates a single item of the specified type.
        /// </summary>
        public void GenerateSingle<T>(Action<ForgeGenerationResult<T>> callback, string additionalContext = "") where T : class, new()
        {
            var request = ForgeGenerationRequest.Single<T>(additionalContext);
            Generate(request, callback);
        }
        
        /// <summary>
        /// Generates a batch of items of the specified type.
        /// </summary>
        public void GenerateBatch<T>(int count, Action<ForgeGenerationResult<T>> callback, string additionalContext = "") where T : class, new()
        {
            var request = ForgeGenerationRequest.Batch<T>(count, additionalContext);
            Generate(request, callback);
        }
        
        /// <summary>
        /// Generates items based on the provided request.
        /// </summary>
        public void Generate<T>(ForgeGenerationRequest request, Action<ForgeGenerationResult<T>> callback) where T : class, new()
        {
            if (request.itemType != typeof(T))
            {
                callback?.Invoke(ForgeGenerationResult<T>.Error("Request type mismatch."));
                return;
            }
            
            // Auto-load existing assets if enabled
            if (settings.autoLoadExistingAssets)
            {
                LoadExistingAssetsIntoContext<T>();
            }
            
            StartCoroutine(GenerateCoroutine(request, callback));
        }
        
        /// <summary>
        /// Loads existing assets of the specified type into the generation context.
        /// This helps the AI generate items that complement existing ones.
        /// </summary>
        public void LoadExistingAssetsIntoContext<T>() where T : class
        {
            if (string.IsNullOrEmpty(settings.existingAssetsSearchPath))
            {
                return;
            }
            
            try
            {
                // First, try to find ScriptableObject assets of type T
                if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
                {
                    var method = typeof(ForgeAssetDiscovery).GetMethod("DiscoverAssets");
                    var genericMethod = method.MakeGenericMethod(typeof(T));
                    var discoveredAssets = genericMethod.Invoke(null, new object[] { settings.existingAssetsSearchPath });
                    
                    if (discoveredAssets != null)
                    {
                        var assetList = discoveredAssets as System.Collections.IEnumerable;
                        if (assetList != null)
                        {
                            foreach (var asset in assetList)
                            {
                                if (asset != null)
                                {
                                    var json = JsonUtility.ToJson(asset);
                                    if (!string.IsNullOrEmpty(json) && !settings.existingItemsJson.Contains(json))
                                    {
                                        settings.existingItemsJson.Add(json);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Also check for ForgeGeneratedItemAsset instances of this type
                var generatedAssets = ForgeAssetDiscovery.DiscoverGeneratedAssets(typeof(T).Name, settings.existingAssetsSearchPath);
                var jsonStrings = ForgeAssetDiscovery.GeneratedAssetsToJsonStrings(generatedAssets);
                
                foreach (var json in jsonStrings)
                {
                    if (!settings.existingItemsJson.Contains(json))
                    {
                        settings.existingItemsJson.Add(json);
                    }
                }
                
                if (settings.existingItemsJson.Count > 0)
                {
                    ForgeLogger.Log($"Loaded {settings.existingItemsJson.Count} existing items into generation context");
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Warn($"Failed to load existing assets into context: {e.Message}");
            }
        }
        
        private IEnumerator GenerateCoroutine<T>(ForgeGenerationRequest request, Action<ForgeGenerationResult<T>> callback) where T : class, new()
        {
            var client = ForgeOpenAIClient.Instance;
            
            // Configure client
            client.SetModel(settings.model);
            client.SetTemperature(settings.temperature);
            client.SetSystemRole(BuildSystemPrompt());
            
            // Build the user prompt
            var prompt = BuildUserPrompt<T>(request);
            
            ForgeLogger.Log($"Generating {request.count} {typeof(T).Name} item(s)...");
            
            ForgeGenerationResult<T> result = null;
            bool completed = false;
            
            client.Chat(prompt, response =>
            {
                result = ProcessResponse<T>(response, request.count);
                completed = true;
            });
            
            // Wait for completion
            while (!completed)
                yield return null;
            
            callback?.Invoke(result);
        }
        
        private string BuildSystemPrompt()
        {
            return @"You are a game item generation API. Your job is to generate game items based on provided schemas and context.

CRITICAL RULES:
1. ALWAYS respond with valid JSON that matches the exact structure requested.
2. For single items, respond with a JSON object.
3. For multiple items, respond with a JSON array.
4. DO NOT include any text before or after the JSON.
5. DO NOT use markdown code blocks.
6. Ensure all field names match exactly as specified.
7. Generate creative, balanced, and game-appropriate content.
8. Respect any value ranges or constraints provided.";
        }
        
        private string BuildUserPrompt<T>(ForgeGenerationRequest request) where T : class
        {
            var schema = ForgeSchemaExtractor.ExtractSchema<T>();
            var template = ForgeSchemaExtractor.GenerateJsonTemplate(schema);
            var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);
            
            var sb = new StringBuilder();
            
            // Game context
            sb.AppendLine("=== GAME CONTEXT ===");
            sb.AppendLine($"Game: {settings.gameName}");
            sb.AppendLine($"Description: {settings.gameDescription}");
            sb.AppendLine($"Audience: {settings.targetAudience}");
            
            if (!string.IsNullOrEmpty(settings.additionalRules))
            {
                sb.AppendLine($"Additional Rules: {settings.additionalRules}");
            }
            sb.AppendLine();
            
            // Item schema
            sb.AppendLine("=== ITEM SCHEMA ===");
            sb.AppendLine(schemaDesc);
            sb.AppendLine();
            
            sb.AppendLine("=== JSON TEMPLATE ===");
            sb.AppendLine(template);
            sb.AppendLine();
            
            // Existing items context
            if (settings.existingItemsJson.Count > 0)
            {
                sb.AppendLine("=== EXISTING ITEMS (for reference, generate different items) ===");
                sb.AppendLine(settings.GetExistingItemsContext());
                sb.AppendLine();
            }
            
            // Field overrides
            if (request.fieldOverrides != null && request.fieldOverrides.Count > 0)
            {
                sb.AppendLine("=== FIELD CONSTRAINTS ===");
                foreach (var ov in request.fieldOverrides)
                {
                    sb.Append($"- {ov.fieldName}:");
                    if (!string.IsNullOrEmpty(ov.customInstruction))
                        sb.Append($" {ov.customInstruction}");
                    if (ov.minValue.HasValue || ov.maxValue.HasValue)
                        sb.Append($" [range: {ov.minValue?.ToString() ?? "any"} to {ov.maxValue?.ToString() ?? "any"}]");
                    if (ov.allowedValues != null && ov.allowedValues.Length > 0)
                        sb.Append($" [allowed: {string.Join(", ", ov.allowedValues)}]");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            
            // Additional context
            if (!string.IsNullOrEmpty(request.additionalContext))
            {
                sb.AppendLine("=== ADDITIONAL CONTEXT ===");
                sb.AppendLine(request.additionalContext);
                sb.AppendLine();
            }
            
            // Generation request
            sb.AppendLine("=== REQUEST ===");
            if (request.count == 1)
            {
                sb.AppendLine($"Generate exactly 1 unique {schema.typeName}.");
                sb.AppendLine("Respond with a single JSON object (not an array).");
            }
            else
            {
                sb.AppendLine($"Generate exactly {request.count} unique {schema.typeName} items.");
                sb.AppendLine("Respond with a JSON array containing all items.");
            }
            
            return sb.ToString();
        }
        
        private ForgeGenerationResult<T> ProcessResponse<T>(ForgeOpenAIClient.OpenAIResponse response, int expectedCount) where T : class, new()
        {
            if (response == null)
                return ForgeGenerationResult<T>.Error("No response from API.");
            
            if (response.choices == null || response.choices.Count == 0)
                return ForgeGenerationResult<T>.Error("Empty choices in response.");
            
            var content = response.choices[0].message?.content;
            if (string.IsNullOrEmpty(content))
                return ForgeGenerationResult<T>.Error("Empty content in response.");
            
            // Clean up the content (remove markdown if present)
            content = CleanJsonContent(content);
            
            ForgeLogger.Log($"Raw response:\n{content}");
            
            try
            {
                var items = new List<T>();
                
                if (expectedCount == 1)
                {
                    // Single item - parse as object
                    var item = JsonUtility.FromJson<T>(content);
                    if (item != null)
                    {
                        if (item is ForgeItemDefinition fid)
                            fid.OnDeserialized();
                        items.Add(item);
                    }
                }
                else
                {
                    // Batch - parse as array using wrapper
                    items = ParseJsonArray<T>(content);
                    foreach (var item in items)
                    {
                        if (item is ForgeItemDefinition fid)
                            fid.OnDeserialized();
                    }
                }
                
                int promptTokens = response.usage?.prompt_tokens ?? 0;
                int completionTokens = response.usage?.completion_tokens ?? 0;
                
                ForgeLogger.Log($"Successfully parsed {items.Count} item(s). Tokens: {promptTokens} prompt, {completionTokens} completion.");
                
                return ForgeGenerationResult<T>.Success(items, promptTokens, completionTokens);
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to parse items: {e.Message}");
                return ForgeGenerationResult<T>.Error($"JSON parsing failed: {e.Message}\nContent: {content}");
            }
        }
        
        private string CleanJsonContent(string content)
        {
            // Remove markdown code blocks
            content = content.Trim();
            
            // Remove ```json or ``` markers at the start
            if (content.StartsWith("```"))
            {
                var firstNewline = content.IndexOf('\n');
                if (firstNewline > 0)
                {
                    content = content.Substring(firstNewline + 1);
                }
                else
                {
                    // No newline found, try to find the first { or [
                    var jsonStart = content.IndexOfAny(new[] { '{', '[' });
                    if (jsonStart > 0)
                        content = content.Substring(jsonStart);
                }
            }
            
            // Remove trailing ``` markers
            if (content.EndsWith("```"))
            {
                content = content.Substring(0, content.Length - 3);
            }
            
            return content.Trim();
        }
        
        /// <summary>
        /// Parses a JSON array into a list of items.
        /// Unity's JsonUtility doesn't support root-level arrays, so we use a wrapper.
        /// </summary>
        private List<T> ParseJsonArray<T>(string json) where T : class
        {
            // Wrap the array in an object
            var wrapped = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
            return wrapper?.items ?? new List<T>();
        }
        
        [Serializable]
        private class JsonArrayWrapper<T>
        {
            public List<T> items;
        }
        
        /// <summary>
        /// Updates the generator settings.
        /// </summary>
        public void UpdateSettings(ForgeGeneratorSettings newSettings)
        {
            settings = newSettings;
        }
        
        /// <summary>
        /// Adds existing items to provide context for generation.
        /// </summary>
        public void AddExistingItems<T>(IEnumerable<T> items) where T : class
        {
            settings.AddExistingItems(items);
        }
        
        /// <summary>
        /// Clears existing items context.
        /// </summary>
        public void ClearExistingItems()
        {
            settings.ClearExistingItems();
        }
    }
}
