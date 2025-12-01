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
    /// Generic ScriptableObject container for storing generated items of any type.
    /// Use this to save generated items as Unity assets that can be referenced in the Editor.
    /// </summary>
    /// <typeparam name="T">The type of item to store. Must be serializable.</typeparam>
    public class ForgeItemAsset<T> : ForgeItemAsset where T : class
    {
        /// <summary>The generated item data stored in this asset.</summary>
        [SerializeField]
        private T itemData;
        
        /// <summary>Gets the stored item data.</summary>
        public T Data => itemData;
        
        /// <inheritdoc/>
        public override string ItemTypeName => typeof(T).Name;
        
        /// <inheritdoc/>
        public override string ItemId
        {
            get
            {
                if (itemData is ForgeItemDefinition fid)
                    return fid.id;
                return name;
            }
        }
        
        /// <inheritdoc/>
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
        /// Creates a new ForgeItemAsset containing the specified item.
        /// Note: In Editor, use ForgeAssetExporter.CreateAsset for proper asset creation.
        /// </summary>
        public static ForgeItemAsset<T> CreateInstance(T item)
        {
            var asset = ScriptableObject.CreateInstance<ForgeItemAsset<T>>();
            asset.SetItem(item);
            return asset;
        }
        
        /// <summary>
        /// Sets the item data for this asset.
        /// </summary>
        internal void SetItem(T item)
        {
            itemData = item;
            OnCreated();
            
            // Set the asset name based on the item
            if (item is ForgeItemDefinition fid && !string.IsNullOrEmpty(fid.name))
            {
                name = SanitizeAssetName(fid.name);
            }
            else
            {
                name = $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }
        
        /// <summary>
        /// Sanitizes a string to be used as an asset name.
        /// </summary>
        private static string SanitizeAssetName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unnamed";
                
            // Replace invalid characters with underscores
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-' && chars[i] != ' ')
                {
                    chars[i] = '_';
                }
            }
            
            var result = new string(chars).Trim();
            
            // Ensure it doesn't start with a number
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            
            return string.IsNullOrEmpty(result) ? "Unnamed" : result;
        }
    }
}
