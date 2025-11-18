using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class TowerUnit : BaseUnit
    {
        [HideInInspector] public TowerSO towerSO;
        [SerializeField] private TowerWeapon t_Weapon;
        [SerializeField] private TowerBase t_Base;
        public DamageComponent damageComponent;
        public HealthComponent healthComponent;


        protected override void Awake()
        {
            if (unitData is TowerSO)
            {
                towerSO = (TowerSO)unitData;
            }
            base.Awake();
            damageComponent = GetComponent<DamageComponent>();
            healthComponent = GetComponent<HealthComponent>();
            t_Weapon.Init();
        }

    }
}
