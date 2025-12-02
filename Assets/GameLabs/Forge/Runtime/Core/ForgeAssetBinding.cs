using System;
using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Attribute that binds a ForgeItemDefinition class to a ScriptableObject asset type.
    /// When generating items, Forge will automatically create instances of the bound
    /// ScriptableObject type and populate all matching fields.
    /// 
    /// <example>
    /// <code>
    /// // First, define your item definition
    /// [Serializable]
    /// [ForgeDescription("A melee weapon for close combat")]
    /// [ForgeAssetBinding(typeof(MeleeWeaponAsset))]
    /// public class MeleeWeapon : ForgeItemDefinition
    /// {
    ///     public int damage;
    ///     public float weight;
    /// }
    /// 
    /// // Then, create a matching ScriptableObject
    /// [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Game/Items/Melee Weapon")]
    /// public class MeleeWeaponAsset : ForgeTypedAsset
    /// {
    ///     public int damage;
    ///     public float weight;
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ForgeAssetBindingAttribute : Attribute
    {
        /// <summary>The ScriptableObject type to create when generating this item type.</summary>
        public Type AssetType { get; }
        
        /// <summary>
        /// Creates a binding between a ForgeItemDefinition and a ScriptableObject type.
        /// </summary>
        /// <param name="assetType">The ScriptableObject type to instantiate. Must inherit from ScriptableObject.</param>
        public ForgeAssetBindingAttribute(Type assetType)
        {
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));
                
            if (!typeof(ScriptableObject).IsAssignableFrom(assetType))
                throw new ArgumentException($"Asset type {assetType.Name} must inherit from ScriptableObject", nameof(assetType));
                
            AssetType = assetType;
        }
    }
}
