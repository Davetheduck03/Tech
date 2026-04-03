using UnityEngine;
using TowerDefenseTK;

[CreateAssetMenu(menuName = "TD Toolkit/Behaviours/Chain Lightning")]
public class ChainLightningBehaviour : TowerBehaviourSO
{
    public int chainCount = 3;
    public float chainRadius = 3f;

    public override void Tick(TowerBehaviourContext ctx)
    {
        ctx.FindTarget();
        if (ctx.Target == null || !ctx.CanFire()) return;

        // Hit the primary target
        ctx.DealDamageTo(ctx.Target.gameObject);

        // Chain to nearby enemies
        Collider[] nearby = Physics.OverlapSphere(
            ctx.Target.transform.position, chainRadius, ctx.EnemyLayer);

        int chained = 0;
        foreach (var hit in nearby)
        {
            if (chained >= chainCount) break;
            if (hit.gameObject == ctx.Target.gameObject) continue;
            ctx.DealDamageTo(hit.gameObject);
            chained++;
        }

        ctx.RegisterFire();
    }
}