using System;
using TowerDefenseTK;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BaseProjectile : MonoBehaviour, IPoolable
{
    private float p_Speed;
    private float p_AOERadius;
    [SerializeField] private LayerMask enemyLayer;
    private readonly float p_Lifetime = 3f;
    private float p_Timer;
    private float updateTimer = 0.2f;
    private BaseUnit target;
    private TowerWeapon parent;

    public void OnDespawned()
    {
        
    }

    public void OnSpawned()
    {
        p_Timer = p_Lifetime;
        Debug.Log("projectile spawned");
    }

    public void Init(float speed, float AOE, BaseUnit target, TowerWeapon parent)
    {
        p_Speed = speed;
        p_AOERadius = AOE;
        this.target = target;
        this.parent = parent;
    }

    private void Update()
    {
        MoveTowards(target.transform.position);

        if (Vector3.Distance(transform.position, target.transform.position) < 0.3f)
            Impact();

        p_Timer -= Time.deltaTime;
        if (p_Timer <= 0f)
            Despawn();
    }

    private void MoveTowards(Vector3 pos)
    {
        transform.position = Vector3.MoveTowards(transform.position, pos, p_Speed * Time.deltaTime);
    }

    private void Impact()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, p_AOERadius, enemyLayer);
        foreach (Collider hit in hits)
        {
            parent.damageComponent.TryDealDamage(hit.gameObject);
        }

        // TODO: spawn impact effect (also from pool)

        Despawn();
    }

    private void Despawn()
    {
        PoolManager.Instance.Despawn(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, p_AOERadius);
    }
}


