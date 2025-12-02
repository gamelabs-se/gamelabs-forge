using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Factory for creating and populating typed ScriptableObject assets from generated item data.
    /// Handles the mapping between ForgeItemDefinition classes and their bound ScriptableObject types.
    /// </summary>
    public static class ForgeTypedAssetFactory
    {
        private static readonly Dictionary<Type, Type> _bindingCache = new Dictionary<Type, Type>();
        
        /// <summary>
        /// Gets the bound ScriptableObject type for a ForgeItemDefinition type.
        /// Returns null if no binding is defined.
        /// </summary>
        /// <typeparam name="T">The ForgeItemDefinition type.</typeparam>
        /// <returns>The bound ScriptableObject type, or null.</returns>
        public static Type GetBoundAssetType<T>() where T : class
        {
            return GetBoundAssetType(typeof(T));
        }
        
        /// <summary>
        /// Gets the bound ScriptableObject type for a ForgeItemDefinition type.
        /// Returns null if no binding is defined.
        /// </summary>
        /// <param name="definitionType">The ForgeItemDefinition type.</param>
        /// <returns>The bound ScriptableObject type, or null.</returns>
        public static Type GetBoundAssetType(Type definitionType)
        {
            if (definitionType == null) return null;
            
            // Check cache first
            if (_bindingCache.TryGetValue(definitionType, out var cachedType))
                return cachedType;
            
            // Look for ForgeAssetBinding attribute
            var bindingAttr = definitionType.GetCustomAttribute<ForgeAssetBindingAttribute>();
            if (bindingAttr != null)
            {
                _bindingCache[definitionType] = bindingAttr.AssetType;
                return bindingAttr.AssetType;
            }
            
            // Cache null result to avoid repeated lookups
            _bindingCache[definitionType] = null;
            return null;
        }
        
        /// <summary>
        /// Checks if a ForgeItemDefinition type has a bound ScriptableObject type.
        /// </summary>
        public static bool HasBinding<T>() where T : class
        {
            return GetBoundAssetType<T>() != null;
        }
        
        /// <summary>
        /// Checks if a ForgeItemDefinition type has a bound ScriptableObject type.
        /// </summary>
        public static bool HasBinding(Type definitionType)
        {
            return GetBoundAssetType(definitionType) != null;
        }
        
        /// <summary>
        /// Creates a typed ScriptableObject asset from generated item data.
        /// </summary>
        /// <typeparam name="TDefinition">The ForgeItemDefinition type.</typeparam>
        /// <param name="item">The generated item data.</param>
        /// <returns>A populated ScriptableObject, or null if no binding exists or creation fails.</returns>
        public static ScriptableObject CreateTypedAsset<TDefinition>(TDefinition item) where TDefinition : class
        {
            if (item == null)
            {
                ForgeLogger.Error("Cannot create typed asset from null item.");
                return null;
            }
            
            var assetType = GetBoundAssetType<TDefinition>();
            if (assetType == null)
            {
                ForgeLogger.Log($"No asset binding found for {typeof(TDefinition).Name}. Use ForgeGeneratedItemAsset instead.");
                return null;
            }
            
            return CreateAndPopulateAsset(assetType, item, typeof(TDefinition));
        }
        
        /// <summary>
        /// Creates a typed ScriptableObject asset from generated item data using the specified asset type.
        /// </summary>
        /// <param name="assetType">The ScriptableObject type to create.</param>
        /// <param name="item">The source item data.</param>
        /// <param name="definitionType">The ForgeItemDefinition type.</param>
        /// <returns>A populated ScriptableObject, or null if creation fails.</returns>
        public static ScriptableObject CreateAndPopulateAsset(Type assetType, object item, Type definitionType)
        {
            if (assetType == null || item == null)
            {
                ForgeLogger.Error("Asset type and item cannot be null.");
                return null;
            }
            
            try
            {
                // Create the ScriptableObject instance
                var asset = ScriptableObject.CreateInstance(assetType);
                if (asset == null)
                {
                    ForgeLogger.Error($"Failed to create instance of {assetType.Name}");
                    return null;
                }
                
                // Get source JSON for reference
                string sourceJson = null;
                try
                {
                    sourceJson = JsonUtility.ToJson(item, true);
                }
                catch { /* Ignore serialization errors */ }
                
                // Populate fields from the source item
                PopulateFields(asset, item);
                
                // Set asset name
                string itemName = GetItemName(item);
                if (!string.IsNullOrEmpty(itemName))
                {
                    asset.name = SanitizeAssetName(itemName);
                }
                else
                {
                    asset.name = $"{assetType.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                }
                
                // Call OnPopulated if the asset is a ForgeTypedAsset
                if (asset is ForgeTypedAsset typedAsset)
                {
                    typedAsset.OnPopulated(definitionType?.Name ?? "Unknown", sourceJson);
                    typedAsset.OnValidateGenerated();
                }
                
                ForgeLogger.Log($"Created typed asset: {asset.name} ({assetType.Name})");
                return asset;
            }
            catch (Exception e)
            {
                ForgeLogger.Error($"Failed to create typed asset: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Populates fields on the target object from the source object.
        /// Matches fields by name and compatible types.
        /// </summary>
        private static void PopulateFields(object target, object source)
        {
            if (target == null || source == null) return;
            
            var targetType = target.GetType();
            var sourceType = source.GetType();
            
            // Get all public instance fields from source
            var sourceFields = sourceType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var sourceField in sourceFields)
            {
                try
                {
                    // Find matching field in target
                    var targetField = targetType.GetField(sourceField.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (targetField != null && CanAssign(sourceField.FieldType, targetField.FieldType))
                    {
                        var value = sourceField.GetValue(source);
                        var convertedValue = ConvertValue(value, sourceField.FieldType, targetField.FieldType);
                        targetField.SetValue(target, convertedValue);
                        continue;
                    }
                    
                    // Try matching property
                    var targetProperty = targetType.GetProperty(sourceField.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (targetProperty != null && targetProperty.CanWrite && CanAssign(sourceField.FieldType, targetProperty.PropertyType))
                    {
                        var value = sourceField.GetValue(source);
                        var convertedValue = ConvertValue(value, sourceField.FieldType, targetProperty.PropertyType);
                        targetProperty.SetValue(target, convertedValue);
                    }
                }
                catch (Exception e)
                {
                    ForgeLogger.Warn($"Failed to populate field {sourceField.Name}: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// Checks if a value of sourceType can be assigned to targetType.
        /// </summary>
        private static bool CanAssign(Type sourceType, Type targetType)
        {
            if (targetType.IsAssignableFrom(sourceType))
                return true;
            
            // Handle numeric conversions
            if (IsNumericType(sourceType) && IsNumericType(targetType))
                return true;
            
            // Handle enum to string/int
            if (sourceType.IsEnum && (targetType == typeof(string) || targetType == typeof(int)))
                return true;
            
            if (targetType.IsEnum && (sourceType == typeof(string) || sourceType == typeof(int)))
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Converts a value from sourceType to targetType.
        /// </summary>
        private static object ConvertValue(object value, Type sourceType, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            
            if (targetType.IsAssignableFrom(sourceType))
                return value;
            
            // Handle numeric conversions
            if (IsNumericType(sourceType) && IsNumericType(targetType))
            {
                return Convert.ChangeType(value, targetType);
            }
            
            // Handle enum to string
            if (sourceType.IsEnum && targetType == typeof(string))
            {
                return value.ToString();
            }
            
            // Handle string to enum
            if (targetType.IsEnum && sourceType == typeof(string))
            {
                try
                {
                    return Enum.Parse(targetType, (string)value, true);
                }
                catch
                {
                    return Enum.GetValues(targetType).GetValue(0);
                }
            }
            
            // Handle enum to int
            if (sourceType.IsEnum && targetType == typeof(int))
            {
                return Convert.ToInt32(value);
            }
            
            // Handle int to enum
            if (targetType.IsEnum && sourceType == typeof(int))
            {
                return Enum.ToObject(targetType, value);
            }
            
            return value;
        }
        
        /// <summary>
        /// Checks if a type is a numeric type.
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                   type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(decimal) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(ushort) || type == typeof(sbyte);
        }
        
        /// <summary>
        /// Gets the name of an item using reflection.
        /// </summary>
        private static string GetItemName(object item)
        {
            if (item == null) return null;
            
            // Check for ForgeItemDefinition
            if (item is ForgeItemDefinition fid && !string.IsNullOrEmpty(fid.name))
            {
                return fid.name;
            }
            
            // Try reflection
            var nameField = item.GetType().GetField("name", BindingFlags.Public | BindingFlags.Instance);
            if (nameField != null)
            {
                return nameField.GetValue(item) as string;
            }
            
            var nameProperty = item.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance) ??
                               item.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            if (nameProperty != null)
            {
                return nameProperty.GetValue(item) as string;
            }
            
            return null;
        }
        
        /// <summary>
        /// Sanitizes a string to be a valid Unity asset name.
        /// </summary>
        private static string SanitizeAssetName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unnamed";
            
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-' && chars[i] != ' ')
                {
                    chars[i] = '_';
                }
            }
            
            var result = new string(chars).Trim();
            
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            
            return string.IsNullOrEmpty(result) ? "Unnamed" : result;
        }
        
        /// <summary>
        /// Clears the binding cache. Useful when types are recompiled.
        /// </summary>
        public static void ClearCache()
        {
            _bindingCache.Clear();
        }
        
        /// <summary>
        /// Gets all registered type bindings.
        /// </summary>
        public static Dictionary<Type, Type> GetAllBindings()
        {
            var bindings = new Dictionary<Type, Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsClass && !type.IsAbstract)
                        {
                            var attr = type.GetCustomAttribute<ForgeAssetBindingAttribute>();
                            if (attr != null)
                            {
                                bindings[type] = attr.AssetType;
                            }
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be inspected
                }
            }
            
            return bindings;
        }
    }
}
