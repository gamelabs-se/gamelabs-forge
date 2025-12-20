# FORGE Quick Start

**AI-powered item generator for Unity**

## 1. Setup (2 minutes)

1. **Get OpenAI API Key**
   - Visit https://platform.openai.com/api-keys
   - Create new API key

2. **Run Setup Wizard**
   - Unity menu: `GameLabs/Forge/Setup Wizard`
   - Enter API key
   - Configure game settings
   - Click "Finish"

## 2. Generate Items (30 seconds)

1. **Open FORGE**
   - Unity menu: `GameLabs/Forge/FORGE`

2. **Select Template**
   - Drag any ScriptableObject to "Template" field
   - Or use demo templates from `Assets/GameLabs/Forge/Demo/Items/`

3. **Generate**
   - Set item count (1-50)
   - Click "Generate X Items"
   - Done! Items appear in results section

4. **Save**
   - Items auto-save to `Assets/Resources/Generated/` (if enabled)
   - Or click "Save" on individual items
   - Or click "Save All"

## 3. Using Generated Items

Generated items are regular Unity ScriptableObjects:
- Drag into inspector fields
- Reference in code: `Resources.Load<YourType>("Generated/TypeName/ItemName")`
- Use in prefabs, inventories, etc.

## Tips

- **Blueprints**: Save generation settings for reuse
- **Existing Items**: FORGE auto-discovers items to avoid duplicates
- **Debug Mode**: Enable in Settings for verbose logs
- **Cost**: ~$0.0001 per item with GPT-4o-mini

## Need Help?

- Full docs: See `README.md` in package folder
- Settings: `GameLabs/Forge/Settings` (accessible via FORGE window)
- Issues: Check console for errors, enable debug mode in Settings

---

**That's it!** You're generating AI-powered game items in under 3 minutes.
