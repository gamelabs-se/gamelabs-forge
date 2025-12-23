# FORGE Unity Package Export Guide

## ⚠️ Important: User Settings Security

**Great news!** As of the latest version, **ALL user settings** are stored in EditorPrefs (user-specific), NOT in any file. This means:

- ✅ Your API key will **NEVER** be included in .unitypackage exports
- ✅ Your game name and description will **NEVER** be included
- ✅ All your generation preferences are user-specific
- ✅ Settings are **NEVER** committed to Git (stored in EditorPrefs only)
- ✅ Each user configures their own settings via Setup Wizard
- ✅ No need to manually exclude files when exporting

The `forge.config.json` file is now optional and can be used by developers for system defaults only.

---

## Creating the .unitypackage

### Method 1: Unity Editor (Recommended)

1. **In Unity:**
   - Right-click on `Assets/GameLabs/Forge` folder
   - Select `Export Package...`
   - ✅ Ensure all items are checked
   - ✅ Check "Include dependencies" (should be none)
   - Click `Export...`
   - Save as: `FORGE-v1.0.0-beta.unitypackage`

### Method 2: From Unity Menu

1. **In Unity:**
   - `Assets → Export Package...`
   - Navigate to and select only: `Assets/GameLabs/Forge`
   - ✅ Ensure all items checked
   - ✅ Check "Include dependencies"
   - Click `Export...`
   - Save as: `FORGE-v1.0.0-beta.unitypackage`

---

## Testing the Package (Before Sending)

### 1. Create Test Project

1. Create new Unity project (any template)
2. Unity version: 2021.3 or newer recommended

### 2. Import Package

1. `Assets → Import Package → Custom Package`
2. Select `FORGE-v1.0.0-beta.unitypackage`
3. Click `Import All`
4. Wait for import to complete

### 3. Verify Installation

Check that these exist:
- ✅ `Assets/GameLabs/Forge/` folder
- ✅ Menu item: `GameLabs/Forge/FORGE`
- ✅ Menu item: `GameLabs/Forge/Setup Wizard`
- ✅ Demo items in `Assets/GameLabs/Forge/Demo/Items/`

### 4. Quick Test

1. Open `GameLabs → Forge → Setup Wizard`
2. Enter your OpenAI API key
3. Click through wizard, click "Finish"
4. Open `GameLabs → Forge → FORGE`
5. Drag `Assets/GameLabs/Forge/Demo/Items/MeleeWeapon.asset` to Template field
6. Set count to 3
7. Click "Generate 3 Items"
8. ✅ Should generate 3 weapons
9. Check `Assets/Resources/Generated/MeleeWeapon/` for saved assets

### 5. Verify Console

- Should see: `[Forge] ✓ Generated 3 items...`
- Should NOT see: 999+ debug messages
- If debug messages appear: Check Settings → Debug is OFF

---

## Package Contents

```
Assets/GameLabs/Forge/
├── QUICKSTART.md          ← Start here!
├── README.md              ← Full documentation
├── CHANGELOG.md           ← Release notes
├── Demo/                  ← Example items (optional)
│   └── Items/
│       ├── MeleeWeapon.cs
│       ├── Armor.cs
│       └── Consumable.cs
├── Editor/                ← Editor scripts
│   ├── ForgeWindow.cs
│   ├── ForgeSetupWizard.cs
│   └── ...
├── Runtime/               ← Runtime scripts
│   ├── Core/
│   └── Integration/
└── Settings/              ← Will contain user config
```

---

## Sending to Third Party

### Package File
- Send: `FORGE-v1.0.0-beta.unitypackage`
- Size: ~100-200 KB

### Documentation
**Include these files (extract from package for reference):**
1. `QUICKSTART.md` ← Most important!
2. `README.md` (optional, for advanced users)

### Quick Instructions for Them

```
FORGE - AI Item Generator

1. Import: Assets → Import Package → Custom Package → Select FORGE-v1.0.0-beta.unitypackage
2. Setup: GameLabs → Forge → Setup Wizard → Enter OpenAI API key → Finish
3. Generate: GameLabs → Forge → FORGE → Select template → Generate

See QUICKSTART.md for details (3-minute guide)
```

### Requirements
- Unity 2021.3 or newer (Unity 6 supported)
- OpenAI API key (free tier works: https://platform.openai.com/api-keys)
- Internet connection during generation

### Cost Warning
"Typical cost: ~$0.0001 per item with GPT-4o-mini model (free tier: $5 credit)"

---

## Troubleshooting

### Import Fails
- Try Unity 2021.3+ (minimum version)
- Check console for errors
- Ensure no conflicting packages

### Menu Items Don't Appear
- Restart Unity
- Check `Assets/GameLabs/Forge/Editor/` exists
- Check for compilation errors

### Generation Fails
- Verify API key in Setup Wizard
- Check internet connection
- Enable Debug mode in Settings to see detailed logs

---

## Version Info

- **Version:** 1.0.0-beta
- **Release Date:** 2024-12-20
- **Unity Version:** 2021.3+
- **Dependencies:** None (self-contained)

---

**Ready to ship!** 🚀
