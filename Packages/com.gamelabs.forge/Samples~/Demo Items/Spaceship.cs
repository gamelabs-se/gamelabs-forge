using UnityEngine;

namespace GameLabs.Forge.Demo
{
    [CreateAssetMenu(fileName = "Spaceship", menuName = "GameLabs/Demo/Spaceship")]
    public class Spaceship : ScriptableObject
    {

        [Tooltip("Display name shown to the player.")]
        public string displayName;

        [TextArea]
        [Tooltip("Lore / description shown in UI, tooltips and codex.")]
        public string description;

        [Header("Classification")]
        [Tooltip("High-level size class of the ship.")]
        public ShipSizeClass sizeClass;

        [Tooltip("Primary role of this ship in gameplay.")]
        public ShipRole role;

        [Tooltip("Faction this ship belongs to by default.")]
        public ShipFaction faction;

        [Tooltip("Type of propulsion used by this ship.")]
        public PropulsionType propulsionType;

        [Tooltip("Primary hull material, used for resistances and visuals.")]
        public HullMaterial hullMaterial;

        [Header("Durability")]
        [Range(10, 10000)]
        [Tooltip("Base hull hit points before the ship is destroyed.")]
        public int hullPoints = 100;

        [Range(0, 10000)]
        [Tooltip("Maximum shield hit points when fully charged.")]
        public int shieldPoints = 0;

        [Range(0f, 1f)]
        [Tooltip("Fraction of damage that shields can absorb before the hull is hit (0–1).")]
        public float shieldAbsorption = 0.8f;

        [Range(0f, 1f)]
        [Tooltip("Flat damage reduction applied to incoming hull damage (0–1).")]
        public float armorReduction = 0.1f;

        [Range(0f, 10f)]
        [Tooltip("Hull repair per second when self-repair is active.")]
        public float hullRegenRate = 0f;

        [Range(0f, 50f)]
        [Tooltip("Shield regeneration per second while not taking damage.")]
        public float shieldRegenRate = 5f;

        [Header("Power & Resources")]
        [Range(0f, 1000f)]
        [Tooltip("Total reactor output available for systems and weapons.")]
        public float reactorOutput = 100f;

        [Range(0f, 1000f)]
        [Tooltip("Maximum energy storage capacity for abilities and weapons.")]
        public float energyCapacity = 200f;

        [Range(0f, 500f)]
        [Tooltip("Rate at which energy is regenerated per second.")]
        public float energyRegenRate = 20f;

        [Range(0f, 100000f)]
        [Tooltip("Maximum fuel capacity for FTL or special maneuvers.")]
        public float fuelCapacity = 0f;

        [Range(0f, 1000f)]
        [Tooltip("Fuel consumed per second at maximum throttle or FTL burn.")]
        public float fuelConsumption = 0f;

        [Header("Movement")]
        [Range(0f, 200f)]
        [Tooltip("Maximum linear speed in units per second.")]
        public float maxSpeed = 50f;

        [Range(0f, 50f)]
        [Tooltip("Linear acceleration from thrusters.")]
        public float acceleration = 10f;

        [Range(0f, 360f)]
        [Tooltip("Degrees per second when rotating / yawing.")]
        public float turnRate = 90f;

        [Range(0f, 10f)]
        [Tooltip("How quickly the ship responds to steering input.")]
        public float maneuverability = 5f;

        [Range(0f, 100000f)]
        [Tooltip("Effective mass used in physics or movement calculations.")]
        public float mass = 1000f;

        [Header("Combat")]
        [Range(0, 16)]
        [Tooltip("Number of primary weapon hardpoints.")]
        public int primaryHardpointCount = 2;

        [Range(0, 16)]
        [Tooltip("Number of secondary weapon hardpoints (missiles, torpedoes, etc.).")]
        public int secondaryHardpointCount = 0;

        [Tooltip("Default weapon mount type used for primary hardpoints.")]
        public WeaponMountType primaryWeaponMountType = WeaponMountType.Fixed;

        [Tooltip("Default weapon mount type used for secondary hardpoints.")]
        public WeaponMountType secondaryWeaponMountType = WeaponMountType.MissileRack;

        [Range(0f, 5000f)]
        [Tooltip("Maximum effective targeting range for the ship's sensors.")]
        public float targetingRange = 1500f;

        [Range(0f, 1f)]
        [Tooltip("Chance to critically hit the target (0–1).")]
        public float criticalHitChance = 0.05f;

        [Range(1f, 5f)]
        [Tooltip("Damage multiplier applied to critical hits.")]
        public float criticalHitMultiplier = 2f;

        [Header("Cargo & Crew")]
        [Range(0, 1000)]
        [Tooltip("Maximum number of crew members this ship supports.")]
        public int maxCrew = 4;

        [Range(0, 100)]
        [Tooltip("Minimum recommended crew to operate effectively.")]
        public int recommendedCrew = 2;

        [Range(0f, 100000f)]
        [Tooltip("Maximum cargo mass this ship can carry.")]
        public float cargoCapacity = 1000f;

        [Range(0, 100)]
        [Tooltip("Number of passenger berths.")]
        public int passengerCapacity = 0;

        [Header("AI & Control")]
        [Tooltip("Default AI behavior profile for autopilot or NPC usage.")]
        public ShipAIProfile aiProfile = ShipAIProfile.Balanced;

        [Range(0f, 1f)]
        [Tooltip("How aggressively this ship's AI engages enemies (0 passive, 1 reckless).")]
        public float aggression = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How likely the AI is to retreat when heavily damaged (0 never, 1 always).")]
        public float cowardice = 0.2f;

        [Range(0f, 1f)]
        [Tooltip("How much the AI prioritizes protecting allies over self-preservation.")]
        public float supportiveness = 0.3f;

        [Header("Economy & Meta")]
        [Range(0, 1000000)]
        [Tooltip("Base credit cost to purchase this ship.")]
        public int basePrice = 10000;

        [Range(0, 10)]
        [Tooltip("Tier / tech level of the ship used for progression systems.")]
        public int techTier = 1;

        [Range(0f, 5f)]
        [Tooltip("How rare this ship is when generating shops or fleets (0 common, 5 legendary).")]
        public float rarity = 0.5f;

        [Header("Audio & Visuals")]
        [Tooltip("Prefab used when spawning this ship in the scene.")]
        public GameObject prefab;

        [Tooltip("Icon shown in UI for this ship.")]
        public Sprite icon;

        [Tooltip("Engine trail or particle effect to use for this ship.")]
        public GameObject engineEffectPrefab;

        [Tooltip("Sound to play when this ship's engines are at full throttle.")]
        public AudioClip engineSound;

        [Tooltip("Sound to play on ship destruction.")]
        public AudioClip explosionSound;
    }

    // =======================
    // Enums used by Spaceship
    // =======================

    public enum ShipSizeClass
    {
        Fighter,
        Corvette,
        Frigate,
        Destroyer,
        Cruiser,
        Battleship,
        Carrier,
        Dreadnought,
        Supercapital
    }

    public enum ShipRole
    {
        Interceptor,
        Brawler,
        Artillery,
        Bomber,
        Carrier,
        Support,
        Trader,
        Miner,
        Explorer,
        Transport,
        CapitalFlagship
    }

    public enum ShipFaction
    {
        Neutral,
        Player,
        Pirate,
        Civilian,
        Corporate,
        Military,
        Alien,
        Unknown
    }

    public enum PropulsionType
    {
        Chemical,
        Ion,
        Fusion,
        Antimatter,
        Warp,
        JumpDrive,
        SolarSail,
        Experimental
    }

    public enum HullMaterial
    {
        SteelAlloy,
        TitaniumComposite,
        NanoLaminate,
        OrganicBioHull,
        Crystalline,
        Exotic
    }

    public enum WeaponMountType
    {
        Fixed,
        Turret,
        Gimbal,
        Spinal,
        MissileRack,
        BombBay,
        PointDefense
    }

    public enum ShipAIProfile
    {
        Passive,
        Defensive,
        Balanced,
        Aggressive,
        Kamikaze,
        Escort,
        HitAndRun
    }
}
