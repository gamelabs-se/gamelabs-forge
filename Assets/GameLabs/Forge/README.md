# 🔥 Forge - AI-Powered Item Generator for Unity

**Generate game items with AI. Fast, flexible, and production-ready.**

Forge uses OpenAI's GPT models to generate game items based on your custom ScriptableObject definitions. Perfect for rapid prototyping or populating your game world with unique items.

## Installation

### Unity Package

1. Download `FORGE-v1.0.0-beta.unitypackage`
2. In Unity: `Assets → Import Package → Custom Package`
3. Select the downloaded file
4. Click "Import" (imports to `Assets/GameLabs/Forge/`)

### First Run

After importing:
1. Open `GameLabs → Forge → Setup Wizard`
2. Enter your OpenAI API key ([get one here](https://platform.openai.com/api-keys))
3. Configure game settings
4. Done!

📖 **See [QUICKSTART.md](QUICKSTART.md) for a 3-minute guide.**

---

## Features

- **Item-Agnostic**: Define any item type as a C# class - Forge extracts the schema automatically
- **Dynamic Schema Extraction**: Uses reflection to understand your item structure including ranges, enums, and descriptions
- **Single & Batch Generation**: Generate one item or many at once
- **Typed Assets** ⭐ NEW: Create real ScriptableObject assets with actual properties - usable immediately!
- **ScriptableObject Assets**: Save generated items as ScriptableObject assets organized by type
- **Context-Aware**: Provide existing items as reference to ensure variety
- **JSON Export/Import**: Save generated items for use in your game
- **Generator Window**: Easy-to-use editor window for generating and saving items
- **Setup Wizard**: Easy first-time configuration with cost estimation
- **Custom Attributes**: Fine-tune generation with `[ForgeDescription]`, `[ForgeConstraint]`, and `[ForgeAssetBinding]`

## Quick Start

### 1. Run the Setup Wizard

Open **GameLabs → Forge → Setup Wizard** from the Unity menu.

1. **API Configuration**: Enter your OpenAI API key (get one at https://platform.openai.com/api-keys)
2. **Game Context**: Describe your game's setting, theme, and target audience
3. **Generation Settings**: Configure batch size and AI creativity (temperature)
4. Click **Finish** to save your configuration

### 2. Define Your Item Type

Create a C# class for your item. You can optionally inherit from `ForgeItemDefinition`:

```csharp
using System;
using UnityEngine;
using GameLabs.Forge;

[Serializable]
[ForgeDescription("A melee weapon used in close combat")]
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
    
    [Tooltip("Type/category of melee weapon")]
    public WeaponType weaponType;
    
    [Tooltip("Rarity tier")]
    public ItemRarity rarity;
}

public enum WeaponType { Sword, Axe, Mace, Dagger, Spear }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
```

**No base class required!** You can also use plain C# classes:

```csharp
[Serializable]
public class Potion
{
    public string name;
    public string description;
    [Range(1, 100)]
    public int healAmount;
    public PotionType type;
}
```

### 3. Generate Items

#### Option A: Using the Demo Controller

1. Add an empty GameObject to your scene
2. Add the `ForgeDemoController` component
3. Use the Inspector buttons to generate items

#### Option B: Via Code

```csharp
using GameLabs.Forge;

public class MyItemManager : MonoBehaviour
{
    void GenerateWeapons()
    {
        var generator = ForgeItemGenerator.Instance;
        
        // Generate a single item
        generator.GenerateSingle<MeleeWeapon>(result =>
        {
            if (result.success)
            {
                var weapon = result.items[0];
                Debug.Log($"Generated: {weapon.name} - {weapon.damage} damage");
            }
        });
        
        // Generate a batch of items
        generator.GenerateBatch<MeleeWeapon>(5, result =>
        {
            if (result.success)
            {
                Debug.Log($"Generated {result.items.Count} weapons");
                foreach (var weapon in result.items)
                {
                    Debug.Log($"  - {weapon.name}");
                }
            }
        });
    }
}
```

### 4. Use Context for Better Results

Provide existing items as context to help the AI understand your game's style and avoid duplicates:

```csharp
var generator = ForgeItemGenerator.Instance;

// Add existing items as context
generator.AddExistingItems(myExistingWeapons);

// Generate with additional context
generator.GenerateSingle<MeleeWeapon>(result => 
{
    // Handle result
}, "Generate a legendary fire-enchanted sword for a level 50 player");
```

### 5. Export Generated Items

Save your generated items to JSON for use in your game:

```csharp
using GameLabs.Forge;

// Export a list of items
ForgeItemExporter.ExportItems(myWeapons, "weapons.json");

// Import items later
var weapons = ForgeItemExporter.ImportItems<MeleeWeapon>("path/to/weapons.json");
```

### 6. Save as ScriptableObject Assets

The most powerful way to use generated items is to save them as ScriptableObject assets. These can be referenced directly in your game:

#### Using the Generator Window (Recommended)

1. Open **GameLabs → Forge → Generate Items**
2. Select your item type from the dropdown
3. Configure the count and optional context
4. Enable "Auto-Save as Asset" (enabled by default)
5. Enable "Create Typed Assets" for real ScriptableObjects (if available)
6. Click **🔥 Generate Items**

Items are automatically saved in `Assets/Resources/Generated/{TypeName}/` (configurable)

### 7. ⭐ Typed Assets - Ready-to-Use ScriptableObjects

**NEW!** Typed Assets are real ScriptableObjects with actual properties - not JSON storage. Generate items and use them immediately in your game!

#### Step 1: Create Your Asset Class

Create a ScriptableObject that matches your item definition:

```csharp
using UnityEngine;
using GameLabs.Forge;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game/Items/Weapon")]
public class MeleeWeaponAsset : ForgeTypedAsset
{
    [Header("Combat Stats")]
    public int damage;
    public float attackSpeed;
    
    [Header("Properties")]
    public float weight;
    public int value;
    public ItemRarity rarity;
    
    // Add custom methods!
    public float CalculateDPS() => damage * attackSpeed;
}
```

#### Step 2: Bind Your Definition to the Asset

Add `[ForgeAssetBinding]` to your item definition:

```csharp
[Serializable]
[ForgeDescription("A melee weapon")]
[ForgeAssetBinding(typeof(MeleeWeaponAsset))]  // ← This line!
public class MeleeWeapon : ForgeItemDefinition
{
    public int damage;
    public float attackSpeed;
    public float weight;
    public int value;
    public ItemRarity rarity;
}
```

#### Step 3: Generate and Use!

```csharp
// Generate items - they become real MeleeWeaponAsset ScriptableObjects!
generator.GenerateBatch<MeleeWeapon>(5, result =>
{
    if (result.success)
    {
        // Creates MeleeWeaponAsset files with real properties
        var assets = ForgeAssetExporter.CreateAssets(result.items);
    }
});
```

#### Using Typed Assets in Your Game

```csharp
public class WeaponManager : MonoBehaviour
{
    // Reference typed assets directly - no JSON parsing needed!
    [SerializeField] private MeleeWeaponAsset[] weapons;
    
    void Start()
    {
        foreach (var weapon in weapons)
        {
            // Access properties directly
            Debug.Log($"{weapon.name}: {weapon.damage} damage, DPS: {weapon.CalculateDPS()}");
        }
    }
}
```

#### Via Code (Legacy JSON Storage)

```csharp
#if UNITY_EDITOR
using GameLabs.Forge;
using GameLabs.Forge.Editor;

// Generate and save in one step
generator.GenerateSingle<MeleeWeapon>(result =>
{
    if (result.success)
    {
        // Save as ScriptableObject asset
        var asset = ForgeAssetExporter.CreateAsset(result.items[0]);
        Debug.Log($"Saved: {asset.name}");
    }
});

// Save multiple items at once
generator.GenerateBatch<MeleeWeapon>(5, result =>
{
    if (result.success)
    {
        // Saves to Generated/MeleeWeapon/ folder
        var assets = ForgeAssetExporter.CreateAssets(result.items);
        
        // Or use a custom folder name
        var customAssets = ForgeAssetExporter.CreateAssets(result.items, "MyWeapons");
    }
});
#endif
```

#### Using Saved Assets in Your Game

```csharp
public class WeaponManager : MonoBehaviour
{
    // Reference assets directly in the Inspector
    [SerializeField] private ForgeItemAsset<MeleeWeapon>[] weapons;
    
    void Start()
    {
        foreach (var weaponAsset in weapons)
        {
            MeleeWeapon weapon = weaponAsset.Data;
            Debug.Log($"Loaded: {weapon.name} - {weapon.damage} damage");
        }
    }
}
```

## Supported Attributes

### Schema Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[ForgeDescription("...")]` | Class, Field | Provides description for AI context |
| `[Range(min, max)]` | Field | Sets numeric range constraints |
| `[Min(value)]` | Field | Sets minimum value |
| `[Tooltip("...")]` | Field | Used as field description if no ForgeDescription |
| `[ForgeAssetBinding(type)]` | Class | Binds to a ScriptableObject type for typed assets |

### Custom Constraints

```csharp
// Constrain numeric range
[ForgeConstraint(MinValue = 10, MaxValue = 100)]
public int damage;

// Limit to specific string values
[ForgeConstraint(AllowedValues = new[] { "Fire", "Ice", "Lightning" })]
public string element;

// Make field optional
[ForgeConstraint(Required = false)]
public string loreText;
```

### Asset Binding

```csharp
// Bind your item definition to a ScriptableObject type
[ForgeAssetBinding(typeof(MeleeWeaponAsset))]
public class MeleeWeapon : ForgeItemDefinition
{
    // Fields will be automatically mapped to MeleeWeaponAsset
}
```

## Configuration

Settings are stored in `Assets/GameLabs/Forge/Settings/forge.config.json` (gitignored by default).

### Asset Path Configuration

Forge now supports configurable paths for asset discovery and generation:

- **Existing Assets Search Path**: Define where Forge looks for existing ScriptableObjects to use as context (default: `Resources`)
- **Generated Assets Base Path**: Define where generated assets are saved (default: `Resources/Generated`)
- **Auto-Load Existing Assets**: When enabled, Forge automatically discovers existing assets of the same type and uses them as context for generation

**Cross-Platform Support**: All paths use Unity's built-in path handling (`Path.Combine`, `Application.dataPath`) ensuring compatibility across Windows, macOS, Linux, iOS, Android, Xbox, and other platforms.

#### Example Configuration:

```json
{
  "openaiApiKey": "your-api-key",
  "gameName": "My Game",
  "gameDescription": "A fantasy RPG",
  "existingAssetsSearchPath": "Resources/Items",
  "generatedAssetsBasePath": "Resources/Generated/Items",
  "autoLoadExistingAssets": true
}
```

You can also configure these in the Unity Inspector when selecting a ForgeItemGenerator component.

### Generator Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `gameName` | Your game's name | "My Game" |
| `gameDescription` | Game setting/theme description | "" |
| `targetAudience` | Target audience (General, Casual, Hardcore, etc.) | "General" |
| `defaultBatchSize` | Default items per batch | 5 |
| `maxBatchSize` | Maximum items per request | 20 |
| `temperature` | AI creativity (0-2, higher = more creative) | 0.8 |
| `model` | OpenAI model to use | "gpt-4o-mini" |
| `existingAssetsSearchPath` | Where to search for existing assets | "Assets" |
| `generatedAssetsBasePath` | Where to save generated assets | "Resources/Generated" |
| `autoLoadExistingAssets` | Auto-load existing assets into context | true |

## API Reference

### ForgeItemGenerator

```csharp
// Singleton instance
ForgeItemGenerator.Instance

// Generate single item
void GenerateSingle<T>(Action<ForgeGenerationResult<T>> callback, string additionalContext = "")

// Generate batch
void GenerateBatch<T>(int count, Action<ForgeGenerationResult<T>> callback, string additionalContext = "")

// Context management
void AddExistingItems<T>(IEnumerable<T> items)
void ClearExistingItems()
```

### ForgeGenerationResult<T>

```csharp
bool success                 // Whether generation succeeded
string errorMessage          // Error message if failed
List<T> items               // Generated items
int promptTokens            // Tokens used for prompt
int completionTokens        // Tokens used for completion
float estimatedCost         // Estimated cost in USD
```

### ForgeItemExporter

```csharp
// Export items to JSON file
static string ExportItem<T>(T item, string filename = null, string path = null)
static string ExportItems<T>(IEnumerable<T> items, string filename = null, string path = null)

// Import items from JSON file
static T ImportItem<T>(string filepath)
static List<T> ImportItems<T>(string filepath)
```

### ForgeAssetExporter (Editor Only)

```csharp
// Create a single ScriptableObject asset (auto-detects typed vs JSON)
static ScriptableObject CreateAsset<T>(T item, string customFolder = null, bool preferTypedAsset = true)

// Create multiple ScriptableObject assets
static List<ScriptableObject> CreateAssets<T>(IEnumerable<T> items, string customFolder = null, bool preferTypedAssets = true)

// Force typed asset creation (requires [ForgeAssetBinding])
static ScriptableObject CreateTypedAsset<T>(T item, string customFolder = null)
static List<ScriptableObject> CreateTypedAssets<T>(IEnumerable<T> items, string customFolder = null)

// Force JSON storage asset creation
static ForgeGeneratedItemAsset CreateJsonAsset<T>(T item, string customFolder = null)
static List<ForgeGeneratedItemAsset> CreateJsonAssets<T>(IEnumerable<T> items, string customFolder = null)

// Load typed assets
static List<TAsset> LoadTypedAssets<TAsset>(string customFolder = null) where TAsset : ScriptableObject

// Load JSON assets
static List<ForgeGeneratedItemAsset> LoadJsonAssets(string customFolder)
static List<ForgeGeneratedItemAsset> LoadAssets<T>(string customFolder = null)

// Check if type has asset binding
static bool HasTypedAssetBinding<T>()
static Type GetBoundAssetType<T>()

// Asset management
static int GetAssetCount<T>(string customFolder = null)
static int ClearTypeAssets<T>(string customFolder = null)
static void RevealTypeFolder<T>(string customFolder = null)
```

### ForgeTypedAsset

Base class for typed ScriptableObject assets:

```csharp
public abstract class ForgeTypedAsset : ScriptableObject
{
    // Base fields (automatically populated)
    public string id;
    public new string name;
    public string description;
    
    // Override for custom post-processing
    public virtual void OnValidateGenerated() { }
    
    // Metadata
    public string SourceTypeName { get; }
    public DateTime GeneratedAt { get; }
    public string SourceJson { get; }
}
```

### ForgeTypedAssetFactory

```csharp
// Check for bindings
static bool HasBinding<T>()
static Type GetBoundAssetType<T>()

// Create typed assets programmatically
static ScriptableObject CreateTypedAsset<T>(T item)
static ScriptableObject CreateAndPopulateAsset(Type assetType, object item, Type definitionType)
```

### ForgeItemAsset<T> (Legacy JSON Storage)

```csharp
// The wrapped item data
T Data { get; }

// Item metadata
string ItemTypeName { get; }
string ItemId { get; }
string ItemName { get; }
DateTime CreatedAt { get; }
```

## Demo Items Included

The package includes example item types in `Assets/GameLabs/Forge/Demo/Items/`:

### Item Definitions
- **MeleeWeapon** - Swords, axes, maces with damage, weight, durability
- **Consumable** - Potions, food with effects and duration
- **Collectible** - Treasures with lore and rarity
- **Armor** - Equipment with defense, weight, slots

### Typed Asset Classes (NEW!)
- **MeleeWeaponAsset** - Real ScriptableObject with combat stats and helper methods
- **ConsumableAsset** - Ready-to-use consumable items
- **CollectibleAsset** - Treasures with value calculations
- **ArmorAsset** - Equipment with defense calculations

## Cost Estimation

Using GPT-4o-mini (recommended for cost efficiency):
- Single item: ~$0.00002
- Batch of 5 items: ~$0.00008
- Batch of 20 items: ~$0.00025

## Troubleshooting

### "API key missing" error
Run the Setup Wizard (**GameLabs → Forge → Setup Wizard**) to configure your API key.

### Items not matching schema
Ensure your class is marked `[Serializable]` and fields are public. The AI respects `[Range]` and enum constraints.

### Empty or null items
Check the Unity Console for error messages. The AI response may have failed to parse - try reducing batch size or simplifying your item schema.

## File Structure

```
Assets/GameLabs/Forge/
├── Demo/                    # Example items and demo controller
│   ├── Items/              # Sample item definitions + typed assets
│   │   ├── MeleeWeapon.cs      # Item definition
│   │   ├── MeleeWeaponAsset.cs # Typed ScriptableObject asset
│   │   ├── Consumable.cs
│   │   ├── ConsumableAsset.cs
│   │   └── ...
│   └── ForgeDemoController.cs
├── Editor/                  # Unity Editor tools
│   ├── ForgeSetupWizard.cs     # First-time setup
│   ├── ForgeGeneratorWindow.cs # Item generation window
│   ├── ForgeAssetExporter.cs   # Asset creation (typed + JSON)
│   └── *Editor.cs files
├── Generated/              # Legacy location for generated assets
Resources/                  # Default location for generated assets (NEW!)
└── Generated/              # Generated ScriptableObject assets (configurable)
    ├── MeleeWeaponAsset/   # Typed assets (organized by asset type)
    ├── MeleeWeapon/        # JSON assets (organized by definition type)
    └── ...
├── Runtime/
│   ├── Core/               # Main system
│   │   ├── ForgeItemGenerator.cs
│   │   ├── ForgeSchemaExtractor.cs
│   │   ├── ForgeItemExporter.cs
│   │   ├── ForgeItemAsset.cs       # JSON storage wrapper
│   │   ├── ForgeTypedAsset.cs      # Base class for typed assets (NEW!)
│   │   ├── ForgeAssetBinding.cs    # Binding attribute (NEW!)
│   │   ├── ForgeTypedAssetFactory.cs # Factory for typed assets (NEW!)
│   │   └── ...
│   └── Integration/
│       └── OpenAI/         # OpenAI API client
└── Settings/               # Configuration (gitignored)
```

## License

See LICENSE.txt in the repository root.
