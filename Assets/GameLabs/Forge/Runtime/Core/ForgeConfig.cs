using System.IO;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>Lightweight config loader for FORGE.</summary>
    public static class ForgeConfig
    {
        public const string DefaultPath = "Assets/GameLabs/Forge/Settings/forge.config.json";

        [System.Serializable]
        private class ForgeConfigDto { public string openaiApiKey; }

        public static string GetOpenAIKey(string path = DefaultPath)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                var dto = JsonUtility.FromJson<ForgeConfigDto>(json);
                return string.IsNullOrWhiteSpace(dto?.openaiApiKey) ? null : dto.openaiApiKey.Trim();
            }
            catch { return null; }
        }
    }
}
