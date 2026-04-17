using UnityEngine;
using UnityEngine.Rendering;

namespace TowerDefenseTK
{
    /// <summary>
    /// Procedural flat ring drawn with a LineRenderer in WORLD space.
    /// Positions are recalculated every LateUpdate so the ring follows the parent
    /// regardless of that parent's scale, rotation, or movement.
    ///
    /// Usage:
    ///   var ring = RangeIndicator.Attach(someTransform, radius: 5f, color: Color.cyan);
    ///   ring.SetRadius(newRadius);
    ///   ring.SetColor(Color.yellow);
    ///   Destroy(ring.gameObject);
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RangeIndicator : MonoBehaviour
    {
        private const int Segments = 64;

        // Vertical offset above the parent's world Y so the ring never clips the ground
        private const float YOffset = 0.15f;

        private LineRenderer lr;
        private float        _radius;
        private Color        _color = Color.white;
        private Transform    _parent; // cached for LateUpdate

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a RangeIndicator as a child of <paramref name="parent"/>.
        /// Positions are maintained in world space so parent scale/rotation don't distort the ring.
        /// </summary>
        public static RangeIndicator Attach(Transform parent, float radius, Color color)
        {
            var go = new GameObject("RangeIndicator");
            // Parent it so the object moves with the tower but we handle positions ourselves
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var ri          = go.AddComponent<RangeIndicator>();
            ri._parent      = parent;
            ri._radius      = radius;
            ri._color       = color;
            return ri;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_parent == null) _parent = transform.parent;
            lr = GetComponent<LineRenderer>();
            ConfigureRenderer();
            RebuildPositions();
        }

        private void LateUpdate()
        {
            // Recompute world-space positions every frame so scale/rotation never distort the ring
            RebuildPositions();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetRadius(float radius)
        {
            _radius = Mathf.Max(0f, radius);
            // Positions will be refreshed next LateUpdate automatically
        }

        public void SetColor(Color color)
        {
            _color = color;
            if (lr == null) return;
            lr.startColor = color;
            lr.endColor   = color;
            if (lr.material != null)
            {
                if (lr.material.HasProperty("_Color"))     lr.material.SetColor("_Color",     color);
                if (lr.material.HasProperty("_BaseColor")) lr.material.SetColor("_BaseColor", color);
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        // ── Internals ─────────────────────────────────────────────────────────

        private void ConfigureRenderer()
        {
            lr.loop              = true;
            lr.positionCount     = Segments;
            lr.useWorldSpace     = true;   // world-space: immune to parent scale/rotation
            lr.widthMultiplier   = 0.15f;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.startColor        = _color;
            lr.endColor          = _color;

            // Pick the best available shader.
            // Sprites/Default tints via vertex colour in all pipelines.
            // Fall back to Unlit/Color which at least renders solidly.
            Shader shader =
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color")      ??
                Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     _color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _color);
                lr.material = mat;
            }

            lr.startColor = _color;
            lr.endColor   = _color;
        }

        private void RebuildPositions()
        {
            if (lr == null || _parent == null) return;

            // Use the parent's world position + a flat Y offset; ignore parent rotation/scale
            Vector3 center = _parent.position;
            center.y += YOffset;

            float step = 360f / Segments * Mathf.Deg2Rad;
            lr.positionCount = Segments;

            for (int i = 0; i < Segments; i++)
            {
                float a = i * step;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * _radius, 0f, Mathf.Sin(a) * _radius));
            }
        }
    }
}
