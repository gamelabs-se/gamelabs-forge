using UnityEngine;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// RPG skill/ability definition for Forge demo.
    /// Demonstrates complex interdependent stats that AI can balance.
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "FORGE Samples/Skill")]
    public class SampleSkill : ScriptableObject
    {
        [Tooltip("Display name of the skill")]
        public new string name;

        [TextArea(2, 4)]
        [Tooltip("Description shown to the player")]
        public string description;

        [Header("Classification")]
        [Tooltip("School or category of magic/combat this skill belongs to")]
        public SkillSchool school;

        [Tooltip("How the skill is activated")]
        public SkillActivationType activationType;

        [Tooltip("What the skill primarily targets")]
        public SkillTargetType targetType;

        [Header("Resource Costs")]
        [Range(0, 200)]
        [Tooltip("Mana/energy cost to cast")]
        public int manaCost = 10;

        [Range(0f, 60f)]
        [Tooltip("Cooldown in seconds before skill can be used again")]
        public float cooldown = 5f;

        [Range(0, 10)]
        [Tooltip("Number of charges before cooldown begins (0 = no charge system)")]
        public int charges = 0;

        [Header("Power & Scaling")]
        [Range(1, 500)]
        [Tooltip("Base damage or healing amount")]
        public int basePower = 25;

        [Range(0f, 3f)]
        [Tooltip("How much the skill scales with the user's primary stat (0-3)")]
        public float statScaling = 1.0f;

        [Range(1, 20)]
        [Tooltip("Required character level to learn this skill")]
        public int levelRequirement = 1;

        [Header("Area & Range")]
        [Range(0f, 50f)]
        [Tooltip("Maximum cast range (0 = self only)")]
        public float range = 10f;

        [Range(0f, 20f)]
        [Tooltip("Area of effect radius (0 = single target)")]
        public float areaRadius = 0f;

        [Range(1, 10)]
        [Tooltip("Maximum number of targets affected")]
        public int maxTargets = 1;

        [Header("Duration & Ticks")]
        [Range(0f, 60f)]
        [Tooltip("Duration of effect in seconds (0 = instant)")]
        public float duration = 0f;

        [Range(0f, 5f)]
        [Tooltip("Time between damage/heal ticks for DoT/HoT effects")]
        public float tickInterval = 1f;

        [Header("Special Properties")]
        [Range(0f, 1f)]
        [Tooltip("Chance to critically strike (0-1)")]
        public float critChance = 0.1f;

        [Range(1f, 4f)]
        [Tooltip("Critical strike damage multiplier")]
        public float critMultiplier = 2f;

        [Tooltip("Status effect applied on hit (if any)")]
        public StatusEffectType statusEffect;

        [Range(0f, 1f)]
        [Tooltip("Chance to apply the status effect (0-1)")]
        public float statusChance = 0f;

        [Header("Visuals & Audio")]
        [Tooltip("Particle effect prefab for casting")]
        public GameObject castEffect;

        [Tooltip("Particle effect prefab for impact")]
        public GameObject impactEffect;

        [Tooltip("Sound played when skill is cast")]
        public AudioClip castSound;

        [Tooltip("Icon displayed in the skill bar")]
        public Sprite icon;
    }

    public enum SkillSchool
    {
        Fire,
        Ice,
        Lightning,
        Earth,
        Light,
        Shadow,
        Arcane,
        Nature,
        Physical,
        Martial,
        Support,
        Utility
    }

    public enum SkillActivationType
    {
        Instant,
        Channeled,
        Charged,
        Toggle,
        Passive,
        Combo
    }

    public enum SkillTargetType
    {
        Self,
        SingleEnemy,
        SingleAlly,
        AllEnemies,
        AllAllies,
        GroundTarget,
        Cone,
        Line,
        Chain
    }

    public enum StatusEffectType
    {
        None,
        Burn,
        Freeze,
        Stun,
        Slow,
        Poison,
        Bleed,
        Blind,
        Silence,
        Root,
        Fear,
        Charm,
        Weaken,
        Strengthen,
        Haste,
        Regeneration,
        Shield
    }
}
