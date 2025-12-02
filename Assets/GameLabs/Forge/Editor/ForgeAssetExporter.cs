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
    /// 
    /// Supports two modes:
    /// 1. JSON Storage Mode (default): Saves items as ForgeGeneratedItemAsset with JSON data
    /// 2. Typed Asset Mode: Creates typed ScriptableObject assets with real properties
    ///    - Requires [ForgeAssetBinding] attribute on the ForgeItemDefinition class
    ///    - The bound ScriptableObject type should inherit from ForgeTypedAsset
    /// </summary>
    public static class ForgeAssetExporter
    {
        /// <summary>Base path for all generated assets.</summary>
        public const string GeneratedBasePath = "Assets/GameLabs/Forge/Generated";
        
        /// <summary>
        /// Creates a ScriptableObject asset from a generated item.
        /// If the item type has a [ForgeAssetBinding] attribute, creates a typed asset.
        /// Otherwise, creates a ForgeGeneratedItemAsset with JSON data.
        /// The asset is saved in: Generated/{TypeName}/{ItemName}.asset
        /// </summary>
        /// <typeparam name="T">The type of item to save.</typeparam>
        /// <param name="item">The item to save as an asset.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <param name="preferTypedAsset">If true, creates typed assets when binding exists. Default is true.</param>
        /// <returns>The created asset, or null if creation failed.</returns>
        public static ScriptableObject CreateAsset<T>(T item, string customFolder = null, bool preferTypedAsset = true) where T : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create asset from null item.");
                return null;
            }
            
            try
            {
                // Check if we should create a typed asset
                if (preferTypedAsset && ForgeTypedAssetFactory.HasBinding<T>())
                {
                    return CreateTypedAsset(item, customFolder);
                }
                
                // Fall back to JSON storage
                return CreateJsonAsset(item, customFolder);
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create asset: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a typed ScriptableObject asset from a generated item.
        /// Requires [ForgeAssetBinding] attribute on the item type.
        /// </summary>
        /// <typeparam name="T">The type of item to save.</typeparam>
        /// <param name="item">The item to save as an asset.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to asset type name.</param>
        /// <returns>The created typed asset, or null if creation failed.</returns>
        public static ScriptableObject CreateTypedAsset<T>(T item, string customFolder = null) where T : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create typed asset from null item.");
                return null;
            }
            
            var assetType = ForgeTypedAssetFactory.GetBoundAssetType<T>();
            if (assetType == null)
            {
                ForgeLogger.Error($"No asset binding found for {typeof(T).Name}. Add [ForgeAssetBinding(typeof(YourAssetType))] to the class.");
                return null;
            }
            
            try
            {
                // Determine folder path (use asset type name for typed assets)
                string typeFolderName = customFolder ?? assetType.Name;
                string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
                
                // Ensure directory exists
                EnsureDirectoryExists(folderPath);
                
                // Create the typed asset
                var asset = ForgeTypedAssetFactory.CreateTypedAsset(item);
                if (asset == null)
                {
                    ForgeLogger.Error($"Failed to create typed asset for {typeof(T).Name}");
                    return null;
                }
                
                // Generate unique filename
                string assetName = asset.name;
                if (string.IsNullOrEmpty(assetName))
                {
                    assetName = $"{assetType.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                }
                
                string fileName = GetUniqueFileName(folderPath, assetName);
                string fullPath = Path.Combine(folderPath, fileName + ".asset");
                
                // Save the asset
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
                
                ForgeLogger.Log($"Created typed asset: {fullPath}");
                return asset;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create typed asset: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a JSON-storage ScriptableObject asset from a generated item.
        /// This is the legacy behavior that stores item data as JSON.
        /// </summary>
        /// <typeparam name="T">The type of item to save.</typeparam>
        /// <param name="item">The item to save as an asset.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>The created JSON asset, or null if creation failed.</returns>
        public static ForgeGeneratedItemAsset CreateJsonAsset<T>(T item, string customFolder = null) where T : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create asset from null item.");
                return null;
            }
            
            try
            {
                // Determine folder path
                string typeFolderName = customFolder ?? typeof(T).Name;
                string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
                
                // Ensure directory exists
                EnsureDirectoryExists(folderPath);
                
                // Create the ScriptableObject using the concrete class
                var asset = ForgeGeneratedItemAsset.CreateInstance(item);
                if (asset == null)
                {
                    ForgeLogger.Error($"Failed to create ForgeGeneratedItemAsset instance for type {typeof(T).Name}");
                    return null;
                }
                
                // Generate unique filename
                string assetName = asset.name;
                if (string.IsNullOrEmpty(assetName))
                {
                    assetName = $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                }
                
                string fileName = GetUniqueFileName(folderPath, assetName);
                string fullPath = Path.Combine(folderPath, fileName + ".asset");
                
                // Save the asset
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
                
                ForgeLogger.Log($"Created asset: {fullPath}");
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
        /// If the item type has a [ForgeAssetBinding] attribute, creates typed assets.
        /// Otherwise, creates ForgeGeneratedItemAsset with JSON data.
        /// </summary>
        /// <typeparam name="T">The type of items to save.</typeparam>
        /// <param name="items">The items to save as assets.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <param name="preferTypedAssets">If true, creates typed assets when binding exists. Default is true.</param>
        /// <returns>List of created assets.</returns>
        public static List<ScriptableObject> CreateAssets<T>(IEnumerable<T> items, string customFolder = null, bool preferTypedAssets = true) where T : class
        {
            var createdAssets = new List<ScriptableObject>();
            
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
                    var asset = CreateAsset(item, customFolder, preferTypedAssets);
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
            
            ForgeLogger.Log($"Created {createdAssets.Count} assets in {Path.Combine(GeneratedBasePath, customFolder ?? typeof(T).Name)}");
            return createdAssets;
        }
        
        /// <summary>
        /// Creates multiple typed ScriptableObject assets from a list of generated items.
        /// Requires [ForgeAssetBinding] attribute on the item type.
        /// </summary>
        /// <typeparam name="T">The type of items to save.</typeparam>
        /// <param name="items">The items to save as typed assets.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to asset type name.</param>
        /// <returns>List of created typed assets.</returns>
        public static List<ScriptableObject> CreateTypedAssets<T>(IEnumerable<T> items, string customFolder = null) where T : class
        {
            var createdAssets = new List<ScriptableObject>();
            
            if (items == null)
            {
                ForgeLogger.Error("Cannot create typed assets from null collection.");
                return createdAssets;
            }
            
            if (!ForgeTypedAssetFactory.HasBinding<T>())
            {
                ForgeLogger.Error($"No asset binding found for {typeof(T).Name}. Add [ForgeAssetBinding(typeof(YourAssetType))] to the class.");
                return createdAssets;
            }
            
            // Batch the asset creation
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in items)
                {
                    var asset = CreateTypedAsset(item, customFolder);
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
            
            var assetType = ForgeTypedAssetFactory.GetBoundAssetType<T>();
            ForgeLogger.Log($"Created {createdAssets.Count} typed assets in {Path.Combine(GeneratedBasePath, customFolder ?? assetType?.Name ?? typeof(T).Name)}");
            return createdAssets;
        }
        
        /// <summary>
        /// Creates multiple JSON-storage ScriptableObject assets from a list of generated items.
        /// This is the legacy behavior that stores item data as JSON.
        /// </summary>
        /// <typeparam name="T">The type of items to save.</typeparam>
        /// <param name="items">The items to save as JSON assets.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>List of created JSON assets.</returns>
        public static List<ForgeGeneratedItemAsset> CreateJsonAssets<T>(IEnumerable<T> items, string customFolder = null) where T : class
        {
            var createdAssets = new List<ForgeGeneratedItemAsset>();
            
            if (items == null)
            {
                ForgeLogger.Error("Cannot create JSON assets from null collection.");
                return createdAssets;
            }
            
            // Batch the asset creation
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in items)
                {
                    var asset = CreateJsonAsset(item, customFolder);
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
            
            ForgeLogger.Log($"Created {createdAssets.Count} JSON assets in {Path.Combine(GeneratedBasePath, customFolder ?? typeof(T).Name)}");
            return createdAssets;
        }
        
        /// <summary>
        /// Gets all saved JSON assets from the Generated folder.
        /// </summary>
        /// <param name="customFolder">Custom subfolder name.</param>
        /// <returns>List of loaded JSON assets.</returns>
        public static List<ForgeGeneratedItemAsset> LoadJsonAssets(string customFolder)
        {
            string folderPath = Path.Combine(GeneratedBasePath, customFolder);
            
            var assets = new List<ForgeGeneratedItemAsset>();
            
            if (!Directory.Exists(folderPath))
            {
                ForgeLogger.Log($"No assets found in folder {customFolder}");
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
            
            ForgeLogger.Log($"Loaded {assets.Count} JSON assets from {customFolder}");
            return assets;
        }
        
        /// <summary>
        /// Gets all saved JSON assets of a specific type from the Generated folder.
        /// For backwards compatibility, this is an alias for LoadJsonAssets.
        /// </summary>
        /// <typeparam name="T">The type of items to load.</typeparam>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>List of loaded JSON assets.</returns>
        public static List<ForgeGeneratedItemAsset> LoadAssets<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            return LoadJsonAssets(typeFolderName);
        }
        
        // Keep old method name for backwards compatibility
        [Obsolete("Use LoadJsonAssets instead")]
        public static List<ForgeGeneratedItemAsset> LoadAssets(string customFolder)
        {
            return LoadJsonAssets(customFolder);
        }
        
        /// <summary>
        /// Loads all typed assets from a folder.
        /// </summary>
        /// <typeparam name="TAsset">The ScriptableObject asset type to load.</typeparam>
        /// <param name="customFolder">Optional custom folder. Defaults to asset type name.</param>
        /// <returns>List of loaded typed assets.</returns>
        public static List<TAsset> LoadTypedAssets<TAsset>(string customFolder = null) where TAsset : ScriptableObject
        {
            string typeFolderName = customFolder ?? typeof(TAsset).Name;
            string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
            
            var assets = new List<TAsset>();
            
            if (!Directory.Exists(folderPath))
            {
                ForgeLogger.Log($"No assets found in folder {typeFolderName}");
                return assets;
            }
            
            // Find all assets of the specified type in the folder
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            ForgeLogger.Log($"Loaded {assets.Count} {typeof(TAsset).Name} assets from {typeFolderName}");
            return assets;
        }
        
        /// <summary>
        /// Checks if a ForgeItemDefinition type has a bound ScriptableObject type.
        /// </summary>
        /// <typeparam name="T">The ForgeItemDefinition type to check.</typeparam>
        /// <returns>True if a binding exists, false otherwise.</returns>
        public static bool HasTypedAssetBinding<T>() where T : class
        {
            return ForgeTypedAssetFactory.HasBinding<T>();
        }
        
        /// <summary>
        /// Gets the bound ScriptableObject type for a ForgeItemDefinition type.
        /// </summary>
        /// <typeparam name="T">The ForgeItemDefinition type.</typeparam>
        /// <returns>The bound ScriptableObject type, or null if no binding exists.</returns>
        public static Type GetBoundAssetType<T>() where T : class
        {
            return ForgeTypedAssetFactory.GetBoundAssetType<T>();
        }
        
        /// <summary>
        /// Gets the folder path for a specific item type.
        /// </summary>
        public static string GetTypeFolderPath<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            return Path.Combine(GeneratedBasePath, typeFolderName);
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
            string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
            
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
            ForgeLogger.Log($"Deleted {count} {typeof(T).Name} assets");
            return count;
        }
        
        /// <summary>
        /// Gets the count of saved assets for a specific type.
        /// </summary>
        public static int GetAssetCount<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
            
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }
            
            return Directory.GetFiles(folderPath, "*.asset").Length;
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
                ForgeLogger.Log($"Created folder: {path}");
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
