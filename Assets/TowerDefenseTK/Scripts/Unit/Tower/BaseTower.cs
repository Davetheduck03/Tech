using System.Collections;
using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class BaseTower : BaseUnit
{
    private TowerSO towerSO;
    

    protected override void Awake()
    {
        if( unitData is TowerSO)
        {
            towerSO = (TowerSO)unitData;
        }
        base.Awake();
    }

}
