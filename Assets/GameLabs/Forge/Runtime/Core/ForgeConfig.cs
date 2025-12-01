using System.IO;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>Lightweight config loader for FORGE.</summary>
    public static class ForgeConfig
    {
        public const string DefaultPath = "Assets/GameLabs/Forge/Settings/forge.config.json";

        [System.Serializable]
        private class ForgeConfigDto
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
        }

        private static ForgeConfigDto _cachedConfig;
        
        public static string GetOpenAIKey(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            return string.IsNullOrWhiteSpace(config?.openaiApiKey) ? null : config.openaiApiKey.Trim();
        }
        
        public static string GetModel(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            return string.IsNullOrWhiteSpace(config?.model) ? "gpt-4o-mini" : config.model;
        }
        
        public static float GetTemperature(string path = DefaultPath)
        {
            var config = LoadConfig(path);
            return config?.temperature ?? 0.8f;
        }
        
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
                model = config.model ?? "gpt-4o-mini",
                additionalRules = config.additionalRules ?? ""
            };
        }

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
        
        /// <summary>Clears cached config to force reload.</summary>
        public static void ClearCache()
        {
            _cachedConfig = null;
        }
    }
}
