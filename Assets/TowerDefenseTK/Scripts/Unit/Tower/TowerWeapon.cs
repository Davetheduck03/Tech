using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class TowerWeapon : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private bool smoothRotation = true;

        [Header("Recoil Settings")]
        [SerializeField] private float recoilDistance = 0.3f;
        [SerializeField] private float recoilRecoverySpeed = 10f;

        private Vector3 originalLocalPosition;
        private float currentRecoil;

        [Header("Muzzle (Optional)")]
        [SerializeField] private Transform shootingPoint;

        private TowerUnit parentTower;

        private BaseEnemy currentTarget;
        private Vector3 targetDirection;
        private Quaternion targetRotation;
        public DamageComponent damageComponent;


        private float lastFireTime;
        private float targetUpdateTimer;

        // Support tower aura
        [Header("Support Tower")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("Layer mask for allied towers. Used by Buff-type support towers.\n" +
                 "Leave as Nothing to scan all layers (slower but zero setup required).")]
        [SerializeField] private LayerMask towerLayer;
        private float supportPulseTimer;
        private bool _towerLayerWarned; // suppress repeated console spam

        // Cached buff component on this tower (null if tower has none)
        private TowerBuffComponent buffComponent;

        // Resource (Miner) tower
        private float resourceTimer;

        // Custom behaviour context (created lazily when customBehaviour is assigned)
        private TowerBehaviourContext customCtx;

        public void Init(TowerUnit parent)
        {
            parentTower = parent;
            shootingPoint ??= transform;
            originalLocalPosition = transform.localPosition;
            buffComponent = parent.GetComponent<TowerBuffComponent>();

            // Build the context if a custom behaviour is assigned on this tower
            if (parent.towerSO.customBehaviour != null)
            {
                customCtx = new TowerBehaviourContext
                {
                    Tower           = parent,
                    Weapon          = this,
                    DamageComponent = GetComponent<DamageComponent>() ?? parent.GetComponentInChildren<DamageComponent>(),
                    ShootingPoint   = shootingPoint,
                    EnemyLayer      = enemyLayer,
                    EffectiveRange  = parent.towerSO.range,
                    EffectiveFireRate = parent.towerSO.fireRate,
                };
                parent.towerSO.customBehaviour.OnInit(customCtx);
            }
        }

        private void Update()
        {
            // Custom behaviour overrides the built-in enum dispatch entirely
            if (parentTower.towerSO.customBehaviour != null && customCtx != null)
            {
                // Refresh effective stats each frame so buff changes are reflected
                customCtx.EffectiveRange     = GetEffectiveRange();
                customCtx.EffectiveFireRate  = GetEffectiveFireRate();
                parentTower.towerSO.customBehaviour.Tick(customCtx);
                return;
            }

            switch (parentTower.towerSO.towerType)
            {
                case TowerType.Turret:
                    HandleRecoil();
                    TurretTick();
                    break;
                case TowerType.Support:
                    SupportTick();
                    break;
                case TowerType.AoE:
                    AOETick(); // HandleRecoil is called inside each AOE sub-method
                    break;
                case TowerType.Resource:
                    ResourceTick();
                    break;
            }
        }

        /// <summary>
        /// Passively generates gold every second based on goldPerSecond in the TowerSO.
        /// No targeting or enemies required — just place and profit.
        /// </summary>
        private void ResourceTick()
        {
            resourceTimer += Time.deltaTime;
            if (resourceTimer < 1f) return;

            resourceTimer -= 1f; // subtract rather than reset so fractional ticks don't drift

            float gps = parentTower.towerSO.goldPerSecond;
            if (gps > 0f && CurrencyManager.Instance != null)
                CurrencyManager.Instance.Add(Mathf.RoundToInt(gps));
        }

        #region AOE Func

        private void AOETick()
        {
            switch (parentTower.towerSO.AOEType)
            {
                case AOEType.Cone:
                    HandleRecoil();
                    ConeAOETick();
                    break;
                case AOEType.Circle:
                    HandleRecoil();
                    CircleAOETick();
                    break;
                case AOEType.Turret_AOE:
                    AOETurretTick();
                    break;
            }
        }

        public void AOETurretTick()
        {
            HandleRecoil();
            targetUpdateTimer += Time.deltaTime;
            if (targetUpdateTimer >= 0.2f)
            {
                targetUpdateTimer = 0f;
                UpdateTarget();
            }

            RotateToTarget();

            if (currentTarget != null &&
                Time.time - lastFireTime >= 1f / GetEffectiveFireRate())
            {
                ShootProjectile();
                TriggerRecoil();
                lastFireTime = Time.time;
            }
        }

        private void ShootProjectile()
        {
            if (currentTarget == null) return;

            GameObject projectileOBJ = PoolManager.Instance.Spawn(
                "Projectile", shootingPoint.position, Quaternion.identity);

            BaseProjectile projectile = projectileOBJ.GetComponent<BaseProjectile>();
            if (projectile == null) return;

            projectile.Init(
                parentTower.towerSO.projectileSpeed,
                parentTower.towerSO.AOE_Radius,
                parentTower.towerSO.projectileConfig,
                currentTarget,
                this);
        }

        /// <summary>
        /// Cone AOE — instant damage in a forward-facing arc, fired at the tower's fire rate.
        ///
        /// The tower rotates toward its current target so the cone is aimed.
        /// All enemies inside the cone's radius AND within half the coneAngle of the
        /// tower's forward direction are hit.
        ///
        /// Configure via TowerSO:  range = reach of the cone, coneAngle = opening angle.
        /// </summary>
        private void ConeAOETick()
        {
            targetUpdateTimer += Time.deltaTime;
            if (targetUpdateTimer >= 0.2f)
            {
                targetUpdateTimer = 0f;
                UpdateTarget();
            }

            RotateToTarget();

            if (currentTarget == null) return;
            if (Time.time - lastFireTime < 1f / GetEffectiveFireRate()) return;

            lastFireTime = Time.time;
            TriggerRecoil();

            float effectiveRange = GetEffectiveRange();
            float halfAngle      = parentTower.towerSO.coneAngle * 0.5f;
            Vector3 origin       = parentTower.transform.position;
            Vector3 forward      = transform.forward;

            if ((int)enemyLayer != 0)
            {
                Collider[] hits = Physics.OverlapSphere(origin, effectiveRange, enemyLayer);
                foreach (Collider hit in hits)
                {
                    Vector3 dir = (hit.transform.position - origin).normalized;
                    if (Vector3.Angle(forward, dir) <= halfAngle)
                        HitEnemy(hit.gameObject);
                }
            }
            else if (EnemyManager.Instance != null)
            {
                var enemies = EnemyManager.Instance.GetEnemiesInRange(
                    origin, effectiveRange, parentTower.towerSO.targetGroup);
                foreach (var enemy in enemies)
                {
                    Vector3 dir = (enemy.transform.position - origin).normalized;
                    if (Vector3.Angle(forward, dir) <= halfAngle)
                        HitEnemy(enemy.gameObject);
                }
            }
        }

        /// <summary>
        /// Circle AOE — instant 360° pulse around the tower at the tower's fire rate.
        /// Hits everything in range simultaneously.
        ///
        /// Falls back to EnemyManager when enemyLayer is "Nothing" so no layer setup is required.
        /// Configure via TowerSO:  range = pulse radius, fireRate = pulses per second.
        /// </summary>
        private void CircleAOETick()
        {
            float interval = GetEffectiveFireRate() > 0f ? 1f / GetEffectiveFireRate() : float.MaxValue;
            if (Time.time - lastFireTime < interval) return;

            lastFireTime = Time.time;
            TriggerRecoil();

            float effectiveRange = GetEffectiveRange();
            Vector3 origin       = parentTower.transform.position;

            if ((int)enemyLayer != 0)
            {
                Collider[] hits = Physics.OverlapSphere(origin, effectiveRange, enemyLayer);
                foreach (Collider hit in hits)
                    HitEnemy(hit.gameObject);
            }
            else if (EnemyManager.Instance != null)
            {
                var enemies = EnemyManager.Instance.GetEnemiesInRange(
                    origin, effectiveRange, parentTower.towerSO.targetGroup);
                foreach (var enemy in enemies)
                    HitEnemy(enemy.gameObject);
            }
        }

        #endregion

        /// <summary>
        /// Central hit handler for all AOE ticks.
        ///
        /// • If a DamageComponent is assigned, delegates fully to it (damage + on-hit effect).
        /// • If DamageComponent is missing (e.g. pure slow/debuff tower), applies the
        ///   TowerSO's statusEffect directly so no DamageComponent is required.
        /// </summary>
        private void HitEnemy(GameObject target)
        {
            if (damageComponent != null)
            {
                damageComponent.TryDealDamage(target);
            }
            else
            {
                // No DamageComponent — apply status effect directly (pure debuff tower)
                StatusEffectSO effect = parentTower.towerSO.statusEffect;
                if (effect != null)
                    target.GetComponent<StatusEffectComponent>()?.Apply(effect);
            }
        }

        /// <summary>
        /// Pulses every 0.5 s and handles two mutually exclusive support modes:
        ///
        /// ENEMY DEBUFF — if statusEffect is set on the TowerSO:
        ///   Uses EnemyManager (no layer-mask required) to find all live enemies in
        ///   range and applies the StatusEffectSO (Slow, Stun, DOT) to each one.
        ///
        /// ALLIED BUFF — if towerBuff is set on the TowerSO:
        ///   Physics.OverlapSphere with the towerLayer mask (falls back to all layers
        ///   when towerLayer is "Nothing") finds every collider in range; any that
        ///   has a TowerBuffComponent anywhere in its hierarchy is buffed.
        ///   The buff tower itself is excluded.
        ///
        /// Both fields can be set simultaneously (hybrid tower).
        /// </summary>
        private void SupportTick()
        {
            const float PulseInterval = 0.5f;

            supportPulseTimer += Time.deltaTime;
            if (supportPulseTimer < PulseInterval) return;
            supportPulseTimer = 0f;

            float effectiveRange = GetEffectiveRange();
            Vector3 origin = parentTower.transform.position;

            // ── Enemy debuff aura ─────────────────────────────────────────────
            StatusEffectSO effect = parentTower.towerSO.statusEffect;
            if (effect != null && EnemyManager.Instance != null)
            {
                var enemies = EnemyManager.Instance.GetEnemiesInRange(
                    origin, effectiveRange, TargetGroup.Both);

                foreach (var enemy in enemies)
                    enemy.GetComponent<StatusEffectComponent>()?.Apply(effect);
            }

            // ── Allied tower buff aura ────────────────────────────────────────
            TowerBuffSO buff = parentTower.towerSO.towerBuff;
            if (buff != null)
            {
                // When towerLayer is "Nothing" (value 0), fall back to all layers
                // so the buff works without any Inspector setup.
                int queryMask = (int)towerLayer != 0 ? (int)towerLayer : ~0;

                if ((int)towerLayer == 0 && !_towerLayerWarned)
                {
                    _towerLayerWarned = true;
                    Debug.LogWarning(
                        $"[TowerWeapon] '{parentTower.name}': towerLayer is not set. " +
                        "Scanning all layers — assign a dedicated tower layer for better performance.",
                        this);
                }

                Collider[] towerHits = Physics.OverlapSphere(origin, effectiveRange, queryMask);

                foreach (var hit in towerHits)
                {
                    // Don't buff this tower itself
                    if (hit.transform.IsChildOf(parentTower.transform) ||
                        hit.gameObject == parentTower.gameObject) continue;

                    // GetComponentInParent handles colliders on child mesh objects
                    hit.GetComponentInParent<TowerBuffComponent>()?.ApplyBuff(buff);
                }
            }
        }

        #region Buff Helpers

        /// <summary>
        /// Effective range factoring in any active TowerBuffComponent multiplier.
        /// Falls back to the base SO value when no buff is present.
        /// </summary>
        private float GetEffectiveRange() =>
            parentTower.towerSO.range * (buffComponent != null ? buffComponent.RangeMultiplier : 1f);

        /// <summary>Public read-only access to the effective range (used by UI / range indicators).</summary>
        public float EffectiveRange => GetEffectiveRange();

        /// <summary>
        /// Effective fire rate factoring in any active TowerBuffComponent multiplier.
        /// </summary>
        private float GetEffectiveFireRate() =>
            parentTower.towerSO.fireRate * (buffComponent != null ? buffComponent.FireRateMultiplier : 1f);

        #endregion

        #region Recoil

        private void HandleRecoil()
        {
            currentRecoil = Mathf.Lerp(currentRecoil, 0f, recoilRecoverySpeed * Time.deltaTime);

            transform.localPosition = originalLocalPosition - transform.localRotation * Vector3.forward * currentRecoil;
        }

        private void TriggerRecoil()
        {
            currentRecoil = recoilDistance;
        }

        #endregion


        #region Turret Func

        private void UpdateTarget()
        {
            float effectiveRange = GetEffectiveRange();

            var newTarget = EnemyManager.Instance.GetTarget(
                transform.position,
                effectiveRange,
                parentTower.towerSO.towerTargetType,
                parentTower.towerSO.targetGroup
            );

            if (currentTarget != null &&
                currentTarget.gameObject.activeInHierarchy &&
                Vector3.Distance(currentTarget.transform.position, transform.position) <= effectiveRange)
            {
                return;
            }

            currentTarget = newTarget;

            if (currentTarget != null)
            {
                Debug.Log($"New target: {currentTarget.name} ({parentTower.towerSO.towerTargetType})");
                UpdateRotationTarget();
            }
            else
            {
                targetDirection = transform.forward;
            }
        }



        private void TurretTick()
        {
            targetUpdateTimer += Time.deltaTime;
            if (targetUpdateTimer >= 0.2f)
            {
                targetUpdateTimer = 0f;
                UpdateTarget();
            }

            RotateToTarget();

            if (currentTarget != null &&
                Time.time - lastFireTime >= 1f / GetEffectiveFireRate())
            {
                ShootInstant();
                lastFireTime = Time.time;
            }
        }

        private void RotateToTarget()
        {
            if (currentTarget == null) return;

            UpdateRotationTarget();


            if (smoothRotation)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            else
            {
                transform.rotation = targetRotation;
            }
        }

        private void UpdateRotationTarget()
        {
            if (currentTarget == null) return;

            targetDirection = (currentTarget.transform.position - transform.position).normalized;
            targetDirection.y = 0;

            targetRotation = Quaternion.LookRotation(targetDirection);
        }

        private void ShootInstant()
        {
            if (currentTarget == null) return;
            damageComponent.TryDealDamage(currentTarget.gameObject);
            Debug.DrawLine(transform.position, currentTarget.transform.position, Color.red, 0.1f);
            TriggerRecoil();
        }

        private void OnDrawGizmosSelected()
        {
            if (parentTower == null || parentTower.towerSO == null) return;

            float gizmoRange = GetEffectiveRange();

            // Yellow = range sphere; green tint when buffed
            Gizmos.color = (buffComponent != null && buffComponent.IsBuffed) ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gizmoRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, targetDirection * gizmoRange);
        }

        private void OnDrawGizmos()
        {
            if (parentTower == null)
                parentTower = GetComponentInParent<TowerUnit>();
            if (parentTower != null && parentTower.towerSO != null)
            {
                Gizmos.color = Color.yellow * 0.3f;
                Gizmos.DrawWireSphere(transform.position, parentTower.towerSO.range);
            }
        }
        #endregion

    }
}