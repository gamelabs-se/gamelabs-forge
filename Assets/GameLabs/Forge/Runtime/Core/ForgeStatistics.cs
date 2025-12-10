using System;
using System.IO;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Tracks usage statistics for FORGE item generation.
    /// Statistics persist across sessions and can be reset.
    /// </summary>
    [Serializable]
    public class ForgeStatistics
    {
        private const string StatsFilePath = "Assets/GameLabs/Forge/Settings/forge.stats.json";
        
        [Header("Generation Stats")]
        public int totalGenerations = 0;
        public int totalItemsRequested = 0;
        public int totalItemsGenerated = 0;
        public int totalFailures = 0;
        
        [Header("Token Usage")]
        public long totalPromptTokens = 0;
        public long totalCompletionTokens = 0;
        
        [Header("Cost Tracking")]
        public float totalCostUSD = 0f;
        
        [Header("Session Info")]
        public string firstUsed = "";
        public string lastUsed = "";
        
        // Transient (not saved)
        [NonSerialized] public int sessionGenerations = 0;
        [NonSerialized] public int sessionItemsGenerated = 0;
        
        private static ForgeStatistics _instance;
        
        /// <summary>
        /// Gets the singleton statistics instance (loads from disk if needed).
        /// </summary>
        public static ForgeStatistics Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Records a successful generation.
        /// </summary>
        public void RecordGeneration(int itemsRequested, int itemsGenerated, int promptTokens, int completionTokens, float cost)
        {
            totalGenerations++;
            sessionGenerations++;
            
            totalItemsRequested += itemsRequested;
            totalItemsGenerated += itemsGenerated;
            sessionItemsGenerated += itemsGenerated;
            
            totalPromptTokens += promptTokens;
            totalCompletionTokens += completionTokens;
            totalCostUSD += cost;
            
            if (itemsGenerated < itemsRequested)
            {
                // Partial failure
                totalFailures++;
                ForgeLogger.Warn($"Partial generation: requested {itemsRequested}, got {itemsGenerated}");
            }
            
            UpdateTimestamps();
            Save();
        }
        
        /// <summary>
        /// Records a failed generation.
        /// </summary>
        public void RecordFailure(string reason)
        {
            totalGenerations++;
            totalFailures++;
            sessionGenerations++;
            
            ForgeLogger.Error($"Generation failed: {reason}");
            
            UpdateTimestamps();
            Save();
        }
        
        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public float GetSuccessRate()
        {
            if (totalGenerations == 0) return 100f;
            int successes = totalGenerations - totalFailures;
            return (successes / (float)totalGenerations) * 100f;
        }
        
        /// <summary>
        /// Gets the average items per generation.
        /// </summary>
        public float GetAverageItemsPerGeneration()
        {
            if (totalGenerations == 0) return 0f;
            return totalItemsGenerated / (float)totalGenerations;
        }
        
        /// <summary>
        /// Gets the average cost per generation.
        /// </summary>
        public float GetAverageCostPerGeneration()
        {
            if (totalGenerations == 0) return 0f;
            return totalCostUSD / totalGenerations;
        }
        
        /// <summary>
        /// Gets the average cost per item.
        /// </summary>
        public float GetAverageCostPerItem()
        {
            if (totalItemsGenerated == 0) return 0f;
            return totalCostUSD / totalItemsGenerated;
        }
        
        /// <summary>
        /// Gets the total tokens used.
        /// </summary>
        public long GetTotalTokens()
        {
            return totalPromptTokens + totalCompletionTokens;
        }
        
        /// <summary>
        /// Gets the fulfillment rate (items generated / items requested).
        /// </summary>
        public float GetFulfillmentRate()
        {
            if (totalItemsRequested == 0) return 100f;
            return (totalItemsGenerated / (float)totalItemsRequested) * 100f;
        }
        
        /// <summary>
        /// Resets all statistics to zero.
        /// </summary>
        public void Reset()
        {
            totalGenerations = 0;
            totalItemsRequested = 0;
            totalItemsGenerated = 0;
            totalFailures = 0;
            totalPromptTokens = 0;
            totalCompletionTokens = 0;
            totalCostUSD = 0f;
            firstUsed = "";
            lastUsed = "";
            sessionGenerations = 0;
            sessionItemsGenerated = 0;
            
            Save();
            ForgeLogger.Log("Statistics reset.");
        }
        
        /// <summary>
        /// Saves statistics to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StatsFilePath));
                var json = JsonUtility.ToJson(this, true);
                File.WriteAllText(StatsFilePath, json);
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to save statistics: {e.Message}");
            }
        }
        
        /// <summary>
        /// Loads statistics from disk.
        /// </summary>
        private static ForgeStatistics Load()
        {
            try
            {
                if (File.Exists(StatsFilePath))
                {
                    var json = File.ReadAllText(StatsFilePath);
                    var stats = JsonUtility.FromJson<ForgeStatistics>(json);
                    if (stats != null)
                    {
                        ForgeLogger.Log("Statistics loaded.");
                        return stats;
                    }
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Warn($"Failed to load statistics: {e.Message}");
            }
            
            // Return new instance if load fails
            var newStats = new ForgeStatistics();
            newStats.UpdateTimestamps();
            return newStats;
        }
        
        private void UpdateTimestamps()
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            if (string.IsNullOrEmpty(firstUsed))
            {
                firstUsed = now;
            }
            
            lastUsed = now;
        }
        
        /// <summary>
        /// Gets a formatted summary string.
        /// </summary>
        public override string ToString()
        {
            return $"FORGE Statistics\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Total Generations: {totalGenerations}\n" +
                   $"Total Items: {totalItemsGenerated} / {totalItemsRequested} requested\n" +
                   $"Success Rate: {GetSuccessRate():F1}%\n" +
                   $"Fulfillment Rate: {GetFulfillmentRate():F1}%\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Total Tokens: {GetTotalTokens():N0} ({totalPromptTokens:N0} prompt + {totalCompletionTokens:N0} completion)\n" +
                   $"Total Cost: ${totalCostUSD:F4}\n" +
                   $"Avg Cost/Generation: ${GetAverageCostPerGeneration():F6}\n" +
                   $"Avg Cost/Item: ${GetAverageCostPerItem():F6}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"First Used: {firstUsed}\n" +
                   $"Last Used: {lastUsed}";
        }
    }
}
