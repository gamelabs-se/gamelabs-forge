# Forge Settings

## Quick Start

**Don't manually edit these files!** Use the Setup Wizard instead:

1. Open Unity
2. Go to **GameLabs → Forge → Setup Wizard**
3. Follow the wizard to configure your settings

## How Settings Work

### All Settings Stored in EditorPrefs (User-Specific)

**ALL your Forge settings** are stored in **EditorPrefs** (not in any file). This means:
- ✅ Your API key is never included when exporting .unitypackage files
- ✅ Your API key is never committed to Git
- ✅ Each team member uses their own API key
- ✅ **Game name and description are user-specific** (not shared)
- ✅ All generation preferences are user-specific
- ✅ No accidental sharing of credentials or project-specific settings

### Configuration File (Optional/Developer-Controlled)

The `forge.config.json` file can be used by developers to:
- Define available models and system defaults
- Control behavior without forcing settings on users
- Provide templates for new users

**Note:** This file is no longer used to store user settings. All user settings are in EditorPrefs.

## Files in This Folder

- **forge.config.json** - Optional developer-controlled configuration (no longer used for user settings)
- **forge.config.template.json** - Template file (for reference only)
- **forge.stats.json** - Usage statistics (auto-generated, in .gitignore)

## For Package Developers

If you're **creating a .unitypackage** to share with others:

1. You can optionally include `forge.config.json` for system defaults
2. Users will need to run the Setup Wizard to configure their own settings
3. All user settings are stored in EditorPrefs (never exported)

## Troubleshooting

### "Missing API Key" Error

Run the Setup Wizard: **GameLabs → Forge → Setup Wizard**

### Need to Change Settings

Go to: **GameLabs → Forge → Settings**

### Lost Your Settings

All settings are stored in Unity's EditorPrefs. If you need to reset them, just run the Setup Wizard again.

## Technical Details

For developers who want to understand the implementation:

- All settings are stored in `EditorPrefs` with keys prefixed: `GameLabs.Forge.*`
- API key: `GameLabs.Forge.OpenAIKey`
- Game name: `GameLabs.Forge.GameName`
- Game description: `GameLabs.Forge.GameDescription`
- And all other settings...
- Config file is optional and only used for developer-controlled system configuration
- To access settings: `ForgeConfig.GetGeneratorSettings()`
- To save settings: `ForgeConfig.SaveGeneratorSettings(settings)`
- To set API key: `ForgeConfig.SetOpenAIKey(string)`

