# Migration Summary: Runtime to Editor + API Key Security

## Changes Made

### Task 1: Move Runtime Folder to Editor

**Why:** This package is meant to be used in the Unity Editor only, not at runtime.

**Changes:**
1. ✅ Moved all files from `Runtime/Core/` to `Editor/Core/`
2. ✅ Moved all files from `Runtime/Integration/` to `Editor/Integration/`
3. ✅ Updated all namespaces:
   - `GameLabs.Forge` → `GameLabs.Forge.Editor`
   - `GameLabs.Forge.Integration.OpenAI` → `GameLabs.Forge.Editor.Integration.OpenAI`
4. ✅ Deleted `Runtime/` folder and `GameLabs.Forge.Runtime.asmdef`
5. ✅ Updated Editor assembly definition to remove self-reference
6. ✅ Updated Demo assembly definition to remove Forge dependency
7. ✅ Removed unused `using GameLabs.Forge;` from Demo files

**Impact:**
- All Forge functionality is now Editor-only
- Demo ScriptableObjects remain usable at runtime (they don't depend on Forge code)
- Cleaner package structure

### Task 2: API Key Security

**Problem:** When exporting .unitypackage, the `forge.config.json` file would include the developer's OpenAI API key, creating security and UX issues.

**Solution:** Store API key in EditorPrefs (user-specific), not in any file.

**Changes:**
1. ✅ Modified `ForgeConfig.cs`:
   - All user settings now stored in EditorPrefs (not just API key)
   - Added getter/setter methods for all settings
   - Added `SaveGeneratorSettings()` and `GetGeneratorSettings()` methods
   - Automatic migration from config file to EditorPrefs
   - Config file now optional, used only for developer-controlled system defaults
   - EditorPrefs keys: `GameLabs.Forge.OpenAIKey`, `GameLabs.Forge.GameName`, `GameLabs.Forge.GameDescription`, etc.

2. ✅ Updated `ForgeSetupWizard.cs`:
   - Saves all settings to EditorPrefs (not config file)
   - Loads from EditorPrefs (with auto-migration from old config files)

3. ✅ Updated `ForgeSettingsWindow.cs`:
   - Saves all settings to EditorPrefs (not config file)

4. ✅ Updated documentation:
   - `Settings/README.md` - Explains that ALL settings are now in EditorPrefs
   - `Settings/forge.config.template.json` - Marked as optional/developer-controlled
   - Updated `PACKAGE_EXPORT_GUIDE.md` with new approach

**Benefits:**
- ✅ **DX (Developer Experience):** No risk of accidentally sharing API key or project-specific settings
- ✅ **UX (User Experience):** Users never receive someone else's settings, game name, or API key
- ✅ **Security:** All user settings are user-specific, never in version control or packages
- ✅ **Team-friendly:** Each team member has their own configuration
- ✅ **Backwards compatible:** Old config files are automatically migrated on first use
- ✅ **Clean exports:** .unitypackage files don't contain any user-specific data

## Testing Checklist

### Task 1 (Runtime → Editor)
- [ ] Unity project compiles without errors
- [ ] All Editor windows (Forge, Setup Wizard, Settings, Statistics) open correctly
- [ ] Demo items are still visible and usable in Project window
- [ ] Can drag demo items to templates in Forge window

### Task 2 (All Settings in EditorPrefs)
- [ ] Setup Wizard saves all settings to EditorPrefs
- [ ] Setup Wizard loads settings from EditorPrefs correctly
- [ ] Settings window saves/loads all settings from EditorPrefs
- [ ] Can generate items successfully with settings from EditorPrefs
- [ ] Old forge.config.json files with API key are migrated automatically
- [ ] Exporting .unitypackage doesn't include API key in any file
- [ ] Importing package in new project requires running Setup Wizard

## Files Changed

### Modified Files:
- `Assets/GameLabs/Forge/Editor/Core/*.cs` (13 files) - Namespace updates
- `Assets/GameLabs/Forge/Editor/Integration/OpenAI/ForgeOpenAIClient.cs` - Namespace update
- `Assets/GameLabs/Forge/Editor/ForgeSetupWizard.cs` - EditorPrefs integration
- `Assets/GameLabs/Forge/Editor/ForgeSettingsWindow.cs` - EditorPrefs integration
- `Assets/GameLabs/Forge/Editor/GameLabs.Forge.Editor.asmdef` - Remove references
- `Assets/GameLabs/Forge/Demo/GameLabs.Forge.Demo.asmdef` - Remove Runtime reference
- `Assets/GameLabs/Forge/Demo/Items/*.cs` (4 files) - Remove unused using statements
- `PACKAGE_EXPORT_GUIDE.md` - Add security notes

### New Files:
- `Assets/GameLabs/Forge/Settings/README.md` - Documentation
- `Assets/GameLabs/Forge/Settings/forge.config.template.json` - Template

### Deleted Files:
- `Assets/GameLabs/Forge/Runtime/` (entire folder)
- `Assets/GameLabs/Forge/Runtime/GameLabs.Forge.Runtime.asmdef`

## Migration Notes for Users

If you have an existing project with the old version:

1. **All settings will be automatically migrated** from forge.config.json to EditorPrefs on first use
2. **This includes:**
   - API key
   - Game name and description
   - All generation settings (model, temperature, batch sizes, etc.)
   - Asset paths and preferences
3. **The old config file is preserved** for backwards compatibility:
   - If you roll back to an older version, it will still work
   - Future saves no longer write to this file
   - You can safely delete it if you want
4. **No action needed** - everything should work seamlessly
5. **Benefits:** All your settings are now user-specific and won't be shared accidentally

## For Package Developers

When creating .unitypackage exports:

1. Simply export `Assets/GameLabs/Forge` as usual
2. The forge.config.json will be included (without API key)
3. Users will need to run Setup Wizard to add their API key
4. This is the intended behavior - security first!
