using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Add to any tower prefab to make it receive buffs from Buff-type support towers.
    ///
    /// Buff towers call ApplyBuff() on this component every 0.5 s.
    /// Multiple different TowerBuffSOs can be active simultaneously (e.g. two buff
    /// towers with different presets overlapping). For the same preset the timer is
    /// simply refreshed rather than stacked.
    ///
    /// Multiplier properties (FireRateMultiplier, DamageMultiplier, RangeMultiplier)
    /// return the PRODUCT of all currently active buffs, falling back to 1.0
    /// when no buffs are present.
    /// </summary>
    public class TowerBuffComponent : MonoBehaviour
    {
        // ── Runtime state ─────────────────────────────────────────────────────

        private class ActiveBuff
        {
            public TowerBuffSO data;
            public float remainingTime;

            public ActiveBuff(TowerBuffSO data)
            {
                this.data      = data;
                remainingTime  = data.duration;
            }
        }

        private readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>(4);

        // ── Cached renderer references for tinting ────────────────────────────

        private Renderer[]            renderers;
        private MaterialPropertyBlock propBlock;
        private static readonly Color DefaultTint = Color.white;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (activeBuffs.Count == 0) return;

            bool dirty = false;
            float dt   = Time.deltaTime;

            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].remainingTime -= dt;
                if (activeBuffs[i].remainingTime <= 0f)
                {
                    activeBuffs.RemoveAt(i);
                    dirty = true;
                }
            }

            if (dirty) UpdateVisuals();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Product of all active fire-rate multipliers (≥ 1.0).</summary>
        public float FireRateMultiplier => ComputeProduct(b => b.data.fireRateMultiplier);

        /// <summary>Product of all active damage multipliers (≥ 1.0).</summary>
        public float DamageMultiplier   => ComputeProduct(b => b.data.damageMultiplier);

        /// <summary>Product of all active range multipliers (≥ 1.0).</summary>
        public float RangeMultiplier    => ComputeProduct(b => b.data.rangeMultiplier);

        /// <summary>True while at least one buff is active.</summary>
        public bool IsBuffed => activeBuffs.Count > 0;

        /// <summary>
        /// Called by the buff tower's SupportTick every 0.5 s.
        /// Refreshes the timer if the same SO is already active; otherwise adds it.
        /// </summary>
        public void ApplyBuff(TowerBuffSO buff)
        {
            if (buff == null) return;

            // Refresh existing entry for this SO
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].data == buff)
                {
                    activeBuffs[i].remainingTime = buff.duration;
                    UpdateVisuals();
                    return;
                }
            }

            // New buff SO — add it
            activeBuffs.Add(new ActiveBuff(buff));
            UpdateVisuals();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private float ComputeProduct(System.Func<ActiveBuff, float> selector)
        {
            float product = 1f;
            foreach (var b in activeBuffs)
                product *= selector(b);
            return product;
        }

        /// <summary>
        /// Tints the tower green while buffed, white when unbuffed.
        /// If multiple buffs are active the first one's color is used.
        /// </summary>
        private void UpdateVisuals()
        {
            Color tint = activeBuffs.Count > 0 ? activeBuffs[0].data.tintColor : DefaultTint;

            foreach (var r in renderers)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color",     tint);
                propBlock.SetColor("_BaseColor", tint); // URP / HDRP
                r.SetPropertyBlock(propBlock);
            }
        }
    }
}
