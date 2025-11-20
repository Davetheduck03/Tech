using UnityEngine;

namespace TowerDefenseTK
{

    public class EnemySpawner : MonoBehaviour
    {
        public GameObject enemy;

        void Start()
        {
            var customPath = this.gameObject.GetComponent<CustomPathHandler>();
            PoolManager.Instance.Spawn("Basic Enemy", this.transform.position, Quaternion.identity);
            Debug.Log("Enemy spawned from pool");
            enemy.GetComponent<MovementComponent>().targetTransform = customPath.endGoal;
        }
    }
}
