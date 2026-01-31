using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Represents a single generation event with all its context.
    /// </summary>
    [Serializable]
    public class ForgeGenerationRecord
    {
        public string id;
        public string timestamp;
        public string blueprintName;
        public string templateTypeName;
        public string generationMode; // "New" or "Variant"
        public string modelUsed;
        public int itemsRequested;
        public int itemsGenerated;
        public int promptTokens;
        public int completionTokens;
        public float estimatedCost;
        public float durationSeconds;
        public string userInstructions;
        public List<string> generatedAssetPaths = new List<string>();
        public List<string> generatedItemNames = new List<string>();
        public bool hadValidationErrors;
        public int retryCount;
        public string sourceAssetPath; // For variant mode - the original asset
        
        // Status
        public bool wasSuccessful;
        public string errorMessage;
        
        public ForgeGenerationRecord()
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8);
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// Gets a short summary for display.
        /// </summary>
        public string GetSummary()
        {
            string mode = generationMode == "Variant" ? "🔄" : "✨";
            string status = wasSuccessful ? "✓" : "✗";
            return $"{mode} [{timestamp}] {blueprintName ?? templateTypeName} - {itemsGenerated}/{itemsRequested} items {status}";
        }
    }
    
    /// <summary>
    /// Tracks generation history for FORGE.
    /// Allows users to review past generations, regenerate with same settings, and track patterns.
    /// </summary>
    [Serializable]
    public class ForgeGenerationHistory
    {
        private const string HistoryFilePath = "Assets/GameLabs/Forge/Settings/forge.history.json";
        private const int MaxHistorySize = 100; // Keep last 100 generations
        
        public List<ForgeGenerationRecord> records = new List<ForgeGenerationRecord>();
        
        private static ForgeGenerationHistory _instance;
        
        /// <summary>
        /// Gets the singleton history instance.
        /// </summary>
        public static ForgeGenerationHistory Instance
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
        /// Adds a new generation record.
        /// </summary>
        public ForgeGenerationRecord AddRecord(
            string blueprintName,
            string templateTypeName,
            bool isVariantMode,
            ForgeAIModel model,
            int itemsRequested,
            int itemsGenerated,
            int promptTokens,
            int completionTokens,
            float durationSeconds,
            string userInstructions,
            List<string> assetPaths,
            List<string> itemNames,
            bool hadValidationErrors,
            int retryCount,
            string sourceAssetPath = null,
            bool wasSuccessful = true,
            string errorMessage = null)
        {
            var record = new ForgeGenerationRecord
            {
                blueprintName = blueprintName,
                templateTypeName = templateTypeName,
                generationMode = isVariantMode ? "Variant" : "New",
                modelUsed = ForgeAIModelHelper.GetModelId(model),
                itemsRequested = itemsRequested,
                itemsGenerated = itemsGenerated,
                promptTokens = promptTokens,
                completionTokens = completionTokens,
                estimatedCost = ForgeAIModelHelper.CalculateCost(model, promptTokens, completionTokens),
                durationSeconds = durationSeconds,
                userInstructions = userInstructions,
                generatedAssetPaths = assetPaths ?? new List<string>(),
                generatedItemNames = itemNames ?? new List<string>(),
                hadValidationErrors = hadValidationErrors,
                retryCount = retryCount,
                sourceAssetPath = sourceAssetPath,
                wasSuccessful = wasSuccessful,
                errorMessage = errorMessage
            };
            
            records.Insert(0, record); // Most recent first
            
            // Trim to max size
            while (records.Count > MaxHistorySize)
            {
                records.RemoveAt(records.Count - 1);
            }
            
            Save();
            ForgeLogger.DebugLog($"Generation recorded: {record.GetSummary()}");
            
            return record;
        }
        
        /// <summary>
        /// Gets recent records, optionally filtered.
        /// </summary>
        public List<ForgeGenerationRecord> GetRecent(int count = 10, string blueprintFilter = null, string modeFilter = null)
        {
            IEnumerable<ForgeGenerationRecord> filtered = records;
            
            if (!string.IsNullOrEmpty(blueprintFilter))
            {
                filtered = filtered.Where(r => r.blueprintName == blueprintFilter || r.templateTypeName == blueprintFilter);
            }
            
            if (!string.IsNullOrEmpty(modeFilter))
            {
                filtered = filtered.Where(r => r.generationMode == modeFilter);
            }
            
            return filtered.Take(count).ToList();
        }
        
        /// <summary>
        /// Gets a record by ID.
        /// </summary>
        public ForgeGenerationRecord GetById(string id)
        {
            return records.FirstOrDefault(r => r.id == id);
        }
        
        /// <summary>
        /// Gets all unique blueprint/template names used.
        /// </summary>
        public List<string> GetUniqueBlueprints()
        {
            return records
                .Select(r => string.IsNullOrEmpty(r.blueprintName) ? r.templateTypeName : r.blueprintName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Calculates statistics for a specific blueprint/template.
        /// </summary>
        public (int totalItems, float avgTokens, float avgCost, float successRate) GetBlueprintStats(string blueprintName)
        {
            var blueprintRecords = records.Where(r => 
                r.blueprintName == blueprintName || r.templateTypeName == blueprintName).ToList();
            
            if (blueprintRecords.Count == 0)
                return (0, 0, 0, 0);
            
            int totalItems = blueprintRecords.Sum(r => r.itemsGenerated);
            float avgTokens = blueprintRecords.Average(r => r.promptTokens + r.completionTokens);
            float avgCost = blueprintRecords.Average(r => r.estimatedCost);
            float successRate = blueprintRecords.Count(r => r.wasSuccessful) / (float)blueprintRecords.Count * 100f;
            
            return (totalItems, avgTokens, avgCost, successRate);
        }
        
        /// <summary>
        /// Gets the most frequently used instructions for a blueprint (for suggestions).
        /// </summary>
        public List<string> GetCommonInstructions(string blueprintName, int count = 5)
        {
            return records
                .Where(r => (r.blueprintName == blueprintName || r.templateTypeName == blueprintName) 
                            && !string.IsNullOrEmpty(r.userInstructions))
                .GroupBy(r => r.userInstructions)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToList();
        }
        
        /// <summary>
        /// Clears all history.
        /// </summary>
        public void Clear()
        {
            records.Clear();
            Save();
            ForgeLogger.Success("Generation history cleared.");
        }
        
        /// <summary>
        /// Saves history to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryFilePath));
                var json = JsonUtility.ToJson(this, true);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to save history: {e.Message}");
            }
        }
        
        /// <summary>
        /// Loads history from disk.
        /// </summary>
        private static ForgeGenerationHistory Load()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    var json = File.ReadAllText(HistoryFilePath);
                    var history = JsonUtility.FromJson<ForgeGenerationHistory>(json);
                    if (history != null)
                    {
                        ForgeLogger.DebugLog($"Generation history loaded: {history.records.Count} records.");
                        return history;
                    }
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Warn($"Failed to load history: {e.Message}");
            }
            
            return new ForgeGenerationHistory();
        }
    }
}
