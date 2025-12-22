using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameLabs.Forge.Editor.Integration.OpenAI;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Template-based item generator that uses ScriptableObject templates
    /// to generate new items. No reflection-based binding required.
    /// </summary>
    public class ForgeTemplateGenerator
    {
        private ForgeGeneratorSettings settings = new ForgeGeneratorSettings();

        /// <summary>Current settings for generation.</summary>
        public ForgeGeneratorSettings Settings => settings;

        private static ForgeTemplateGenerator _instance;

        /// <summary>Singleton instance of the template generator.</summary>
        public static ForgeTemplateGenerator Instance
        {
            get
            {
                if (_instance != null) return _instance;

                try
                {
                    ForgeLogger.DebugLog("Creating new ForgeTemplateGenerator instance");
                    _instance = new ForgeTemplateGenerator();
                    
                    // Load settings from EditorPrefs
                    _instance.settings = ForgeConfig.GetGeneratorSettings();
                    if (_instance.settings == null)
                    {
                        _instance.settings = new ForgeGeneratorSettings();
                    }
                    
                    ForgeLogger.DebugLog("ForgeTemplateGenerator instance created successfully");
                }
                catch (System.Exception e)
                {
                    ForgeLogger.Error($"Exception creating ForgeTemplateGenerator instance: {e.Message}\n{e.StackTrace}");
                    return null;
                }
                
                return _instance;
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

            ForgeEditorCoroutine.Start(GenerateFromTemplateCoroutine(template, count, callback, additionalContext));
        }

        /// <summary>
        /// Generates items based on a ForgeBlueprint, which includes template, instructions, and duplicate handling.
        /// </summary>
        /// <param name="blueprint">The blueprint containing template, instructions, and settings.</param>
        /// <param name="count">Number of items to generate.</param>
        /// <param name="callback">Callback with generated ScriptableObject instances.</param>
        public void GenerateFromBlueprint(
            ForgeBlueprint blueprint,
            int count,
            Action<ForgeTemplateGenerationResult> callback)
        {
            if (blueprint == null)
            {
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Blueprint cannot be null."));
                return;
            }

            if (blueprint.Template == null)
            {
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Blueprint template cannot be null."));
                return;
            }

            ForgeEditorCoroutine.Start(GenerateFromBlueprintCoroutine(blueprint, count, callback));
        }

        private IEnumerator GenerateFromBlueprintCoroutine(
            ForgeBlueprint blueprint,
            int count,
            Action<ForgeTemplateGenerationResult> callback)
        {
            var client = ForgeOpenAIClient.Instance;

            // Configure client
            string modelName = ForgeAIModelHelper.GetModelName(settings.model);
            client.SetModel(modelName);
            client.SetTemperature(settings.temperature);
            client.SetSystemRole(BuildSystemPrompt());

            // Extract schema from blueprint template
            var templateType = blueprint.Template.GetType();
            var schema = ForgeSchemaExtractor.ExtractSchema(templateType);

            // Build the user prompt with blueprint strategy
            var prompt = BuildBlueprintPrompt(schema, count, blueprint);

            ForgeLogger.DebugLog($"Generating {count} {templateType.Name} item(s) from blueprint '{blueprint.DisplayName}'...");
            ForgeLogger.DebugLog($"Duplicate strategy: {blueprint.DuplicateStrategy}");
            ForgeLogger.DebugLog($"Schema fields: {schema.fields.Count}");

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

        private IEnumerator GenerateFromTemplateCoroutine(
            ScriptableObject template,
            int count,
            Action<ForgeTemplateGenerationResult> callback,
            string additionalContext)
        {
            var client = ForgeOpenAIClient.Instance;

            // Configure client
            string modelName = ForgeAIModelHelper.GetModelName(settings.model);
            client.SetModel(modelName);
            client.SetTemperature(settings.temperature);
            client.SetSystemRole(BuildSystemPrompt());

            // Extract schema from template type
            var templateType = template.GetType();
            var schema = ForgeSchemaExtractor.ExtractSchema(templateType);

            // Build the user prompt
            var prompt = BuildUserPrompt(schema, count, additionalContext);

            ForgeLogger.DebugLog($"Generating {count} {templateType.Name} item(s) from template...");
            ForgeLogger.DebugLog($"Template type: {templateType.FullName}");
            ForgeLogger.DebugLog($"Schema fields: {schema.fields.Count}");

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

            // Item schema - this is the most important part
            sb.AppendLine("=== ITEM SCHEMA ===");
            sb.AppendLine(schemaDesc);
            sb.AppendLine();

            sb.AppendLine("=== JSON TEMPLATE ===");
            sb.AppendLine(template);
            sb.AppendLine();

            // Existing items context (CRITICAL for preventing duplicates)
            if (settings.existingItemsJson != null && settings.existingItemsJson.Count > 0)
            {
                sb.AppendLine("=== EXISTING ITEMS ===");
                sb.AppendLine(settings.GetExistingItemsContext());
                sb.AppendLine();
            }

            // Additional context from user (if provided)
            if (!string.IsNullOrEmpty(additionalContext))
            {
                sb.AppendLine("=== GENERATION CONTEXT ===");
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

            sb.AppendLine();
            sb.AppendLine("IMPORTANT:");
            sb.AppendLine("- For enum fields, use ONLY the allowed values specified in the schema.");
            sb.AppendLine("- Respect all [Range] constraints for numeric fields.");
            sb.AppendLine("- Use the field descriptions as guidance for appropriate values.");

            // Add explicit duplicate prevention instruction if existing items are present
            if (settings.existingItemsJson != null && settings.existingItemsJson.Count > 0)
            {
                sb.AppendLine("- DO NOT duplicate any items from the EXISTING ITEMS list above.");
                sb.AppendLine("- Generate completely NEW and UNIQUE items that are different from existing ones.");
            }

            return sb.ToString();
        }

        private string BuildBlueprintPrompt(ForgeSchemaExtractor.TypeSchema schema, int count, ForgeBlueprint blueprint)
        {
            var template = ForgeSchemaExtractor.GenerateJsonTemplate(schema);
            var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);

            var sb = new StringBuilder();

            // Item schema
            sb.AppendLine("=== ITEM SCHEMA ===");
            sb.AppendLine(schemaDesc);
            sb.AppendLine();

            sb.AppendLine("=== JSON TEMPLATE ===");
            sb.AppendLine(template);
            sb.AppendLine();

            // Existing items context based on duplicate strategy
            if (blueprint.DuplicateStrategy != ForgeDuplicateStrategy.Ignore && blueprint.ExistingItems.Count > 0)
            {
                if (blueprint.DuplicateStrategy == ForgeDuplicateStrategy.NamesOnly)
                {
                    sb.AppendLine("=== EXISTING ITEM NAMES (AVOID THESE) ===");
                    foreach (var item in blueprint.ExistingItems)
                    {
                        if (item != null)
                        {
                            string nameValue = item.name;
                            var nameProp = item.GetType().GetProperty("Name");
                            if (nameProp != null)
                            {
                                nameValue = nameProp.GetValue(item)?.ToString() ?? item.name;
                            }
                            else
                            {
                                var nameField = item.GetType().GetField("Name");
                                if (nameField != null)
                                {
                                    nameValue = nameField.GetValue(item)?.ToString() ?? item.name;
                                }
                            }
                            sb.AppendLine($"- {nameValue}");
                        }
                    }
                    sb.AppendLine();
                    sb.AppendLine("IMPORTANT: Avoid creating items with names matching those listed above.");
                    sb.AppendLine();
                }
                else if (blueprint.DuplicateStrategy == ForgeDuplicateStrategy.FullComposition)
                {
                    sb.AppendLine("=== EXISTING ITEMS (AVOID DUPLICATING) ===");
                    foreach (var item in blueprint.ExistingItems)
                    {
                        if (item != null)
                        {
                            sb.AppendLine(JsonUtility.ToJson(item, true));
                        }
                    }
                    sb.AppendLine();
                    sb.AppendLine("IMPORTANT: Do NOT create items that match the above items in structure or values.");
                    sb.AppendLine();
                }
            }

            // Blueprint-specific instructions
            if (!string.IsNullOrEmpty(blueprint.Instructions))
            {
                sb.AppendLine("=== CUSTOM INSTRUCTIONS ===");
                sb.AppendLine(blueprint.Instructions);
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

            sb.AppendLine();
            sb.AppendLine("IMPORTANT:");
            sb.AppendLine("- For enum fields, use ONLY the allowed values specified in the schema.");
            sb.AppendLine("- Respect all [Range] constraints for numeric fields.");
            sb.AppendLine("- Use the field descriptions as guidance for appropriate values.");

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

            ForgeLogger.DebugLog($"Raw response:\n{content}");

            try
            {
                var items = new List<ScriptableObject>();

                if (expectedCount == 1)
                {
                    // Single item - parse as object
                    ForgeLogger.DebugLog("Parsing single item from JSON...");
                    var item = CreateAndPopulateScriptableObject(templateType, content);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
                else
                {
                    // Batch - parse as array
                    ForgeLogger.DebugLog($"Parsing {expectedCount} items from JSON array...");
                    items = ParseJsonArray(templateType, content);
                    ForgeLogger.DebugLog($"Parsed {items.Count} items from JSON");
                }

                int promptTokens = response.usage?.prompt_tokens ?? 0;
                int completionTokens = response.usage?.completion_tokens ?? 0;

                ForgeLogger.Success($"Generated {items.Count} item(s). Tokens: {promptTokens} prompt, {completionTokens} completion.");

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

                // Preprocess JSON to convert enum string values to integers
                // Unity's JsonUtility requires enums as integers, not strings
                json = ConvertEnumStringsToIntegers(json, type);

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

                ForgeLogger.DebugLog($"Created ScriptableObject: {instance.name} ({type.Name})");

                return instance;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create and populate {type.Name}: {e.Message}");
                return null;
            }
        }

        private string ConvertEnumStringsToIntegers(string json, Type type)
        {
            try
            {
                // Get all enum fields in the type
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType.IsEnum)
                    {
                        // Find the pattern: "fieldName":"EnumValue"
                        var enumValues = Enum.GetNames(field.FieldType);

                        foreach (var enumValue in enumValues)
                        {
                            var pattern = $"\"{field.Name}\"\\s*:\\s*\"{enumValue}\"";
                            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);

                            if (match.Success)
                            {
                                // Get the integer value for this enum
                                var enumIndex = Array.IndexOf(enumValues, enumValue);

                                // Replace the string with the integer
                                var replacement = $"\"{field.Name}\":{enumIndex}";
                                json = System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);

                                ForgeLogger.DebugLog($"Converted enum field '{field.Name}' from '{enumValue}' to {enumIndex}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Warn($"Failed to convert enum strings: {e.Message}");
            }

            return json;
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
