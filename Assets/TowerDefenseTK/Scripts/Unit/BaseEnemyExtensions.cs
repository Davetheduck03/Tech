//using UnityEngine;

//namespace TowerDefenseTK
//{
//    /// <summary>
//    /// Extension methods for BaseEnemy to support ExitZone damage.
//    /// Add this partial class or add the method directly to your BaseEnemy.
//    /// </summary>
//    public static class BaseEnemyExtensions
//    {
//        /// <summary>
//        /// Get the EnemySO data from a BaseEnemy.
//        /// Returns null if not found.
//        /// </summary>
//        public static EnemySO GetEnemyData(this BaseEnemy enemy)
//        {
//            if (enemy == null) return null;

//            // Try to get from BaseUnit
//            BaseUnit unit = enemy.GetComponent<BaseUnit>();
//            if (unit != null && unit.unitData != null)
//            {
//                return unit.unitData as EnemySO;
//            }

//            return null;
//        }
//    }
//}

///*
// * ALTERNATIVE: If you prefer, add this method directly to your BaseEnemy class:
// * 
// * public EnemySO GetEnemyData()
// * {
// *     BaseUnit unit = GetComponent<BaseUnit>();
// *     if (unit != null && unit.unitData != null)
// *     {
// *         return unit.unitData as EnemySO;
// *     }
// *     return null;
// * }
// * 
// * Also, add this field to your EnemySO.cs:
// * 
// * [Header("Base Damage")]
// * [Tooltip("Damage dealt to player when enemy reaches the exit")]
// * public int damageToBase = 1;
// */
