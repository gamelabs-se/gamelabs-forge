using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Utility for exporting and importing generated items.
    /// Supports JSON serialization for offline game dev workflow.
    /// </summary>
    public static class ForgeItemExporter
    {
        private const string DefaultExportPath = "Assets/GameLabs/Forge/Generated";
        
        /// <summary>
        /// Exports a single item to a JSON file.
        /// </summary>
        public static string ExportItem<T>(T item, string filename = null, string path = null) where T : class
        {
            path ??= DefaultExportPath;
            filename ??= $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            EnsureDirectory(path);
            
            var json = JsonUtility.ToJson(item, true);
            var fullPath = Path.Combine(path, filename);
            
            File.WriteAllText(fullPath, json);
            ForgeLogger.Log($"Exported item to: {fullPath}");
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            
            return fullPath;
        }
        
        /// <summary>
        /// Exports multiple items to a single JSON file as an array.
        /// </summary>
        public static string ExportItems<T>(IEnumerable<T> items, string filename = null, string path = null) where T : class
        {
            path ??= DefaultExportPath;
            filename ??= $"{typeof(T).Name}s_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            EnsureDirectory(path);
            
            // Unity's JsonUtility doesn't support root-level arrays, so we wrap it
            var wrapper = new ItemArrayWrapper<T> { items = new List<T>(items) };
            var json = JsonUtility.ToJson(wrapper, true);
            
            // Find the array boundaries in the wrapper object
            // The wrapper format is: {"items":[ ... ]}
            // We need to find the array that follows "items":
            const string itemsKey = "\"items\":";
            var itemsIndex = json.IndexOf(itemsKey, StringComparison.Ordinal);
            if (itemsIndex >= 0)
            {
                var arrayStart = json.IndexOf('[', itemsIndex + itemsKey.Length);
                if (arrayStart >= 0)
                {
                    // Find matching closing bracket by counting brackets
                    int depth = 0;
                    int arrayEnd = -1;
                    for (int i = arrayStart; i < json.Length; i++)
                    {
                        if (json[i] == '[') depth++;
                        else if (json[i] == ']')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                arrayEnd = i;
                                break;
                            }
                        }
                    }
                    
                    if (arrayEnd > arrayStart)
                    {
                        json = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
                    }
                }
            }
            
            var fullPath = Path.Combine(path, filename);
            File.WriteAllText(fullPath, json);
            
            ForgeLogger.Log($"Exported {wrapper.items.Count} items to: {fullPath}");
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            
            return fullPath;
        }
        
        /// <summary>
        /// Imports a single item from a JSON file.
        /// </summary>
        public static T ImportItem<T>(string filepath) where T : class
        {
            if (!File.Exists(filepath))
            {
                ForgeLogger.Error($"File not found: {filepath}");
                return null;
            }
            
            try
            {
                var json = File.ReadAllText(filepath);
                var item = JsonUtility.FromJson<T>(json);
                ForgeLogger.Log($"Imported item from: {filepath}");
                return item;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to import item: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Imports multiple items from a JSON array file.
        /// </summary>
        public static List<T> ImportItems<T>(string filepath) where T : class
        {
            if (!File.Exists(filepath))
            {
                ForgeLogger.Error($"File not found: {filepath}");
                return new List<T>();
            }
            
            try
            {
                var json = File.ReadAllText(filepath);
                
                // Wrap array in object for JsonUtility
                var wrappedJson = $"{{\"items\":{json}}}";
                var wrapper = JsonUtility.FromJson<ItemArrayWrapper<T>>(wrappedJson);
                
                ForgeLogger.Log($"Imported {wrapper?.items?.Count ?? 0} items from: {filepath}");
                return wrapper?.items ?? new List<T>();
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to import items: {e.Message}");
                return new List<T>();
            }
        }
        
        /// <summary>
        /// Gets the default export path for generated items.
        /// </summary>
        public static string GetDefaultExportPath()
        {
            return DefaultExportPath;
        }
        
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                ForgeLogger.Log($"Created directory: {path}");
            }
        }
        
        [Serializable]
        private class ItemArrayWrapper<T>
        {
            public List<T> items;
        }
    }
}
