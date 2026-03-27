using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Designer-facing preset for a tower buff, applied by Buff-type support towers.
    ///
    /// Create assets via:  TD Toolkit / Tower Buff
    ///
    /// The buff tower pulses nearby allied towers every 0.5 s.
    /// Each receiving tower must have a TowerBuffComponent attached to benefit.
    ///
    /// All three multipliers stack multiplicatively if several buff towers overlap.
    /// Leave a multiplier at 1.0 to leave that stat unchanged.
    /// </summary>
    [CreateAssetMenu(fileName = "New Tower Buff", menuName = "TD Toolkit/Tower Buff")]
    public class TowerBuffSO : ScriptableObject
    {
        [Header("General")]
        public string buffName = "New Buff";

        [Tooltip("How many seconds the buff lasts on the receiving tower. " +
                 "Set slightly above the pulse interval (0.5 s) so it never drops.")]
        public float duration = 1.5f;

        [Header("Multipliers  (1.0 = no change)")]
        [Range(1f, 3f)]
        [Tooltip("Multiplied with the tower's base fire rate. 1.3 = 30 % faster.")]
        public float fireRateMultiplier = 1f;

        [Range(1f, 3f)]
        [Tooltip("Multiplied with the tower's base damage per shot.")]
        public float damageMultiplier = 1f;

        [Range(1f, 3f)]
        [Tooltip("Multiplied with the tower's base detection range.")]
        public float rangeMultiplier = 1f;

        [Header("Visual")]
        [Tooltip("Tint applied to buffed towers while the effect is active.")]
        public Color tintColor = new Color(0.4f, 1f, 0.4f); // soft green
    }
}
