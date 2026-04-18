using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Spawns a jagged multi-segment lightning arc through a series of world-space
    /// positions, fades it out, then destroys itself.
    ///
    /// Usage:
    ///   LightningVFX.Spawn(nodes, duration, color, segmentsPerLink, displacement);
    ///
    /// Each consecutive pair in <paramref name="nodes"/> gets its own jagged arc so
    /// the bolt visually chains from enemy to enemy.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningVFX : MonoBehaviour
    {
        // ── Public factory ────────────────────────────────────────────────────

        /// <summary>
        /// Create a chain-lightning arc through the given world positions.
        /// </summary>
        /// <param name="nodes">Ordered positions: muzzle → target1 → target2 …</param>
        /// <param name="duration">Seconds the arc stays visible.</param>
        /// <param name="color">Base bolt colour (defaults to icy blue).</param>
        /// <param name="segmentsPerLink">Jagged subdivisions between each pair of nodes.</param>
        /// <param name="displacement">Max sideways jitter per subdivision (world units).</param>
        public static LightningVFX Spawn(
            IList<Vector3> nodes,
            float          duration        = 0.15f,
            Color?         color           = null,
            int            segmentsPerLink = 8,
            float          displacement    = 0.35f)
        {
            if (nodes == null || nodes.Count < 2) return null;

            var go  = new GameObject("LightningVFX");
            var vfx = go.AddComponent<LightningVFX>();
            vfx.Init(nodes, duration, color ?? new Color(0.3f, 0.65f, 1f), segmentsPerLink, displacement);
            return vfx;
        }

        // ── Private state ─────────────────────────────────────────────────────

        private LineRenderer _lr;
        private float        _duration;
        private float        _elapsed;
        private Color        _boltColor;

        // ── Initialise ────────────────────────────────────────────────────────

        private void Init(IList<Vector3> nodes, float duration, Color color, int segsPerLink, float disp)
        {
            _duration  = duration;
            _boltColor = color;

            _lr = GetComponent<LineRenderer>();
            ConfigureRenderer();
            BuildPositions(nodes, segsPerLink, disp);
            ApplyGradient(1f);  // full opacity at birth
        }

        // ── Renderer setup ────────────────────────────────────────────────────

        private void ConfigureRenderer()
        {
            _lr.useWorldSpace     = true;
            _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lr.receiveShadows    = false;
            _lr.alignment         = LineAlignment.View;
            _lr.numCapVertices    = 2;

            // Taper from thick at origin to thin at tip
            _lr.widthCurve = AnimationCurve.EaseInOut(0f, 0.07f, 1f, 0.02f);

            // ── Material ─────────────────────────────────────────────────────
            // Sprites/Default is unlit, pipeline-agnostic, and correctly applies
            // LineRenderer vertex colours — essential for the gradient to show.
            // URP Particles/Unlit works too but needs vertex colour enabled in the
            // asset's material import settings, which we can't guarantee at runtime.
            Shader shader =
                Shader.Find("Sprites/Default")                           ??
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Unlit/Color");

            if (shader != null)
                _lr.material = new Material(shader) { name = "LightningVFX_Runtime" };
        }

        // ── Gradient ──────────────────────────────────────────────────────────

        /// <summary>Build and apply the LineRenderer colour gradient at the given alpha.</summary>
        private void ApplyGradient(float alpha)
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white,                    0.00f),
                    new GradientColorKey(new Color(0.85f, 0.95f, 1.00f), 0.10f),
                    new GradientColorKey(_boltColor,                     0.40f),
                    new GradientColorKey(_boltColor * 0.55f,             1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(alpha,       0.00f),
                    new GradientAlphaKey(alpha,       0.75f),
                    new GradientAlphaKey(0f,          1.00f),
                }
            );
            _lr.colorGradient = g;
        }

        // ── Position builder ──────────────────────────────────────────────────

        /// <summary>
        /// Build the full jagged polyline through all chain nodes.
        /// Between each consecutive pair we midpoint-displace to get the
        /// zigzag lightning look.
        /// </summary>
        private void BuildPositions(IList<Vector3> nodes, int segsPerLink, float disp)
        {
            var pts = new List<Vector3>();

            for (int n = 0; n < nodes.Count - 1; n++)
            {
                var seg = new List<Vector3> { nodes[n], nodes[n + 1] };
                MidpointDisplace(seg, segsPerLink, disp);

                // Skip the first point on subsequent links to avoid duplicates
                int start = (n == 0) ? 0 : 1;
                for (int i = start; i < seg.Count; i++)
                    pts.Add(seg[i]);
            }

            _lr.positionCount = pts.Count;
            _lr.SetPositions(pts.ToArray());
        }

        /// <summary>
        /// Iteratively insert jittered midpoints until there are at least
        /// <paramref name="targetSegs"/> segments between the two endpoints.
        /// </summary>
        private static void MidpointDisplace(List<Vector3> pts, int targetSegs, float disp)
        {
            int iterations = Mathf.Max(1, Mathf.CeilToInt(Mathf.Log(targetSegs + 1, 2f)));

            for (int iter = 0; iter < iterations; iter++)
            {
                // Iterate backwards so insertions don't shift upcoming indices
                for (int i = pts.Count - 1; i > 0; i--)
                {
                    Vector3 a   = pts[i - 1];
                    Vector3 b   = pts[i];
                    Vector3 mid = (a + b) * 0.5f;

                    // Perpendicular to segment in the XZ plane
                    Vector3 along = (b - a).normalized;
                    Vector3 perp  = new Vector3(-along.z, 0f, along.x);
                    if (perp.sqrMagnitude < 0.001f)
                        perp = new Vector3(0f, 0f, 1f);

                    mid += perp  * Random.Range(-disp, disp);
                    mid += Vector3.up * Random.Range(-disp * 0.4f, disp * 0.4f);

                    pts.Insert(i, mid);
                }
                disp *= 0.55f; // reduce jitter each pass so fine detail looks natural
            }
        }

        // ── Lifetime & fade ───────────────────────────────────────────────────

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(_elapsed / _duration);
            float alpha = 1f - (t * t);        // fast at start, quick fall-off

            ApplyGradient(alpha);

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }
    }
}
