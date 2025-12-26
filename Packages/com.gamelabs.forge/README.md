# GameLabs FORGE

**Unity editor tool for generating ScriptableObject assets from existing templates**

GameLabs FORGE generates new ScriptableObject assets directly inside the Unity editor, using your existing data definitions and Unity metadata (tooltips, ranges, enums, etc.).

No base classes.
No inheritance.
No schema configuration.

---

## Overview

FORGE works by inspecting a selected ScriptableObject and using its structure as the source of truth for generation. You provide a short context (genre, balance intent, theme), and FORGE generates new assets that serialize cleanly back into your project.

The tool is editor-only and fully modular — you can generate assets and remove the tool without affecting your project.

---

## Quick Start

### 1. Import

- Download the latest `.unitypackage` from Releases
- In Unity: `Assets → Import Package → Custom Package`
- Import the package

### 2. Initial Setup (one time)

- Open `GameLabs → Forge → Setup Wizard`
- Enter your OpenAI API key (stored locally in `EditorPrefs`)
- Configure basic project context
- Finish

### 3. Generate Assets

- Open `GameLabs → Forge → FORGE`
- Drag any ScriptableObject into the **Template** field
- Choose how many assets to generate
- Click **Generate**
- Assets are created and saved directly into the project

---

## How It Works

FORGE inspects the selected ScriptableObject using reflection and extracts:

- Field types (`int`, `float`, `string`, enums, etc.)
- Unity constraints (`[Range]`, `[Min]`, `[Tooltip]`)
- Enum values
- Existing assets (to reduce duplicates)

Generation is constrained by this extracted schema rather than free-form prompting.

Object reference fields are left unset and can be filled in manually if needed.

---

## Features

- **Template-driven generation** — uses existing ScriptableObjects as input
- **Unity metadata aware** — respects ranges, enums, and tooltips
- **Editor-only** — no runtime dependency
- **Batch generation** — generate multiple assets in one pass
- **Automatic serialization** — assets saved directly to the project
- **Reusable presets** — save generation settings for repeated use
- **No project lock-in** — remove FORGE after generation if desired

---

## Example Template

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

Drag this ScriptableObject into FORGE and generate new assets that respect the defined structure and constraints.

---

## Configuration

All configuration is stored per-user using `EditorPrefs`:

- OpenAI API key (never exported)
- Project context (genre, theme, tone)
- Generation parameters
- Asset paths

Settings are accessible via `GameLabs → Forge → Settings`.

---

## Requirements

- Unity 2021.3 or newer
- OpenAI API key
- Internet connection during generation

---

## License

GameLabs FORGE may be used freely to develop and ship games (including commercial titles).

Redistribution or resale of the tool itself is not permitted.

See `LICENSE` for full terms.

---

## Notes

FORGE is currently focused on single-layer data objects. Complex nested structures and deep object graphs are intentionally out of scope for now.

---
