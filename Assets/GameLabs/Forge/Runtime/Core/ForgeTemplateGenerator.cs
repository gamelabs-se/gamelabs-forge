using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameLabs.Forge.Integration.OpenAI;

namespace GameLabs.Forge
{
    /// <summary>
    /// Template-based item generator that uses ScriptableObject templates
    /// to generate new items. No reflection-based binding required.
    /// </summary>
    [ExecuteAlways]
    public class ForgeTemplateGenerator : MonoBehaviour
    {
        [Header("Generator Settings")]
        [SerializeField] private ForgeGeneratorSettings settings = new ForgeGeneratorSettings();
        
        /// <summary>Current settings for generation.</summary>
        public ForgeGeneratorSettings Settings => settings;
        
        private static ForgeTemplateGenerator _instance;
        
        /// <summary>Singleton instance of the template generator.</summary>
        public static ForgeTemplateGenerator Instance
        {
            get
            {
                if (_instance != null) return _instance;
                
                _instance = FindFirstObjectByType<ForgeTemplateGenerator>();
                if (_instance != null) return _instance;
                
#if UNITY_EDITOR
                var go = new GameObject("~ForgeTemplateGenerator");
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.AddComponent<ForgeTemplateGenerator>();
#else
                var go = new GameObject("ForgeTemplateGenerator");
                _instance = go.AddComponent<ForgeTemplateGenerator>();
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
        /// Generates items based on a ScriptableObject template.
        /// </summary>
        /// <param name="template">The ScriptableObject template to use for schema extraction.</param>
        /// <param name="count">Number of items to generate.</param>
        /// <param name="callback">Callback with generated ScriptableObject instances.</param>
        /// <param name="additionalContext">Optional context for generation.</param>
        public void GenerateFromTemplate(
            ScriptableObject template, 
            int count,
            Action<ForgeTemplateGenerationResult> callback,
            string additionalContext = "")
        {
            if (template == null)
            {
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Template cannot be null."));
                return;
            }
            
            StartCoroutine(GenerateFromTemplateCoroutine(template, count, callback, additionalContext));
        }
        
        private IEnumerator GenerateFromTemplateCoroutine(
            ScriptableObject template,
            int count,
            Action<ForgeTemplateGenerationResult> callback,
            string additionalContext)
        {
            var client = ForgeOpenAIClient.Instance;
            
            // Configure client
            client.SetModel(settings.model);
            client.SetTemperature(settings.temperature);
            client.SetSystemRole(BuildSystemPrompt());
            
            // Extract schema from template type
            var templateType = template.GetType();
            var schema = ForgeSchemaExtractor.ExtractSchema(templateType);
            
            // Build the user prompt
            var prompt = BuildUserPrompt(schema, count, additionalContext);
            
            ForgeLogger.Log($"Generating {count} {templateType.Name} item(s) from template...");
            ForgeLogger.Log($"Template type: {templateType.FullName}");
            ForgeLogger.Log($"Schema fields: {schema.fields.Count}");
            
            ForgeTemplateGenerationResult result = null;
            bool completed = false;
            
            client.Chat(prompt, response =>
            {
                result = ProcessResponse(response, templateType, count);
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
8. Respect all value ranges and enum constraints provided.";
        }
        
        private string BuildUserPrompt(ForgeSchemaExtractor.TypeSchema schema, int count, string additionalContext)
        {
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
            
            // Additional context
            if (!string.IsNullOrEmpty(additionalContext))
            {
                sb.AppendLine("=== ADDITIONAL CONTEXT ===");
                sb.AppendLine(additionalContext);
                sb.AppendLine();
            }
            
            // Generation request
            sb.AppendLine("=== REQUEST ===");
            if (count == 1)
            {
                sb.AppendLine($"Generate exactly 1 unique {schema.typeName}.");
                sb.AppendLine("Respond with a single JSON object (not an array).");
            }
            else
            {
                sb.AppendLine($"Generate exactly {count} unique {schema.typeName} items.");
                sb.AppendLine("Respond with a JSON array containing all items.");
            }
            
            return sb.ToString();
        }
        
        private ForgeTemplateGenerationResult ProcessResponse(
            ForgeOpenAIClient.OpenAIResponse response,
            Type templateType,
            int expectedCount)
        {
            if (response == null)
                return ForgeTemplateGenerationResult.Error("No response from API.");
            
            if (response.choices == null || response.choices.Count == 0)
                return ForgeTemplateGenerationResult.Error("Empty choices in response.");
            
            var content = response.choices[0].message?.content;
            if (string.IsNullOrEmpty(content))
                return ForgeTemplateGenerationResult.Error("Empty content in response.");
            
            // Clean up the content (remove markdown if present)
            content = CleanJsonContent(content);
            
            ForgeLogger.Log($"Raw response:\n{content}");
            
            try
            {
                var items = new List<ScriptableObject>();
                
                if (expectedCount == 1)
                {
                    // Single item - parse as object
                    ForgeLogger.Log("Parsing single item from JSON...");
                    var item = CreateAndPopulateScriptableObject(templateType, content);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
                else
                {
                    // Batch - parse as array
                    ForgeLogger.Log($"Parsing {expectedCount} items from JSON array...");
                    items = ParseJsonArray(templateType, content);
                    ForgeLogger.Log($"Parsed {items.Count} items from JSON");
                }
                
                int promptTokens = response.usage?.prompt_tokens ?? 0;
                int completionTokens = response.usage?.completion_tokens ?? 0;
                
                ForgeLogger.Log($"Successfully created {items.Count} ScriptableObject(s). Tokens: {promptTokens} prompt, {completionTokens} completion.");
                
                return ForgeTemplateGenerationResult.Success(items, templateType, promptTokens, completionTokens);
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create ScriptableObjects: {e.Message}");
                return ForgeTemplateGenerationResult.Error($"JSON parsing failed: {e.Message}\nContent: {content}");
            }
        }
        
        private ScriptableObject CreateAndPopulateScriptableObject(Type type, string json)
        {
            try
            {
                var instance = ScriptableObject.CreateInstance(type);
                if (instance == null)
                {
                    ForgeLogger.Error($"Failed to create instance of {type.Name}");
                    return null;
                }
                
                // Use Unity's JsonUtility to populate the instance
                JsonUtility.FromJsonOverwrite(json, instance);
                
                // Try to extract a name from the JSON to set as the asset name
                string assetName = ExtractNameFromJson(json, type);
                if (!string.IsNullOrEmpty(assetName))
                {
                    instance.name = SanitizeAssetName(assetName);
                }
                else
                {
                    instance.name = $"{type.Name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                }
                
                ForgeLogger.Log($"Created ScriptableObject: {instance.name} ({type.Name})");
                
                return instance;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create and populate {type.Name}: {e.Message}");
                return null;
            }
        }
        
        private string ExtractNameFromJson(string json, Type type)
        {
            try
            {
                // Try to find common name fields
                var nameFields = new[] { "name", "weaponName", "itemName", "displayName", "title" };
                
                foreach (var fieldName in nameFields)
                {
                    // Check if the type has this field
                    var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (field != null && field.FieldType == typeof(string))
                    {
                        // Try to extract the value from JSON using a simple regex
                        var pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"]+)\"";
                        var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
                        if (match.Success && match.Groups.Count > 1)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors and return empty
            }
            
            return null;
        }
        
        private string SanitizeAssetName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unnamed";
            
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-' && chars[i] != ' ')
                {
                    chars[i] = '_';
                }
            }
            
            var result = new string(chars).Trim();
            
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            
            return string.IsNullOrEmpty(result) ? "Unnamed" : result;
        }
        
        private List<ScriptableObject> ParseJsonArray(Type type, string json)
        {
            var items = new List<ScriptableObject>();
            
            try
            {
                // Wrap the array in an object for JsonUtility
                var wrapped = $"{{\"items\":{json}}}";
                
                // Use a generic wrapper approach
                var wrapperType = typeof(JsonArrayWrapper<>).MakeGenericType(typeof(Dictionary<string, object>));
                
                // Parse as raw dictionaries first, then convert
                // This is a workaround since JsonUtility doesn't support root arrays
                // We'll parse each item individually
                var arrayMatch = System.Text.RegularExpressions.Regex.Match(json, @"^\s*\[(.*)\]\s*$", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (arrayMatch.Success)
                {
                    // Extract individual JSON objects
                    var arrayContent = arrayMatch.Groups[1].Value;
                    var objects = SplitJsonArray(arrayContent);
                    
                    foreach (var objJson in objects)
                    {
                        var item = CreateAndPopulateScriptableObject(type, objJson);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to parse JSON array: {e.Message}");
            }
            
            return items;
        }
        
        private List<string> SplitJsonArray(string arrayContent)
        {
            var result = new List<string>();
            var depth = 0;
            var currentObject = new StringBuilder();
            var inString = false;
            var escapeNext = false;
            
            foreach (char c in arrayContent)
            {
                if (escapeNext)
                {
                    currentObject.Append(c);
                    escapeNext = false;
                    continue;
                }
                
                if (c == '\\')
                {
                    currentObject.Append(c);
                    escapeNext = true;
                    continue;
                }
                
                if (c == '"')
                {
                    inString = !inString;
                    currentObject.Append(c);
                    continue;
                }
                
                if (!inString)
                {
                    if (c == '{')
                    {
                        depth++;
                        currentObject.Append(c);
                    }
                    else if (c == '}')
                    {
                        depth--;
                        currentObject.Append(c);
                        
                        if (depth == 0)
                        {
                            result.Add(currentObject.ToString().Trim());
                            currentObject.Clear();
                        }
                    }
                    else if (c == ',' && depth == 0)
                    {
                        // Skip commas between objects
                        continue;
                    }
                    else
                    {
                        currentObject.Append(c);
                    }
                }
                else
                {
                    currentObject.Append(c);
                }
            }
            
            // Add any remaining object
            if (currentObject.Length > 0)
            {
                var remaining = currentObject.ToString().Trim();
                if (!string.IsNullOrEmpty(remaining))
                {
                    result.Add(remaining);
                }
            }
            
            return result;
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
        
        [Serializable]
        private class JsonArrayWrapper<T>
        {
            public List<T> items;
        }
    }
    
    /// <summary>
    /// Result of a template-based generation request.
    /// </summary>
    [Serializable]
    public class ForgeTemplateGenerationResult
    {
        public bool success;
        public string errorMessage;
        public List<ScriptableObject> items = new List<ScriptableObject>();
        public Type itemType;
        public int promptTokens;
        public int completionTokens;
        public float estimatedCost;
        
        public static ForgeTemplateGenerationResult Error(string message)
        {
            return new ForgeTemplateGenerationResult
            {
                success = false,
                errorMessage = message
            };
        }
        
        public static ForgeTemplateGenerationResult Success(
            List<ScriptableObject> items,
            Type itemType,
            int promptTokens = 0,
            int completionTokens = 0)
        {
            return new ForgeTemplateGenerationResult
            {
                success = true,
                items = items,
                itemType = itemType,
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
