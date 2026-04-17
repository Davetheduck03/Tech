using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Handles tower-targeting and attacking for enemies.
    /// Place this on the Weapon child of an enemy prefab — mirrors the role of TowerWeapon.
    /// Only activates when canAttackTowers is true on the enemy's EnemySO.
    /// </summary>
    public class EnemyWeapon : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private bool smoothRotation = true;

        [Header("Tower Layer")]
        [Tooltip("Set this to the layer your tower GameObjects are on.")]
        [SerializeField] private LayerMask towerLayer;

        [Header("References")]
        [Tooltip("Assign the DamageComponent on the root enemy GameObject.")]
        public DamageComponent damageComponent;

        private BaseEnemy parentEnemy;
        private EnemySO enemyData;
        private bool canAttack;

        private TowerUnit currentTarget;
        private Vector3 targetDirection;
        private Quaternion targetRotation;

        private float lastFireTime;
        private float targetUpdateTimer;

        public void Init(BaseEnemy parent)
        {
            parentEnemy = parent;
            enemyData   = parent.GetEnemyData();
            canAttack   = enemyData != null && enemyData.canAttackTowers;

            targetDirection = transform.forward;
            targetRotation  = transform.rotation;
        }

        private void Update()
        {
            if (!canAttack || enemyData == null) return;

            targetUpdateTimer += Time.deltaTime;
            if (targetUpdateTimer >= 0.2f)
            {
                targetUpdateTimer = 0f;
                UpdateTarget();
            }

            RotateToTarget();

            if (currentTarget != null &&
                Time.time - lastFireTime >= 1f / enemyData.attackRate)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }

        private void UpdateTarget()
        {
            if (currentTarget != null &&
                currentTarget.gameObject.activeInHierarchy &&
                Vector3.Distance(currentTarget.transform.position, transform.position) <= enemyData.attackRange)
            {
                return;
            }

            currentTarget = null;

            Collider[] hits = Physics.OverlapSphere(transform.position, enemyData.attackRange, towerLayer);
            float closestSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                TowerUnit tower = hit.GetComponent<TowerUnit>();
                if (tower == null) continue;

                float distSqr = Vector3.SqrMagnitude(transform.position - hit.transform.position);
                if (distSqr < closestSqr)
                {
                    closestSqr    = distSqr;
                    currentTarget = tower;
                }
            }

            if (currentTarget != null)
                UpdateRotationTarget();
            else
                targetDirection = transform.forward;
        }

        private void RotateToTarget()
        {
            if (currentTarget == null) return;

            UpdateRotationTarget();

            if (smoothRotation)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            else
                transform.rotation = targetRotation;
        }

        private void UpdateRotationTarget()
        {
            if (currentTarget == null) return;
            targetDirection   = (currentTarget.transform.position - transform.position).normalized;
            targetDirection.y = 0f;
            targetRotation    = Quaternion.LookRotation(targetDirection);
        }

        private void Fire()
        {
            if (currentTarget == null || damageComponent == null) return;
            damageComponent.TryDealDamage(currentTarget.gameObject);
            Debug.DrawLine(transform.position, currentTarget.transform.position, Color.yellow, 0.15f);
        }

        private void OnDrawGizmosSelected()
        {
            if (enemyData == null) return;
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }

        private void OnDrawGizmos()
        {
            if (parentEnemy == null)
                parentEnemy = GetComponentInParent<BaseEnemy>();

            if (enemyData == null && parentEnemy != null)
                enemyData = parentEnemy.GetEnemyData();

            if (enemyData != null && enemyData.canAttackTowers)
            {
                Gizmos.color = Color.orange * 0.3f;
                Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
            }
        }
    }
}
