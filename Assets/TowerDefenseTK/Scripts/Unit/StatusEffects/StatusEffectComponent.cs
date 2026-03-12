using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    // ── Runtime wrapper ──────────────────────────────────────────────────────

    /// <summary>
    /// Holds the live state of one applied status effect on an enemy.
    /// Not a ScriptableObject — created at runtime by StatusEffectComponent.
    /// </summary>
    public class ActiveEffect
    {
        public StatusEffectSO data;
        public float remainingTime;

        /// <summary>
        /// Optional: which tower's DamageComponent caused this effect,
        /// so kill credit can be given if DOT delivers the killing blow.
        /// </summary>
        public DamageComponent source;

        public ActiveEffect(StatusEffectSO data, DamageComponent source = null)
        {
            this.data = data;
            this.remainingTime = data.duration;
            this.source = source;
        }
    }

    // ── Main component ───────────────────────────────────────────────────────

    /// <summary>
    /// Add to any enemy prefab to make it react to status effects.
    ///
    /// Supports three types defined in StatusEffectSO:
    ///   Slow  — speed multiplier, only the strongest active slow is used.
    ///   DOT   — damage per second, ticks through the normal DamageTable.
    ///   Stun  — zero movement speed for the duration.
    ///
    /// Each effect type keeps at most ONE active instance:
    ///   Slow  — incoming replaces current if it is stronger; otherwise refreshes timer.
    ///   DOT   — refreshes/replaces when the same SO is reapplied.
    ///   Stun  — incoming replaces current if its duration is longer; otherwise refreshes.
    ///
    /// Tint priority: Stun > Slow > DOT > none (white).
    /// </summary>
    public class StatusEffectComponent : MonoBehaviour
    {
        // Active effects — one entry per StatusEffectType maximum (three slots total)
        private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>(3);

        // Cached refs
        private HealthComponent healthComponent;
        private Renderer[]      renderers;
        private MaterialPropertyBlock propBlock;

        private static readonly Color DefaultTint = Color.white;

        // ── Public state ─────────────────────────────────────────────────────

        /// <summary>True if a Stun effect is currently active.</summary>
        public bool IsStunned => HasActiveType(StatusEffectType.Stun);

        /// <summary>
        /// Combined speed multiplier from all active Slow effects.
        /// Returns 0 when stunned so MovementComponent knows to stop.
        /// </summary>
        public float GetSpeedMultiplier()
        {
            if (IsStunned) return 0f;

            float result = 1f;
            foreach (var e in activeEffects)
                if (e.data.effectType == StatusEffectType.Slow)
                    result = Mathf.Min(result, e.data.slowMultiplier);

            return result;
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            renderers       = GetComponentsInChildren<Renderer>();
            propBlock       = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (activeEffects.Count == 0) return;

            bool visualDirty = false;
            float dt = Time.deltaTime;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveEffect e = activeEffects[i];

                // ── DOT tick ────────────────────────────────────────────────
                if (e.data.effectType == StatusEffectType.DOT && healthComponent != null)
                {
                    // Pass the source DamageComponent so kill credit flows correctly
                    healthComponent.TakeDamage(e.data.damagePerSecond * dt, e.data.dotDamageType, e.source);
                }

                // ── Expire ──────────────────────────────────────────────────
                e.remainingTime -= dt;
                if (e.remainingTime <= 0f)
                {
                    activeEffects.RemoveAt(i);
                    visualDirty = true;
                }
            }

            if (visualDirty) UpdateVisuals();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Apply a status effect from a preset SO.
        /// Pass the tower's DamageComponent as <paramref name="source"/> so DOT
        /// kill credit flows back to the right tower (optional).
        /// </summary>
        public void Apply(StatusEffectSO effectSO, DamageComponent source = null)
        {
            if (effectSO == null) return;

            switch (effectSO.effectType)
            {
                case StatusEffectType.Slow:
                    // Keep the strongest (lowest multiplier); always refresh timer
                    ApplySingle(effectSO, source,
                        replaceIf: existing => effectSO.slowMultiplier < existing.data.slowMultiplier);
                    break;

                case StatusEffectType.Stun:
                    // Keep whichever leaves the enemy stunned longer
                    ApplySingle(effectSO, source,
                        replaceIf: existing => effectSO.duration > existing.remainingTime);
                    break;

                case StatusEffectType.DOT:
                    // One DOT stack — always refresh (same SO re-applied resets the timer)
                    ApplySingle(effectSO, source,
                        replaceIf: existing => true);
                    break;
            }

            UpdateVisuals();
        }

        // ── Internals ────────────────────────────────────────────────────────

        /// <summary>
        /// Find an existing effect of the same type. If found, replace or extend it
        /// according to <paramref name="replaceIf"/>. If not found, add a new entry.
        /// </summary>
        private void ApplySingle(StatusEffectSO effectSO, DamageComponent source,
                                  System.Func<ActiveEffect, bool> replaceIf)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].data.effectType != effectSO.effectType) continue;

                if (replaceIf(activeEffects[i]))
                    activeEffects[i] = new ActiveEffect(effectSO, source);   // replace
                else
                    activeEffects[i].remainingTime =                         // extend only
                        Mathf.Max(activeEffects[i].remainingTime, effectSO.duration);

                return;
            }

            activeEffects.Add(new ActiveEffect(effectSO, source));
        }

        private bool HasActiveType(StatusEffectType type)
        {
            foreach (var e in activeEffects)
                if (e.data.effectType == type) return true;
            return false;
        }

        /// <summary>
        /// Re-compute and apply the tint based on which effects are currently active.
        /// Priority: Stun (yellow) > Slow (blue) > DOT (orange) > none (white).
        /// Each SO's own tintColor is used, so designers can override these defaults.
        /// </summary>
        private void UpdateVisuals()
        {
            Color tint = DefaultTint;

            if (HasActiveType(StatusEffectType.Stun))
                tint = TintForType(StatusEffectType.Stun);
            else if (HasActiveType(StatusEffectType.Slow))
                tint = TintForType(StatusEffectType.Slow);
            else if (HasActiveType(StatusEffectType.DOT))
                tint = TintForType(StatusEffectType.DOT);

            foreach (var r in renderers)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color",     tint);
                propBlock.SetColor("_BaseColor", tint); // URP / HDRP
                r.SetPropertyBlock(propBlock);
            }
        }

        private Color TintForType(StatusEffectType type)
        {
            foreach (var e in activeEffects)
                if (e.data.effectType == type) return e.data.tintColor;
            return DefaultTint;
        }
    }
}
