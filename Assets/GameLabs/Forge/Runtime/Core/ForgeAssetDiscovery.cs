using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Utility for discovering existing assets in the project.
    /// Searches for ScriptableObject assets that match specific types.
    /// </summary>
    public static class ForgeAssetDiscovery
    {
        /// <summary>
        /// Discovers existing ScriptableObject assets of a specific type.
        /// Searches in the specified search path (relative to Assets folder).
        /// Cross-platform compatible using Unity's path handling.
        /// </summary>
        /// <typeparam name="T">The type of asset to search for.</typeparam>
        /// <param name="searchPath">Search path relative to Assets folder (e.g., "Resources").</param>
        /// <returns>List of discovered assets.</returns>
        public static List<T> DiscoverAssets<T>(string searchPath = "Resources") where T : ScriptableObject
        {
            var assets = new List<T>();
            
#if UNITY_EDITOR
            // In editor, use AssetDatabase for searching
            string fullPath = NormalizePath(Path.Combine("Assets", searchPath));
            
            if (!Directory.Exists(fullPath))
            {
                ForgeLogger.Debug($"Search path does not exist: {fullPath}");
                return assets;
            }
            
            // Find all assets of type T in the search path
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { fullPath });
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            ForgeLogger.Debug($"Discovered {assets.Count} existing {typeof(T).Name} assets in {fullPath}");
#else
            // At runtime, use Resources.LoadAll if the search path is Resources or starts with Resources/
            bool isResourcesPath = searchPath.Equals("Resources", StringComparison.OrdinalIgnoreCase) || 
                                  searchPath.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase) ||
                                  searchPath.StartsWith("Resources\\", StringComparison.OrdinalIgnoreCase);
            
            if (isResourcesPath)
            {
                // Extract the path after Resources/
                string resourcesPath = "";
                if (searchPath.Length > "Resources".Length)
                {
                    resourcesPath = searchPath.Substring("Resources".Length).TrimStart('/', '\\');
                }
                
                T[] loadedAssets;
                if (string.IsNullOrEmpty(resourcesPath))
                {
                    // Load from root Resources folder
                    loadedAssets = Resources.LoadAll<T>("");
                }
                else
                {
                    // Load from specific subfolder
                    loadedAssets = Resources.LoadAll<T>(resourcesPath);
                }
                
                assets.AddRange(loadedAssets);
                ForgeLogger.Debug($"Loaded {assets.Count} existing {typeof(T).Name} assets from Resources");
            }
            else
            {
                ForgeLogger.Warn($"Runtime asset discovery only supports Resources folder. Search path '{searchPath}' is not supported.");
            }
#endif
            
            return assets;
        }
        
        /// <summary>
        /// Discovers existing ForgeGeneratedItemAsset files of a specific type.
        /// This is used to find previously generated items.
        /// </summary>
        /// <param name="typeName">The type name to search for.</param>
        /// <param name="searchPath">Search path relative to Assets folder.</param>
        /// <returns>List of discovered ForgeGeneratedItemAsset instances.</returns>
        public static List<ForgeGeneratedItemAsset> DiscoverGeneratedAssets(string typeName, string searchPath = "Resources")
        {
            var assets = new List<ForgeGeneratedItemAsset>();
            
#if UNITY_EDITOR
            string fullPath = NormalizePath(Path.Combine("Assets", searchPath));
            
            if (!Directory.Exists(fullPath))
            {
                ForgeLogger.Debug($"Search path does not exist: {fullPath}");
                return assets;
            }
            
            // Find all ForgeGeneratedItemAsset files
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ForgeGeneratedItemAsset", new[] { fullPath });
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ForgeGeneratedItemAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ForgeGeneratedItemAsset>(assetPath);
                
                if (asset != null && asset.ItemTypeName == typeName)
                {
                    assets.Add(asset);
                }
            }
            
            ForgeLogger.Debug($"Discovered {assets.Count} existing {typeName} generated assets in {fullPath}");
#else
            // At runtime, use Resources.LoadAll if the search path is Resources or starts with Resources/
            bool isResourcesPath = searchPath.Equals("Resources", StringComparison.OrdinalIgnoreCase) || 
                                  searchPath.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase) ||
                                  searchPath.StartsWith("Resources\\", StringComparison.OrdinalIgnoreCase);
            
            if (isResourcesPath)
            {
                string resourcesPath = "";
                if (searchPath.Length > "Resources".Length)
                {
                    resourcesPath = searchPath.Substring("Resources".Length).TrimStart('/', '\\');
                }
                
                ForgeGeneratedItemAsset[] loadedAssets;
                if (string.IsNullOrEmpty(resourcesPath))
                {
                    loadedAssets = Resources.LoadAll<ForgeGeneratedItemAsset>("");
                }
                else
                {
                    loadedAssets = Resources.LoadAll<ForgeGeneratedItemAsset>(resourcesPath);
                }
                
                foreach (var asset in loadedAssets)
                {
                    if (asset != null && asset.ItemTypeName == typeName)
                    {
                        assets.Add(asset);
                    }
                }
                
                ForgeLogger.Debug($"Loaded {assets.Count} existing {typeName} generated assets from Resources");
            }
            else
            {
                ForgeLogger.Warn($"Runtime asset discovery only supports Resources folder. Search path '{searchPath}' is not supported at runtime.");
            }
#endif
            
            return assets;
        }
        
        /// <summary>
        /// Discovers existing ScriptableObject assets and returns them as JSON strings.
        /// This is a type-safe method for ScriptableObject types.
        /// </summary>
        /// <typeparam name="T">The ScriptableObject type to discover.</typeparam>
        /// <param name="searchPath">Search path relative to Assets folder.</param>
        /// <returns>List of JSON strings representing discovered assets.</returns>
        public static List<string> DiscoverScriptableObjectsAsJson<T>(string searchPath = "Resources") where T : ScriptableObject
        {
            var jsonStrings = new List<string>();
            var assets = DiscoverAssets<T>(searchPath);
            
            foreach (var asset in assets)
            {
                if (asset != null)
                {
                    try
                    {
                        string json = JsonUtility.ToJson(asset);
                        if (!string.IsNullOrEmpty(json))
                        {
                            jsonStrings.Add(json);
                        }
                    }
                    catch (Exception e)
                    {
                        ForgeLogger.Warn($"Failed to serialize asset to JSON: {e.Message}");
                    }
                }
            }
            
            return jsonStrings;
        }
        
        /// <summary>
        /// Discovers existing assets and returns them as JSON strings.
        /// This method works for any type by checking ForgeGeneratedItemAsset instances.
        /// For ScriptableObject types, also discovers direct ScriptableObject assets.
        /// </summary>
        /// <typeparam name="T">The type of asset to discover.</typeparam>
        /// <param name="searchPath">Search path relative to Assets folder.</param>
        /// <returns>List of JSON strings representing discovered assets.</returns>
        public static List<string> DiscoverAssetsAsJson<T>(string searchPath = "Resources") where T : class
        {
            var jsonStrings = new List<string>();
            
            // Check for ForgeGeneratedItemAsset instances of this type
            var generatedAssets = DiscoverGeneratedAssets(typeof(T).Name, searchPath);
            jsonStrings.AddRange(GeneratedAssetsToJsonStrings(generatedAssets));
            
            // If T is a ScriptableObject, also try to discover direct ScriptableObject assets
            // We use reflection here as a fallback, but it's acceptable since this is a convenience method
            if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
            {
                try
                {
                    var method = typeof(ForgeAssetDiscovery).GetMethod(nameof(DiscoverScriptableObjectsAsJson), 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var genericMethod = method.MakeGenericMethod(typeof(T));
                    var result = genericMethod.Invoke(null, new object[] { searchPath }) as List<string>;
                    
                    if (result != null)
                    {
                        jsonStrings.AddRange(result);
                    }
                }
                catch (Exception e)
                {
                    ForgeLogger.Warn($"Failed to discover ScriptableObject assets: {e.Message}");
                }
            }
            
            return jsonStrings;
        }
        
        /// <summary>
        /// Converts discovered assets to JSON strings for context.
        /// </summary>
        public static List<string> AssetsToJsonStrings<T>(IEnumerable<T> assets) where T : ScriptableObject
        {
            var jsonStrings = new List<string>();
            
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                
                try
                {
                    string json = JsonUtility.ToJson(asset);
                    if (!string.IsNullOrEmpty(json))
                    {
                        jsonStrings.Add(json);
                    }
                }
                catch (Exception e)
                {
                    ForgeLogger.Warn($"Failed to serialize asset to JSON: {e.Message}");
                }
            }
            
            return jsonStrings;
        }
        
        /// <summary>
        /// Converts ForgeGeneratedItemAsset instances to their JSON data.
        /// </summary>
        public static List<string> GeneratedAssetsToJsonStrings(IEnumerable<ForgeGeneratedItemAsset> assets)
        {
            var jsonStrings = new List<string>();
            
            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.JsonData)) continue;
                jsonStrings.Add(asset.JsonData);
            }
            
            return jsonStrings;
        }
        
        /// <summary>
        /// Normalizes a path to use the correct directory separator for the current platform.
        /// Ensures cross-platform compatibility.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            // Replace all backslashes and forward slashes with the platform-specific separator
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
        
        /// <summary>
        /// Gets the full absolute path for a given relative path from the Assets folder.
        /// Uses Unity's Application.dataPath for cross-platform compatibility.
        /// </summary>
        public static string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return Application.dataPath;
            
            // Normalize the relative path
            relativePath = NormalizePath(relativePath);
            
            // Combine with Application.dataPath which points to the Assets folder
            return Path.Combine(Application.dataPath, relativePath);
        }
    }
}
