using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class BaseTower : BaseUnit
    {
        public TowerSO towerSO;
        [SerializeField] private TowerWeapon t_Weapon;
        [SerializeField] private TowerBase t_Base;


        protected override void Awake()
        {
            if (unitData is TowerSO)
            {
                towerSO = (TowerSO)unitData;
            }
            base.Awake();
            t_Weapon.Init();
        }

    }
}
