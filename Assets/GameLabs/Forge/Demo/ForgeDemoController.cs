using System.Collections.Generic;
using UnityEngine;
using GameLabs.Forge;
#if UNITY_EDITOR
using GameLabs.Forge.Editor;
#endif

/// <summary>
/// Demo controller showcasing ForgeItemGenerator usage.
/// Demonstrates single item generation, batch generation, saving as ScriptableObjects, and using existing items as context.
/// </summary>
public class ForgeDemoController : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Number of items to generate in batch mode")]
    [Range(1, 20)]
    public int batchSize = 5;
    
    [Tooltip("Additional context for generation")]
    [TextArea(2, 4)]
    public string additionalContext = "Generate items suitable for a level 10-20 character";
    
    [Header("Save Options")]
    [Tooltip("Automatically save generated items as ScriptableObject assets")]
    public bool autoSaveAsAssets = true;
    
    [Tooltip("Custom folder name for saved assets (leave empty for type name)")]
    public string customAssetFolder = "";
    
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
                
                // Auto-save as ScriptableObject asset
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAsset(result.items[0], folder);
                }
#endif
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
                
                // Auto-save as ScriptableObject assets
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAssets(result.items, folder);
                }
#endif
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
                
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAsset(result.items[0], folder);
                }
#endif
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
                
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAssets(result.items, folder);
                }
#endif
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
                
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAsset(result.items[0], folder);
                }
#endif
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
                
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAssets(result.items, folder);
                }
#endif
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
                
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAsset(result.items[0], folder);
                }
#endif
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
                
#if UNITY_EDITOR
                if (autoSaveAsAssets)
                {
                    string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
                    ForgeAssetExporter.CreateAssets(result.items, folder);
                }
#endif
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
    
#if UNITY_EDITOR
    [ContextMenu("Forge/Save Weapons as Assets")]
    public void SaveWeaponsAsAssets()
    {
        if (generatedWeapons.Count == 0)
        {
            ForgeLogger.Warn("No weapons to save as assets");
            return;
        }
        string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
        ForgeAssetExporter.CreateAssets(generatedWeapons, folder);
    }
    
    [ContextMenu("Forge/Save Consumables as Assets")]
    public void SaveConsumablesAsAssets()
    {
        if (generatedConsumables.Count == 0)
        {
            ForgeLogger.Warn("No consumables to save as assets");
            return;
        }
        string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
        ForgeAssetExporter.CreateAssets(generatedConsumables, folder);
    }
    
    [ContextMenu("Forge/Save Collectibles as Assets")]
    public void SaveCollectiblesAsAssets()
    {
        if (generatedCollectibles.Count == 0)
        {
            ForgeLogger.Warn("No collectibles to save as assets");
            return;
        }
        string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
        ForgeAssetExporter.CreateAssets(generatedCollectibles, folder);
    }
    
    [ContextMenu("Forge/Save Armor as Assets")]
    public void SaveArmorAsAssets()
    {
        if (generatedArmor.Count == 0)
        {
            ForgeLogger.Warn("No armor to save as assets");
            return;
        }
        string folder = string.IsNullOrEmpty(customAssetFolder) ? null : customAssetFolder;
        ForgeAssetExporter.CreateAssets(generatedArmor, folder);
    }
    
    [ContextMenu("Forge/Save All Items as Assets")]
    public void SaveAllItemsAsAssets()
    {
        SaveWeaponsAsAssets();
        SaveConsumablesAsAssets();
        SaveCollectiblesAsAssets();
        SaveArmorAsAssets();
    }
    
    [ContextMenu("Forge/Open Generator Window")]
    public void OpenGeneratorWindow()
    {
        ForgeGeneratorWindow.OpenWindow();
    }
#endif
}
