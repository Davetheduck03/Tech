using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class TowerWeapon : MonoBehaviour
    {
        [Header("Tower Data")]
        [SerializeField] private TowerSO towerData;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private bool smoothRotation = true;

        [Header("Muzzle (Optional)")]
        [SerializeField] private Transform shootingPoint;

        private TowerUnit parentTower;
        private BaseEnemy currentTarget;
        private float lastFireTime;
        private float targetUpdateTimer;

        private Vector3 targetDirection;
        private Quaternion targetRotation;

        public DamageComponent damageComponent;
        public HealthComponent healthComponent;

        public void Init()
        {
            parentTower = GetComponentInParent<TowerUnit>();
            towerData ??= parentTower.towerSO;
            damageComponent = parentTower.damageComponent;
            healthComponent = parentTower.healthComponent;

            if (shootingPoint == null)
                shootingPoint = transform.Find("ShootingPoint") ?? transform;
        }

        private void Update()
        {
            switch (towerData.towerType)
            {
                case TowerType.Turret:
                    TurretTick();
                    break;
                case TowerType.Support:
                    SupportTick();
                    break;
                case TowerType.AoE:
                    AOETick();
                    break;
                case TowerType.Resource:
                    ResourceTick();
                    break;
            }
        }

        private void ResourceTick()
        {
            throw new NotImplementedException();
        }

        #region AOE Func

        private void AOETick()
        {
            switch (towerData.AOEType)
            {
                case AOEType.Cone:

                    break;
                case AOEType.Circle:

                    break;
                case AOEType.Turret_AOE:
                    
                    break;
            }

        }

        public void AOETurretTick()
        {
            targetUpdateTimer += Time.deltaTime;
            if (targetUpdateTimer >= 0.2f)
            {
                targetUpdateTimer = 0f;
                UpdateTarget();
            }

            RotateToTarget();

            if (currentTarget != null &&
                Time.time - lastFireTime >= 1f / towerData.fireRate)
            {
                ShootProjectile();
                lastFireTime = Time.time;
            }
        }

        private void ShootProjectile()
        {
            if(currentTarget == null) return;

        }

        #endregion

        private void SupportTick()
        {
            throw new NotImplementedException();
        }



        #region Turret Func

        private void UpdateTarget()
        {
            var newTarget = EnemyManager.Instance.GetTarget(
                transform.position,
                towerData.range,
                towerData.towerTargetType,
                towerData.targetGroup
            );

            if (currentTarget != null &&
                currentTarget.gameObject.activeInHierarchy &&
                Vector3.Distance(currentTarget.transform.position, transform.position) <= towerData.range)
            {
                return;
            }

            currentTarget = newTarget;

            if (currentTarget != null)
            {
                Debug.Log($"New target: {currentTarget.name} ({towerData.towerTargetType})");
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
                Time.time - lastFireTime >= 1f / towerData.fireRate)
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
        }



        private void OnDrawGizmosSelected()
        {
            if (towerData == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, towerData.range);

            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, targetDirection * towerData.range);
        }

        private void OnDrawGizmos()
        {
            if (towerData != null)
            {
                Gizmos.color = Color.yellow * 0.3f;
                Gizmos.DrawWireSphere(transform.position, towerData.range);
            }
        }
        #endregion

    }
}