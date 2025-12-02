using System;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Base ScriptableObject class for storing generated items as assets.
    /// This allows generated items to be saved persistently and referenced in the Unity Editor.
    /// </summary>
    public abstract class ForgeItemAsset : ScriptableObject
    {
        /// <summary>The type name of the item stored in this asset.</summary>
        public abstract string ItemTypeName { get; }
        
        /// <summary>The unique ID of the stored item.</summary>
        public abstract string ItemId { get; }
        
        /// <summary>The display name of the stored item.</summary>
        public abstract string ItemName { get; }
        
        /// <summary>Timestamp when this asset was created.</summary>
        [SerializeField, HideInInspector]
        protected string createdAt;
        
        /// <summary>Gets the creation timestamp of this asset.</summary>
        public DateTime CreatedAt => DateTime.TryParse(createdAt, out var dt) ? dt : DateTime.MinValue;
        
        /// <summary>
        /// Called when the asset is created. Override for custom initialization.
        /// </summary>
        protected virtual void OnCreated()
        {
            createdAt = DateTime.Now.ToString("o");
        }
    }
    
    /// <summary>
    /// Concrete ScriptableObject for storing generated items as JSON data.
    /// This avoids Unity's limitation with generic ScriptableObjects.
    /// </summary>
    public class ForgeGeneratedItemAsset : ForgeItemAsset
    {
        [SerializeField] private string _itemTypeName = "";
        [SerializeField] private string _itemName = "";
        [SerializeField, TextArea(3, 10)] private string _jsonData = "";
        
        public override string ItemTypeName => _itemTypeName;
        public override string ItemId => name;
        public override string ItemName => _itemName;
        
        /// <summary>Gets the raw JSON data of the stored item.</summary>
        public string JsonData => _jsonData;
        
        /// <summary>
        /// Creates a new ForgeGeneratedItemAsset from an item.
        /// </summary>
        public static ForgeGeneratedItemAsset CreateInstance<T>(T item) where T : class
        {
            var asset = ScriptableObject.CreateInstance<ForgeGeneratedItemAsset>();
            if (asset == null)
            {
                Debug.LogError("[Forge] Failed to create ForgeGeneratedItemAsset instance");
                return null;
            }
            
            asset.SetItem(item);
            return asset;
        }
        
        /// <summary>
        /// Sets the item data for this asset.
        /// </summary>
        public void SetItem<T>(T item) where T : class
        {
            OnCreated();
            _itemTypeName = typeof(T).Name;
            
            if (item == null)
            {
                _itemName = "Unnamed";
                _jsonData = "{}";
                name = $"{_itemTypeName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                return;
            }
            
            // Serialize to JSON
            try
            {
                _jsonData = JsonUtility.ToJson(item, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Forge] Failed to serialize item to JSON: {e.Message}");
                _jsonData = "{}";
            }
            
            // Try to get item name
            string itemName = null;
            try
            {
                if (item is ForgeItemDefinition fid && !string.IsNullOrEmpty(fid.name))
                {
                    itemName = fid.name;
                }
                else
                {
                    var nameField = typeof(T).GetField("name");
                    if (nameField != null)
                    {
                        itemName = nameField.GetValue(item) as string;
                    }
                    else
                    {
                        var nameProperty = typeof(T).GetProperty("name") ?? typeof(T).GetProperty("Name");
                        if (nameProperty != null)
                        {
                            itemName = nameProperty.GetValue(item) as string;
                        }
                    }
                }
            }
            catch
            {
                // Reflection failed
            }
            
            _itemName = itemName ?? "Unnamed";
            
            if (!string.IsNullOrEmpty(itemName))
            {
                name = SanitizeAssetName(itemName);
            }
            else
            {
                name = $"{_itemTypeName}_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }
        
        /// <summary>
        /// Deserializes the stored JSON data back to the specified type.
        /// </summary>
        public T GetData<T>() where T : class
        {
            if (string.IsNullOrEmpty(_jsonData))
                return null;
                
            try
            {
                return JsonUtility.FromJson<T>(_jsonData);
            }
            catch
            {
                return null;
            }
        }
        
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
    }
    
    /// <summary>
    /// Generic wrapper for type-safe access (kept for backwards compatibility but not used for asset creation).
    /// Note: Unity cannot instantiate generic ScriptableObjects, so ForgeGeneratedItemAsset should be used instead.
    /// </summary>
    /// <typeparam name="T">The type of item to store.</typeparam>
    [Obsolete("Use ForgeGeneratedItemAsset instead. Unity cannot instantiate generic ScriptableObjects.")]
    public class ForgeItemAsset<T> : ForgeItemAsset where T : class
    {
        [SerializeField]
        private T itemData;
        
        public T Data => itemData;
        
        public override string ItemTypeName => typeof(T).Name;
        
        public override string ItemId
        {
            get
            {
                if (itemData is ForgeItemDefinition fid)
                    return fid.id;
                return name;
            }
        }
        
        public override string ItemName
        {
            get
            {
                if (itemData is ForgeItemDefinition fid)
                    return fid.name;
                return name;
            }
        }
        
        /// <summary>
        /// Creates a new ForgeItemAsset. 
        /// WARNING: This may return null due to Unity's generic ScriptableObject limitations.
        /// Use ForgeGeneratedItemAsset.CreateInstance instead.
        /// </summary>
        public static ForgeItemAsset<T> CreateInstance(T item)
        {
            // Unity cannot instantiate generic ScriptableObjects reliably
            // This will likely return null
            Debug.LogWarning("[Forge] ForgeItemAsset<T>.CreateInstance is deprecated. Use ForgeGeneratedItemAsset.CreateInstance instead.");
            
            try
            {
                var asset = ScriptableObject.CreateInstance<ForgeItemAsset<T>>();
                if (asset != null)
                {
                    asset.SetItem(item);
                    return asset;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Forge] Failed to create ForgeItemAsset<{typeof(T).Name}>: {e.Message}");
            }
            
            return null;
        }
        
        internal void SetItem(T item)
        {
            itemData = item;
            OnCreated();
            
            if (item == null)
            {
                name = $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                return;
            }
            
            string itemName = null;
            try
            {
                if (item is ForgeItemDefinition fid && !string.IsNullOrEmpty(fid.name))
                {
                    itemName = fid.name;
                }
                else
                {
                    var nameField = typeof(T).GetField("name");
                    if (nameField != null)
                    {
                        itemName = nameField.GetValue(item) as string;
                    }
                }
            }
            catch { }
            
            name = !string.IsNullOrEmpty(itemName) ? SanitizeAssetName(itemName) : $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
        
        private static string SanitizeAssetName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unnamed";
                
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-' && chars[i] != ' ')
                    chars[i] = '_';
            }
            
            var result = new string(chars).Trim();
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;
            
            return string.IsNullOrEmpty(result) ? "Unnamed" : result;
        }
    }
}
