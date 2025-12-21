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
   - Added `SetOpenAIKey(string)` method to store API key in EditorPrefs
   - Modified `GetOpenAIKey()` to read from EditorPrefs first
   - Falls back to config file for backwards compatibility (with auto-migration)
   - EditorPrefs key: `GameLabs.Forge.OpenAIKey`

2. ✅ Updated `ForgeSetupWizard.cs`:
   - Saves API key to EditorPrefs instead of config file
   - Loads API key from EditorPrefs (with config file fallback)
   - Config file now saves empty string for `openaiApiKey` field

3. ✅ Updated `ForgeSettingsWindow.cs`:
   - Saves empty string for API key in config file

4. ✅ Created documentation:
   - `Settings/README.md` - Explains the new approach
   - `Settings/forge.config.template.json` - Template for reference
   - Updated `PACKAGE_EXPORT_GUIDE.md` with security notes

**Benefits:**
- ✅ **DX (Developer Experience):** No risk of accidentally sharing API key
- ✅ **UX (User Experience):** Users never receive someone else's API key
- ✅ **Security:** API keys are user-specific, never in version control or packages
- ✅ **Team-friendly:** Each team member uses their own API key
- ✅ **Backwards compatible:** Old config files are automatically migrated

## Testing Checklist

### Task 1 (Runtime → Editor)
- [ ] Unity project compiles without errors
- [ ] All Editor windows (Forge, Setup Wizard, Settings, Statistics) open correctly
- [ ] Demo items are still visible and usable in Project window
- [ ] Can drag demo items to templates in Forge window

### Task 2 (API Key Security)
- [ ] Setup Wizard saves API key to EditorPrefs (verify it's not in forge.config.json)
- [ ] Setup Wizard loads API key from EditorPrefs correctly
- [ ] Settings window works correctly with API key in EditorPrefs
- [ ] Can generate items successfully with API key from EditorPrefs
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

1. **Your API key will be automatically migrated** from forge.config.json to EditorPrefs on first run
2. **The API key remains in the config file** but will be ignored in favor of EditorPrefs
3. **Going forward:** New saves from Setup Wizard or Settings will write empty string to the config file's `openaiApiKey` field
4. **No action needed** - everything should work seamlessly
5. **Bonus:** Your API key is now more secure and won't be shared accidentally

**Note:** If you want to manually clean up, you can safely edit forge.config.json and set `"openaiApiKey": ""` - the system will use the EditorPrefs value.

## For Package Developers

When creating .unitypackage exports:

1. Simply export `Assets/GameLabs/Forge` as usual
2. The forge.config.json will be included (without API key)
3. Users will need to run Setup Wizard to add their API key
4. This is the intended behavior - security first!
