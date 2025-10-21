using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DamageModifierRow
{
    public DamageType damageRow;
    public List<float> multipliers = new List<float>();
}



[CreateAssetMenu(fileName = "DamageTable", menuName = "TD Toolkit/Damage Table")]
public class DamageTable : ScriptableObject
{
    public List<DamageType> types = new List<DamageType>();
    public List<DamageModifierRow> rows = new List<DamageModifierRow>();

    public float GetMultiplier(DamageType attackType, DamageType defenseType)
    {
        int rowIndex = types.IndexOf(attackType);
        int colIndex = types.IndexOf(defenseType);

        if (rowIndex < 0 || colIndex < 0) return 1f;
        return rows[rowIndex].multipliers[colIndex];
    }

    public void InitializeTable()
    {
        rows.Clear();
        foreach (var type in types)
        {
            var row = new DamageModifierRow();
            row.damageRow = type;

            for (int i = 0; i < types.Count; i++)
                row.multipliers.Add(1f);

            rows.Add(row);
        }
    }
}
