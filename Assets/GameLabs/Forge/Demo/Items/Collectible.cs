using System;
using UnityEngine;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// Example collectible/treasure item for Forge demo.
    /// Use this as a template in the Forge Template Generator.
    /// </summary>
    [CreateAssetMenu(fileName = "New Collectible", menuName = "GameLabs/Forge Demo/Collectible")]
    public class Collectible : ScriptableObject
{
    [Tooltip("Name of the collectible")]
    public new string name;
    [Tooltip("Category of collectible")]
    public CollectibleCategory category;
    
    [Tooltip("Gold value")]
    [Range(1, 50000)]
    public int value = 100;
    
    [Tooltip("Lore/backstory of the item")]
    [TextArea(2, 4)]
    public string lore = "";
    
    [Tooltip("Rarity tier")]
    public ItemRarity rarity;
    
    [Tooltip("Is this item unique (only one can exist)?")]
    public bool isUnique = false;
    
    [Tooltip("Set name if part of a collection")]
    public string setName = "";
    }

    /// <summary>
    /// Categories of collectibles.
    /// </summary>
    public enum CollectibleCategory
    {
        Gem,
        Artifact,
        Relic,
        Scroll,
        Coin,
        Trophy,
        Art,
        Book
    }
}
