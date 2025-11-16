using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class TowerWeapon : MonoBehaviour
    {
        private TowerSO towerData;


        public void Init()
        {
            towerData = gameObject.GetComponentInParent<BaseTower>().towerSO;
        }

        private void Update()
        {
            Target(towerData.FindTarget(transform.position, towerData.range));
        }

        private void Target(BaseEnemy currentEnemy)
        {
            gameObject.transform.LookAt(currentEnemy.transform);
        }

        private void Shoot()
        {

        }
    }
}
