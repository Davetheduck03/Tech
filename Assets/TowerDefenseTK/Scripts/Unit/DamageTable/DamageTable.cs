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

    [Header("Table Data")]
    public List<DefenseRow> rows = new List<DefenseRow>();

    /// <summary>
    /// Returns the multiplier for a given attack and defense pairing.
    /// </summary>
    public float GetMultiplier(DamageType attackType, DefenseType defenseType)
    {
        var row = rows.Find(r => r.defenseType == defenseType);
        int colIndex = attackTypes.IndexOf(attackType);

        if (row == null || colIndex < 0)
            return 1f; // default multiplier if not found

        return row.multipliers[colIndex];
    }

    /// <summary>
    /// Ensures all rows are aligned with the attack type list.
    /// Adds missing multipliers and trims extras if necessary.
    /// </summary>
    public void InitializeTable()
    {
        foreach (var row in rows)
        {
            // Add multipliers if there are fewer than attack types
            while (row.multipliers.Count < attackTypes.Count)
                row.multipliers.Add(1f);

            // Trim extra multipliers if attackTypes list shrank
            while (row.multipliers.Count > attackTypes.Count)
                row.multipliers.RemoveAt(row.multipliers.Count - 1);
        }
    }
}
