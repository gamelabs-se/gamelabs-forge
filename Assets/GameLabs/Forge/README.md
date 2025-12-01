# 🔥 Forge - AI-Powered Item Generator for Unity

Forge is a dynamic, AI-powered item generation system for Unity that uses OpenAI's GPT models to generate game items based on your custom C# class definitions. It's designed for offline game development workflows, helping you quickly create batches of items for your game.

## Features

- **Item-Agnostic**: Define any item type as a C# class - Forge extracts the schema automatically
- **Dynamic Schema Extraction**: Uses reflection to understand your item structure including ranges, enums, and descriptions
- **Single & Batch Generation**: Generate one item or many at once
- **Context-Aware**: Provide existing items as reference to ensure variety
- **JSON Export/Import**: Save generated items for use in your game
- **Setup Wizard**: Easy first-time configuration with cost estimation
- **Custom Attributes**: Fine-tune generation with `[ForgeDescription]` and `[ForgeConstraint]`

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

## Supported Attributes

### Schema Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[ForgeDescription("...")]` | Class, Field | Provides description for AI context |
| `[Range(min, max)]` | Field | Sets numeric range constraints |
| `[Min(value)]` | Field | Sets minimum value |
| `[Tooltip("...")]` | Field | Used as field description if no ForgeDescription |

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

## Configuration

Settings are stored in `Assets/GameLabs/Forge/Settings/forge.config.json` (gitignored by default).

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

## Demo Items Included

The package includes example item types in `Assets/GameLabs/Forge/Demo/Items/`:

- **MeleeWeapon** - Swords, axes, maces with damage, weight, durability
- **Consumable** - Potions, food with effects and duration
- **Collectible** - Treasures with lore and rarity
- **Armor** - Equipment with defense, weight, slots

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
│   ├── Items/              # Sample item definitions
│   └── ForgeDemoController.cs
├── Editor/                  # Unity Editor tools
│   ├── ForgeSetupWizard.cs
│   └── *Editor.cs files
├── Generated/              # Export destination (gitignored)
├── Runtime/
│   ├── Core/               # Main system
│   │   ├── ForgeItemGenerator.cs
│   │   ├── ForgeSchemaExtractor.cs
│   │   ├── ForgeItemExporter.cs
│   │   └── ...
│   └── Integration/
│       └── OpenAI/         # OpenAI API client
└── Settings/               # Configuration (gitignored)
```

## License

See LICENSE.txt in the repository root.
