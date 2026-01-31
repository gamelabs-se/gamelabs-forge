using System;
using UnityEngine;
using GameLabs.Forge.Editor;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// Example melee weapon item for Forge demo.
    /// Use this as a template in the Forge Template Generator.
    /// </summary>
    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "FORGE Samples/Weapon")]
    public class SampleWeapon : ScriptableObject, IForgeValidatable
    {
        [Tooltip("Name of the weapon")]
        public new string name;
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

        /// <summary>
        /// Validates the weapon data. Returns null if valid, error message if invalid.
        /// FORGE will automatically retry generation with this feedback if validation fails.
        /// </summary>
        public string ValidateForgeItem()
        {
            // Name must be present and reasonable length
            if (string.IsNullOrWhiteSpace(name))
                return "Weapon name cannot be empty";
            
            if (name.Length < 3)
                return "Weapon name must be at least 3 characters";
            
            if (name.Length > 50)
                return "Weapon name is too long (max 50 characters)";

            // Damage-to-value ratio check (heavier weapons should be more valuable)
            float damagePerGold = (float)damage / value;
            if (damagePerGold > 1f)
                return $"Weapon is underpriced: {damage} damage for {value} gold is too cheap. Increase value or decrease damage.";

            // Weight-to-damage ratio check (realistic physics)
            if (weight > 10f && damage < 30)
                return $"Heavy weapon ({weight}kg) should deal more damage. Either reduce weight or increase damage above 30.";

            // Attack speed should match weapon type
            if (weaponType == MeleeWeaponType.Dagger && attackSpeed < 2f)
                return "Daggers should have fast attack speed (2.0+). Increase attackSpeed or change weaponType.";

            if ((weaponType == MeleeWeaponType.Hammer || weaponType == MeleeWeaponType.Axe) && attackSpeed > 2f)
                return $"{weaponType} should have slower attack speed (under 2.0). Decrease attackSpeed.";

            // Rarity should match power level
            int powerScore = damage + durability / 10 + value / 50;
            
            if (rarity == ItemRarity.Common && powerScore > 150)
                return $"Stats too powerful for Common rarity (power score: {powerScore}). Reduce stats or increase rarity.";
            
            if (rarity == ItemRarity.Legendary && powerScore < 200)
                return $"Stats too weak for Legendary rarity (power score: {powerScore}). Increase stats or reduce rarity.";

            return null; // All validation passed
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
}
