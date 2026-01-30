using UnityEngine;

namespace GameLabs.Forge.Demo
{
    /// <summary>
    /// Game configuration preset for Forge demo.
    /// Demonstrates how AI can generate balanced gameplay parameter sets.
    /// </summary>
    [CreateAssetMenu(fileName = "New GameConfig", menuName = "FORGE Samples/Game Config")]
    public class SampleGameConfig : ScriptableObject
    {
        [Tooltip("Display name for this configuration preset")]
        public new string name;

        [TextArea(2, 4)]
        [Tooltip("Description of this preset's intended playstyle or game mode")]
        public string description;

        [Header("Movement")]
        [Range(1f, 20f)]
        [Tooltip("Base movement speed in units per second")]
        public float moveSpeed = 5f;

        [Range(0f, 30f)]
        [Tooltip("Vertical velocity applied when jumping")]
        public float jumpHeight = 8f;

        [Range(1, 5)]
        [Tooltip("Maximum number of jumps before landing (1 = no double jump)")]
        public int maxJumps = 1;

        [Range(0f, 1f)]
        [Tooltip("How much control the player has while airborne (0 = none, 1 = full)")]
        public float airControl = 0.5f;

        [Range(5f, 50f)]
        [Tooltip("Gravity strength affecting falling speed")]
        public float gravity = 20f;

        [Range(1f, 5f)]
        [Tooltip("Sprint speed multiplier when running")]
        public float sprintMultiplier = 1.5f;

        [Header("Combat")]
        [Range(10, 1000)]
        [Tooltip("Starting health for the player")]
        public int startingHealth = 100;

        [Range(0f, 10f)]
        [Tooltip("Health regenerated per second (0 = no regeneration)")]
        public float healthRegenRate = 0f;

        [Range(0f, 5f)]
        [Tooltip("Seconds of invincibility after taking damage")]
        public float invincibilityDuration = 0.5f;

        [Range(0.5f, 3f)]
        [Tooltip("Global damage multiplier for all attacks")]
        public float damageMultiplier = 1f;

        [Range(0f, 2f)]
        [Tooltip("Knockback force multiplier when hit")]
        public float knockbackMultiplier = 1f;

        [Header("Environment Hazards")]
        [Tooltip("Whether falling from height causes damage")]
        public bool fallDamageEnabled = true;

        [Range(5f, 50f)]
        [Tooltip("Minimum fall distance before taking damage")]
        public float fallDamageThreshold = 10f;

        [Range(0.1f, 2f)]
        [Tooltip("Damage per unit fallen beyond threshold")]
        public float fallDamagePerUnit = 0.5f;

        [Tooltip("Whether touching water causes instant death")]
        public bool waterIsLethal = false;

        [Tooltip("Whether the player can drown over time in water")]
        public bool drowningEnabled = true;

        [Range(5f, 60f)]
        [Tooltip("Seconds player can survive underwater")]
        public float breathHoldDuration = 30f;

        [Header("Resources")]
        [Range(0, 500)]
        [Tooltip("Starting currency/gold amount")]
        public int startingCurrency = 0;

        [Range(1, 100)]
        [Tooltip("Maximum items that can be carried")]
        public int inventorySize = 20;

        [Range(1, 10)]
        [Tooltip("Number of quick-access item slots")]
        public int quickSlots = 4;

        [Tooltip("Whether items drop on death")]
        public bool dropItemsOnDeath = false;

        [Tooltip("Whether currency is lost on death")]
        public bool loseCurrencyOnDeath = false;

        [Range(0f, 1f)]
        [Tooltip("Percentage of currency lost on death (if enabled)")]
        public float currencyLossPercent = 0.1f;

        [Header("Progression")]
        [Range(100, 10000)]
        [Tooltip("Experience points needed for first level up")]
        public int baseXPRequired = 100;

        [Range(1f, 3f)]
        [Tooltip("XP requirement multiplier per level")]
        public float xpScalingFactor = 1.5f;

        [Range(1, 100)]
        [Tooltip("Maximum achievable level")]
        public int maxLevel = 50;

        [Tooltip("Whether skills unlock automatically with level")]
        public bool autoUnlockSkills = true;

        [Header("Timing")]
        [Range(0f, 300f)]
        [Tooltip("Time limit for level completion in seconds (0 = no limit)")]
        public float levelTimeLimit = 0f;

        [Range(1, 99)]
        [Tooltip("Number of lives before game over")]
        public int startingLives = 3;

        [Tooltip("Whether extra lives can be earned")]
        public bool canEarnLives = true;

        [Range(1000, 1000000)]
        [Tooltip("Score threshold for earning an extra life")]
        public int lifeBonus = 10000;

        [Header("Difficulty Modifiers")]
        [Tooltip("Preset difficulty level affecting multiple parameters")]
        public DifficultyPreset difficulty;

        [Range(0.5f, 2f)]
        [Tooltip("Multiplier for all incoming damage")]
        public float incomingDamageMultiplier = 1f;

        [Range(0.5f, 2f)]
        [Tooltip("Multiplier for resource/pickup spawn rates")]
        public float pickupSpawnMultiplier = 1f;

        [Range(0f, 1f)]
        [Tooltip("Chance for critical pickups to spawn")]
        public float rarePickupChance = 0.1f;

        [Header("Accessibility")]
        [Tooltip("Whether to show damage numbers")]
        public bool showDamageNumbers = true;

        [Tooltip("Whether to highlight interactive objects")]
        public bool highlightInteractables = true;

        [Range(0.5f, 2f)]
        [Tooltip("Game speed multiplier for slow-motion or fast-forward")]
        public float gameSpeedMultiplier = 1f;

        [Tooltip("Whether auto-aim assistance is enabled")]
        public bool autoAimEnabled = false;

        [Range(0f, 1f)]
        [Tooltip("Strength of auto-aim when enabled (0 = subtle, 1 = strong)")]
        public float autoAimStrength = 0.3f;
    }

    public enum DifficultyPreset
    {
        Story,
        Easy,
        Normal,
        Hard,
        Nightmare,
        Custom
    }
}
