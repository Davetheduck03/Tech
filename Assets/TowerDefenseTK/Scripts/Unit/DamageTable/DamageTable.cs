using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DefenseRow
{
    public DefenseType defenseType;
    public List<float> multipliers = new List<float>();
}

[CreateAssetMenu(fileName = "DamageTable", menuName = "TD Toolkit/Damage Table")]
public class DamageTable : ScriptableObject
{
    [Header("Damage Type Lists")]
    public List<DamageType> attackTypes = new List<DamageType>();
    public List<DefenseType> defenseTypes = new List<DefenseType>();

    [Header("Table Data")]
    public List<DefenseRow> rows = new List<DefenseRow>();

    /// <summary>
    /// Returns the multiplier for a given attack and defense pairing.
    /// </summary>
    public float GetMultiplier(DamageType attackType, DefenseType defenseType)
    {
        int rowIndex = defenseTypes.IndexOf(defenseType);
        int colIndex = attackTypes.IndexOf(attackType);

        if (rowIndex < 0 || colIndex < 0)
            return 1f;

        return rows[rowIndex].multipliers[colIndex];
    }

    /// <summary>
    /// Initializes or rebuilds the table structure.
    /// </summary>
    public void InitializeTable()
    {
        rows.Clear();

        foreach (var defense in defenseTypes)
        {
            var row = new DefenseRow();
            row.defenseType = defense;

            // Initialize one multiplier per attack type
            for (int i = 0; i < attackTypes.Count; i++)
                row.multipliers.Add(1f);

            rows.Add(row);
        }
    }
}
