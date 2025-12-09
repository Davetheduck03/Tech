using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    [CreateAssetMenu(fileName = "TowerDatabase", menuName = "TD Toolkit/Database/Tower Database")]
    public class TowerDatabase : ScriptableObject
    {
        public List<TowerSO> towers;
    }
}
