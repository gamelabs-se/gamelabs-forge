# FORGE Sample Templates

Example ScriptableObject templates demonstrating FORGE capabilities.

## Templates

| File | Description | Fields |
|------|-------------|--------|
| `SampleWeapon.cs` | Melee weapon with damage, speed, rarity | 8 |
| `SampleArmor.cs` | Armor with defense, slots, materials | 10 |
| `SampleSkill.cs` | RPG skill with scaling, targeting, effects | 25+ |
| `SampleSpaceship.cs` | Complex spaceship with 40+ parameters | 40+ |
| `SampleGameConfig.cs` | Game settings preset (movement, combat, etc.) | 35+ |

## Usage

1. Import this sample via Package Manager
2. Open **GameLabs → Forge → Forge Window**
3. Drag any `Sample*.cs` file into the Template Class field
4. Click **Generate**

## Example Assets

Pre-generated examples included:
- `Example Weapon.asset` - A sample weapon
- `Example Armor.asset` - A sample armor piece  
- `Example Spaceship.asset` - A sample spaceship

## Creating Your Own

Copy any `Sample*.cs` as a starting point:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "My Game/My Item")]
public class MyItem : ScriptableObject
{
    [Tooltip("Item name")]
    public new string name;
    
    [Range(1, 100)]
    [Tooltip("How much damage this deals")]
    public int damage = 10;
    
    // Add more fields...
}
```

Key tips:
- Use `[Tooltip()]` to guide AI generation
- Use `[Range()]` to constrain numeric values
- Use enums for categorical choices
- Keep field names descriptive
