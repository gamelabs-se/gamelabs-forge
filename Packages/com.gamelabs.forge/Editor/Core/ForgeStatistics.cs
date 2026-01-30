using System;
using System.IO;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Tracks usage statistics for FORGE item generation.
    /// All metrics are token-based - costs are calculated from tokens using current model pricing.
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
        
        [Header("GPT-5-mini Stats")]
        public int gpt5MiniGenerations = 0;
        public int gpt5MiniItemsGenerated = 0;
        public long gpt5MiniPromptTokens = 0;
        public long gpt5MiniCompletionTokens = 0;
        
        [Header("GPT-4o Stats")]
        public int gpt4oGenerations = 0;
        public int gpt4oItemsGenerated = 0;
        public long gpt4oPromptTokens = 0;
        public long gpt4oCompletionTokens = 0;
        
        [Header("o1 Stats")]
        public int o1Generations = 0;
        public int o1ItemsGenerated = 0;
        public long o1PromptTokens = 0;
        public long o1CompletionTokens = 0;
        
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
        /// Records a successful generation. Cost is calculated from tokens.
        /// </summary>
        public void RecordGeneration(int itemsRequested, int itemsGenerated, int promptTokens, int completionTokens, float cost, ForgeAIModel model)
        {
            totalGenerations++;
            sessionGenerations++;
            
            totalItemsRequested += itemsRequested;
            totalItemsGenerated += itemsGenerated;
            sessionItemsGenerated += itemsGenerated;
            
            // Track per-model (tokens and items)
            switch (model)
            {
                case ForgeAIModel.GPT5Mini:
                    gpt5MiniGenerations++;
                    gpt5MiniItemsGenerated += itemsGenerated;
                    gpt5MiniPromptTokens += promptTokens;
                    gpt5MiniCompletionTokens += completionTokens;
                    break;
                case ForgeAIModel.GPT4o:
                    gpt4oGenerations++;
                    gpt4oItemsGenerated += itemsGenerated;
                    gpt4oPromptTokens += promptTokens;
                    gpt4oCompletionTokens += completionTokens;
                    break;
                case ForgeAIModel.O1:
                    o1Generations++;
                    o1ItemsGenerated += itemsGenerated;
                    o1PromptTokens += promptTokens;
                    o1CompletionTokens += completionTokens;
                    break;
            }
            
            if (itemsGenerated < itemsRequested)
            {
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
        
        // ===== Calculated Properties (Token-based) =====
        
        /// <summary>Total tokens across all models.</summary>
        public long GetTotalTokens()
        {
            return gpt5MiniPromptTokens + gpt5MiniCompletionTokens +
                   gpt4oPromptTokens + gpt4oCompletionTokens +
                   o1PromptTokens + o1CompletionTokens;
        }
        
        /// <summary>Total prompt (input) tokens across all models.</summary>
        public long GetTotalPromptTokens()
        {
            return gpt5MiniPromptTokens + gpt4oPromptTokens + o1PromptTokens;
        }
        
        /// <summary>Total completion (output) tokens across all models.</summary>
        public long GetTotalCompletionTokens()
        {
            return gpt5MiniCompletionTokens + gpt4oCompletionTokens + o1CompletionTokens;
        }
        
        /// <summary>Calculate cost for a model from its token usage.</summary>
        public float CalculateModelCost(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => ForgeAIModelHelper.CalculateCost(model, (int)gpt5MiniPromptTokens, (int)gpt5MiniCompletionTokens),
                ForgeAIModel.GPT4o => ForgeAIModelHelper.CalculateCost(model, (int)gpt4oPromptTokens, (int)gpt4oCompletionTokens),
                ForgeAIModel.O1 => ForgeAIModelHelper.CalculateCost(model, (int)o1PromptTokens, (int)o1CompletionTokens),
                _ => 0f
            };
        }
        
        /// <summary>Total estimated cost across all models (calculated from tokens).</summary>
        public float GetTotalCost()
        {
            return CalculateModelCost(ForgeAIModel.GPT5Mini) +
                   CalculateModelCost(ForgeAIModel.GPT4o) +
                   CalculateModelCost(ForgeAIModel.O1);
        }
        
        /// <summary>Average tokens per item for a specific model.</summary>
        public float GetAvgTokensPerItem(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => gpt5MiniItemsGenerated > 0 
                    ? (gpt5MiniPromptTokens + gpt5MiniCompletionTokens) / (float)gpt5MiniItemsGenerated 
                    : 0f,
                ForgeAIModel.GPT4o => gpt4oItemsGenerated > 0 
                    ? (gpt4oPromptTokens + gpt4oCompletionTokens) / (float)gpt4oItemsGenerated 
                    : 0f,
                ForgeAIModel.O1 => o1ItemsGenerated > 0 
                    ? (o1PromptTokens + o1CompletionTokens) / (float)o1ItemsGenerated 
                    : 0f,
                _ => 0f
            };
        }
        
        /// <summary>Average cost per item for a specific model.</summary>
        public float GetAvgCostPerItem(ForgeAIModel model)
        {
            int items = model switch
            {
                ForgeAIModel.GPT5Mini => gpt5MiniItemsGenerated,
                ForgeAIModel.GPT4o => gpt4oItemsGenerated,
                ForgeAIModel.O1 => o1ItemsGenerated,
                _ => 0
            };
            
            if (items == 0) return 0f;
            return CalculateModelCost(model) / items;
        }
        
        /// <summary>Average tokens per item across all models.</summary>
        public float GetOverallAvgTokensPerItem()
        {
            if (totalItemsGenerated == 0) return 0f;
            return GetTotalTokens() / (float)totalItemsGenerated;
        }
        
        /// <summary>Average cost per item across all models.</summary>
        public float GetOverallAvgCostPerItem()
        {
            if (totalItemsGenerated == 0) return 0f;
            return GetTotalCost() / totalItemsGenerated;
        }
        
        /// <summary>Success rate as percentage.</summary>
        public float GetSuccessRate()
        {
            if (totalGenerations == 0) return 100f;
            int successes = totalGenerations - totalFailures;
            return (successes / (float)totalGenerations) * 100f;
        }
        
        /// <summary>Fulfillment rate (items generated / items requested).</summary>
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
            
            gpt5MiniGenerations = 0;
            gpt5MiniItemsGenerated = 0;
            gpt5MiniPromptTokens = 0;
            gpt5MiniCompletionTokens = 0;
            
            gpt4oGenerations = 0;
            gpt4oItemsGenerated = 0;
            gpt4oPromptTokens = 0;
            gpt4oCompletionTokens = 0;
            
            o1Generations = 0;
            o1ItemsGenerated = 0;
            o1PromptTokens = 0;
            o1CompletionTokens = 0;
            
            firstUsed = "";
            lastUsed = "";
            sessionGenerations = 0;
            sessionItemsGenerated = 0;
            
            Save();
            ForgeLogger.Success("Statistics reset.");
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
                        ForgeLogger.DebugLog("Statistics loaded.");
                        return stats;
                    }
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Warn($"Failed to load statistics: {e.Message}");
            }
            
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
                   $"Total Tokens: {GetTotalTokens():N0} ({GetTotalPromptTokens():N0} in + {GetTotalCompletionTokens():N0} out)\n" +
                   $"Avg Tokens/Item: {GetOverallAvgTokensPerItem():F0}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"GPT-5-mini: {gpt5MiniItemsGenerated} items, {gpt5MiniPromptTokens + gpt5MiniCompletionTokens:N0} tokens, ~${CalculateModelCost(ForgeAIModel.GPT5Mini):F4}\n" +
                   $"GPT-4o: {gpt4oItemsGenerated} items, {gpt4oPromptTokens + gpt4oCompletionTokens:N0} tokens, ~${CalculateModelCost(ForgeAIModel.GPT4o):F4}\n" +
                   $"o1: {o1ItemsGenerated} items, {o1PromptTokens + o1CompletionTokens:N0} tokens, ~${CalculateModelCost(ForgeAIModel.O1):F4}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Est. Total Cost: ~${GetTotalCost():F4}\n" +
                   $"Avg Cost/Item: ~${GetOverallAvgCostPerItem():F6}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"First Used: {firstUsed}\n" +
                   $"Last Used: {lastUsed}";
        }
    }
}
