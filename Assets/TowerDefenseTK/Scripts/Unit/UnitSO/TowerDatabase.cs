using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerDatabase", menuName = "TD Toolkit/Database/Tower Database")]
public class TowerDatabase : ScriptableObject
{
    public List<TowerSO> towers;
}
