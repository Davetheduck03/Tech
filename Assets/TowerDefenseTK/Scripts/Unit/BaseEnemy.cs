using System.Collections;
using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class BaseEnemy : BaseUnit, IPoolable
{
    [HideInInspector] public int nodesPassed;
    [HideInInspector] public int totalPathNodes;
    [HideInInspector] public bool isFlying;

    [Header("Parts")]
    [SerializeField] private EnemyBody e_Body;
    [SerializeField] private EnemyWeapon e_Weapon;

    // ── Custom Behaviour ──────────────────────────────────────────────────────
    private EnemyBehaviorSO      _behavior;
    private EnemyBehaviorContext _behaviorContext;

    /// <summary>
    /// Read-only access to the active behavior context.
    /// Returns null when no custom behavior is assigned.
    /// </summary>
    public EnemyBehaviorContext BehaviorContext => _behaviorContext;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        EnemySO data = GetEnemyData();
        if (data != null)
        {
            isFlying = data.isFlying;

            // Build behavior context once (reused across pool cycles)
            if (data.customBehavior != null)
            {
                _behavior        = data.customBehavior;
                _behaviorContext = new EnemyBehaviorContext();
                _behaviorContext.Refresh(this);
                _behavior.OnInit(_behaviorContext);
            }
        }

        // Subscribe to the death event to forward it to the behavior
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
        EnemyManager.Instance.RegisterEnemy(this);

        if (_behavior != null && _behaviorContext != null)
        {
            _behaviorContext.ResetForSpawn();
            _behavior.OnSpawned(_behaviorContext);
        }
    }

    public void OnDespawned()
    {
        EnemyManager.Instance.UnregisterEnemy(this);
    }

    // ── Exit trigger ──────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider collision)
    {
        if (!collision.gameObject.CompareTag("End")) return;

        // Notify behavior before damage is applied so it can modify damageToBase
        _behavior?.OnReachExit(_behaviorContext);

        if (PlayerHealthManager.Instance != null)
        {
            EnemySO enemyData = GetEnemyData();
            int damage = enemyData != null ? enemyData.damageToBase : 1;
            PlayerHealthManager.Instance.TakeDamage(damage);
            Debug.Log($"Enemy '{name}' reached exit! Player takes {damage} damage.");
        }

        PoolManager.Instance.Despawn(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Get the EnemySO data for this enemy.
    /// </summary>
    public EnemySO GetEnemyData() => unitData as EnemySO;

    // ── Death forwarding ──────────────────────────────────────────────────────

    private void HandleEnemyDied(GameObject deadGO, DamageComponent killer)
    {
        // Only respond to our own death event
        if (deadGO != gameObject) return;
        _behavior?.OnDeath(_behaviorContext);
    }
}
