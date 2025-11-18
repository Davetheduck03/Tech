using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class TowerUnit : BaseUnit
    {
        [SerializeField] private TowerWeapon t_Weapon;
        [SerializeField] private TowerBase t_Base;
        public HealthComponent healthComponent;

        public TowerSO towerSO { get; private set; }


        protected override void Awake()
        {
            if (unitData is TowerSO)
            {
                towerSO = (TowerSO)unitData;
            }
            base.Awake();
            healthComponent = GetComponent<HealthComponent>();
            t_Weapon.Init(this);
        }

    }
}
