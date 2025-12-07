using UnityEngine;

namespace GameLabs.Forge.Sample
{

    [CreateAssetMenu(fileName = "MeleeWeapon", menuName = "GameLabs/Sample/MeleeWeapon")]
    public class MeleeWeapon : ScriptableObject
    {

        [TextArea]
        [Tooltip("A brief description of the weapon.")]
        public string description = "";

        [Range(1, 100)]
        [Tooltip("Base damage dealt by the weapon, before modifiers.")]
        public int baseDamage;

        [Range(1, 25)]
        [Tooltip("This value affects how fast the weapon can be swung and inventory weight.")]
        public int weight;

        [Range(1, 10000)]
        [Tooltip("base value of the weapon in in-game currency.")]
        public int value;

    }

}