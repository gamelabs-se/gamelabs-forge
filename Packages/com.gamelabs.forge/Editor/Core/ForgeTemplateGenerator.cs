using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="sessionInstructions">Optional session-specific instructions (not persisted).</param>
        public void GenerateFromBlueprint(
            ForgeBlueprint blueprint,
            int count,
            Action<ForgeTemplateGenerationResult> callback,
            string sessionInstructions = null)
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

            ForgeEditorCoroutine.Start(GenerateFromBlueprintCoroutine(blueprint, count, callback, sessionInstructions));
        }

        private IEnumerator GenerateFromBlueprintCoroutine(
            ForgeBlueprint blueprint,
            int count,
            Action<ForgeTemplateGenerationResult> callback,
            string sessionInstructions = null)
        {
            var client = ForgeOpenAIClient.Instance;
            
            if (client == null)
            {
                ForgeLogger.Error("Failed to get ForgeOpenAIClient instance");
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Failed to initialize OpenAI client"));
                yield break;
            }

            // Configure client - use blueprint's effective model
            var effectiveModel = blueprint.GetEffectiveModel();
            string modelName = ForgeAIModelHelper.GetModelName(effectiveModel);
            client.SetModel(modelName);
            client.SetTemperature(settings.temperature);
            client.SetSystemRole(BuildSystemPrompt());

            // Extract schema from blueprint template
            var templateType = blueprint.Template.GetType();
            var schema = ForgeSchemaExtractor.ExtractSchema(templateType);

            // Build the user prompt with blueprint strategy
            var prompt = BuildBlueprintPrompt(schema, templateType, count, blueprint, sessionInstructions);

            ForgeLogger.DebugLog($"Generating {count} {templateType.Name} item(s) from blueprint '{blueprint.DisplayName}' using model {effectiveModel}...");
            var effectiveStrategy = blueprint.GetEffectiveDuplicateStrategy();
            bool isOverride = blueprint.OverrideDuplicateStrategy;
            var globalSettings = ForgeConfig.GetGeneratorSettings();
            var globalStrategy = globalSettings?.duplicateStrategy ?? ForgeDuplicateStrategy.Ignore;
            
            ForgeLogger.DebugLog($"Blueprint override enabled: {isOverride}");
            ForgeLogger.DebugLog($"Blueprint strategy: {blueprint.DuplicateStrategy}");
            ForgeLogger.DebugLog($"Global strategy: {globalStrategy}");
            ForgeLogger.DebugLog($"Effective strategy: {effectiveStrategy}");
            ForgeLogger.DebugLog($"Schema fields: {schema.fields.Count}");
            ForgeLogger.DebugLog($"Prompt length: {prompt.Length} characters");
            if (ForgeLogger.DebugEnabled)
            {
                ForgeLogger.DebugLog($"=== FULL PROMPT ===\n{prompt}\n=== END PROMPT ===");
            }

            ForgeTemplateGenerationResult result = null;
            int maxRetries = globalSettings?.maxValidationRetries ?? 3;
            
            // Retry loop for validation
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                bool completed = false;
                string currentPrompt = (attempt == 1) ? prompt : prompt; // Will be modified for retries

                client.Chat(currentPrompt, response =>
                {
                    result = ProcessResponse(response, templateType, count);
                    completed = true;
                });

                // Wait for completion
                while (!completed)
                    yield return null;

                // Check if generation succeeded
                if (!result.success)
                {
                    ForgeLogger.Warn($"Generation attempt {attempt}/{maxRetries} failed: {result.errorMessage}");
                    if (attempt < maxRetries)
                    {
                        yield return new UnityEngine.WaitForSeconds(1f); // Brief pause before retry
                        continue;
                    }
                    break;
                }

                // Validate items
                var validationErrors = ValidateItems(result.items);
                
                if (validationErrors.Count == 0)
                {
                    // All items valid - success!
                    ForgeLogger.Success($"All {result.items.Count} items validated successfully");
                    break;
                }
                
                // Validation failed
                if (attempt < maxRetries)
                {
                    ForgeLogger.Warn($"Validation failed on attempt {attempt}/{maxRetries}. Retrying with feedback...");
                    
                    // Build retry prompt with validation errors
                    var errorSummary = string.Join("\n", validationErrors);
                    prompt = prompt + $"\n\n=== VALIDATION ERRORS FROM PREVIOUS ATTEMPT ===\n{errorSummary}\n\nPlease fix these validation errors and regenerate the items.";
                    
                    yield return new UnityEngine.WaitForSeconds(1f); // Brief pause before retry
                }
                else
                {
                    // Max retries reached - return with validation errors
                    ForgeLogger.Error($"Max validation retries ({maxRetries}) reached. Items may have validation errors.");
                    result.errorMessage = $"Validation warnings:\n{string.Join("\n", validationErrors)}";
                }
            }

            callback?.Invoke(result);
        }
        
        /// <summary>
        /// Generates variants of an existing ScriptableObject item.
        /// </summary>
        /// <param name="sourceItem">The source item to create variants of.</param>
        /// <param name="count">Number of variants to generate.</param>
        /// <param name="variantInstructions">Instructions describing how variants should differ.</param>
        /// <param name="callback">Callback with generated ScriptableObject instances.</param>
        public void GenerateVariants(
            ScriptableObject sourceItem,
            int count,
            string variantInstructions,
            Action<ForgeTemplateGenerationResult> callback)
        {
            if (sourceItem == null)
            {
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Source item cannot be null."));
                return;
            }

            ForgeEditorCoroutine.Start(GenerateVariantsCoroutine(sourceItem, count, variantInstructions, callback));
        }
        
        private IEnumerator GenerateVariantsCoroutine(
            ScriptableObject sourceItem,
            int count,
            string variantInstructions,
            Action<ForgeTemplateGenerationResult> callback)
        {
            var client = ForgeOpenAIClient.Instance;
            
            if (client == null)
            {
                ForgeLogger.Error("Failed to get ForgeOpenAIClient instance");
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Failed to initialize OpenAI client"));
                yield break;
            }

            // Get effective model from global settings
            var globalSettings = ForgeConfig.GetGeneratorSettings();
            var effectiveModel = globalSettings?.model ?? ForgeAIModel.GPT5Mini;
            string modelName = ForgeAIModelHelper.GetModelName(effectiveModel);
            
            client.SetModel(modelName);
            client.SetTemperature(settings.temperature);
            client.SetSystemRole(BuildVariantSystemPrompt());

            // Extract schema and current values from source item
            var itemType = sourceItem.GetType();
            var schema = ForgeSchemaExtractor.ExtractSchema(itemType);
            var sourceJson = JsonUtility.ToJson(sourceItem, true);

            // Build the variant prompt
            var prompt = BuildVariantPrompt(schema, sourceItem, sourceJson, count, variantInstructions);

            ForgeLogger.DebugLog($"Generating {count} variants of {sourceItem.name} ({itemType.Name}) using model {effectiveModel}...");
            if (ForgeLogger.DebugEnabled)
            {
                ForgeLogger.DebugLog($"=== VARIANT PROMPT ===\n{prompt}\n=== END PROMPT ===");
            }

            ForgeTemplateGenerationResult result = null;
            int maxRetries = globalSettings?.maxValidationRetries ?? 3;
            
            // Retry loop for validation
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                bool completed = false;

                client.Chat(prompt, response =>
                {
                    result = ProcessResponse(response, itemType, count);
                    completed = true;
                });

                // Wait for completion
                while (!completed)
                    yield return null;

                // Check if generation succeeded
                if (!result.success)
                {
                    ForgeLogger.Warn($"Generation attempt {attempt}/{maxRetries} failed: {result.errorMessage}");
                    if (attempt < maxRetries)
                    {
                        yield return new UnityEngine.WaitForSeconds(1f);
                        continue;
                    }
                    break;
                }

                // Validate items
                var validationErrors = ValidateItems(result.items);
                
                if (validationErrors.Count == 0)
                {
                    ForgeLogger.Success($"All {result.items.Count} variants validated successfully");
                    break;
                }
                
                // Validation failed
                if (attempt < maxRetries)
                {
                    ForgeLogger.Warn($"Validation failed on attempt {attempt}/{maxRetries}. Retrying with feedback...");
                    var errorSummary = string.Join("\n", validationErrors);
                    prompt = prompt + $"\n\n=== VALIDATION ERRORS FROM PREVIOUS ATTEMPT ===\n{errorSummary}\n\nPlease fix these validation errors and regenerate the variants.";
                    yield return new UnityEngine.WaitForSeconds(1f);
                }
                else
                {
                    ForgeLogger.Error($"Max validation retries ({maxRetries}) reached. Variants may have validation errors.");
                    result.errorMessage = $"Validation warnings:\n{string.Join("\n", validationErrors)}";
                }
            }

            callback?.Invoke(result);
        }
        
        private string BuildVariantSystemPrompt()
        {
            return @"You are a game item variant generation API. Your job is to create variants of existing game items.

CRITICAL RULES:
1. ALWAYS respond with valid JSON that matches the exact structure of the source item.
2. For single variants, respond with a JSON object.
3. For multiple variants, respond with a JSON array.
4. DO NOT include any text before or after the JSON.
5. DO NOT use markdown code blocks.
6. Ensure all field names match exactly as specified in the schema.
7. Each variant should be meaningfully different while maintaining game balance.
8. Preserve the essence of the original item while creating interesting variations.
9. Consider gameplay implications of stat changes.
10. For Unity asset references (GameObject, Sprite, AudioClip, Texture, Material, etc.), ALWAYS use empty object notation: {""instanceID"": 0}
11. NEVER generate fake GUIDs or fileIDs for asset references.";
        }
        
        private string BuildVariantPrompt(ForgeSchemaExtractor.TypeSchema schema, ScriptableObject sourceItem, string sourceJson, int count, string variantInstructions)
        {
            var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);
            var template = ForgeSchemaExtractor.GenerateJsonTemplate(schema);
            
            var sb = new StringBuilder();
            
            sb.AppendLine("=== TASK: CREATE VARIANTS ===");
            sb.AppendLine($"Create {count} variant(s) of the source item below.");
            sb.AppendLine("Each variant should be a unique version with modified values while preserving the item's core identity.");
            sb.AppendLine();
            
            sb.AppendLine("=== CRITICAL: ASSET REFERENCES ===");
            sb.AppendLine("For any field that references Unity assets (GameObject, Sprite, AudioClip, Texture, Material, Prefab, etc.):");
            sb.AppendLine("- Use EXACTLY this format: {\"instanceID\": 0}");
            sb.AppendLine("- Do NOT generate GUIDs, fileIDs, or any other reference format.");
            sb.AppendLine("- These fields will be assigned manually by the developer later.");
            sb.AppendLine();
            
            // User instructions take priority
            if (!string.IsNullOrEmpty(variantInstructions))
            {
                sb.AppendLine("=== IMPORTANT: VARIANT REQUIREMENTS ===");
                sb.AppendLine("Pay special attention to these instructions for how variants should differ:");
                sb.AppendLine(variantInstructions);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("=== DEFAULT VARIANT GUIDANCE ===");
                sb.AppendLine("Create variants with:");
                sb.AppendLine("- Different names that suggest the variation");
                sb.AppendLine("- Adjusted stats that create meaningful gameplay differences");
                sb.AppendLine("- Varied rarity or tier levels if applicable");
                sb.AppendLine("- Thematic consistency with the original");
                sb.AppendLine();
            }
            
            sb.AppendLine("=== SOURCE ITEM ===");
            sb.AppendLine($"Name: {sourceItem.name}");
            sb.AppendLine($"Type: {schema.typeName}");
            sb.AppendLine("Current values:");
            sb.AppendLine(sourceJson);
            sb.AppendLine();
            
            sb.AppendLine("=== ITEM SCHEMA ===");
            sb.AppendLine(schemaDesc);
            sb.AppendLine();
            
            sb.AppendLine("=== JSON TEMPLATE ===");
            sb.AppendLine(template);
            sb.AppendLine();
            
            sb.AppendLine("=== OUTPUT FORMAT ===");
            if (count == 1)
            {
                sb.AppendLine("Respond with a single JSON object representing the variant.");
            }
            else
            {
                sb.AppendLine($"Respond with a JSON array containing exactly {count} variant objects.");
            }
            sb.AppendLine("Each variant must follow the exact schema structure.");
            
            return sb.ToString();
        }

        private IEnumerator GenerateFromTemplateCoroutine(
            ScriptableObject template,
            int count,
            Action<ForgeTemplateGenerationResult> callback,
            string additionalContext)
        {
            var client = ForgeOpenAIClient.Instance;
            
            if (client == null)
            {
                ForgeLogger.Error("Failed to get ForgeOpenAIClient instance");
                callback?.Invoke(ForgeTemplateGenerationResult.Error("Failed to initialize OpenAI client"));
                yield break;
            }

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
            ForgeLogger.DebugLog($"Prompt length: {prompt.Length} characters");
            if (ForgeLogger.DebugEnabled)
            {
                ForgeLogger.DebugLog($"=== FULL PROMPT ===\n{prompt}\n=== END PROMPT ===");
            }

            ForgeTemplateGenerationResult result = null;
            var globalSettings = ForgeConfig.GetGeneratorSettings();
            int maxRetries = globalSettings?.maxValidationRetries ?? 3;
            
            // Retry loop for validation
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                bool completed = false;

                client.Chat(prompt, response =>
                {
                    result = ProcessResponse(response, templateType, count);
                    completed = true;
                });

                // Wait for completion
                while (!completed)
                    yield return null;

                // Check if generation succeeded
                if (!result.success)
                {
                    ForgeLogger.Warn($"Generation attempt {attempt}/{maxRetries} failed: {result.errorMessage}");
                    if (attempt < maxRetries)
                    {
                        yield return new UnityEngine.WaitForSeconds(1f);
                        continue;
                    }
                    break;
                }

                // Validate items
                var validationErrors = ValidateItems(result.items);
                
                if (validationErrors.Count == 0)
                {
                    ForgeLogger.Success($"All {result.items.Count} items validated successfully");
                    break;
                }
                
                // Validation failed
                if (attempt < maxRetries)
                {
                    ForgeLogger.Warn($"Validation failed on attempt {attempt}/{maxRetries}. Retrying with feedback...");
                    var errorSummary = string.Join("\n", validationErrors);
                    prompt = prompt + $"\n\n=== VALIDATION ERRORS FROM PREVIOUS ATTEMPT ===\n{errorSummary}\n\nPlease fix these validation errors and regenerate the items.";
                    yield return new UnityEngine.WaitForSeconds(1f);
                }
                else
                {
                    ForgeLogger.Error($"Max validation retries ({maxRetries}) reached. Items may have validation errors.");
                    result.errorMessage = $"Validation warnings:\n{string.Join("\n", validationErrors)}";
                }
            }

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
8. Respect all value ranges and enum constraints provided.
9. For Unity asset references (GameObject, Sprite, AudioClip, Texture, Material, etc.), ALWAYS use empty object notation: {""instanceID"": 0}
10. NEVER generate fake GUIDs or fileIDs for asset references.";
        }

        /// <summary>
        /// Appends game context information (game name, description, target audience, additional rules) 
        /// to the provided StringBuilder. This ensures all user settings are included in AI prompts.
        /// </summary>
        private void AppendGameContext(StringBuilder sb)
        {
            // Check if we have any game context to add
            bool hasGameContext = !string.IsNullOrEmpty(settings.gameName) ||
                                  !string.IsNullOrEmpty(settings.gameDescription) ||
                                  !string.IsNullOrEmpty(settings.targetAudience);

            if (hasGameContext)
            {
                // Game context - critical for flavor and style
                sb.AppendLine("=== GAME CONTEXT ===");
                if (!string.IsNullOrEmpty(settings.gameName))
                {
                    sb.AppendLine($"Game: {settings.gameName}");
                }
                if (!string.IsNullOrEmpty(settings.gameDescription))
                {
                    sb.AppendLine($"Description: {settings.gameDescription}");
                }
                if (!string.IsNullOrEmpty(settings.targetAudience))
                {
                    sb.AppendLine($"Target Audience: {settings.targetAudience}");
                }
                sb.AppendLine();
            }

            // Additional rules from settings
            if (!string.IsNullOrEmpty(settings.additionalRules))
            {
                sb.AppendLine("=== ADDITIONAL RULES ===");
                sb.AppendLine(settings.additionalRules);
                sb.AppendLine();
            }
        }

        private string BuildUserPrompt(ForgeSchemaExtractor.TypeSchema schema, int count, string additionalContext)
        {
            var template = ForgeSchemaExtractor.GenerateJsonTemplate(schema);
            var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);

            var sb = new StringBuilder();

            // Add game context and rules
            AppendGameContext(sb);

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

        private string BuildBlueprintPrompt(ForgeSchemaExtractor.TypeSchema schema, Type templateType, int count, ForgeBlueprint blueprint, string sessionInstructions = null)
        {
            var template = ForgeSchemaExtractor.GenerateJsonTemplate(schema);
            var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);

            var sb = new StringBuilder();

            // Session instructions come FIRST (highest priority, user's immediate intent)
            if (!string.IsNullOrEmpty(sessionInstructions))
            {
                sb.AppendLine("=== IMPORTANT: USER REQUEST ===");
                sb.AppendLine("Pay special attention to the following user instructions for this generation:");
                sb.AppendLine(sessionInstructions);
                sb.AppendLine();
            }

            // Blueprint-specific instructions (saved with blueprint)
            if (!string.IsNullOrEmpty(blueprint.Instructions))
            {
                sb.AppendLine("=== GENERATION GUIDELINES ===");
                sb.AppendLine(blueprint.Instructions);
                sb.AppendLine();
            }
            else if (string.IsNullOrEmpty(sessionInstructions))
            {
                // Only use global game context if neither session nor blueprint has instructions
                AppendGameContext(sb);
            }

            // Item schema
            sb.AppendLine("=== ITEM SCHEMA ===");
            sb.AppendLine(schemaDesc);
            sb.AppendLine();

            sb.AppendLine("=== JSON TEMPLATE ===");
            sb.AppendLine(template);
            sb.AppendLine();

            // Existing items context based on duplicate strategy
            var effectiveStrategy = blueprint.GetEffectiveDuplicateStrategy();
            if (effectiveStrategy != ForgeDuplicateStrategy.Ignore)
            {
                // Auto-discover existing items based on strategy
                string discoveryPath = blueprint.GetEffectiveDiscoveryPath();
                
                // Call the generic method using reflection
                var method = typeof(ForgeAssetDiscovery).GetMethod(nameof(ForgeAssetDiscovery.DiscoverAssetsAsJson),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var genericMethod = method.MakeGenericMethod(templateType);
                var existingItemsJson = genericMethod.Invoke(null, new object[] { discoveryPath }) as List<string> ?? new List<string>();
                
                ForgeLogger.DebugLog($"Auto-discovered {existingItemsJson.Count} existing items from '{discoveryPath}'");
                
                if (existingItemsJson.Count > 0)
                {
                    if (effectiveStrategy == ForgeDuplicateStrategy.NamesOnly)
                    {
                        sb.AppendLine("=== EXISTING ITEM NAMES (AVOID THESE) ===");
                        foreach (var json in existingItemsJson)
                        {
                            try
                            {
                                // Deserialize to get the name
                                var tempItem = ScriptableObject.CreateInstance(templateType);
                                JsonUtility.FromJsonOverwrite(json, tempItem);
                                
                                // Extract name using DeclaredOnly to avoid shadowing issues
                                var nameField = templateType.GetField("name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                                string itemName = nameField?.GetValue(tempItem)?.ToString() ?? tempItem.name ?? "";
                                
                                if (!string.IsNullOrEmpty(itemName))
                                {
                                    sb.AppendLine($"- {itemName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                ForgeLogger.Warn($"Failed to extract name from existing item: {ex.Message}");
                            }
                        }
                        sb.AppendLine();
                        sb.AppendLine("IMPORTANT: Avoid creating items with names matching those listed above.");
                        sb.AppendLine();
                    }
                    else if (effectiveStrategy == ForgeDuplicateStrategy.FullComposition)
                    {
                        sb.AppendLine("=== EXISTING ITEMS (AVOID DUPLICATING) ===");
                        foreach (var json in existingItemsJson)
                        {
                            sb.AppendLine(json);
                        }
                        sb.AppendLine();
                        sb.AppendLine("IMPORTANT: Do NOT create items that match the above items in structure or values.");
                        sb.AppendLine();
                    }
                }
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
                int totalTokens = promptTokens + completionTokens;
                
                var settings = ForgeConfig.GetGeneratorSettings();
                var model = settings?.model ?? ForgeAIModel.GPT5Mini;
                float cost = ForgeAIModelHelper.CalculateCost(model, promptTokens, completionTokens);

                ForgeLogger.Success($"Generated {items.Count} item(s) | Tokens: {totalTokens:N0} ({promptTokens:N0} prompt + {completionTokens:N0} completion) | Cost: ${cost:F6}");

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
        
        /// <summary>
        /// Validates an item using IForgeValidatable interface or reflection.
        /// Returns null if valid, error message if invalid.
        /// </summary>
        private void ValidateItem(ScriptableObject item, int itemIndex, List<string> allErrors)
        {
            if (item == null)
            {
                allErrors.Add($"Item {itemIndex + 1}: Item is null");
                return;
            }
            
            var itemErrors = new List<string>();
            
            // Check if item implements IForgeValidatable interface
            if (item is IForgeValidatable validatable)
            {
                validatable.ValidateForgeItem(itemErrors);
            }
            else
            {
                // Fall back to reflection - look for ValidateForgeItem method
                var method = item.GetType().GetMethod("ValidateForgeItem", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(List<string>))
                    {
                        try
                        {
                            method.Invoke(item, new object[] { itemErrors });
                        }
                        catch (Exception e)
                        {
                            ForgeLogger.Error($"Validation method threw exception: {e.Message}");
                            itemErrors.Add($"Validation error: {e.Message}");
                        }
                    }
                }
            }
            
            // Add item errors with item context
            foreach (var error in itemErrors)
            {
                allErrors.Add($"Item {itemIndex + 1} ({item.name ?? "unnamed"}): {error}");
            }
        }
        
        /// <summary>
        /// Validates all items in a list. Returns list of error messages for failed items.
        /// Aggregates duplicate errors and sorts by frequency.
        /// </summary>
        private List<string> ValidateItems(List<ScriptableObject> items)
        {
            var allErrors = new List<string>();
            
            // Collect all validation errors
            for (int i = 0; i < items.Count; i++)
            {
                ValidateItem(items[i], i, allErrors);
            }
            
            if (allErrors.Count == 0)
                return allErrors;
            
            // Aggregate errors: count occurrences and group
            var errorCounts = new Dictionary<string, int>();
            foreach (var error in allErrors)
            {
                if (errorCounts.ContainsKey(error))
                    errorCounts[error]++;
                else
                    errorCounts[error] = 1;
            }
            
            // Sort by frequency (most common first), then alphabetically
            var sortedErrors = errorCounts
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value > 1 ? $"{kvp.Key} (×{kvp.Value})" : kvp.Key)
                .ToList();
            
            return sortedErrors;
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
            // Use actual model pricing from settings
            var settings = ForgeConfig.GetGeneratorSettings();
            var model = settings?.model ?? ForgeAIModel.GPT5Mini;
            return ForgeAIModelHelper.CalculateCost(model, prompt, completion);
        }
    }
}
