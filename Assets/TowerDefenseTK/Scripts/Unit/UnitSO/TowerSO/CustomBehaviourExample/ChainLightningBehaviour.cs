using System.Collections.Generic;
using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Chain-lightning tower behaviour.
///
/// • Rotates the weapon head toward the primary target every frame.
/// • Drops the current target the moment it walks out of effective range (fixes
///   the "shooting past the range indicator" bug).
/// • On fire: hits up to (1 + chainCount) enemies — the primary target plus
///   up to chainCount nearest unchained enemies within chainRadius of each link.
/// • Draws a jagged blue arc from the muzzle through every chained enemy.
/// </summary>
[CreateAssetMenu(menuName = "TD Toolkit/Behaviours/Chain Lightning")]
public class ChainLightningBehaviour : TowerBehaviourSO
{
    [Header("Chain Settings")]
    [Tooltip("Number of bounce targets after the primary (total hits = chainCount + 1).")]
    public int   chainCount  = 4;

    [Tooltip("Max distance from the current node to the next chained enemy.")]
    public float chainRadius = 4f;

    [Header("Rotation")]
    [Tooltip("Degrees per second the weapon rotates toward its target.")]
    public float rotationSpeed = 12f;

    [Header("VFX")]
    [Tooltip("How long the lightning arc stays visible (seconds).")]
    public float arcDuration = 0.15f;

    [Tooltip("Jagged subdivisions per bolt segment.")]
    public int segmentsPerLink = 8;

    [Tooltip("Max sideways jitter per segment (world units).")]
    public float displacement = 0.35f;

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick(TowerBehaviourContext ctx)
    {
        // ── Target management ─────────────────────────────────────────────────
        // Drop the target if it walked out of range (fixes range-overshoot bug)
        if (ctx.Target != null)
        {
            bool outOfRange = Vector3.Distance(
                ctx.Tower.transform.position,
                ctx.Target.transform.position) > ctx.EffectiveRange;

            if (outOfRange || !ctx.Target.gameObject.activeInHierarchy)
                ctx.Target = null;
        }

        if (ctx.Target == null)
            ctx.FindTarget();

        // ── Rotation — runs every frame even between shots ────────────────────
        RotateWeapon(ctx);

        // ── Fire ──────────────────────────────────────────────────────────────
        if (ctx.Target == null || !ctx.CanFire()) return;

        List<BaseEnemy> chain = BuildChain(ctx);

        // Arc nodes: muzzle → enemy1 → enemy2 → …
        var nodes = new List<Vector3> { ctx.ShootingPoint.position };

        foreach (var e in chain)
        {
            ctx.DealDamageTo(e.gameObject);
            // Lift slightly so the arc passes through torso rather than ground
            nodes.Add(e.transform.position + Vector3.up * 0.6f);
        }

        // Spawn the VFX — one continuous jagged arc through all nodes
        if (nodes.Count >= 2)
        {
            LightningVFX.Spawn(
                nodes,
                arcDuration,
                new Color(0.3f, 0.65f, 1f),
                segmentsPerLink,
                displacement);
        }

        ctx.RegisterFire();
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    private void RotateWeapon(TowerBehaviourContext ctx)
    {
        if (ctx.Target == null) return;

        Transform weapon = ctx.Weapon.transform;
        Vector3 dir = ctx.Target.transform.position - weapon.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        weapon.rotation = Quaternion.Slerp(
            weapon.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    // ── Chain builder ─────────────────────────────────────────────────────────

    private List<BaseEnemy> BuildChain(TowerBehaviourContext ctx)
    {
        var chain  = new List<BaseEnemy>();
        var visited = new HashSet<int>();

        chain.Add(ctx.Target);
        visited.Add(ctx.Target.GetInstanceID());

        Vector3 currentPos = ctx.Target.transform.position;

        for (int i = 0; i < chainCount; i++)
        {
            BaseEnemy next = FindNearest(currentPos, chainRadius, visited, ctx);
            if (next == null) break;

            chain.Add(next);
            visited.Add(next.GetInstanceID());
            currentPos = next.transform.position;
        }

        return chain;
    }

    private static BaseEnemy FindNearest(
        Vector3 origin, float radius, HashSet<int> visited, TowerBehaviourContext ctx)
    {
        BaseEnemy best     = null;
        float     bestDist = float.MaxValue;

        if ((int)ctx.EnemyLayer != 0)
        {
            Collider[] hits = Physics.OverlapSphere(origin, radius, ctx.EnemyLayer);
            foreach (var col in hits)
            {
                BaseEnemy e = col.GetComponentInParent<BaseEnemy>();
                if (e == null || visited.Contains(e.GetInstanceID())) continue;
                float d = Vector3.Distance(origin, e.transform.position);
                if (d < bestDist) { bestDist = d; best = e; }
            }
        }
        else if (EnemyManager.Instance != null)
        {
            var enemies = EnemyManager.Instance.GetEnemiesInRange(
                origin, radius, ctx.Data.targetGroup);
            foreach (var e in enemies)
            {
                if (visited.Contains(e.GetInstanceID())) continue;
                float d = Vector3.Distance(origin, e.transform.position);
                if (d < bestDist) { bestDist = d; best = e; }
            }
        }

        return best;
    }
}
