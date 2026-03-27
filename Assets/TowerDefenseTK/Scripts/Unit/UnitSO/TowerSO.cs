using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using Unity.VisualScripting;

namespace TowerDefenseTK
{

    [CreateAssetMenu(fileName = "New Tower Data", menuName = "TD Toolkit/Units/Tower")]

    public class TowerSO : UnitSO
    {
        [Header("Tower Stats")]
        public float fireRate;
        public float range;

        public TowerType towerType;
        public TargetType towerTargetType = TargetType.First;
        public TargetGroup targetGroup;
        public AOEType AOEType;
        public float projectileSpeed;
        public float AOE_Radius;

        [Tooltip("Gold cost to build this tower.")]
        public int buildCost;

        public GameObject previewPrefab;

        [Header("Status Effect")]
        [Tooltip("Effect applied to enemies hit by this tower (turret/AoE: on hit, support: aura pulse).\n" +
                 "Leave empty for no status effect. Create presets via TD Toolkit / Status Effect.")]
        public StatusEffectSO statusEffect;

        [Header("Projectile Config (AoE Turret_AOE type)")]
        [Tooltip("Controls arc height and targeting mode for projectile-based AoE towers.")]
        public ProjectileConfig projectileConfig;

        [Header("Cone AOE")]
        [Range(10f, 180f)]
        [Tooltip("Full opening angle of the cone in degrees. 60 = 30° each side of the tower's forward.")]
        public float coneAngle = 60f;

        [Header("Buff Tower (Support type)")]
        [Tooltip("Buff pulsed to allied towers in range every 0.5 s.\n" +
                 "Set this on Support towers that boost allies instead of debuffing enemies.\n" +
                 "Receiving towers must have a TowerBuffComponent attached.\n" +
                 "Leave empty if this tower debuffs enemies instead.")]
        public TowerBuffSO towerBuff;

        [Header("Resource Tower")]
        [Tooltip("Gold generated per second. Used only by Resource-type (Miner) towers.")]
        public float goldPerSecond = 5f;

        [Header("Custom Behaviour (Optional)")]
        [Tooltip("Assign a TowerBehaviourSO subclass to completely replace the built-in\n" +
                 "Turret / AoE / Support / Resource logic for this tower.\n\n" +
                 "When set, the TowerType field is ignored — your Tick() method runs instead.\n" +
                 "Leave empty to use the standard enum-driven behaviour.")]
        public TowerBehaviourSO customBehaviour;
    }

    /// <summary>
    /// Configures projectile flight behaviour for Turret_AOE towers.
    /// Exposed in the TowerSO inspector under "Projectile Config".
    /// </summary>
    [System.Serializable]
    public struct ProjectileConfig
    {
        [Range(0f, 10f)]
        [Tooltip("Height of the ballistic arc at the midpoint. 0 = flat straight-line flight.")]
        public float arcHeight;

        [Tooltip("When enabled the projectile locks on to the target's position at launch time " +
                 "and flies there even if the target moves or dies. " +
                 "Recommended with arcHeight > 0 so the arc stays meaningful.")]
        public bool useFixedTarget;
    }

    public enum TowerType
    {
        Turret,
        AoE,
        Support,
        Resource
    }

    public enum AOEType
    {
        Cone,
        Circle,
        Turret_AOE
    }
    //Only used for AOE Towers

    public enum TargetType
    {
        First,
        Last,
        Closest,
        Strongest,
        Weakest
    }

    public enum TargetGroup
    {
        Ground,
        Air,
        Both
    }
}
