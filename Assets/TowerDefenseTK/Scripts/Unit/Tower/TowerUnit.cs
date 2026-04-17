using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    [RequireComponent(typeof(HealthComponent))]
    public class TowerUnit : BaseUnit
    {
        [SerializeField] private TowerWeapon t_Weapon;
        [SerializeField] private TowerBase t_Base;
        public HealthComponent healthComponent;

        public TowerSO towerSO { get; private set; }

        /// <summary>
        /// Current attack range, factoring in any active TowerBuffComponent multipliers.
        /// Falls back to the base SO value when the weapon hasn't initialised yet.
        /// </summary>
        public float EffectiveRange =>
            t_Weapon != null ? t_Weapon.EffectiveRange
                             : (towerSO != null ? towerSO.range : 0f);

        protected override void Awake()
        {
            if (unitData is TowerSO)
                towerSO = (TowerSO)unitData;

            // Auto-add TowerBuffComponent if missing — existing prefabs created before
            // the requirement was added won't have it.  Must run BEFORE t_Weapon.Init()
            // because TowerWeapon caches the reference there.
            if (GetComponent<TowerBuffComponent>() == null)
                gameObject.AddComponent<TowerBuffComponent>();

            base.Awake();
            healthComponent = GetComponent<HealthComponent>();
            t_Weapon.Init(this);
        }

    }
}
