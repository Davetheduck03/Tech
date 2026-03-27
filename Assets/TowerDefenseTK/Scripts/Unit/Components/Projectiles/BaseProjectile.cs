using TowerDefenseTK;
using UnityEngine;

/// <summary>
/// Projectile fired by AoE (Turret_AOE) towers.
///
/// Two flight modes, selected via ProjectileConfig.useFixedTarget:
///
///   FIXED TARGET (useFixedTarget = true) — "predictive / fire-and-forget"
///     • Samples the target's world position at launch time.
///     • Flies a smooth ballistic arc to that point (arcHeight controls the peak).
///     • Continues flying and detonates even if the target dies mid-flight.
///     • Projectile faces its direction of travel throughout the arc.
///     • Recommended for slow-moving mortars / artillery with high arc.
///
///   LIVE TRACKING (useFixedTarget = false)
///     • Continuously steers toward the live target position.
///     • Falls back to the last known position if the target dies.
///     • No arc — straight-line chase like a missile.
///     • Recommended for fast projectiles or anti-air.
/// </summary>
public class BaseProjectile : MonoBehaviour, IPoolable
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private LayerMask enemyLayer;

    // ── Runtime state (set in Init) ───────────────────────────────────────────

    private float          p_Speed;
    private float          p_AOERadius;
    private ProjectileConfig p_Config;
    private BaseUnit       target;
    private TowerWeapon    parent;

    // Shared between both modes: last-known world position of the target.
    private Vector3 lastKnownPos;

    // Fixed-target / arc mode
    private Vector3 launchPos;
    private float   travelDuration; // seconds to reach target at given speed
    private float   elapsed;        // seconds since spawn

    // Lifetime failsafe
    private const float MaxLifetime = 5f;
    private float p_Timer;

    // ── IPoolable ─────────────────────────────────────────────────────────────

    public void OnSpawned()
    {
        p_Timer  = MaxLifetime;
        elapsed  = 0f;
    }

    public void OnDespawned() { }

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call immediately after spawning from the pool.
    /// </summary>
    public void Init(float speed, float aoeRadius, ProjectileConfig config,
                     BaseUnit target, TowerWeapon parent)
    {
        p_Speed     = speed;
        p_AOERadius = aoeRadius;
        p_Config    = config;
        this.target = target;
        this.parent = parent;

        // Sample target position now — used as fixed destination or fallback.
        lastKnownPos = target != null ? target.transform.position : transform.position;
        launchPos    = transform.position;

        if (config.useFixedTarget)
        {
            float dist    = Vector3.Distance(launchPos, lastKnownPos);
            travelDuration = speed > 0f ? dist / speed : 0.01f;
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        p_Timer -= Time.deltaTime;
        if (p_Timer <= 0f)
        {
            Despawn();
            return;
        }

        if (p_Config.useFixedTarget)
            FixedTargetUpdate();
        else
            LiveTrackingUpdate();
    }

    // ── Flight modes ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ballistic arc to a fixed world point sampled at launch.
    /// t goes 0→1 and drives both position and facing.
    /// </summary>
    private void FixedTargetUpdate()
    {
        elapsed += Time.deltaTime;
        float t = travelDuration > 0f ? Mathf.Clamp01(elapsed / travelDuration) : 1f;

        Vector3 prev = transform.position;

        // Horizontal lerp + vertical arc
        Vector3 flat = Vector3.Lerp(launchPos, lastKnownPos, t);
        float arcY   = p_Config.arcHeight * Mathf.Sin(t * Mathf.PI);
        transform.position = flat + Vector3.up * arcY;

        // Orient to face direction of travel
        Vector3 vel = transform.position - prev;
        if (vel.sqrMagnitude > 0.0001f)
            transform.forward = vel.normalized;

        if (t >= 1f)
            Impact();
    }

    /// <summary>
    /// Straight-line steering toward the live (or last-known) target position.
    /// </summary>
    private void LiveTrackingUpdate()
    {
        // Refresh last-known position while target is alive
        if (target != null && target.gameObject.activeInHierarchy)
            lastKnownPos = target.transform.position;

        // Face + move toward destination
        Vector3 dir = lastKnownPos - transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir.normalized;

        transform.position = Vector3.MoveTowards(
            transform.position, lastKnownPos, p_Speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, lastKnownPos) < 0.3f)
            Impact();
    }

    // ── Impact ────────────────────────────────────────────────────────────────

    private void Impact()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, p_AOERadius, enemyLayer);
        foreach (Collider hit in hits)
            parent.damageComponent.TryDealDamage(hit.gameObject);

        // TODO: spawn impact VFX from pool here

        Despawn();
    }

    private void Despawn()
    {
        PoolManager.Instance.Despawn(gameObject);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, p_AOERadius);

        if (p_Config.useFixedTarget && launchPos != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(launchPos, lastKnownPos);
        }
    }
}
