using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Spawns world-space billboards above each Spawn and Exit node at runtime.
    /// Hooks into PathNodeGenerator.OnGridGenerated — no manual wiring needed.
    ///
    /// Setup:
    ///   1. Add this component to any persistent GameObject (e.g. your GridManager or a LevelVisuals object).
    ///   2. Optionally assign custom spawn/exit icon sprites.
    ///   3. Press Play — billboards appear automatically after the grid generates.
    ///
    /// No prefabs required. Everything is built procedurally.
    /// </summary>
    public class SpawnExitVisualizer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Visuals")]
        [Tooltip("Height above the node's world position where the billboard floats.")]
        [SerializeField] private float billboardHeight = 2.5f;

        [Tooltip("World-space size of each billboard quad.")]
        [SerializeField] private float billboardSize = 1.2f;

        [Tooltip("Optional sprite for spawn points. Leave null to use the default coloured quad.")]
        [SerializeField] private Sprite spawnIcon;

        [Tooltip("Optional sprite for exit points. Leave null to use the default coloured quad.")]
        [SerializeField] private Sprite exitIcon;

        [Header("Colors (used when no sprite is assigned)")]
        [SerializeField] private Color spawnColor = new Color(0.22f, 0.55f, 1.00f, 0.92f);  // Blue
        [SerializeField] private Color exitColor  = new Color(1.00f, 0.28f, 0.28f, 0.92f);  // Red

        [Header("Label")]
        [SerializeField] private bool showLabel = true;
        [Tooltip("Font size of the world-space label (e.g. 'S 1', 'E 1').")]
        [SerializeField] private int   fontSize = 28;

        [Header("Pulse Animation")]
        [SerializeField] private bool  pulseEnabled = true;
        [SerializeField] private float pulseSpeed   = 1.4f;
        [SerializeField] private float pulseMinScale = 0.85f;
        [SerializeField] private float pulseMaxScale = 1.00f;

        [Header("Outline Ring")]
        [SerializeField] private bool  showRing      = true;
        [SerializeField] private float ringSize       = 1.6f;  // diameter of the ring quad
        [SerializeField] private Color ringSpawnColor = new Color(0.22f, 0.55f, 1.00f, 0.30f);
        [SerializeField] private Color ringExitColor  = new Color(1.00f, 0.28f, 0.28f, 0.30f);
        [SerializeField] private float ringRotateSpeed = 30f;   // degrees per second

        // ── Runtime ──────────────────────────────────────────────────────────

        private readonly List<BillboardEntry> entries = new List<BillboardEntry>();
        private Camera mainCam;

        private class BillboardEntry
        {
            public Transform root;          // parent that holds icon + label + ring
            public Transform iconQuad;
            public Transform ringQuad;
            public NodeType  nodeType;
            public bool      pulsing;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            PathNodeGenerator.OnGridGenerated += OnGridReady;
        }

        private void OnDisable()
        {
            PathNodeGenerator.OnGridGenerated -= OnGridReady;
        }

        private void OnGridReady()
        {
            StartCoroutine(BuildAfterFrame());
        }

        // Wait one extra frame so NodeGetter has fully populated nodeValue
        private IEnumerator BuildAfterFrame()
        {
            yield return null;
            yield return null;
            BuildVisuals();
        }

        private void Start()
        {
            mainCam = Camera.main;
            // If grid was already generated before we registered (rare edge case), build now.
            if (NodeGetter.nodeValue.Count > 0 && entries.Count == 0)
                BuildVisuals();
        }

        private void Update()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            float pulse = pulseEnabled
                ? Mathf.Lerp(pulseMinScale, pulseMaxScale,
                      (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f)
                : 1f;

            float dt = Time.deltaTime;

            foreach (var e in entries)
            {
                if (e.root == null) continue;

                // Billboard: face camera
                e.root.rotation = Quaternion.LookRotation(
                    e.root.position - mainCam.transform.position);

                // Pulse icon
                if (pulseEnabled && e.iconQuad != null)
                    e.iconQuad.localScale = Vector3.one * (billboardSize * pulse);

                // Rotate ring
                if (showRing && e.ringQuad != null)
                    e.ringQuad.Rotate(0f, 0f, ringRotateSpeed * dt, Space.Self);
            }
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildVisuals()
        {
            ClearVisuals();

            BuildForType(NodeType.Start, spawnColor,  ringSpawnColor, "S");
            BuildForType(NodeType.End,   exitColor,   ringExitColor,  "E");
        }

        private void BuildForType(NodeType type, Color color, Color ring, string prefix)
        {
            if (!NodeGetter.nodeValue.TryGetValue(type, out List<PathNode> nodes)) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                PathNode node = nodes[i];
                if (node == null) continue;

                Vector3 worldPos = node.transform.position + Vector3.up * billboardHeight;

                // Root — all sub-objects live under this, so we can rotate it as a unit
                GameObject root = new GameObject($"NodeVisual_{prefix}{i + 1}");
                root.transform.SetParent(transform, true);
                root.transform.position = worldPos;

                // ── Icon quad ──────────────────────────────────────────
                GameObject iconGO = BuildQuad("Icon", root.transform, Vector3.zero,
                                              billboardSize, color,
                                              type == NodeType.Start ? spawnIcon : exitIcon);

                // ── Ring quad ──────────────────────────────────────────
                GameObject ringGO = null;
                if (showRing)
                {
                    ringGO = BuildQuad("Ring", root.transform, Vector3.zero,
                                       ringSize, ring, null, isRing: true);
                }

                // ── World-space text label ─────────────────────────────
                if (showLabel)
                {
                    BuildLabel($"{prefix} {i + 1}", root.transform, color);
                }

                // ── Ground marker ring (flat on ground) ───────────────
                BuildGroundMarker(node.transform.position, color, prefix, i);

                // Register entry
                entries.Add(new BillboardEntry
                {
                    root      = root.transform,
                    iconQuad  = iconGO.transform,
                    ringQuad  = ringGO != null ? ringGO.transform : null,
                    nodeType  = type,
                    pulsing   = pulseEnabled
                });
            }
        }

        // ── Quad Builder ──────────────────────────────────────────────────────

        private GameObject BuildQuad(string name, Transform parent, Vector3 localOffset,
                                     float size, Color color, Sprite sprite,
                                     bool isRing = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;
            go.transform.localScale    = Vector3.one * size;

            MeshFilter   mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            mf.sharedMesh = BuildQuadMesh();

            Material mat;

            if (sprite != null && !isRing)
            {
                // Use a sprite/unlit shader so the icon shows clearly
                mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
                mat.mainTexture = sprite.texture;
                mat.color       = Color.white;
            }
            else if (isRing)
            {
                mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
                mat.mainTexture = BuildRingTexture(64);
                mat.color       = color;
            }
            else
            {
                mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
                mat.mainTexture = BuildCircleTexture(64, hasBorder: true);
                mat.color       = color;
            }

            mat.renderQueue = 3000;
            mr.material     = mat;
            mr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows     = false;

            return go;
        }

        // ── Label Builder ─────────────────────────────────────────────────────

        private void BuildLabel(string text, Transform parent, Color color)
        {
            // Use a Canvas in world space so the text is easy to read
            GameObject canvasGO = new GameObject("Label");
            canvasGO.transform.SetParent(parent, false);
            canvasGO.transform.localPosition = new Vector3(0f, -(billboardSize * 0.55f), 0f);
            canvasGO.transform.localScale    = Vector3.one * 0.012f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 60f);

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(canvasGO.transform, false);

            UnityEngine.UI.Text label = textGO.AddComponent<UnityEngine.UI.Text>();
            label.text      = text;
            label.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize  = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color     = Color.white;

            // Slight shadow for readability
            UnityEngine.UI.Shadow shadow = textGO.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(2, -2);

            RectTransform trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        // ── Ground Marker ─────────────────────────────────────────────────────

        private void BuildGroundMarker(Vector3 nodeWorldPos, Color color,
                                       string prefix, int index)
        {
            // A flat quad lying on the ground centred on the node
            float markerSize = GridManager.Instance != null
                               ? GridManager.Instance.cellSize * 0.9f
                               : 1.8f;

            GameObject go = new GameObject($"GroundMarker_{prefix}{index + 1}");
            go.transform.SetParent(transform, true);
            go.transform.position = nodeWorldPos + Vector3.up * 0.02f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * markerSize;

            MeshFilter   mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            mf.sharedMesh = BuildQuadMesh();

            Color groundColor = color;
            groundColor.a = 0.35f;

            Material mat  = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
            mat.mainTexture  = BuildCircleTexture(64, hasBorder: false);
            mat.color        = groundColor;
            mat.renderQueue  = 3000;

            mr.material           = mat;
            mr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows     = false;
        }

        // ── Mesh / Texture Helpers ────────────────────────────────────────────

        private static Mesh BuildQuadMesh()
        {
            Mesh mesh = new Mesh { name = "Billboard Quad" };

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1)
            };

            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>Procedural circle texture (white shape, transparent bg).</summary>
        private static Texture2D BuildCircleTexture(int res, bool hasBorder)
        {
            Texture2D tex  = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.wrapMode   = TextureWrapMode.Clamp;
            float half     = res / 2f;
            float outerR   = half - 1f;
            float innerR   = hasBorder ? outerR - (res * 0.12f) : 0f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx   = x - half + 0.5f;
                    float dy   = y - half + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    bool insideOuter = dist <= outerR;
                    bool outsideInner = !hasBorder || dist >= innerR;

                    tex.SetPixel(x, y, (insideOuter && outsideInner)
                        ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>Procedural ring texture for the rotating outer halo.</summary>
        private static Texture2D BuildRingTexture(int res)
        {
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.wrapMode  = TextureWrapMode.Clamp;
            float half    = res / 2f;
            float outerR  = half - 1f;
            float innerR  = outerR - (res * 0.18f);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx   = x - half + 0.5f;
                    float dy   = y - half + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y,
                        (dist <= outerR && dist >= innerR) ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return tex;
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        private void ClearVisuals()
        {
            entries.Clear();

            // Destroy all children except permanent manager objects
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        /// <summary>
        /// Call this at runtime to force a rebuild — useful after the map changes.
        /// </summary>
        [ContextMenu("Rebuild Visuals")]
        public void RebuildVisuals() => BuildVisuals();

        private void OnDestroy() => ClearVisuals();
    }
}
