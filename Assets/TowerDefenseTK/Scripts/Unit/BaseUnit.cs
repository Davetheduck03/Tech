using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class BaseUnit : MonoBehaviour
    {
        [SerializeField] protected UnitSO unitData;

        private List<UnitComponent> components = new List<UnitComponent>();

        protected virtual void Awake()
        {
            GetComponents(components);
            foreach (var comp in components)
                comp.Setup(this, unitData);
        }
    }
}
