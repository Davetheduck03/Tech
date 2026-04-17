using UnityEngine;

namespace TowerDefenseTK
{
    [RequireComponent(typeof(StatusEffectComponent))]
    public class BaseEnemy : BaseUnit, IPoolable
    {
        [HideInInspector] public int nodesPassed;
        [HideInInspector] public int totalPathNodes;
        [HideInInspector] public bool isFlying;

        [Header("Parts")]
        [SerializeField] private EnemyBody e_Body;
        [SerializeField] private EnemyWeapon e_Weapon;

        [Header("Exit Detection")]
        [Tooltip("Tag on the Exit zone collider. Must match the tag set in your scene.")]
        [SerializeField] private string exitTag = "End";

        // ── Custom Behaviour ──────────────────────────────────────────────────────
        private EnemyBehaviorSO      _behavior;
        private EnemyBehaviorContext _behaviorContext;

        /// <summary>Read-only access to the active behavior context. Null when no custom behavior is assigned.</summary>
        public EnemyBehaviorContext BehaviorContext => _behaviorContext;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            // Guarantee StatusEffectComponent exists before base.Awake() runs so
            // MovementComponent.OnInitialize() always finds and caches it correctly.
            // Required for slow/stun/DOT to work on any enemy regardless of movement system.
            if (GetComponent<StatusEffectComponent>() == null)
                gameObject.AddComponent<StatusEffectComponent>();

            base.Awake();

            EnemySO data = GetEnemyData();
            if (data != null)
            {
                isFlying = data.isFlying;

                if (data.customBehavior != null)
                {
                    _behavior        = data.customBehavior;
                    _behaviorContext = new EnemyBehaviorContext();
                    _behaviorContext.Refresh(this);
                    _behavior.OnInit(_behaviorContext);
                }
            }

            HealthComponent.OnEnemyDied += HandleEnemyDied;
            e_Weapon?.Init(this);
        }

        private void OnDestroy()
        {
            HealthComponent.OnEnemyDied -= HandleEnemyDied;
        }

        private void Update()
        {
            if (_behavior == null || _behaviorContext == null) return;
            _behavior.Tick(_behaviorContext);
        }

        // ── Pool callbacks ────────────────────────────────────────────────────────

        public void OnSpawned()
        {
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.RegisterEnemy(this);

            if (_behavior != null && _behaviorContext != null)
            {
                _behaviorContext.ResetForSpawn();
                _behavior.OnSpawned(_behaviorContext);
            }
        }

        public void OnDespawned()
        {
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.UnregisterEnemy(this);
        }

        // ── Exit trigger ──────────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.gameObject.CompareTag(exitTag)) return;

            _behavior?.OnReachExit(_behaviorContext);

            if (PlayerHealthManager.Instance != null)
            {
                EnemySO enemyData = GetEnemyData();
                int damage = enemyData != null ? enemyData.damageToBase : 1;
                PlayerHealthManager.Instance.TakeDamage(damage);
                Debug.Log($"Enemy '{name}' reached exit! Player takes {damage} damage.");
            }

            if (PoolManager.Instance != null)
                PoolManager.Instance.Despawn(gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Get the EnemySO data for this enemy.</summary>
        public EnemySO GetEnemyData() => unitData as EnemySO;

        // ── Death forwarding ──────────────────────────────────────────────────────

        private void HandleEnemyDied(GameObject deadGO, DamageComponent killer)
        {
            if (deadGO != gameObject) return;
            _behavior?.OnDeath(_behaviorContext);
        }
    }
}
