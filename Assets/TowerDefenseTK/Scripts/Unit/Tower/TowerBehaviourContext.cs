using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Runtime state bundle passed to <see cref="TowerBehaviourSO.Tick"/> every frame.
    ///
    /// Provides read/write access to the tower's data, effective stats (including
    /// buff multipliers), the current target, and convenience helpers for the most
    /// common operations: finding a target, dealing damage, and shooting a projectile.
    ///
    /// Properties marked <c>internal set</c> are refreshed by <see cref="TowerWeapon"/>
    /// each frame before Tick() is called — you should read but not assign them.
    /// </summary>
    public class TowerBehaviourContext
    {
        // ── Tower references ──────────────────────────────────────────────────

        /// <summary>The TowerUnit this weapon belongs to.</summary>
        public TowerUnit Tower { get; internal set; }

        /// <summary>Shortcut to the tower's ScriptableObject data.</summary>
        public TowerSO Data => Tower.towerSO;

        /// <summary>The TowerWeapon MonoBehaviour driving this context.</summary>
        public TowerWeapon Weapon { get; internal set; }

        /// <summary>The DamageComponent on the tower — use <see cref="DealDamageTo"/> for
        /// a cleaner call site, or access this directly for advanced use.</summary>
        public DamageComponent DamageComponent { get; internal set; }

        /// <summary>The transform used as the projectile / raycast origin (the weapon head,
        /// or the tower root if no separate muzzle is assigned).</summary>
        public Transform ShootingPoint { get; internal set; }

        // ── Enemy detection ───────────────────────────────────────────────────

        /// <summary>Layer mask for enemy colliders.
        /// Use in <c>Physics.OverlapSphere</c> when implementing custom AOE logic.</summary>
        public LayerMask EnemyLayer { get; internal set; }

        /// <summary>
        /// The currently locked-on target.
        /// Assign manually if your behaviour manages targeting itself, or call
        /// <see cref="FindTarget"/> to delegate to the standard EnemyManager selector.
        /// </summary>
        public BaseEnemy Target { get; set; }

        // ── Effective stats ───────────────────────────────────────────────────

        /// <summary>Tower range after applying any active <see cref="TowerBuffComponent"/> multiplier.</summary>
        public float EffectiveRange { get; internal set; }

        /// <summary>Tower fire rate after applying any active <see cref="TowerBuffComponent"/> multiplier.</summary>
        public float EffectiveFireRate { get; internal set; }

        // ── Fire-rate timer ───────────────────────────────────────────────────

        /// <summary>The <c>Time.time</c> value when the tower last fired.
        /// Updated by <see cref="RegisterFire"/>.</summary>
        public float LastFireTime { get; private set; }

        /// <summary>
        /// Returns <c>true</c> when enough time has elapsed since the last fire
        /// to allow another shot, based on <see cref="EffectiveFireRate"/>.
        /// </summary>
        public bool CanFire() =>
            EffectiveFireRate > 0f && Time.time - LastFireTime >= 1f / EffectiveFireRate;

        /// <summary>
        /// Stamps <c>Time.time</c> as the last fire moment.
        /// Call this inside <see cref="TowerBehaviourSO.Tick"/> immediately after firing.
        /// </summary>
        public void RegisterFire() => LastFireTime = Time.time;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Finds the best enemy target using the tower's configured
        /// <see cref="TowerSO.towerTargetType"/> and <see cref="TowerSO.targetGroup"/>.
        /// Sets <see cref="Target"/> and returns the result (null if none in range).
        /// </summary>
        public BaseEnemy FindTarget()
        {
            if (EnemyManager.Instance == null) { Target = null; return null; }
            Target = EnemyManager.Instance.GetTarget(
                Tower.transform.position,
                EffectiveRange,
                Data.towerTargetType,
                Data.targetGroup);
            return Target;
        }

        /// <summary>
        /// Deals instant damage to an enemy GameObject via the tower's
        /// <see cref="DamageComponent"/>. Respects the damage type and active
        /// buff multipliers automatically.
        /// </summary>
        public void DealDamageTo(GameObject enemy) =>
            DamageComponent?.TryDealDamage(enemy);

        /// <summary>
        /// Spawns a projectile from the standard <c>"Projectile"</c> pool,
        /// initialises it with this tower's <see cref="TowerSO.projectileConfig"/>,
        /// and returns the <see cref="BaseProjectile"/> handle (null on failure).
        /// </summary>
        public BaseProjectile ShootProjectile(BaseEnemy target)
        {
            if (target == null || PoolManager.Instance == null) return null;

            GameObject obj = PoolManager.Instance.Spawn(
                "Projectile", ShootingPoint.position, Quaternion.identity);
            if (obj == null) return null;

            BaseProjectile proj = obj.GetComponent<BaseProjectile>();
            proj?.Init(Data.projectileSpeed, Data.AOE_Radius, Data.projectileConfig, target, Weapon);
            return proj;
        }
    }
}
