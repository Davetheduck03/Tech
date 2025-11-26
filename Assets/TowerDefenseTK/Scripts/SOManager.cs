using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOManager")]
public class SOManager : ScriptableObject
{
    private static SOManager instance;
    public static SOManager Instance
    {
        get
        {
            if (instance == null)
                return instance = Resources.Load<SOManager>("Data/SOManager");
            return instance;
        }
    }

    public DamageTable DamageTable;

    public List<TowerSO> towers;
}
