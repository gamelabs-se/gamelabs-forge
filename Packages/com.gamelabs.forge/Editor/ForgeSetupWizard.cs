#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// First-run onboarding wizard. After completion, Settings becomes the single source of truth.
    /// </summary>
    [InitializeOnLoad]
    public class ForgeSetupWizard : EditorWindow
    {
        private const string HasCompletedWizardKey = "GameLabs.Forge.HasCompletedWizard";
        
        // Wizard state
        private int currentStep = 0;
        private Vector2 scrollPos;
        
        // Step 1: API Configuration (REQUIRED)
        private string apiKey = "";
        private ForgeAIModel model = ForgeAIModel.GPT5Mini;
        
        // Step 2: Game Context (DEFAULTS ONLY)
        private string gameName = "My Game";
        private string gameDescription = "";
        private string targetAudience = "General";
        private readonly string[] audienceOptions = { "General", "Casual", "Hardcore", "Kids", "Mature" };
        private int selectedAudienceIndex = 0;
        
        // Simplified - only essential defaults
        private float temperature = 1.0f;  // GPT-5-mini only supports 1.0
        private int defaultBatchSize = 5;
        
        // Validation
        private bool apiKeyValid = false;
        
        // Auto-open on first run
        static ForgeSetupWizard()
        {
            EditorApplication.update += CheckFirstRun;
        }
        
        private static void CheckFirstRun()
        {
            EditorApplication.update -= CheckFirstRun;
            
            if (!EditorPrefs.HasKey(HasCompletedWizardKey))
            {
                EditorApplication.delayCall += () => Open();
            }
        }
        
        [MenuItem("GameLabs/Forge/ ", priority = 10)]
        private static void AddSeparator() { }

        [MenuItem("GameLabs/Forge/Re-run Setup Wizard", priority = 11)]
        public static void Open()
        {
            var window = GetWindow<ForgeSetupWizard>("Setup Wizard");
            window.minSize = new Vector2(500, 550);
            window.maxSize = new Vector2(600, 700);
            window.LoadSavedSettings();
        }
        
        [MenuItem("GameLabs/Forge/ ", priority = 10, validate = true)]
        private static bool AddSeparatorValidate() { return false; }
        
        private void OnEnable()
        {
            LoadSavedSettings();
        }
        
        private void LoadSavedSettings()
        {
            try
            {
                // Load from Settings (single source of truth)
                apiKey = ForgeConfig.GetOpenAIKey() ?? "";
                apiKeyValid = !string.IsNullOrEmpty(apiKey);
                
                var settings = ForgeConfig.GetGeneratorSettings();
                model = settings.model;
                gameName = settings.gameName ?? "My Game";
                gameDescription = settings.gameDescription ?? "";
                targetAudience = settings.targetAudience ?? "General";
                temperature = settings.temperature;
                defaultBatchSize = settings.defaultBatchSize;
                
                selectedAudienceIndex = Array.IndexOf(audienceOptions, targetAudience);
                if (selectedAudienceIndex < 0) selectedAudienceIndex = 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Forge] Failed to load settings: {e.Message}");
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                // Write initial values to Settings (which becomes the source of truth)
                ForgeConfig.SetOpenAIKey(apiKey);
                ForgeConfig.SetModel(model);
                ForgeConfig.SetTemperature(temperature);
                
                var settings = new ForgeGeneratorSettings
                {
                    model = model,
                    gameName = gameName,
                    gameDescription = gameDescription,
                    targetAudience = targetAudience,
                    temperature = temperature,
                    defaultBatchSize = defaultBatchSize,
                    maxBatchSize = 20,  // Default
                    additionalRules = "",
                    existingAssetsSearchPath = "Assets",
                    generatedAssetsBasePath = "Resources/Generated",
                    duplicateStrategy = ForgeDuplicateStrategy.Ignore
                };
                
                ForgeConfig.SaveGeneratorSettings(settings);
                
                // Mark wizard as completed
                EditorPrefs.SetBool(HasCompletedWizardKey, true);
                
                ForgeLogger.Success("Initial configuration saved. Use Settings to make changes.");
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
            
            EditorGUILayout.LabelField("Setup Wizard", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 12
            };
            EditorGUILayout.LabelField("Configure FORGE", subtitleStyle);
            
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
                "Enter your OpenAI API key. Required for item generation.\n" +
                "Get your API key from: https://platform.openai.com/api-keys",
                MessageType.Info, true);
            
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
                    EditorGUILayout.HelpBox("✓ API key format looks valid", MessageType.None, true);
                }
                else
                {
                    EditorGUILayout.HelpBox("⚠ API key should start with 'sk-'", MessageType.Warning, true);
                }
            }
            
            EditorGUILayout.Space(15);
            
            EditorGUILayout.LabelField("AI Model", EditorStyles.boldLabel);
            
            model = (ForgeAIModel)EditorGUILayout.EnumPopup("Model", model);
            
            // Show model description
            string modelDesc = ForgeAIModelHelper.GetDescription(model);
            EditorGUILayout.HelpBox(modelDesc, MessageType.Info, true);
            
            // Show pricing
            var (inputCost, outputCost) = ForgeAIModelHelper.GetPricing(model);
            EditorGUILayout.HelpBox(
                $"Pricing: ${inputCost:F2}/1M input tokens, ${outputCost:F2}/1M output tokens",
                MessageType.None, true);
        }
        
        private void DrawGameContextStep()
        {
            DrawSectionHeader("Game Defaults", "These defaults can be changed later in Settings.");
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
            
            gameName = EditorGUILayout.TextField("Game Name", gameName);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Game Description", EditorStyles.miniLabel);
            gameDescription = EditorGUILayout.TextArea(gameDescription, GUILayout.Height(80));
            
            EditorGUILayout.HelpBox(
                "Describe your game's setting, theme, and style to help generate appropriate items.",
                MessageType.Info, true);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Target Audience", EditorStyles.boldLabel);
            selectedAudienceIndex = EditorGUILayout.Popup("Audience", selectedAudienceIndex, audienceOptions);
            targetAudience = audienceOptions[selectedAudienceIndex];
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("AI Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Creativity", GUILayout.Width(150));
            temperature = EditorGUILayout.Slider(temperature, 0f, 1.5f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Batch Size", GUILayout.Width(150));
            defaultBatchSize = EditorGUILayout.IntSlider(defaultBatchSize, 1, 20);
            EditorGUILayout.EndHorizontal();
        }
        
        
        
        private void DrawCompletionStep()
        {
            DrawSectionHeader("FORGE is ready", "");
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Game: {gameName}");
            EditorGUILayout.LabelField($"Model: {model}");
            EditorGUILayout.LabelField($"Audience: {targetAudience}");
            EditorGUILayout.LabelField($"Default Batch Size: {defaultBatchSize}");
            EditorGUILayout.LabelField($"Creativity: {temperature:F1}");
            EditorGUILayout.LabelField($"API Key: {(string.IsNullOrEmpty(apiKey) ? "Not configured" : "✓ Configured")}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.LabelField("Next Steps", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Open FORGE", GUILayout.Height(35)))
            {
                EditorWindow.GetWindow<ForgeWindow>();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("View Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/gamelabs-se/gamelabs-forge");
            }
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.HelpBox(
                "Ready to generate items:\n" +
                "1. Open FORGE (above)\n" +
                "2. Select a template\n" +
                "3. Configure settings\n" +
                "4. Generate items",
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
                        SaveSettings(); // Auto-save when clicking Next/Finish
                        currentStep++;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Close", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    SaveSettings(); // Save one final time on close
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
                        EditorUtility.DisplayDialog("Setup Wizard", "Enter your OpenAI API key.", "OK");
                        return false;
                    }
                    return true;
                    
                case 1:
                    if (string.IsNullOrEmpty(gameName))
                    {
                        EditorUtility.DisplayDialog("Setup Wizard", "Enter your game name.", "OK");
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
        
    }
}
#endif

