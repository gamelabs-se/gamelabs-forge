using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Lightweight configuration loader for Forge.
    /// All settings are stored per-project in UserSettings/ForgeConfig.json (gitignored by default).
    /// </summary>
    public static class ForgeConfig
    {
        /// <summary>Project-specific config path (gitignored by Unity).</summary>
        private const string ConfigPath = "UserSettings/ForgeConfig.json";
        
        /// <summary>Configuration DTO.</summary>
        [System.Serializable]
        private class ForgeConfigDto
        {
            public string openaiApiKey;
            public int model;
            public string gameName;
            public string gameDescription;
            public string targetAudience;
            public int defaultBatchSize = 5;
            public int maxBatchSize = 20;
            public float temperature = 0.8f;
            public string additionalRules;
            public string existingAssetsSearchPath = "Assets";
            public string generatedAssetsBasePath = "Resources/Generated";
            public int duplicateStrategy;
            public bool debugMode;
        }

        private static ForgeConfigDto _cachedConfig;
        
        private static ForgeConfigDto LoadOrCreateConfig()
        {
            if (_cachedConfig != null) return _cachedConfig;
            
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    _cachedConfig = JsonUtility.FromJson<ForgeConfigDto>(json);
                }
                else
                {
                    _cachedConfig = new ForgeConfigDto();
                }
                return _cachedConfig;
            }
            catch
            {
                _cachedConfig = new ForgeConfigDto();
                return _cachedConfig;
            }
        }
        
        private static void SaveConfig(ForgeConfigDto config)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                    
                var json = JsonUtility.ToJson(config, true);
                File.WriteAllText(ConfigPath, json);
                _cachedConfig = config;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Forge] Failed to save config: {e.Message}");
            }
        }
        
        /// <summary>Gets the OpenAI API key from project config.</summary>
        public static string GetOpenAIKey(string path = null)
        {
            var config = LoadOrCreateConfig();
            return string.IsNullOrWhiteSpace(config.openaiApiKey) ? null : config.openaiApiKey.Trim();
        }
        
        /// <summary>Sets the OpenAI API key in project config.</summary>
        public static void SetOpenAIKey(string apiKey)
        {
            var config = LoadOrCreateConfig();
            config.openaiApiKey = apiKey?.Trim();
            SaveConfig(config);
        }
        
        /// <summary>Gets the AI model from project config.</summary>
        public static ForgeAIModel GetModel(string path = null)
        {
            var config = LoadOrCreateConfig();
            return (ForgeAIModel)config.model;
        }
        
        /// <summary>Sets the AI model in project config.</summary>
        public static void SetModel(ForgeAIModel model)
        {
            var config = LoadOrCreateConfig();
            config.model = (int)model;
            SaveConfig(config);
        }
        
        /// <summary>Gets the AI temperature setting from project config.</summary>
        public static float GetTemperature(string path = null)
        {
            var config = LoadOrCreateConfig();
            return config.temperature;
        }
        
        /// <summary>Sets the AI temperature in project config.</summary>
        public static void SetTemperature(float temperature)
        {
            var config = LoadOrCreateConfig();
            config.temperature = temperature;
            SaveConfig(config);
        }
        
        /// <summary>Gets the debug mode setting from project config.</summary>
        public static bool GetDebugMode(string path = null)
        {
            var config = LoadOrCreateConfig();
            return config.debugMode;
        }
        
        /// <summary>Sets the debug mode in project config.</summary>
        public static void SetDebugMode(bool debugMode)
        {
            var config = LoadOrCreateConfig();
            config.debugMode = debugMode;
            SaveConfig(config);
        }
        
        /// <summary>Gets the complete generator settings from project config.</summary>
        public static ForgeGeneratorSettings GetGeneratorSettings(string path = null)
        {
            var config = LoadOrCreateConfig();
            
            return new ForgeGeneratorSettings
            {
                gameName = config.gameName ?? "My Game",
                gameDescription = config.gameDescription ?? "",
                targetAudience = config.targetAudience ?? "General",
                defaultBatchSize = config.defaultBatchSize,
                maxBatchSize = config.maxBatchSize,
                temperature = config.temperature,
                model = (ForgeAIModel)config.model,
                additionalRules = config.additionalRules ?? "",
                existingAssetsSearchPath = config.existingAssetsSearchPath ?? "Assets",
                generatedAssetsBasePath = config.generatedAssetsBasePath ?? "Resources/Generated",
                duplicateStrategy = (ForgeDuplicateStrategy)config.duplicateStrategy
            };
        }
        
        /// <summary>Saves generator settings to project config.</summary>
        public static void SaveGeneratorSettings(ForgeGeneratorSettings settings)
        {
            var config = LoadOrCreateConfig();
            
            config.gameName = settings.gameName;
            config.gameDescription = settings.gameDescription;
            config.targetAudience = settings.targetAudience;
            config.defaultBatchSize = settings.defaultBatchSize;
            config.maxBatchSize = settings.maxBatchSize;
            config.temperature = settings.temperature;
            config.model = (int)settings.model;
            config.additionalRules = settings.additionalRules;
            config.existingAssetsSearchPath = settings.existingAssetsSearchPath;
            config.generatedAssetsBasePath = settings.generatedAssetsBasePath;
            config.duplicateStrategy = (int)settings.duplicateStrategy;
            
            SaveConfig(config);
        }
        
        /// <summary>Clears the cached configuration, forcing a reload on next access.</summary>
        public static void ClearCache()
        {
            _cachedConfig = null;
        }
    }
}
