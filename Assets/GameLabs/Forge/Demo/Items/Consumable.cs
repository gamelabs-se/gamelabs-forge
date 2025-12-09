using System;
using UnityEngine;
using GameLabs.Forge;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// Example consumable item for Forge demo.
    /// Use this as a template in the Forge Template Generator.
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "GameLabs/Forge Demo/Consumable")]
    public class Consumable : ScriptableObject
{
    [Tooltip("Name of the consumable")]
    public string name;
    [Tooltip("Effect type when consumed")]
    public ConsumableEffect effectType;
    
    [Tooltip("Power/magnitude of the effect")]
    [Range(1, 100)]
    public int effectPower = 10;
    
    [Tooltip("Duration of effect in seconds (0 for instant)")]
    [Range(0, 300)]
    public float duration = 0;
    
    [Tooltip("Gold value")]
    [Range(1, 1000)]
    public int value = 25;
    
    [Tooltip("Maximum stack size")]
    [Range(1, 99)]
    public int maxStack = 20;
    
    [Tooltip("Rarity tier")]
    public ItemRarity rarity;
    }

    /// <summary>
    /// Types of consumable effects.
    /// </summary>
    public enum ConsumableEffect
    {
        Heal,
        RestoreMana,
        BuffStrength,
        BuffSpeed,
        BuffDefense,
        Poison,
        Cure,
        Resurrect
    }
}
