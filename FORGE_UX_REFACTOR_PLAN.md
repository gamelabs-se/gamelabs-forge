# FORGE UX Refactor: Single Source of Truth

## Core Problem
Multiple places to edit same values creates user distrust:
- API key editable in Wizard AND Settings
- Game context in Wizard AND Settings  
- Generation settings duplicated

## Solution Architecture

### 1. Setup Wizard = Onboarding Only (ONE TIME)
**Purpose:** Get user from zero → first generation as fast as possible

**Changes Needed:**
- Add `[InitializeOnLoad]` attribute ✅ DONE
- Auto-open on first run via `CheckFirstRun()` ✅ DONE
- Store completion in `EditorPrefs: "GameLabs.Forge.HasCompletedWizard"` ✅ DONE
- Rename menu item to "Re-run Setup Wizard" ✅ DONE

**Simplified to 3 Steps:**
1. **API Configuration** (Required)
   - API key input
   - Model selection (default: GPT-4o-mini)
   - One sentence: "FORGE uses your own OpenAI API key. It's stored locally and never exported."
   - Remove pricing/cost info (too much cognitive load)

2. **Game Defaults** (Starting defaults only)
   - Game Name
   - Game Description  
   - Target Audience
   - Temperature (Creativity): 0.8
   - Default Batch Size: 5
   - **Add at top:** "These defaults can be changed later in Settings."

3. **Done** (Checkpoint, not form)
   - Title: "FORGE is ready"
   - Read-only summary of choices
   - Primary button: "Open FORGE" (opens ForgeWindow)
   - Secondary: "Open Settings"
   - No "Save Settings" button (auto-saved on Next)

**Removed from Wizard:**
- Asset paths (workflow decision, not onboarding)
- Duplicate prevention strategy (advanced)
- Auto-load existing assets (advanced)
- Debug options (not for first-timers)
- Max batch size (not essential)
- Additional rules (optional/advanced)

### 2. Settings Window = Single Source of Truth

**Add to top:**
> "These settings are used as defaults for all generations."

**Structure with Collapsible Sections:**

#### API & Model (NEW - Add this section)
```
▼ API & Model
  API Key: [********] (Edit) (Stored locally)
  Model: [GPT-4o-mini ▼]
```

#### Game Context (Existing, improve)
```
▼ Game Context  
  Game Name: [My Game]
  Game Description: [text area]
  Target Audience: [General]
```

#### Generation Defaults (Existing, rename)
```
▼ Generation Defaults
  Temperature: 0.8 slider
  Default Batch Size: 5
  Max Batch Size: 20
```

#### Asset Paths (Existing, add hint)
```
▼ Asset Paths
  Existing Assets Search Path: [Assets] (Browse) (Reveal)
  Generated Assets Base Path: [Resources/Generated] (Browse) (Reveal)
  
  Hint: "Paths are relative to the Assets folder."
```

#### Duplicate Handling (Existing, rename from "Existing Items")
```
▼ Duplicate Handling
  Strategy: [Prevent Duplicates & Refine Naming ▼]
  Auto-load Existing Assets: ☑
  
  Hint: "Controls how FORGE treats existing assets of the same type."
```

#### Debug (Existing, collapse by default)
```
▸ Debug (collapsed by default)
  Debug Mode: ☐
  
  Hint: "Only needed when diagnosing issues."
```

**Actions:**
- Remove "Save Settings" button → Auto-save on change
- Keep "Open Setup Wizard" → Rename to "Re-run Setup Wizard"
- Add "Reset to Defaults" with confirmation dialog

### 3. Implementation Checklist

#### Setup Wizard Changes
- [x] Add InitializeOnLoad + first-run detection
- [ ] Reduce steps from 5 to 3
- [ ] Simplify Step 1 (API) - remove pricing
- [ ] Simplify Step 2 (Defaults) - add "can be changed" note
- [ ] Simplify Step 3 (Done) - "Open FORGE" primary button
- [ ] Remove advanced options (paths, strategies, etc.)
- [ ] Change save message to "Initial configuration saved. Use Settings to make changes."

#### Settings Window Changes
- [ ] Add "API & Model" section at top
- [ ] Make API key editable in Settings
- [ ] Add collapsible sections (persist state in EditorPrefs)
- [ ] Rename "Generation Settings" → "Generation Defaults"
- [ ] Rename "Existing Items Context" → "Duplicate Handling"
- [ ] Add explanatory hints under each section
- [ ] Collapse "Debug" by default
- [ ] Auto-save on change (remove "Save" button)
- [ ] Add "Reveal Folder" buttons next to paths
- [ ] Add top-level hint: "These settings are used as defaults for all generations."

#### ForgeConfig Changes
- [ ] Ensure all Get* methods pull from project config
- [ ] Ensure all Set* methods write to project config
- [ ] Remove any legacy migration code ✅ DONE
- [ ] Add GetApiKey() and SetApiKey() if missing ✅ DONE

### 4. User Flow

**First-Time User:**
1. Opens Unity project with FORGE
2. Setup Wizard auto-opens
3. Enters API key, sets basic defaults (2 minutes)
4. Clicks "Open FORGE"
5. Never sees wizard again unless explicitly re-run

**Returning User:**
1. Uses FORGE directly
2. Opens Settings when tweaking is needed
3. Settings is the only place values change

### 5. Microcopy Changes

**Wizard:**
- "Setup Wizard" → Keep
- Menu: "Setup Wizard" → "Re-run Setup Wizard"
- Save message: "Configuration saved to UserSettings..." → "Initial configuration saved. Use Settings to make changes."
- Step 2 header: Add subtitle "These defaults can be changed later in Settings."

**Settings:**
- Window title: "Settings" → Keep
- Top hint: Add "These settings are used as defaults for all generations."
- "Existing Items Context" → "Duplicate Handling"
- "Generation Intent" → "Strategy"
- Debug section hint: "Only needed when diagnosing issues."

### 6. Testing Checklist
- [ ] Fresh project: Wizard auto-opens
- [ ] Complete wizard: Never auto-opens again
- [ ] Wizard sets initial values in Settings
- [ ] Settings changes persist
- [ ] Re-running wizard loads current Settings values
- [ ] Completing wizard again doesn't break anything
- [ ] API key editable in Settings
- [ ] No value can be set in two places

## Expected Outcome

**Before:**
- User confused about which screen to trust
- Values scattered across Wizard/Settings
- Wizard feels like parallel config system

**After:**
- Wizard = one-time onboarding (fast, friendly)
- Settings = authoritative, boring, trustworthy
- Clear: "Wizard initializes, Settings controls"
- User confidence: "I know where everything is"

## Files to Modify
1. `ForgeSetupWizard.cs` - Simplify to 3 steps, first-run detection
2. `ForgeSettingsWindow.cs` - Add sections, collapsibility, API editing
3. `ForgeConfig.cs` - Ensure single source of truth ✅ MOSTLY DONE
4. `ForgeWindow.cs` - No changes needed (reads from Settings)

## Estimated Impact
- **New user time-to-first-generation:** 5 min → 2 min
- **User trust in Settings:** 60% → 95%
- **Support questions about "which screen":** Many → Zero
