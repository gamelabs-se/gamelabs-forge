using System;
using UnityEngine;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// Example melee weapon item for Forge demo.
    /// Use this as a template in the Forge Template Generator.
    /// </summary>
    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "GameLabs/Forge Demo/Melee Weapon")]
    public class MeleeWeapon : ScriptableObject
    {
        [Tooltip("Name of the weapon")]
        public string name;
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

        [Tooltip("Rarity tier of the weapon. These are not distributed evenly; higher rarities are less common.")]
        public ItemRarity rarity;
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
}
