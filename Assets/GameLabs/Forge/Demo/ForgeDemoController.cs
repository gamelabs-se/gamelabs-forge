using System.Collections.Generic;
using UnityEngine;
using GameLabs.Forge;

/// <summary>
/// Demo controller showcasing ForgeItemGenerator usage.
/// Demonstrates single item generation, batch generation, and using existing items as context.
/// </summary>
[ExecuteAlways]
public class ForgeDemoController : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Number of items to generate in batch mode")]
    [Range(1, 20)]
    public int batchSize = 5;
    
    [Tooltip("Additional context for generation")]
    [TextArea(2, 4)]
    public string additionalContext = "Generate items suitable for a level 10-20 character";
    
    [Header("Generated Items")]
    [SerializeField] private List<MeleeWeapon> generatedWeapons = new List<MeleeWeapon>();
    [SerializeField] private List<Consumable> generatedConsumables = new List<Consumable>();
    [SerializeField] private List<Collectible> generatedCollectibles = new List<Collectible>();
    [SerializeField] private List<Armor> generatedArmor = new List<Armor>();
    
    [Header("Existing Items (for context)")]
    [Tooltip("Add existing weapons to provide context for generation")]
    [SerializeField] private List<MeleeWeapon> existingWeapons = new List<MeleeWeapon>();
    
    private ForgeItemGenerator Generator => ForgeItemGenerator.Instance;
    
    [ContextMenu("Forge/Generate Single Weapon")]
    public void GenerateSingleWeapon()
    {
        // Add existing items as context
        if (existingWeapons.Count > 0)
        {
            Generator.AddExistingItems(existingWeapons);
        }
        
        Generator.GenerateSingle<MeleeWeapon>(result =>
        {
            if (result.success && result.items.Count > 0)
            {
                generatedWeapons.Add(result.items[0]);
                ForgeLogger.Log($"Generated weapon: {result.items[0].name} (Cost: ${result.estimatedCost:F6})");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate weapon: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Weapon Batch")]
    public void GenerateWeaponBatch()
    {
        if (existingWeapons.Count > 0)
        {
            Generator.AddExistingItems(existingWeapons);
        }
        
        Generator.GenerateBatch<MeleeWeapon>(batchSize, result =>
        {
            if (result.success)
            {
                generatedWeapons.AddRange(result.items);
                ForgeLogger.Log($"Generated {result.items.Count} weapons (Cost: ${result.estimatedCost:F6})");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate weapons: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Single Consumable")]
    public void GenerateSingleConsumable()
    {
        Generator.GenerateSingle<Consumable>(result =>
        {
            if (result.success && result.items.Count > 0)
            {
                generatedConsumables.Add(result.items[0]);
                ForgeLogger.Log($"Generated consumable: {result.items[0].name}");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate consumable: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Consumable Batch")]
    public void GenerateConsumableBatch()
    {
        Generator.GenerateBatch<Consumable>(batchSize, result =>
        {
            if (result.success)
            {
                generatedConsumables.AddRange(result.items);
                ForgeLogger.Log($"Generated {result.items.Count} consumables");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate consumables: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Single Collectible")]
    public void GenerateSingleCollectible()
    {
        Generator.GenerateSingle<Collectible>(result =>
        {
            if (result.success && result.items.Count > 0)
            {
                generatedCollectibles.Add(result.items[0]);
                ForgeLogger.Log($"Generated collectible: {result.items[0].name}");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate collectible: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Collectible Batch")]
    public void GenerateCollectibleBatch()
    {
        Generator.GenerateBatch<Collectible>(batchSize, result =>
        {
            if (result.success)
            {
                generatedCollectibles.AddRange(result.items);
                ForgeLogger.Log($"Generated {result.items.Count} collectibles");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate collectibles: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Single Armor")]
    public void GenerateSingleArmor()
    {
        Generator.GenerateSingle<Armor>(result =>
        {
            if (result.success && result.items.Count > 0)
            {
                generatedArmor.Add(result.items[0]);
                ForgeLogger.Log($"Generated armor: {result.items[0].name}");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate armor: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Generate Armor Batch")]
    public void GenerateArmorBatch()
    {
        Generator.GenerateBatch<Armor>(batchSize, result =>
        {
            if (result.success)
            {
                generatedArmor.AddRange(result.items);
                ForgeLogger.Log($"Generated {result.items.Count} armor pieces");
            }
            else
            {
                ForgeLogger.Error($"Failed to generate armor: {result.errorMessage}");
            }
        }, additionalContext);
    }
    
    [ContextMenu("Forge/Clear All Generated Items")]
    public void ClearAllGenerated()
    {
        generatedWeapons.Clear();
        generatedConsumables.Clear();
        generatedCollectibles.Clear();
        generatedArmor.Clear();
        Generator.ClearExistingItems();
        ForgeLogger.Log("Cleared all generated items");
    }
    
    [ContextMenu("Forge/Add Generated Weapons to Existing")]
    public void AddGeneratedToExisting()
    {
        existingWeapons.AddRange(generatedWeapons);
        ForgeLogger.Log($"Added {generatedWeapons.Count} generated weapons to existing items context");
    }
    
    [ContextMenu("Forge/Export All Weapons")]
    public void ExportAllWeapons()
    {
        if (generatedWeapons.Count == 0)
        {
            ForgeLogger.Warn("No weapons to export");
            return;
        }
        ForgeItemExporter.ExportItems(generatedWeapons, "weapons.json");
    }
    
    [ContextMenu("Forge/Export All Consumables")]
    public void ExportAllConsumables()
    {
        if (generatedConsumables.Count == 0)
        {
            ForgeLogger.Warn("No consumables to export");
            return;
        }
        ForgeItemExporter.ExportItems(generatedConsumables, "consumables.json");
    }
    
    [ContextMenu("Forge/Export All Collectibles")]
    public void ExportAllCollectibles()
    {
        if (generatedCollectibles.Count == 0)
        {
            ForgeLogger.Warn("No collectibles to export");
            return;
        }
        ForgeItemExporter.ExportItems(generatedCollectibles, "collectibles.json");
    }
    
    [ContextMenu("Forge/Export All Armor")]
    public void ExportAllArmor()
    {
        if (generatedArmor.Count == 0)
        {
            ForgeLogger.Warn("No armor to export");
            return;
        }
        ForgeItemExporter.ExportItems(generatedArmor, "armor.json");
    }
    
    [ContextMenu("Forge/Export All Items")]
    public void ExportAllItems()
    {
        ExportAllWeapons();
        ExportAllConsumables();
        ExportAllCollectibles();
        ExportAllArmor();
    }
}
