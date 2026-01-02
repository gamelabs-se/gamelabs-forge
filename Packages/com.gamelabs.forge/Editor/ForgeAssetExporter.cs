#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Utility for creating and managing ScriptableObject assets from generated items.
    /// Organizes assets by type in the Generated folder for easy access.
    /// Saves items as ForgeGeneratedItemAsset with JSON data.
    /// </summary>
    public static class ForgeAssetExporter
    {
        /// <summary>Default base path for all generated assets.</summary>
        public const string DefaultGeneratedBasePath = "Assets/Resources/Generated";
        
        /// <summary>Legacy base path (kept for backwards compatibility).</summary>
        [Obsolete("Use GetGeneratedBasePath() instead")]
        public const string GeneratedBasePath = "Assets/Resources/Generated";
        
        /// <summary>Gets the base path for generated assets from settings or default.</summary>
        public static string GetGeneratedBasePath()
        {
            var settings = ForgeConfig.GetGeneratorSettings();
            if (settings != null && !string.IsNullOrEmpty(settings.generatedAssetsBasePath))
            {
                // Ensure it starts with "Assets/"
                string path = settings.generatedAssetsBasePath;
                if (!path.StartsWith("Assets/"))
                {
                    path = "Assets/" + path.TrimStart('/');
                }
                return path;
            }
            return DefaultGeneratedBasePath;
        }
        
        /// <summary>
        /// Creates a ScriptableObject asset from a generated item.
        /// The asset is saved in: Generated/{TypeName}/{ItemName}.asset
        /// </summary>
        /// <typeparam name="T">The type of item to save.</typeparam>
        /// <param name="item">The item to save as an asset.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>The created asset, or null if creation failed.</returns>
        public static ForgeGeneratedItemAsset CreateAsset<T>(T item, string customFolder = null) where T : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create asset from null item.");
                return null;
            }
            
            return CreateJsonAsset(item, customFolder);
        }
        
        /// <summary>
        /// Creates a JSON-storage ScriptableObject asset from a generated item.
        /// </summary>
        /// <typeparam name="T">The type of item to save.</typeparam>
        /// <param name="item">The item to save as an asset.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>The created JSON asset, or null if creation failed.</returns>
        private static ForgeGeneratedItemAsset CreateJsonAsset<T>(T item, string customFolder = null) where T : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create asset from null item.");
                return null;
            }
            
            try
            {
                // Determine folder path using configurable base path
                string basePath = GetGeneratedBasePath();
                string typeFolderName = customFolder ?? typeof(T).Name;
                string folderPath = Path.Combine(basePath, typeFolderName);
                
                // Ensure directory exists
                EnsureDirectoryExists(folderPath);
                
                // Create the ScriptableObject using the concrete class
                var asset = ForgeGeneratedItemAsset.CreateInstance(item);
                if (asset == null)
                {
                    ForgeLogger.Error($"Failed to create ForgeGeneratedItemAsset instance for type {typeof(T).Name}");
                    return null;
                }
                
                // Generate unique filename from item data
                string assetName = asset.name;
                if (string.IsNullOrEmpty(assetName))
                {
                    // Try to extract name from the item's properties (e.g., name, displayName, itemName)
                    var nameProperty = typeof(T).GetProperty("name") 
                        ?? typeof(T).GetProperty("displayName") 
                        ?? typeof(T).GetProperty("itemName");
                    
                    if (nameProperty != null)
                    {
                        var nameValue = nameProperty.GetValue(item);
                        if (nameValue != null && !string.IsNullOrEmpty(nameValue.ToString()))
                        {
                            assetName = nameValue.ToString();
                        }
                    }
                    
                    // Final fallback: just use the type name
                    if (string.IsNullOrEmpty(assetName))
                    {
                        assetName = typeof(T).Name;
                    }
                }
                
                string fileName = GetUniqueFileName(folderPath, assetName);
                string fullPath = Path.Combine(folderPath, fileName + ".asset");
                
                // Save the asset
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
                
                ForgeLogger.DebugLog($"Created asset: {fullPath}");
                return asset;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create asset: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates multiple ScriptableObject assets from a list of generated items.
        /// </summary>
        /// <typeparam name="T">The type of items to save.</typeparam>
        /// <param name="items">The items to save as assets.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>List of created assets.</returns>
        public static List<ForgeGeneratedItemAsset> CreateAssets<T>(IEnumerable<T> items, string customFolder = null) where T : class
        {
            var createdAssets = new List<ForgeGeneratedItemAsset>();
            
            if (items == null)
            {
                ForgeLogger.Error("Cannot create assets from null collection.");
                return createdAssets;
            }
            
            // Batch the asset creation
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in items)
                {
                    var asset = CreateAsset(item, customFolder);
                    if (asset != null)
                    {
                        createdAssets.Add(asset);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            ForgeLogger.Success($"Created {createdAssets.Count} assets in {Path.Combine(GetGeneratedBasePath(), customFolder ?? typeof(T).Name)}");
            return createdAssets;
        }
        
        /// <summary>
        /// Gets all saved assets from the Generated folder.
        /// </summary>
        /// <param name="customFolder">Custom subfolder name.</param>
        /// <returns>List of loaded assets.</returns>
        public static List<ForgeGeneratedItemAsset> LoadAssets(string customFolder)
        {
            string folderPath = Path.Combine(GetGeneratedBasePath(), customFolder);
            
            var assets = new List<ForgeGeneratedItemAsset>();
            
            if (!Directory.Exists(folderPath))
            {
                ForgeLogger.DebugLog($"No assets found in folder {customFolder}");
                return assets;
            }
            
            // Find all ForgeGeneratedItemAsset assets in the folder
            string[] guids = AssetDatabase.FindAssets("t:ForgeGeneratedItemAsset", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ForgeGeneratedItemAsset>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            ForgeLogger.DebugLog($"Loaded {assets.Count} assets from {customFolder}");
            return assets;
        }
        
        /// <summary>
        /// Gets all saved assets of a specific type from the Generated folder.
        /// </summary>
        /// <typeparam name="T">The type of items to load.</typeparam>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>List of loaded assets.</returns>
        public static List<ForgeGeneratedItemAsset> LoadAssets<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            return LoadAssets(typeFolderName);
        }
        
        /// <summary>
        /// Gets the folder path for a specific item type.
        /// </summary>
        public static string GetTypeFolderPath<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            return Path.Combine(GetGeneratedBasePath(), typeFolderName);
        }
        
        /// <summary>
        /// Opens the folder for a specific item type in the Project window.
        /// </summary>
        public static void RevealTypeFolder<T>(string customFolder = null) where T : class
        {
            string folderPath = GetTypeFolderPath<T>(customFolder);
            
            if (!Directory.Exists(folderPath))
            {
                EnsureDirectoryExists(folderPath);
            }
            
            var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
            if (folderAsset != null)
            {
                EditorGUIUtility.PingObject(folderAsset);
                Selection.activeObject = folderAsset;
            }
        }
        
        /// <summary>
        /// Deletes all generated assets of a specific type.
        /// </summary>
        public static int ClearTypeAssets<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            string folderPath = Path.Combine(GetGeneratedBasePath(), typeFolderName);
            
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }
            
            string[] files = Directory.GetFiles(folderPath, "*.asset");
            int count = 0;
            
            foreach (string file in files)
            {
                if (AssetDatabase.DeleteAsset(file))
                {
                    count++;
                }
            }
            
            AssetDatabase.Refresh();
            ForgeLogger.Success($"Deleted {count} {typeof(T).Name} assets");
            return count;
        }
        
        /// <summary>
        /// Gets the count of saved assets for a specific type.
        /// </summary>
        public static int GetAssetCount<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            string folderPath = Path.Combine(GetGeneratedBasePath(), typeFolderName);
            
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }
            
            return Directory.GetFiles(folderPath, "*.asset").Length;
        }
        
        /// <summary>
        /// Gets the save path for a specific type.
        /// </summary>
        public static string GetSavePathFor(Type type, string customFolder = null)
        {
            string typeFolderName = customFolder ?? type.Name;
            return Path.Combine(GetGeneratedBasePath(), typeFolderName);
        }
        
        /// <summary>
        /// Ensures a directory exists, creating it if necessary.
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (Directory.Exists(path))
                return;
                
            // Create parent directories as needed
            string parentPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath))
            {
                EnsureDirectoryExists(parentPath);
            }
            
            // Create the directory via AssetDatabase for proper Unity integration
            string parentFolder = Path.GetDirectoryName(path);
            string newFolderName = Path.GetFileName(path);
            
            if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(newFolderName))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
                ForgeLogger.DebugLog($"Created folder: {path}");
            }
        }
        
        /// <summary>
        /// Generates a unique filename to avoid overwriting existing assets.
        /// </summary>
        private static string GetUniqueFileName(string folderPath, string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "Item";
            }
            
            string fileName = baseName;
            string fullPath = Path.Combine(folderPath, fileName + ".asset");
            int counter = 1;
            
            // Use AssetDatabase to check for existing assets for better Unity integration
            while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath) != null)
            {
                fileName = $"{baseName}_{counter}";
                fullPath = Path.Combine(folderPath, fileName + ".asset");
                counter++;
            }
            
            return fileName;
        }
    }
}
#endif
