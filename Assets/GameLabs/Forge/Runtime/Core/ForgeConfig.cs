using System.IO;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Lightweight configuration loader for Forge.
    /// Reads settings from a JSON configuration file and provides access to API keys,
    /// model settings, and generation preferences.
    /// </summary>
    public static class ForgeConfig
    {
        /// <summary>Default path to the configuration file.</summary>
        public const string DefaultPath = "Assets/GameLabs/Forge/Settings/forge.config.json";

        /// <summary>Internal DTO for deserializing configuration from JSON.</summary>
        [System.Serializable]
        private class ForgeConfigDto
        {
            public string openaiApiKey;
            public int model; // ForgeAIModel enum value
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

        private static ForgeConfigDto _cachedConfig;
        
        /// <summary>Gets the OpenAI API key from the configuration file.</summary>
        /// <param name="path">Path to the config file (defaults to DefaultPath).</param>
        /// <returns>The API key, or null if not found.</returns>
        public static string GetOpenAIKey(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            return string.IsNullOrWhiteSpace(config?.openaiApiKey) ? null : config.openaiApiKey.Trim();
        }
        
        /// <summary>Gets the AI model from the configuration.</summary>
        /// <param name="path">Path to the config file (defaults to DefaultPath).</param>
        /// <returns>The ForgeAIModel enum value, or GPT4o as default.</returns>
        public static ForgeAIModel GetModel(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            if (config == null) return ForgeAIModel.GPT4o;
            return (ForgeAIModel)config.model;
        }
        
        /// <summary>Gets the AI temperature setting (creativity level) from the configuration.</summary>
        /// <param name="path">Path to the config file (defaults to DefaultPath).</param>
        /// <returns>The temperature value (0.0 to 2.0), or 0.8 as default.</returns>
        public static float GetTemperature(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            return config?.temperature ?? 0.8f;
        }
        
        /// <summary>
        /// Gets the complete generator settings from the configuration file.
        /// Returns default settings if config file is not found.
        /// </summary>
        /// <param name="path">Path to the config file (defaults to DefaultPath).</param>
        /// <returns>A ForgeGeneratorSettings object with all configuration values.</returns>
        public static ForgeGeneratorSettings GetGeneratorSettings(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            if (config == null) return new ForgeGeneratorSettings();
            
            return new ForgeGeneratorSettings
            {
                gameName = config.gameName ?? "My Game",
                gameDescription = config.gameDescription ?? "",
                targetAudience = config.targetAudience ?? "General",
                defaultBatchSize = config.defaultBatchSize > 0 ? config.defaultBatchSize : 5,
                maxBatchSize = config.maxBatchSize > 0 ? config.maxBatchSize : 20,
                temperature = config.temperature,
                model = (ForgeAIModel)config.model,
                additionalRules = config.additionalRules ?? "",
                existingAssetsSearchPath = string.IsNullOrEmpty(config.existingAssetsSearchPath) ? "Assets" : config.existingAssetsSearchPath,
                generatedAssetsBasePath = string.IsNullOrEmpty(config.generatedAssetsBasePath) ? "Resources/Generated" : config.generatedAssetsBasePath,
                autoLoadExistingAssets = config.autoLoadExistingAssets,
                intent = (ExistingItemsIntent)config.intent
            };
        }

        /// <summary>
        /// Loads configuration from the JSON file.
        /// Results are cached for performance.
        /// </summary>
        /// <param name="path">Path to the config file.</param>
        /// <returns>The loaded config DTO, or null if file doesn't exist or parsing fails.</returns>
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
        /// Call this after modifying the config file.
        /// </summary>
        public static void ClearCache()
        {
            _cachedConfig = null;
        }
    }
}
