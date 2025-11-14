using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class TowerWeapon : MonoBehaviour
    {
        private TowerSO towerData;
        private BaseEnemy currentEnemy;


        private void Awake()
        {
            towerData = gameObject.GetComponent<TowerSO>();
        }

        private void Update()
        {
             currentEnemy = towerData.FindTarget(transform.position, towerData.range);
             
        }
    }
}
