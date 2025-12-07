#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace GameLabs.Forge.Editor
{
    public class ForgeSetupWizard : EditorWindow
    {
        private const string ConfigPath = "Assets/GameLabs/Forge/Settings/forge.config.json";
        private const string SettingsKey = "ForgeWizardSettings";
        
        // Wizard state
        private int currentStep = 0;
        private Vector2 scrollPos;
        
        // Step 1: API Configuration
        private string apiKey = "";
        private string model = "gpt-4o-mini";
        private readonly string[] availableModels = { "gpt-4o-mini", "gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo" };
        private int selectedModelIndex = 0;
        
        // Step 2: Game Context
        private string gameName = "My Game";
        private string gameDescription = "A fantasy RPG with dark themes and challenging combat.";
        private string targetAudience = "General";
        private readonly string[] audienceOptions = { "General", "Casual", "Hardcore", "Kids", "Mature" };
        private int selectedAudienceIndex = 0;
        
        // Step 3: Generation Settings
        private int defaultBatchSize = 5;
        private int maxBatchSize = 20;
        private float temperature = 0.8f;
        private string additionalRules = "";
        
        // Step 4: Asset Path Settings
        private string existingAssetsSearchPath = "Resources";
        private string generatedAssetsBasePath = "Resources/Generated";
        private bool autoLoadExistingAssets = true;
        private ExistingItemsIntent intent = ExistingItemsIntent.PreventDuplicatesAndRefineNaming;
        
        // Validation
        private bool apiKeyValid = false;
        private string validationMessage = "";
        
        [MenuItem("GameLabs/Forge/Setup Wizard", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<ForgeSetupWizard>("Forge Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.LoadSavedSettings();
        }
        
        [MenuItem("GameLabs/Forge/Quick Settings", priority = 1)]
        public static void OpenQuickSettings()
        {
            var window = GetWindow<ForgeSetupWizard>("Forge Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.LoadSavedSettings();
            window.currentStep = 0; // Go to API key step
        }
        
        private void OnEnable()
        {
            LoadSavedSettings();
        }
        
        private void LoadSavedSettings()
        {
            // Load from config file if exists
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonUtility.FromJson<ForgeConfigData>(json);
                    if (config != null)
                    {
                        apiKey = config.openaiApiKey ?? "";
                        model = config.model ?? "gpt-4o-mini";
                        gameName = config.gameName ?? "My Game";
                        gameDescription = config.gameDescription ?? "";
                        targetAudience = config.targetAudience ?? "General";
                        defaultBatchSize = config.defaultBatchSize > 0 ? config.defaultBatchSize : 5;
                        maxBatchSize = config.maxBatchSize > 0 ? config.maxBatchSize : 20;
                        temperature = config.temperature;
                        additionalRules = config.additionalRules ?? "";
                        existingAssetsSearchPath = string.IsNullOrEmpty(config.existingAssetsSearchPath) ? "Resources" : config.existingAssetsSearchPath;
                        generatedAssetsBasePath = string.IsNullOrEmpty(config.generatedAssetsBasePath) ? "Resources/Generated" : config.generatedAssetsBasePath;
                        autoLoadExistingAssets = config.autoLoadExistingAssets;
                        intent = (ExistingItemsIntent)config.intent;
                        
                        // Update indices
                        selectedModelIndex = Array.IndexOf(availableModels, model);
                        if (selectedModelIndex < 0) selectedModelIndex = 0;
                        
                        selectedAudienceIndex = Array.IndexOf(audienceOptions, targetAudience);
                        if (selectedAudienceIndex < 0) selectedAudienceIndex = 0;
                        
                        apiKeyValid = !string.IsNullOrEmpty(apiKey);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Forge] Failed to load config: {e.Message}");
                }
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                
                var config = new ForgeConfigData
                {
                    openaiApiKey = apiKey,
                    model = model,
                    gameName = gameName,
                    gameDescription = gameDescription,
                    targetAudience = targetAudience,
                    defaultBatchSize = defaultBatchSize,
                    maxBatchSize = maxBatchSize,
                    temperature = temperature,
                    additionalRules = additionalRules,
                    existingAssetsSearchPath = existingAssetsSearchPath,
                    generatedAssetsBasePath = generatedAssetsBasePath,
                    autoLoadExistingAssets = autoLoadExistingAssets,
                    intent = (int)intent
                };
                
                var json = JsonUtility.ToJson(config, true);
                File.WriteAllText(ConfigPath, json);
                AssetDatabase.Refresh();
                
                ForgeLogger.Log("Configuration saved successfully.");
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to save config: {e.Message}");
            }
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            DrawStepIndicator();
            
            EditorGUILayout.Space(10);
            
            switch (currentStep)
            {
                case 0:
                    DrawApiConfigStep();
                    break;
                case 1:
                    DrawGameContextStep();
                    break;
                case 2:
                    DrawGenerationSettingsStep();
                    break;
                case 3:
                    DrawCompletionStep();
                    break;
            }
            
            EditorGUILayout.Space(20);
            DrawNavigationButtons();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("🔥 FORGE Setup Wizard", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 12
            };
            EditorGUILayout.LabelField("Dynamic AI-Powered Item Generator", subtitleStyle);
            
            EditorGUILayout.Space(5);
            DrawSeparator();
        }
        
        private void DrawStepIndicator()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            string[] steps = { "1. API", "2. Game", "3. Settings", "4. Done" };
            
            for (int i = 0; i < steps.Length; i++)
            {
                var style = new GUIStyle(EditorStyles.miniButton);
                if (i == currentStep)
                {
                    style.fontStyle = FontStyle.Bold;
                    GUI.color = new Color(0.3f, 0.7f, 1f);
                }
                else if (i < currentStep)
                {
                    GUI.color = new Color(0.5f, 0.9f, 0.5f);
                }
                else
                {
                    GUI.color = Color.gray;
                }
                
                GUILayout.Label(steps[i], style, GUILayout.Width(80));
                GUI.color = Color.white;
                
                if (i < steps.Length - 1)
                {
                    GUILayout.Label("→", GUILayout.Width(20));
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawApiConfigStep()
        {
            DrawSectionHeader("API Configuration", "Configure your OpenAI API credentials");
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("OpenAI API Key", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter your OpenAI API key. This is required for item generation.\n" +
                "Get your API key from: https://platform.openai.com/api-keys",
                MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginChangeCheck();
            apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
            if (EditorGUI.EndChangeCheck())
            {
                apiKeyValid = !string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("sk-");
            }
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                if (apiKey.StartsWith("sk-"))
                {
                    EditorGUILayout.HelpBox("✓ API key format looks valid", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("⚠ API key should start with 'sk-'", MessageType.Warning);
                }
            }
            
            EditorGUILayout.Space(15);
            
            EditorGUILayout.LabelField("AI Model", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select the AI model to use for generation.\n" +
                "• gpt-4o-mini: Fast & cheap (recommended)\n" +
                "• gpt-4o: Higher quality, more expensive\n" +
                "• gpt-4-turbo: Legacy high-quality model\n" +
                "• gpt-3.5-turbo: Fastest, lowest cost",
                MessageType.Info);
            
            selectedModelIndex = EditorGUILayout.Popup("Model", selectedModelIndex, availableModels);
            model = availableModels[selectedModelIndex];
        }
        
        private void DrawGameContextStep()
        {
            DrawSectionHeader("Game Context", "Tell Forge about your game");
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
            
            gameName = EditorGUILayout.TextField("Game Name", gameName);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Game Description");
            gameDescription = EditorGUILayout.TextArea(gameDescription, GUILayout.Height(80));
            
            EditorGUILayout.HelpBox(
                "Describe your game's setting, theme, art style, and any unique characteristics. " +
                "This helps the AI generate items that fit your game.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Target Audience", EditorStyles.boldLabel);
            selectedAudienceIndex = EditorGUILayout.Popup("Audience", selectedAudienceIndex, audienceOptions);
            targetAudience = audienceOptions[selectedAudienceIndex];
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Additional Rules (Optional)", EditorStyles.boldLabel);
            additionalRules = EditorGUILayout.TextArea(additionalRules, GUILayout.Height(60));
            EditorGUILayout.HelpBox(
                "Add any specific rules or guidelines for item generation.\n" +
                "Example: 'All weapons should have unique names' or 'Avoid generic fantasy tropes'",
                MessageType.Info);
        }
        
        private void DrawGenerationSettingsStep()
        {
            DrawSectionHeader("Generation Settings", "Configure how items are generated");
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Batch Settings", EditorStyles.boldLabel);
            
            defaultBatchSize = EditorGUILayout.IntSlider("Default Batch Size", defaultBatchSize, 1, 50);
            maxBatchSize = EditorGUILayout.IntSlider("Max Batch Size", maxBatchSize, 1, 100);
            
            if (maxBatchSize < defaultBatchSize)
            {
                maxBatchSize = defaultBatchSize;
            }
            
            EditorGUILayout.HelpBox(
                "Batch size determines how many items are generated per request.\n" +
                "Larger batches are more cost-effective but may hit token limits.",
                MessageType.Info);
            
            EditorGUILayout.Space(15);
            
            EditorGUILayout.LabelField("AI Creativity", EditorStyles.boldLabel);
            temperature = EditorGUILayout.Slider("Temperature", temperature, 0f, 2f);
            
            string tempDesc = temperature switch
            {
                < 0.3f => "Very consistent/predictable output",
                < 0.6f => "Balanced consistency and creativity",
                < 1.0f => "Creative with some consistency",
                < 1.5f => "Very creative/varied output",
                _ => "Maximum creativity (may be unpredictable)"
            };
            EditorGUILayout.HelpBox(tempDesc, MessageType.None);
            
            EditorGUILayout.Space(15);
            
            EditorGUILayout.LabelField("Asset Paths", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure where Forge searches for existing assets and saves generated ones.\n" +
                "Paths are relative to the Assets folder and work across all platforms.",
                MessageType.Info);
            
            existingAssetsSearchPath = EditorGUILayout.TextField("Search Path", existingAssetsSearchPath);
            generatedAssetsBasePath = EditorGUILayout.TextField("Generated Path", generatedAssetsBasePath);
            autoLoadExistingAssets = EditorGUILayout.Toggle("Auto-Load Existing", autoLoadExistingAssets);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Existing Items Intent", EditorStyles.boldLabel);
            intent = (ExistingItemsIntent)EditorGUILayout.EnumPopup("Usage Intent", intent);
            
            string intentDesc = intent switch
            {
                ExistingItemsIntent.PreventDuplicates => "AI will generate unique items that don't duplicate existing ones",
                ExistingItemsIntent.RefineNaming => "AI will use existing items as examples for naming conventions",
                ExistingItemsIntent.PreventDuplicatesAndRefineNaming => "AI will both prevent duplicates AND follow naming conventions",
                _ => ""
            };
            if (!string.IsNullOrEmpty(intentDesc))
            {
                EditorGUILayout.HelpBox(intentDesc, MessageType.None);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Full Search: Assets/{existingAssetsSearchPath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Full Generated: Assets/{generatedAssetsBasePath}/{{TypeName}}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(15);
            
            DrawCostEstimate();
        }
        
        private void DrawCostEstimate()
        {
            EditorGUILayout.LabelField("Estimated Costs (GPT-4o-mini)", EditorStyles.boldLabel);
            
            // Rough estimates based on typical token usage
            float inputCostPer1K = 0.00015f;
            float outputCostPer1K = 0.0006f;
            
            float singleItemInputTokens = 500;
            float singleItemOutputTokens = 200;
            float batchItemInputTokens = 600;
            float batchItemOutputTokens = 200 * defaultBatchSize;
            
            float singleCost = (singleItemInputTokens * inputCostPer1K + singleItemOutputTokens * outputCostPer1K) / 1000f;
            float batchCost = (batchItemInputTokens * inputCostPer1K + batchItemOutputTokens * outputCostPer1K) / 1000f;
            float costPerItem = batchCost / defaultBatchSize;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Single Item: ~${singleCost:F5}");
            EditorGUILayout.LabelField($"Batch ({defaultBatchSize} items): ~${batchCost:F5}");
            EditorGUILayout.LabelField($"Per Item (batch): ~${costPerItem:F6}");
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCompletionStep()
        {
            DrawSectionHeader("Setup Complete!", "Your Forge is ready to create items");
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Game: {gameName}");
            EditorGUILayout.LabelField($"Model: {model}");
            EditorGUILayout.LabelField($"Audience: {targetAudience}");
            EditorGUILayout.LabelField($"Default Batch Size: {defaultBatchSize}");
            EditorGUILayout.LabelField($"Temperature: {temperature:F1}");
            EditorGUILayout.LabelField($"Generated Path: Assets/{generatedAssetsBasePath}");
            EditorGUILayout.LabelField($"API Key: {(string.IsNullOrEmpty(apiKey) ? "Not configured" : "✓ Configured")}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Add ForgeItemGenerator to Scene", GUILayout.Height(35)))
            {
                CreateForgeGenerator();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Open Demo Scene", GUILayout.Height(25)))
            {
                OpenDemoScene();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("View Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/gamelabs-se/gamelabs-forge");
            }
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.HelpBox(
                "Next Steps:\n" +
                "1. Create your item classes (inherit from ForgeItemDefinition or use any class)\n" +
                "2. Add ForgeItemGenerator to your scene\n" +
                "3. Call GenerateSingle<YourItem>() or GenerateBatch<YourItem>(count)\n" +
                "4. Use the generated items in your game!",
                MessageType.Info);
        }
        
        private void DrawNavigationButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Back button
            EditorGUI.BeginDisabledGroup(currentStep == 0);
            if (GUILayout.Button("← Back", GUILayout.Height(30), GUILayout.Width(100)))
            {
                currentStep--;
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.FlexibleSpace();
            
            // Save button (always available)
            if (GUILayout.Button("Save Settings", GUILayout.Height(30), GUILayout.Width(120)))
            {
                SaveSettings();
                EditorUtility.DisplayDialog("Forge", "Settings saved successfully!", "OK");
            }
            
            GUILayout.FlexibleSpace();
            
            // Next/Finish button
            if (currentStep < 3)
            {
                string nextLabel = currentStep == 2 ? "Finish →" : "Next →";
                if (GUILayout.Button(nextLabel, GUILayout.Height(30), GUILayout.Width(100)))
                {
                    if (ValidateCurrentStep())
                    {
                        SaveSettings();
                        currentStep++;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Close", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    Close();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private bool ValidateCurrentStep()
        {
            switch (currentStep)
            {
                case 0:
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        EditorUtility.DisplayDialog("Forge", "Please enter your OpenAI API key.", "OK");
                        return false;
                    }
                    return true;
                    
                case 1:
                    if (string.IsNullOrEmpty(gameName))
                    {
                        EditorUtility.DisplayDialog("Forge", "Please enter your game name.", "OK");
                        return false;
                    }
                    return true;
                    
                default:
                    return true;
            }
        }
        
        private void DrawSectionHeader(string title, string subtitle)
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            EditorGUILayout.LabelField(title, titleStyle);
            EditorGUILayout.LabelField(subtitle, EditorStyles.centeredGreyMiniLabel);
        }
        
        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }
        
        private void CreateForgeGenerator()
        {
            var existing = FindFirstObjectByType<ForgeItemGenerator>();
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                EditorUtility.DisplayDialog("Forge", "ForgeItemGenerator already exists in scene.", "OK");
                return;
            }
            
            var go = new GameObject("ForgeItemGenerator");
            var generator = go.AddComponent<ForgeItemGenerator>();
            
            // Apply settings
            var settings = new ForgeGeneratorSettings
            {
                gameName = gameName,
                gameDescription = gameDescription,
                targetAudience = targetAudience,
                defaultBatchSize = defaultBatchSize,
                maxBatchSize = maxBatchSize,
                temperature = temperature,
                model = model,
                additionalRules = additionalRules
            };
            generator.UpdateSettings(settings);
            
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
            
            ForgeLogger.Log("ForgeItemGenerator added to scene.");
        }
        
        private void OpenDemoScene()
        {
            string demoScenePath = "Assets/GameLabs/Forge/Demo/ForgeDemo.unity";
            if (File.Exists(demoScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(demoScenePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Forge", "Demo scene not found. Create a new scene and add ForgeItemGenerator.", "OK");
            }
        }
        
        [Serializable]
        private class ForgeConfigData
        {
            public string openaiApiKey;
            public string model;
            public string gameName;
            public string gameDescription;
            public string targetAudience;
            public int defaultBatchSize;
            public int maxBatchSize;
            public float temperature;
            public string additionalRules;
            public string existingAssetsSearchPath;
            public string generatedAssetsBasePath;
            public bool autoLoadExistingAssets;
            public int intent; // ExistingItemsIntent enum value
        }
    }
}
#endif

