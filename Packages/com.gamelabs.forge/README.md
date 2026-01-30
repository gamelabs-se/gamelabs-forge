# GameLabs FORGE

**Unity editor tool for generating ScriptableObject assets from existing templates using AI**

GameLabs FORGE generates new ScriptableObject assets directly inside the Unity editor, using your existing data definitions and Unity metadata (tooltips, ranges, enums, etc.).

No base classes. No inheritance. No schema configuration.

---

## Overview

FORGE works by inspecting a selected ScriptableObject and using its structure as the source of truth for generation. You provide a short context (genre, balance intent, theme), and FORGE generates new assets that serialize cleanly back into your project.

The tool is editor-only and fully modular — you can generate assets and remove the tool without affecting your project.

---

## Quick Start

### 1. Install

**Option A: Unity Package**
- Download the latest `.unitypackage` from Releases
- In Unity: `Assets → Import Package → Custom Package`

**Option B: Package Manager (Git URL)**
- Open Package Manager → Add package from git URL
- Enter: `https://github.com/user/gamelabs-forge.git`

### 2. Setup (one time)

- Open `GameLabs → Forge → Re-run Setup Wizard`
- Enter your OpenAI API key
- Configure game context (name, description, audience)
- Select AI model (GPT-5-mini recommended)
- Click Finish

### 3. Generate

- Open `GameLabs → Forge → Forge Window`
- Drag a `.cs` script file containing a ScriptableObject class into the Template Class field
- Set count (1-50) and click **Generate**
- Assets save to `Assets/Resources/Generated/`

---

## Features

| Feature | Description |
|---------|-------------|
| **Template-driven** | Uses existing ScriptableObjects as schema source |
| **Unity metadata aware** | Reads `[Tooltip]`, `[Range]`, `[Header]`, enums |
| **Template Library** | Browse, search, favorite, and track recent templates |
| **Blueprints** | Save generation presets (template + instructions + settings) |
| **Preview mode** | Review generated items before saving |
| **Duplicate prevention** | Three strategies to avoid similar items |
| **Cost tracking** | Monitor tokens and estimated costs per session |
| **Multiple models** | GPT-5-mini, GPT-4o, or o1 |
| **Batch generation** | Up to 50 assets per request |

---

## How It Works

FORGE uses reflection to extract a schema from your ScriptableObject:

```
Template ScriptableObject
         ↓
    Schema Extraction (fields, types, ranges, enums, tooltips)
         ↓
    AI Generation (constrained by schema)
         ↓
    Deserialization → New ScriptableObject Assets
```

**Extracted metadata:**
- Field names and types (`int`, `float`, `string`, `bool`, enums)
- `[Range(min, max)]` constraints
- `[Tooltip("...")]` descriptions (improves AI understanding)
- `[Header("...")]` groupings
- Enum values and their names
- Existing assets of the same type (for duplicate prevention)

---

## Example: Weapon Template

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon")]
public class Weapon : ScriptableObject
{
    [Tooltip("Display name of the weapon")]
    public new string name;

    [Tooltip("Base damage dealt per hit")]
    [Range(1, 100)]
    public int damage;

    [Tooltip("Weight affecting swing speed")]
    [Range(0.1f, 10f)]
    public float weight;

    [Tooltip("Category of weapon")]
    public WeaponType type;

    [Tooltip("How rare this weapon is")]
    public ItemRarity rarity;
}

public enum WeaponType { Sword, Axe, Mace, Dagger, Spear }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
```

Create one asset manually as a template, then generate 20 more with balanced, varied stats.

---

## Configuration

Settings stored in `UserSettings/ForgeConfig.json` (gitignored):

| Setting | Description |
|---------|-------------|
| OpenAI API Key | Required for generation |
| AI Model | GPT-5-mini (default), GPT-4o, or o1 |
| Game Name | Context for generation |
| Game Description | Theme/genre context |
| Target Audience | Affects tone and complexity |
| Temperature | AI creativity (0-2) |
| Duplicate Strategy | How to handle existing assets |
| Generated Path | Where to save new assets |

Access via ⚙️ button in Forge window or `GameLabs → Forge → Re-run Setup Wizard`.

---

## AI Models

| Model | Best For | Input | Output |
|-------|----------|-------|--------|
| **GPT-4o-mini** | Most use cases | $0.15/1M tokens | $0.60/1M tokens |
| GPT-4o | Complex items | $2.50/1M tokens | $10.00/1M tokens |
| o1 | Premium reasoning | $15.00/1M tokens | $60.00/1M tokens |

GPT-4o-mini recommended for most use cases. Actual costs depend on your template complexity (fields, tooltips, enums). Track your usage via Statistics (📊) to see real tokens/item for your templates.

---

## Advanced Features

### Blueprints

Save and reuse generation configurations:
- Template reference
- Custom instructions ("make items feel medieval", "balance for PvP")
- Duplicate strategy override
- Asset discovery path override

Create via **Advanced Options** in Forge window.

### Duplicate Prevention

| Strategy | Description | Cost Impact |
|----------|-------------|-------------|
| **Ignore** | Don't check existing assets | Lowest |
| **Names Only** | Send existing item names | Low |
| **Full Composition** | Send full item data | Higher |

### Statistics

Track token usage per model via 📊 button:
- Tokens used (input + output, per model)
- Items generated per model
- Average tokens per item (calculated from your actual usage)
- Estimated costs (calculated from token counts)
- Success/fulfillment rates

All costs are **calculated from actual token usage** - no hardcoded estimates.

---

## Samples

Import **Demo Items** sample via Package Manager:

| Template | Fields | Description |
|----------|--------|-------------|
| **MeleeWeapon** | 8 | Swords, axes with damage, speed, rarity |
| **Armor** | 10 | Equipment with slots, defense, modifiers |
| **Skill** | 20 | RPG abilities with costs, scaling, effects |
| **GameConfig** | 35 | Gameplay presets: movement, combat, difficulty |
| **Spaceship** | 40+ | Sci-fi ships with full subsystems |

**GameConfig** demonstrates that FORGE works for any ScriptableObject — not just "items". Generate entire difficulty presets, game modes, or configuration variants.

---

## Requirements

- Unity 2021.3+
- OpenAI API key
- Internet connection (during generation only)

---

## License

GameLabs FORGE may be used freely for game development, including commercial titles.

Redistribution or resale of the tool itself is not permitted.

See `LICENSE.md` for full terms.

---

## Limitations

- **Single-layer objects**: Nested ScriptableObject references are left null
- **No asset references**: Sprite, Prefab, AudioClip fields are skipped
- **Schema-constrained**: Generation follows your defined structure exactly

---
