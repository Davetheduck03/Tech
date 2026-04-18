using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Add to any tower prefab to make it receive buffs from Buff-type support towers.
    ///
    /// Buff towers call ApplyBuff() on this component every 0.5 s.
    /// Multiple different TowerBuffSOs can be active simultaneously.
    /// For the same preset the timer is refreshed rather than stacked.
    ///
    /// While at least one buff is active:
    ///   • The tower is tinted with the first buff's color.
    ///   • A procedural particle aura plays around the tower base.
    /// </summary>
    public class TowerBuffComponent : MonoBehaviour
    {
        // ── Runtime state ─────────────────────────────────────────────────────

        private class ActiveBuff
        {
            public TowerBuffSO data;
            public float remainingTime;
            public ActiveBuff(TowerBuffSO data) { this.data = data; remainingTime = data.duration; }
        }

        private readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>(4);

        // ── Visual refs ───────────────────────────────────────────────────────

        private Renderer[]            renderers;
        private MaterialPropertyBlock propBlock;
        private ParticleSystem        buffParticles;

        private static readonly Color DefaultTint = Color.white;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
            CreateBuffParticles();
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

        private void OnDestroy()
        {
            if (buffParticles != null)
                Destroy(buffParticles.gameObject);
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

            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].data == buff)
                {
                    activeBuffs[i].remainingTime = buff.duration;
                    UpdateVisuals();
                    return;
                }
            }

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
        /// Updates the material tint and the particle aura based on active buffs.
        /// </summary>
        private void UpdateVisuals()
        {
            if (activeBuffs.Count > 0)
            {
                Color tint = activeBuffs[0].data.tintColor;

                // Material tint
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_Color",     tint);
                    propBlock.SetColor("_BaseColor", tint); // URP / HDRP
                    r.SetPropertyBlock(propBlock);
                }

                // Particle aura — update color and start if not already running
                if (buffParticles != null)
                {
                    var main = buffParticles.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(tint.r, tint.g, tint.b, 0.9f),
                        new Color(tint.r * 0.7f, tint.g * 0.7f, tint.b * 0.7f, 0.5f)
                    );

                    if (!buffParticles.isPlaying)
                        buffParticles.Play();
                }
            }
            else
            {
                // Restore white tint
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_Color",     DefaultTint);
                    propBlock.SetColor("_BaseColor", DefaultTint);
                    r.SetPropertyBlock(propBlock);
                }

                // Stop aura
                if (buffParticles != null && buffParticles.isPlaying)
                    buffParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // ── Particle setup ────────────────────────────────────────────────────

        /// <summary>
        /// Builds a looping particle aura procedurally — no prefab required.
        /// Particles orbit the base of the tower and slowly drift upward.
        /// </summary>
        private void CreateBuffParticles()
        {
            GameObject psGO = new GameObject("BuffAura_VFX");
            psGO.transform.SetParent(transform, false);
            psGO.transform.localPosition = Vector3.zero;

            buffParticles = psGO.AddComponent<ParticleSystem>();

            // ── Main module ───────────────────────────────────────────────────
            var main = buffParticles.main;
            main.loop              = true;
            main.startLifetime     = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed        = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
            main.startSize         = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor        = new ParticleSystem.MinMaxGradient(Color.white);
            main.simulationSpace   = ParticleSystemSimulationSpace.World;
            main.maxParticles      = 60;
            main.gravityModifier   = -0.1f; // float upward slightly

            // ── Emission ──────────────────────────────────────────────────────
            var emission = buffParticles.emission;
            emission.enabled        = true;
            emission.rateOverTime   = 18f;

            // ── Shape: ring around tower base ─────────────────────────────────
            var shape = buffParticles.shape;
            shape.enabled    = true;
            shape.shapeType  = ParticleSystemShapeType.Circle;
            shape.radius     = 0.55f;
            shape.radiusThickness = 0.2f; // emit from the outer band only
            shape.rotation   = new Vector3(0f, 0f, 0f); // flat on XZ plane

            // ── Color over lifetime: fade in then fade out ────────────────────
            var col = buffParticles.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f,   0.00f),
                    new GradientAlphaKey(1f,   0.20f),
                    new GradientAlphaKey(0.9f, 0.70f),
                    new GradientAlphaKey(0f,   1.00f)
                }
            );
            col.color = gradient;

            // ── Size over lifetime: shrink as they fade ───────────────────────
            var sizeOL = buffParticles.sizeOverLifetime;
            sizeOL.enabled = true;
            var sizeCurve  = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f);
            sizeCurve.AddKey(0.3f, 1f);
            sizeCurve.AddKey(1f, 0.2f);
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // ── Renderer: unlit so particles are visible in all lighting ──────
            var pr = buffParticles.GetComponent<ParticleSystemRenderer>();
            pr.renderMode  = ParticleSystemRenderMode.Billboard;
            pr.sortingOrder = 10;

            // Pick an unlit particle shader that works in the active render pipeline.
            // Default-Particle.mat only exists in the Built-in pipeline, so we probe
            // shader names in priority order instead.
            Shader particleShader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("HDRP/Unlit")                                ??
                Shader.Find("Particles/Standard Unlit")                  ??
                Shader.Find("Sprites/Default")                           ??
                Shader.Find("Hidden/InternalErrorShader");               // last-resort fallback

            if (particleShader != null)
                pr.material = new Material(particleShader) { name = "BuffParticle_Runtime" };

            // Start stopped — only plays when a buff is active
            buffParticles.Stop();
        }
    }
}
