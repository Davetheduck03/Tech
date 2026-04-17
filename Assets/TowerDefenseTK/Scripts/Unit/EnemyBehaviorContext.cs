using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Runtime state bundle passed to <see cref="EnemyBehaviorSO.Tick"/> every frame.
    ///
    /// Provides read/write access to the enemy's components, path progress,
    /// effective stats, and convenience helpers for the most common operations:
    /// speed overrides, damage dealing, and nearby-tower queries.
    ///
    /// <para>Use <see cref="Blackboard"/> to store per-instance data that
    /// persists across frames but is cleared automatically on each pool spawn.</para>
    /// </summary>
    public class EnemyBehaviorContext
    {
        // ── Unit references ───────────────────────────────────────────────────

        /// <summary>The BaseEnemy this context belongs to.</summary>
        public BaseEnemy Enemy { get; private set; }

        /// <summary>Shortcut to the enemy's ScriptableObject data.</summary>
        public EnemySO Data => Enemy?.GetEnemyData();

        /// <summary>World-space transform of the enemy.</summary>
        public Transform Transform => Enemy?.transform;

        // ── Components ────────────────────────────────────────────────────────

        /// <summary>Health/damage-flash component. Use <see cref="HealthPercent"/> for a quick ratio.</summary>
        public HealthComponent Health { get; private set; }

        /// <summary>Movement + path-follower driver.
        /// Modify <see cref="SetSpeedMultiplier"/> / <see cref="SetSpeedOverride"/> rather than
        /// writing to this directly.</summary>
        public MovementComponent Movement { get; private set; }

        /// <summary>Damage dealing component — use <see cref="DealDamageToNearbyTowers"/> for a
        /// one-liner, or call <c>Damage.TryDealDamage(go)</c> for fine-grained control.</summary>
        public DamageComponent Damage { get; private set; }

        /// <summary>Active status-effect receiver (slow, DOT, stun).</summary>
        public StatusEffectComponent StatusEffects { get; private set; }

        // ── Path state ────────────────────────────────────────────────────────

        /// <summary>Number of path nodes the enemy has passed so far.</summary>
        public int NodesPassed => Enemy != null ? Enemy.nodesPassed : 0;

        /// <summary>Total path nodes from spawn to exit.</summary>
        public int TotalNodes => Enemy != null ? Enemy.totalPathNodes : 0;

        /// <summary>0 = just spawned, 1 = at the exit. Useful for enrage thresholds.</summary>
        public float PathProgress => TotalNodes > 0 ? (float)NodesPassed / TotalNodes : 0f;

        // ── Health helpers ────────────────────────────────────────────────────

        /// <summary>Current HP. 0 when the HealthComponent is missing.</summary>
        public float CurrentHealth => Health != null ? Health.currentHealth : 0f;

        /// <summary>Max HP at spawn. 0 when the HealthComponent is missing.</summary>
        public float MaxHealth => Health != null ? Health.maxHealth : 0f;

        /// <summary>Normalised HP ratio [0, 1]. 0 when health component is absent.</summary>
        public float HealthPercent => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

        // ── Speed control ─────────────────────────────────────────────────────

        private float _baseSpeed;          // cached from EnemySO.Speed on Refresh
        private float _speedMultiplier = 1f;

        /// <summary>
        /// Effective movement speed = baseSpeed × multiplier.
        /// Status-effect slows are applied on top of this by MovementComponent itself.
        /// </summary>
        public float EffectiveSpeed => Movement != null ? Movement.EffectiveSpeed : 0f;

        /// <summary>
        /// Multiply the base speed.  1 = normal, 2 = double, 0.5 = half.
        /// Resets to 1 automatically on each <see cref="OnSpawned"/> call.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = multiplier;
            ApplySpeed();
        }

        /// <summary>
        /// Override the movement speed to an exact value, ignoring the base speed.
        /// Pass <c>-1</c> to revert to the base speed × current multiplier.
        /// </summary>
        public void SetSpeedOverride(float speed)
        {
            if (Movement == null) return;
            Movement.movement_Speed = speed >= 0f ? speed : _baseSpeed * _speedMultiplier;
        }

        /// <summary>Revert to base speed × current multiplier.</summary>
        public void ClearSpeedOverride() => ApplySpeed();

        private void ApplySpeed()
        {
            if (Movement != null)
                Movement.movement_Speed = _baseSpeed * _speedMultiplier;
        }

        // ── Time ──────────────────────────────────────────────────────────────

        /// <summary>Time.deltaTime shortcut for use inside Tick().</summary>
        public float DeltaTime => UnityEngine.Time.deltaTime;

        /// <summary>Time.time shortcut.</summary>
        public float CurrentTime => UnityEngine.Time.time;

        // ── Per-instance blackboard ───────────────────────────────────────────

        /// <summary>
        /// Dictionary for storing arbitrary per-instance data between Tick() calls.
        /// Cleared automatically each time the enemy is spawned from the pool.
        /// Recommended pattern:
        /// <code>
        /// if (!ctx.Blackboard.TryGetValue("chargeTimer", out object v))
        ///     v = 0f;
        /// float t = (float)v + ctx.DeltaTime;
        /// ctx.Blackboard["chargeTimer"] = t;
        /// </code>
        /// </summary>
        public Dictionary<string, object> Blackboard { get; } = new Dictionary<string, object>();

        // ── Convenience helpers ───────────────────────────────────────────────

        /// <summary>
        /// Return this enemy to the object pool immediately.
        /// Prefer this over destroying the GameObject.
        /// </summary>
        public void Despawn()
        {
            if (Enemy != null)
                PoolManager.Instance?.Despawn(Enemy.gameObject);
        }

        /// <summary>
        /// Find the nearest <see cref="TowerUnit"/> within <paramref name="range"/> world units.
        /// Returns null when none is found or PoolManager is unavailable.
        /// </summary>
        public TowerUnit FindNearestTower(float range)
        {
            if (Transform == null) return null;

            Collider[] hits = Physics.OverlapSphere(Transform.position, range);
            TowerUnit best = null;
            float bestDist = float.MaxValue;

            foreach (Collider col in hits)
            {
                TowerUnit tower = col.GetComponentInParent<TowerUnit>();
                if (tower == null) continue;

                float dist = Vector3.Distance(Transform.position, tower.transform.position);
                if (dist < bestDist) { bestDist = dist; best = tower; }
            }

            return best;
        }

        /// <summary>
        /// Deal this enemy's damage to every tower collider within
        /// <paramref name="range"/> world units (uses the enemy's DamageComponent).
        /// </summary>
        public void DealDamageToNearbyTowers(float range)
        {
            if (Transform == null || Damage == null) return;

            Collider[] hits = Physics.OverlapSphere(Transform.position, range);
            foreach (Collider col in hits)
                Damage.TryDealDamage(col.gameObject);
        }

        /// <summary>
        /// Spawn a GameObject from the pool by name at an optional position.
        /// Useful for death-burst effects, spawning sub-enemies, etc.
        /// </summary>
        public GameObject SpawnFromPool(string poolName, Vector3? position = null, Quaternion? rotation = null)
        {
            if (PoolManager.Instance == null) return null;
            Vector3 pos = position ?? Transform.position;
            Quaternion rot = rotation ?? Quaternion.identity;
            return PoolManager.Instance.Spawn(poolName, pos, rot);
        }

        // ── Internal wiring ───────────────────────────────────────────────────

        /// <summary>
        /// Populates (or re-populates) all component references from the given enemy.
        /// Called by <see cref="BaseEnemy"/> on Awake and again on each pool spawn.
        /// </summary>
        internal void Refresh(BaseEnemy enemy)
        {
            Enemy = enemy;
            Health       = enemy.GetComponent<HealthComponent>();
            Movement     = enemy.GetComponent<MovementComponent>();
            Damage       = enemy.GetComponent<DamageComponent>();
            StatusEffects = enemy.GetComponent<StatusEffectComponent>();

            _baseSpeed       = enemy.GetEnemyData()?.Speed ?? (Movement != null ? Movement.movement_Speed : 1f);
            _speedMultiplier = 1f;
        }

        /// <summary>
        /// Clears per-instance blackboard state on pool reuse.
        /// Called by <see cref="BaseEnemy.OnSpawned"/>.
        /// </summary>
        internal void ResetForSpawn()
        {
            Blackboard.Clear();
            _speedMultiplier = 1f;
            ApplySpeed();
        }
    }
}
