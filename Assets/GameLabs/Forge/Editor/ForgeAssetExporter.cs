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
    /// </summary>
    public static class ForgeAssetExporter
    {
        /// <summary>Base path for all generated assets.</summary>
        public const string GeneratedBasePath = "Assets/GameLabs/Forge/Generated";
        
        /// <summary>
        /// Creates a ScriptableObject asset from a generated item.
        /// The asset is saved in: Generated/{TypeName}/{ItemName}.asset
        /// </summary>
        /// <typeparam name="T">The type of item to save.</typeparam>
        /// <param name="item">The item to save as an asset.</param>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>The created asset, or null if creation failed.</returns>
        public static ForgeItemAsset<T> CreateAsset<T>(T item, string customFolder = null) where T : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create asset from null item.");
                return null;
            }
            
            // Determine folder path
            string typeFolderName = customFolder ?? typeof(T).Name;
            string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
            
            // Ensure directory exists
            EnsureDirectoryExists(folderPath);
            
            // Create the ScriptableObject
            var asset = ForgeItemAsset<T>.CreateInstance(item);
            
            // Generate unique filename
            string fileName = GetUniqueFileName(folderPath, asset.name);
            string fullPath = Path.Combine(folderPath, fileName + ".asset");
            
            // Save the asset
            try
            {
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
                
                ForgeLogger.Log($"Created asset: {fullPath}");
                return asset;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create asset: {e.Message}");
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
        public static List<ForgeItemAsset<T>> CreateAssets<T>(IEnumerable<T> items, string customFolder = null) where T : class
        {
            var createdAssets = new List<ForgeItemAsset<T>>();
            
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
            
            ForgeLogger.Log($"Created {createdAssets.Count} assets in {Path.Combine(GeneratedBasePath, customFolder ?? typeof(T).Name)}");
            return createdAssets;
        }
        
        /// <summary>
        /// Gets all saved assets of a specific type from the Generated folder.
        /// </summary>
        /// <typeparam name="T">The type of items to load.</typeparam>
        /// <param name="customFolder">Optional custom subfolder name. Defaults to type name.</param>
        /// <returns>List of loaded assets.</returns>
        public static List<ForgeItemAsset<T>> LoadAssets<T>(string customFolder = null) where T : class
        {
            string typeFolderName = customFolder ?? typeof(T).Name;
            string folderPath = Path.Combine(GeneratedBasePath, typeFolderName);
            
            var assets = new List<ForgeItemAsset<T>>();
            
            if (!Directory.Exists(folderPath))
            {
                ForgeLogger.Log($"No assets found for type {typeof(T).Name}");
                return assets;
            }
            
            // Find all ScriptableObject assets in the folder and filter by type
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ForgeItemAsset<T>>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            ForgeLogger.Log($"Loaded {assets.Count} {typeof(T).Name} assets");
            return assets;
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
