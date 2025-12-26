# GameLabs Forge

**AI-Powered ScriptableObject Generator for Unity**

Generate unlimited game content using OpenAI's GPT models. No inheritance required, no coding needed—just plug and play.

> **Editor-only tool** • **Bring your own OpenAI key** • **Config files are local, never committed**

## Installation

### via Unity Package Manager (Recommended)

1. Open **Package Manager** (`Window → Package Manager`)
2. Click **+** → **Add package from git URL...**
3. Paste this URL:

```
https://github.com/gamelabs-se/gamelabs-forge.git?path=Packages/com.gamelabs.forge#v0.1.5
```

4. Click **Add**

### Manual Installation
- Download the latest `.unitypackage` from [releases](https://github.com/gamelabs-se/gamelabs-forge/releases)
- In Unity: `Assets → Import Package → Custom Package`
- Select the downloaded file and import

## Quick Start

### 1. Setup (One Time)
- Open `GameLabs → Forge → Setup Wizard`
- Enter your OpenAI API key ([get one here](https://platform.openai.com/api-keys))
- Configure your game settings
- Click **Finish**

### 2. Generate Content
- Open `GameLabs → Forge → FORGE`
- Drag any ScriptableObject into the **Template** field
- Set the number of items to generate
- Click **🔥 Generate Items**
- Done! Assets are automatically saved to your project

That's it. No base classes to extend, no code to write.

## How It Works

Forge analyzes your ScriptableObject structure using reflection and uses AI to generate contextually appropriate content. It understands:

- Field types (int, float, string, enums, etc.)
- Constraints (`[Range]`, `[Min]`, `[Tooltip]`)
- Enums and their values
- Your game's context and theme

## Features

✅ **Zero Configuration** - Works with any ScriptableObject out of the box  
✅ **Plug & Play** - No inheritance or base classes required  
✅ **Batch Generation** - Generate 1 to 50+ items at once  
✅ **Auto-Save** - Generated assets saved directly to your project  
✅ **Context-Aware** - Automatically discovers existing items to prevent duplicates  
✅ **Schema Extraction** - Understands your data structure automatically  
✅ **Blueprint System** - Save generation settings for reuse  

## Example

Any ScriptableObject works:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon")]
public class Weapon : ScriptableObject
{
    public new string name;
    
    [Range(1, 100)]
    public int damage;
    
    [Range(0.1f, 10f)]
    public float weight;
    
    public WeaponType type;
}

public enum WeaponType { Sword, Axe, Mace, Dagger }
```

Drag it into Forge, generate—done. You'll get unique, varied weapons that respect your constraints.

## Demo Content

Sample ScriptableObjects included in `Assets/GameLabs/Forge/Demo/Items/`:
- **MeleeWeapon** - Swords, axes, maces with damage and stats
- **Consumable** - Potions and food with effects
- **Armor** - Equipment with defense values
- **Collectible** - Treasures with lore

Use these as references or templates.

## Configuration

All settings are stored per-user (EditorPrefs):
- **API Key** - Never shared or exported
- **Game Context** - Your game's name, theme, and setting
- **Generation Settings** - Model selection, creativity, batch size
- **Asset Paths** - Where to save and search for assets

Access settings anytime via `GameLabs → Forge → Settings`

## Cost

Using GPT-4o-mini (recommended):
- Single item: ~$0.00002
- Batch of 20 items: ~$0.00025

Free tier includes $5 credit—that's ~25,000 items.

## Requirements

- Unity 2021.3 or newer (Unity 6 supported)
- OpenAI API key (free tier available)
- Internet connection during generation

## Documentation

- **Setup Wizard** - First-time configuration
- **FORGE Window** - Main generation interface
- **Statistics** - Track usage and costs
- **Settings** - Configure all preferences

All accessible from `GameLabs → Forge` menu.

## License

© 2025 GameLabs AB. All rights reserved.

This software is proprietary and confidential.

See LICENSE.txt for full terms.

---

**Generate smarter, ship faster.** 🔥
