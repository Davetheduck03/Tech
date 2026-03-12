using UnityEngine;

namespace TowerDefenseTK
{
    public enum StatusEffectType
    {
        Slow,
        DOT,
        Stun
    }

    /// <summary>
    /// Designer-facing preset for a status effect.
    ///
    /// Create assets via:  TD Toolkit / Status Effect
    ///
    /// Each tower SO (or projectile) holds a reference to one of these assets.
    /// The StatusEffectComponent on enemies reads from it at runtime.
    ///
    /// Slow  — reduces movement speed by (1 - slowMultiplier) percent.
    /// DOT   — deals damagePerSecond of a chosen DamageType each frame.
    ///         Goes through the DamageTable like any other hit, so armor works.
    /// Stun  — freezes the enemy in place for the duration.
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "TD Toolkit/Status Effect")]
    public class StatusEffectSO : ScriptableObject
    {
        [Header("General")]
        public string effectName = "New Effect";
        public StatusEffectType effectType = StatusEffectType.Slow;
        [Tooltip("How many seconds the effect lasts.")]
        public float duration = 2f;

        [Header("Visual")]
        [Tooltip("Tint applied to the enemy while this effect is active.")]
        public Color tintColor = Color.white;

        // ── Slow ────────────────────────────────────────────────────────────
        [Header("Slow (Slow type only)")]
        [Range(0.05f, 0.95f)]
        [Tooltip("Speed multiplier while slowed. 0.5 = half speed, 0.2 = 20% speed.")]
        public float slowMultiplier = 0.5f;

        // ── DOT ─────────────────────────────────────────────────────────────
        [Header("DOT (DOT type only)")]
        [Tooltip("Raw damage dealt per second. Processed through the DamageTable.")]
        public float damagePerSecond = 5f;
        [Tooltip("Damage type used for DamageTable lookups (e.g. Fire, Poison).")]
        public DamageType dotDamageType;

        // Stun has no extra fields — duration is all that matters.
    }
}
