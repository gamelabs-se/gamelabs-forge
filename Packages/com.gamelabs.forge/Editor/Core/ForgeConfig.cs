using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Lightweight configuration loader for Forge.
    /// All user settings are stored in EditorPrefs (user-specific).
    /// The config file can be used for developer-controlled defaults and system configuration.
    /// </summary>
    public static class ForgeConfig
    {
        /// <summary>Default path to the configuration file.</summary>
        public const string DefaultPath = "Assets/GameLabs/Forge/Settings/forge.config.json";
        
        // EditorPrefs keys for all settings
        private const string PrefKeyPrefix = "GameLabs.Forge.";
        private const string ApiKeyPrefKey = PrefKeyPrefix + "OpenAIKey";
        private const string GameNamePrefKey = PrefKeyPrefix + "GameName";
        private const string GameDescriptionPrefKey = PrefKeyPrefix + "GameDescription";
        private const string TargetAudiencePrefKey = PrefKeyPrefix + "TargetAudience";
        private const string ModelPrefKey = PrefKeyPrefix + "Model";
        private const string TemperaturePrefKey = PrefKeyPrefix + "Temperature";
        private const string DefaultBatchSizePrefKey = PrefKeyPrefix + "DefaultBatchSize";
        private const string MaxBatchSizePrefKey = PrefKeyPrefix + "MaxBatchSize";
        private const string AdditionalRulesPrefKey = PrefKeyPrefix + "AdditionalRules";
        private const string ExistingAssetsSearchPathPrefKey = PrefKeyPrefix + "ExistingAssetsSearchPath";
        private const string GeneratedAssetsBasePathPrefKey = PrefKeyPrefix + "GeneratedAssetsBasePath";
        private const string AutoLoadExistingAssetsPrefKey = PrefKeyPrefix + "AutoLoadExistingAssets";
        private const string IntentPrefKey = PrefKeyPrefix + "Intent";
        private const string DebugModePrefKey = PrefKeyPrefix + "DebugMode";

        /// <summary>Internal DTO for deserializing configuration from JSON (backwards compatibility).</summary>
        [System.Serializable]
        private class ForgeConfigDto
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

        private static ForgeConfigDto _cachedConfig;
        
        /// <summary>
        /// Gets the OpenAI API key from EditorPrefs.
        /// </summary>
        /// <returns>The API key, or null if not found.</returns>
        public static string GetOpenAIKey(string path = DefaultPath)
        {
#if UNITY_EDITOR
            var apiKey = EditorPrefs.GetString(ApiKeyPrefKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey.Trim();
            }
#endif
            return null;
        }
        
        /// <summary>Sets the OpenAI API key in EditorPrefs.</summary>
        public static void SetOpenAIKey(string apiKey)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(apiKey))
                EditorPrefs.DeleteKey(ApiKeyPrefKey);
            else
                EditorPrefs.SetString(ApiKeyPrefKey, apiKey.Trim());
#endif
        }
        
        /// <summary>Gets the AI model from EditorPrefs.</summary>
        public static ForgeAIModel GetModel(string path = DefaultPath)
        {
#if UNITY_EDITOR
            return (ForgeAIModel)EditorPrefs.GetInt(ModelPrefKey, (int)ForgeAIModel.GPT4o);
#else
            return ForgeAIModel.GPT4o;
#endif
        }
        
        /// <summary>Sets the AI model in EditorPrefs.</summary>
        public static void SetModel(ForgeAIModel model)
        {
#if UNITY_EDITOR
            EditorPrefs.SetInt(ModelPrefKey, (int)model);
#endif
        }
        
        /// <summary>Gets the AI temperature setting from EditorPrefs.</summary>
        public static float GetTemperature(string path = DefaultPath)
        {
#if UNITY_EDITOR
            return EditorPrefs.GetFloat(TemperaturePrefKey, 0.8f);
#else
            return 0.8f;
#endif
        }
        
        /// <summary>Sets the AI temperature in EditorPrefs.</summary>
        public static void SetTemperature(float temperature)
        {
#if UNITY_EDITOR
            EditorPrefs.SetFloat(TemperaturePrefKey, temperature);
#endif
        }
        
        /// <summary>Gets the debug mode setting from EditorPrefs.</summary>
        public static bool GetDebugMode(string path = DefaultPath)
        {
#if UNITY_EDITOR
            return EditorPrefs.GetBool(DebugModePrefKey, false);
#else
            return false;
#endif
        }
        
        /// <summary>Sets the debug mode in EditorPrefs.</summary>
        public static void SetDebugMode(bool debugMode)
        {
#if UNITY_EDITOR
            EditorPrefs.SetBool(DebugModePrefKey, debugMode);
#endif
        }
        
        /// <summary>
        /// Gets the complete generator settings from EditorPrefs.
        /// </summary>
        public static ForgeGeneratorSettings GetGeneratorSettings(string path = DefaultPath)
        {
#if UNITY_EDITOR
            // Load all settings from EditorPrefs
            var settings = new ForgeGeneratorSettings
            {
                gameName = EditorPrefs.GetString(GameNamePrefKey, "My Game"),
                gameDescription = EditorPrefs.GetString(GameDescriptionPrefKey, ""),
                targetAudience = EditorPrefs.GetString(TargetAudiencePrefKey, "General"),
                defaultBatchSize = EditorPrefs.GetInt(DefaultBatchSizePrefKey, 5),
                maxBatchSize = EditorPrefs.GetInt(MaxBatchSizePrefKey, 20),
                temperature = EditorPrefs.GetFloat(TemperaturePrefKey, 0.8f),
                model = (ForgeAIModel)EditorPrefs.GetInt(ModelPrefKey, (int)ForgeAIModel.GPT4o),
                additionalRules = EditorPrefs.GetString(AdditionalRulesPrefKey, ""),
                existingAssetsSearchPath = EditorPrefs.GetString(ExistingAssetsSearchPathPrefKey, "Assets"),
                generatedAssetsBasePath = EditorPrefs.GetString(GeneratedAssetsBasePathPrefKey, "Resources/Generated"),
                autoLoadExistingAssets = EditorPrefs.GetBool(AutoLoadExistingAssetsPrefKey, true),
                intent = (ExistingItemsIntent)EditorPrefs.GetInt(IntentPrefKey, 0)
            };
            
            return settings;
#else
            return new ForgeGeneratorSettings();
#endif
        }
        
        /// <summary>
        /// Saves generator settings to EditorPrefs.
        /// </summary>
        public static void SaveGeneratorSettings(ForgeGeneratorSettings settings)
        {
#if UNITY_EDITOR
            EditorPrefs.SetString(GameNamePrefKey, settings.gameName ?? "My Game");
            EditorPrefs.SetString(GameDescriptionPrefKey, settings.gameDescription ?? "");
            EditorPrefs.SetString(TargetAudiencePrefKey, settings.targetAudience ?? "General");
            EditorPrefs.SetInt(DefaultBatchSizePrefKey, settings.defaultBatchSize);
            EditorPrefs.SetInt(MaxBatchSizePrefKey, settings.maxBatchSize);
            EditorPrefs.SetFloat(TemperaturePrefKey, settings.temperature);
            EditorPrefs.SetInt(ModelPrefKey, (int)settings.model);
            EditorPrefs.SetString(AdditionalRulesPrefKey, settings.additionalRules ?? "");
            EditorPrefs.SetString(ExistingAssetsSearchPathPrefKey, settings.existingAssetsSearchPath ?? "Assets");
            EditorPrefs.SetString(GeneratedAssetsBasePathPrefKey, settings.generatedAssetsBasePath ?? "Resources/Generated");
            EditorPrefs.SetBool(AutoLoadExistingAssetsPrefKey, settings.autoLoadExistingAssets);
            EditorPrefs.SetInt(IntentPrefKey, (int)settings.intent);
#endif
        }
        
        /// <summary>
        /// Migrates settings from config file to EditorPrefs.
        /// </summary>
        private static void MigrateFromConfigFile(ForgeConfigDto config)
        {
#if UNITY_EDITOR
            if (config == null) return;
            
            if (!string.IsNullOrWhiteSpace(config.openaiApiKey))
                SetOpenAIKey(config.openaiApiKey);
            
            EditorPrefs.SetString(GameNamePrefKey, config.gameName ?? "My Game");
            EditorPrefs.SetString(GameDescriptionPrefKey, config.gameDescription ?? "");
            EditorPrefs.SetString(TargetAudiencePrefKey, config.targetAudience ?? "General");
            EditorPrefs.SetInt(DefaultBatchSizePrefKey, config.defaultBatchSize > 0 ? config.defaultBatchSize : 5);
            EditorPrefs.SetInt(MaxBatchSizePrefKey, config.maxBatchSize > 0 ? config.maxBatchSize : 20);
            EditorPrefs.SetFloat(TemperaturePrefKey, config.temperature);
            EditorPrefs.SetInt(ModelPrefKey, config.model);
            EditorPrefs.SetString(AdditionalRulesPrefKey, config.additionalRules ?? "");
            EditorPrefs.SetString(ExistingAssetsSearchPathPrefKey, string.IsNullOrEmpty(config.existingAssetsSearchPath) ? "Assets" : config.existingAssetsSearchPath);
            EditorPrefs.SetString(GeneratedAssetsBasePathPrefKey, string.IsNullOrEmpty(config.generatedAssetsBasePath) ? "Resources/Generated" : config.generatedAssetsBasePath);
            EditorPrefs.SetBool(AutoLoadExistingAssetsPrefKey, config.autoLoadExistingAssets);
            EditorPrefs.SetInt(IntentPrefKey, config.intent);
            EditorPrefs.SetBool(DebugModePrefKey, config.debugMode);
#endif
        }

        /// <summary>
        /// Loads configuration from the JSON file for backwards compatibility.
        /// Results are cached for performance.
        /// </summary>
        private static ForgeConfigDto LoadConfig(string path)
        {
            if (_cachedConfig != null) return _cachedConfig;
            
            try
            {
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                _cachedConfig = JsonUtility.FromJson<ForgeConfigDto>(json);
                return _cachedConfig;
            }
            catch { return null; }
        }
        
        /// <summary>
        /// Clears the cached configuration, forcing a reload on next access.
        /// </summary>
        public static void ClearCache()
        {
            _cachedConfig = null;
        }
    }
}
