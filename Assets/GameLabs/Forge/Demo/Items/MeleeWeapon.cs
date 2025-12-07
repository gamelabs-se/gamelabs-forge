using System;
using UnityEngine;
using GameLabs.Forge;

/// <summary>
/// Example melee weapon item for Forge demo.
/// When generated, this creates a MeleeWeaponAsset ScriptableObject
/// that can be used directly in your game.
/// </summary>
[Serializable]
[ForgeDescription("A melee weapon used in close combat")]
[ForgeAssetBinding(typeof(MeleeWeaponAsset))]
public class MeleeWeapon : ForgeItemDefinition
{
    [Tooltip("Base damage dealt by the weapon")]
    [Range(1, 100)]
    public int damage = 10;

    [Tooltip("Weight of the weapon in kg")]
    [Range(0.1f, 50f)]
    public float weight = 1.0f;

    [Tooltip("Gold value of the weapon")]
    [Range(1, 10000)]
    public int value = 50;

    [Tooltip("Attack speed (attacks per second)")]
    [Range(0.5f, 5f)]
    public float attackSpeed = 1.0f;

    [Tooltip("Durability of the weapon")]
    [Range(1, 500)]
    public int durability = 100;

    [Tooltip("Type/category of melee weapon")]
    public MeleeWeaponType weaponType;

    [Tooltip("Rarity tier of the weapon")]
    public ItemRarity rarity;

    public override bool Validate()
    {
        return base.Validate() && damage > 0 && weight > 0;
    }
}

/// <summary>
/// Types of melee weapons available.
/// </summary>
public enum MeleeWeaponType
{
    Sword,
    Axe,
    Mace,
    Dagger,
    Spear,
    Hammer,
    Staff,
    Flail
}

/// <summary>
/// Rarity tiers for items.
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
