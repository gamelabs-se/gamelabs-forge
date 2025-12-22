using System;
using UnityEngine;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// Example armor/equipment item for Forge demo.
    /// Use this as a template in the Forge Template Generator.
    /// </summary>
    [CreateAssetMenu(fileName = "New Armor", menuName = "GameLabs/Forge Demo/Armor")]
    public class Armor : ScriptableObject
{
    [Tooltip("Name of the armor")]
    public new string name;
    [Tooltip("Equipment slot")]
    public ArmorSlot slot;
    
    [Tooltip("Defense rating")]
    [Range(1, 200)]
    public int defense = 10;
    
    [Tooltip("Weight in kg")]
    [Range(0.1f, 100f)]
    public float weight = 5.0f;
    
    [Tooltip("Gold value")]
    [Range(1, 10000)]
    public int value = 100;
    
    [Tooltip("Durability")]
    [Range(1, 1000)]
    public int durability = 100;
    
    [Tooltip("Armor type/material")]
    public ArmorType armorType;
    
    [Tooltip("Rarity tier")]
    public ItemRarity rarity;
    
    [Tooltip("Bonus health from wearing")]
    [Range(0, 100)]
    public int bonusHealth = 0;
    
    [Tooltip("Movement speed modifier (1.0 = normal)")]
    [Range(0.5f, 1.5f)]
    public float speedModifier = 1.0f;
    }

    /// <summary>
    /// Armor equipment slots.
    /// </summary>
    public enum ArmorSlot
    {
        Head,
        Chest,
        Legs,
        Feet,
        Hands,
        Shield
    }

    /// <summary>
    /// Types of armor materials.
    /// </summary>
    public enum ArmorType
    {
        Cloth,
        Leather,
        Chainmail,
        Plate,
        Scale,
        Bone,
        Magical
    }
}
