#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Settings window for Forge configuration.
    /// </summary>
    public class ForgeSettingsWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private ForgeGeneratorSettings settings;
        
        // Accessible via FORGE window
        // [MenuItem("GameLabs/Forge/Settings", priority = 20)]
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeSettingsWindow>("Settings");
            window.minSize = new Vector2(450, 600);
            window.maxSize = new Vector2(600, 900);
        }
        
        /// <summary>Static method for easy access from other windows.</summary>
        public static void Open() => OpenWindow();
        
        private void OnEnable()
        {
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            settings = ForgeConfig.GetGeneratorSettings();
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawGameContext();
            EditorGUILayout.Space(10);
            
            DrawGenerationSettings();
            EditorGUILayout.Space(10);
            
            DrawAssetPaths();
            EditorGUILayout.Space(10);
            
            DrawExistingItemsSettings();
            EditorGUILayout.Space(10);
            
            DrawDebugSettings();
            EditorGUILayout.Space(15);
            
            DrawActions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Settings", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField("Configure Generation", subtitleStyle);
            
            DrawSeparator();
        }
        
        private void DrawGameContext()
        {
            EditorGUILayout.LabelField("Game Context", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            settings.gameName = EditorGUILayout.TextField("Game Name", settings.gameName);
            
            EditorGUILayout.LabelField("Game Description", EditorStyles.miniLabel);
            settings.gameDescription = EditorGUILayout.TextArea(settings.gameDescription, 
                GUILayout.Height(60), GUILayout.ExpandHeight(false));
            
            settings.targetAudience = EditorGUILayout.TextField("Target Audience", settings.targetAudience);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Settings will be saved when user clicks Save
            }
        }
        
        private void DrawGenerationSettings()
        {
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Batch Size", GUILayout.Width(150));
            settings.defaultBatchSize = EditorGUILayout.IntSlider(settings.defaultBatchSize, 1, 50);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Batch Size", GUILayout.Width(150));
            settings.maxBatchSize = EditorGUILayout.IntSlider(settings.maxBatchSize, 1, 100);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Temperature (Creativity)", GUILayout.Width(150));
            settings.temperature = EditorGUILayout.Slider(settings.temperature, 0f, 2f);
            EditorGUILayout.EndHorizontal();
            
            settings.model = (ForgeAIModel)EditorGUILayout.EnumPopup("Model", settings.model);
            
            // Show model info
            string modelInfo = ForgeAIModelHelper.GetDescription(settings.model);
            EditorGUILayout.HelpBox(modelInfo, MessageType.None, true);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Additional Rules (Optional)", EditorStyles.miniLabel);
            settings.additionalRules = EditorGUILayout.TextArea(settings.additionalRules, 
                GUILayout.Height(60), GUILayout.ExpandHeight(false));
            
            if (EditorGUI.EndChangeCheck())
            {
                // Settings will be saved when user clicks Save
            }
        }
        
        private void DrawAssetPaths()
        {
            EditorGUILayout.LabelField("Asset Paths", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            settings.existingAssetsSearchPath = EditorGUILayout.TextField("Search Path", 
                settings.existingAssetsSearchPath);
            EditorGUILayout.HelpBox("Search location for existing assets (relative to Assets folder)", 
                MessageType.None, true);
            
            EditorGUILayout.Space(3);
            
            settings.generatedAssetsBasePath = EditorGUILayout.TextField("Generated Path", 
                settings.generatedAssetsBasePath);
            EditorGUILayout.HelpBox("Save location for generated assets (relative to Assets folder)", 
                MessageType.None, true);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Settings will be saved when user clicks Save
            }
        }
        
        private void DrawExistingItemsSettings()
        {
            EditorGUILayout.LabelField("Existing Items Context", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            settings.autoLoadExistingAssets = EditorGUILayout.Toggle("Auto-Load Existing Assets", settings.autoLoadExistingAssets);
            
            EditorGUILayout.Space(5);
            settings.intent = (ExistingItemsIntent)EditorGUILayout.EnumPopup("Generation Intent", settings.intent);
            
            // Show description based on intent
            string intentDesc = settings.intent switch
            {
                ExistingItemsIntent.PreventDuplicates => "AI will generate unique items that don't duplicate existing ones",
                ExistingItemsIntent.RefineNaming => "AI will use existing items as examples for naming conventions",
                ExistingItemsIntent.PreventDuplicatesAndRefineNaming => "AI will both prevent duplicates AND follow naming conventions",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(intentDesc))
            {
                EditorGUILayout.HelpBox(intentDesc, MessageType.Info);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                // Settings will be saved when user clicks Save
            }
        }
        
        private void DrawDebugSettings()
        {
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            bool debugMode = ForgeConfig.GetDebugMode();
            debugMode = EditorGUILayout.Toggle("Enable Debug Logging", debugMode);
            
            if (EditorGUI.EndChangeCheck())
            {
                ForgeLogger.DebugMode = debugMode;
            }
            
            EditorGUILayout.HelpBox("When disabled, only errors, warnings, and success messages are logged. Enable for detailed generation logs.", MessageType.None);
        }
        
        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Settings", GUILayout.Height(35)))
            {
                SaveSettings();
            }
            
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", 
                    "Are you sure you want to reset all settings to defaults?", 
                    "Yes", "No"))
                {
                    settings = new ForgeGeneratorSettings();
                    SaveSettings();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Open Setup Wizard", GUILayout.Height(30)))
            {
                ForgeSetupWizard.Open();
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                var configPath = ForgeConfig.DefaultPath;
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath));
                
                var configData = new ForgeConfigDataDto
                {
                    openaiApiKey = ForgeConfig.GetOpenAIKey() ?? "",
                    model = (int)settings.model,
                    gameName = settings.gameName,
                    gameDescription = settings.gameDescription,
                    targetAudience = settings.targetAudience,
                    defaultBatchSize = settings.defaultBatchSize,
                    maxBatchSize = settings.maxBatchSize,
                    temperature = settings.temperature,
                    additionalRules = settings.additionalRules,
                    existingAssetsSearchPath = settings.existingAssetsSearchPath,
                    generatedAssetsBasePath = settings.generatedAssetsBasePath,
                    autoLoadExistingAssets = settings.autoLoadExistingAssets,
                    intent = (int)settings.intent,
                    debugMode = ForgeLogger.DebugMode
                };
                
                var json = JsonUtility.ToJson(configData, true);
                System.IO.File.WriteAllText(configPath, json);
                AssetDatabase.Refresh();
                
                ForgeConfig.ClearCache();
                ForgeLogger.Success("Settings saved.");
                EditorUtility.DisplayDialog("Settings", "Settings saved successfully.", "OK");
            }
            catch (System.Exception e)
            {
                ForgeLogger.Error($"Failed to save settings: {e.Message}");
                EditorUtility.DisplayDialog("Settings", $"Failed to save settings: {e.Message}", "OK");
            }
        }
        
        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }
        
        [System.Serializable]
        private class ForgeConfigDataDto
        {
            public string openaiApiKey;
            public int model;
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
            public int intent;
            public bool debugMode;
        }
    }
}
#endif
