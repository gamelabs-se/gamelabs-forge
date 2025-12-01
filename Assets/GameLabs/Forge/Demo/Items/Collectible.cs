using System;
using UnityEngine;
using GameLabs.Forge;

/// <summary>
/// Example collectible/treasure item for Forge demo.
/// </summary>
[Serializable]
[ForgeDescription("A collectible treasure or artifact with lore and value")]
public class Collectible : ForgeItemDefinition
{
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
