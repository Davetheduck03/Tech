using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BaseProjectile : MonoBehaviour, IPoolable
{
    private float p_Speed;
    private float p_Damage;
    private float p_AOERadius;
    [SerializeField] private LayerMask enemyLayer;
    private float p_Lifetime;
    private float p_Timer;


    public void OnDespawned()
    {
        
    }

    public void OnSpawned()
    {
        
    }

    //private void Update()
    //{
    //    if (target == null)
    //    {
    //        Despawn();
    //        return;
    //    }

    //    // Move toward target
    //    Vector3 dir = (target.transform.position - transform.position).normalized;
    //    transform.position += dir * speed * Time.deltaTime;

    //    // Hit check
    //    if (Vector3.Distance(transform.position, target.transform.position) < 0.3f)
    //    {
    //        Impact();
    //    }

    //    // Expire
    //    lifeTimer += Time.deltaTime;
    //    if (lifeTimer >= lifeTime)
    //    {
    //        Despawn();
    //    }
    //}

    //private void Impact()
    //{
    //    // AOE Damage
    //    Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, enemyLayer);
    //    foreach (Collider hit in hits)
    //    {
    //        damageComp.TryDealDamage(hit.gameObject);
    //    }

    //    // TODO: spawn impact effect (also from pool)

    //    Despawn();
    //}

    private void Despawn()
    {
        PoolManager.Instance.Despawn(gameObject);
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, aoeRadius);
    //}
}


