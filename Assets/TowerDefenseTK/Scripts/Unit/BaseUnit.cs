using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUnit : MonoBehaviour
{
    [SerializeField] protected UnitSO unitData;

    private List<UnitComponent> components = new List<UnitComponent>();

    protected virtual void Awake()
    {
        // Get all components that inherit from UnitComponent
        GetComponents(components);

        foreach (var comp in components)
        {
            comp.Setup(this, unitData);
        }
    }
}
