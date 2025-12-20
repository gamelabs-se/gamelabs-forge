# Changelog

## [1.0.0-beta] - 2024-12-20

### Initial Beta Release

**Core Features:**
- AI-powered item generation using OpenAI GPT models
- Dynamic schema extraction from C# classes
- Single and batch generation support
- ScriptableObject asset creation
- Blueprint system for saving generation profiles
- Auto-discovery of existing items to avoid duplicates

**UI/UX:**
- FORGE main window for item generation
- Setup Wizard for initial configuration
- Settings window for advanced configuration
- Statistics tracking (generations, tokens, costs)
- Clean, professional interface

**Technical:**
- Self-contained package structure
- Proper assembly definitions
- GameLabs.Forge namespace
- Debug mode for verbose logging (OFF by default)
- Production-ready logging

**Defaults:**
- Discovery path: `Assets`
- Generated items: `Assets/Resources/Generated/`
- Model: GPT-4o-mini
- Temperature: 0.8
- Debug mode: OFF

### Known Limitations
- Requires OpenAI API key
- Internet connection required during generation
- Costs apply per API usage (~$0.0001 per item with GPT-4o-mini)

### Supported Unity Versions
- Unity 2021.3 or newer
- Unity 6 supported
