using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Visualizes the current cached path using a dotted, animated line effect.
    /// Automatically updates when paths are recalculated.
    /// </summary>
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Path Selection")]
        [SerializeField] private NodeType startNodeType = NodeType.Start;
        [SerializeField] private NodeType endNodeType = NodeType.End;
        [SerializeField] private int startNodeIndex = 0;
        [SerializeField] private int endNodeIndex = 0;
        [SerializeField] private bool autoUpdateOnPathChange = true;

        [Header("Visual Settings")]
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private Color lineColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        [SerializeField] private float heightOffset = 0.25f;
        [SerializeField] private bool showOnStart = true;

        [Header("Dash Pattern")]
        [SerializeField] private float dashLength = 0.4f;
        [SerializeField] private float gapLength = 0.2f;
        [SerializeField] private int pointsPerUnit = 10;

        [Header("Animation")]
        [SerializeField] private bool animateLine = true;
        [SerializeField] private float scrollSpeed = 1f;

        private LineRenderer lineRenderer;
        private Material lineMaterial;
        private float animationOffset = 0f;
        private PathNode currentStartNode;
        private PathNode currentEndNode;

        private void Start()
        {
            SetupLineRenderer();

            // Subscribe to Astar if available
            if (Astar.Instance != null)
            {
                Astar.Instance.OnPathsRecalculated += OnPathRecalculated;
            }

            if (showOnStart)
            {
                Invoke(nameof(InitializeAndDrawPath), 0.5f);
            }
        }

        private void OnEnable()
        {
            if (autoUpdateOnPathChange)
            {
                // Subscribe to path updates
                PathNodeGenerator.OnGridGenerated += OnPathsGenerated;
            }
        }

        private void OnDisable()
        {
            if (autoUpdateOnPathChange)
            {
                PathNodeGenerator.OnGridGenerated -= OnPathsGenerated;
            }

            // Unsubscribe from Astar
            if (Astar.Instance != null)
            {
                Astar.Instance.OnPathsRecalculated -= OnPathRecalculated;
            }
        }

        private void OnPathsGenerated()
        {
            Invoke(nameof(InitializeAndDrawPath), 0.6f);
        }

        /// <summary>
        /// Called when Astar recalculates paths due to obstacles
        /// </summary>
        private void OnPathRecalculated()
        {
            Debug.Log("PathVisualizer: Detected path recalculation, refreshing visualization");
            Invoke(nameof(RefreshPath), 0.1f);
        }

        private void InitializeAndDrawPath()
        {
            // Get the start and end nodes based on indices
            if (NodeGetter.nodeValue.ContainsKey(startNodeType) &&
                NodeGetter.nodeValue[startNodeType].Count > startNodeIndex)
            {
                currentStartNode = NodeGetter.nodeValue[startNodeType][startNodeIndex];
            }

            if (NodeGetter.nodeValue.ContainsKey(endNodeType) &&
                NodeGetter.nodeValue[endNodeType].Count > endNodeIndex)
            {
                currentEndNode = NodeGetter.nodeValue[endNodeType][endNodeIndex];
            }

            if (currentStartNode != null && currentEndNode != null)
            {
                RefreshPath();
            }
            else
            {
                Debug.LogWarning("PathVisualizer: Could not find start or end nodes!");
            }
        }

        /// <summary>
        /// Manually refresh the path visualization from cache
        /// </summary>
        public void RefreshPath()
        {
            if (currentStartNode == null || currentEndNode == null)
            {
                Debug.LogWarning("PathVisualizer: Start or end node is null!");
                Debug.LogWarning($"  Start Node: {currentStartNode}, End Node: {currentEndNode}");
                return;
            }

            if (Astar.Instance == null)
            {
                Debug.LogError("PathVisualizer: Astar.Instance is null!");
                return;
            }

            var cacheKey = (currentStartNode, currentEndNode);

            Debug.Log($"PathVisualizer: Looking for path from {currentStartNode.name} to {currentEndNode.name}");
            Debug.Log($"PathVisualizer: Cache contains {Astar.Instance.generatedPathCache.Count} paths");

            if (Astar.Instance.generatedPathCache.ContainsKey(cacheKey))
            {
                List<PathNode> path = Astar.Instance.generatedPathCache[cacheKey];
                DrawPath(path);
                Debug.Log($"PathVisualizer: ✓ Drawing cached path with {path.Count} nodes");
            }
            else
            {
                Debug.LogWarning($"PathVisualizer: ✗ No cached path found for {currentStartNode.name} → {currentEndNode.name}");

                // List all available paths in cache for debugging
                Debug.Log("Available paths in cache:");
                foreach (var kvp in Astar.Instance.generatedPathCache)
                {
                    Debug.Log($"  {kvp.Key.Item1.name} → {kvp.Key.Item2.name}");
                }

                ClearPath();
            }
        }

        /// <summary>
        /// Set which path to visualize by node indices
        /// </summary>
        public void SetPathToVisualize(int startIndex, int endIndex)
        {
            startNodeIndex = startIndex;
            endNodeIndex = endIndex;
            InitializeAndDrawPath();
        }

        /// <summary>
        /// Set which path to visualize by specific nodes
        /// </summary>
        public void SetPathToVisualize(PathNode startNode, PathNode endNode)
        {
            currentStartNode = startNode;
            currentEndNode = endNode;
            RefreshPath();
        }

        private void SetupLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            // Use Particles/Standard Unlit for better texture support
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            lineMaterial = new Material(shader);
            lineMaterial.color = lineColor;

            // Create a simple dash texture
            CreateDashTexture();

            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;
            lineRenderer.useWorldSpace = true;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.sortingOrder = 1000;
        }

        private void CreateDashTexture()
        {
            // Create a simple dash pattern texture
            int width = 64;
            int height = 4;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;

            float dashRatio = dashLength / (dashLength + gapLength);
            int dashPixels = Mathf.RoundToInt(width * dashRatio);

            for (int x = 0; x < width; x++)
            {
                Color color = x < dashPixels ? Color.white : Color.clear;
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            lineMaterial.mainTexture = tex;
        }

        private void DrawPath(List<PathNode> path)
        {
            if (path == null || path.Count < 2)
            {
                ClearPath();
                return;
            }

            List<Vector3> pathPositions = new List<Vector3>();
            foreach (var node in path)
            {
                if (node != null)
                    pathPositions.Add(node.transform.position + Vector3.up * heightOffset);
            }

            // Create smooth path with enough points
            List<Vector3> smoothPath = CreateSmoothPath(pathPositions);

            lineRenderer.positionCount = smoothPath.Count;
            lineRenderer.SetPositions(smoothPath.ToArray());

            // Calculate texture tiling
            float totalLength = CalculatePathLength(pathPositions);
            float tilingFactor = totalLength / (dashLength + gapLength);
            lineMaterial.mainTextureScale = new Vector2(tilingFactor, 1f);
        }

        private List<Vector3> CreateSmoothPath(List<Vector3> waypoints)
        {
            List<Vector3> smoothPath = new List<Vector3>();

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 start = waypoints[i];
                Vector3 end = waypoints[i + 1];
                float distance = Vector3.Distance(start, end);
                int points = Mathf.Max(2, Mathf.CeilToInt(distance * pointsPerUnit));

                for (int j = 0; j < points; j++)
                {
                    float t = j / (float)points;
                    smoothPath.Add(Vector3.Lerp(start, end, t));
                }
            }

            // Add final point
            smoothPath.Add(waypoints[waypoints.Count - 1]);

            return smoothPath;
        }

        private float CalculatePathLength(List<Vector3> positions)
        {
            float length = 0f;
            for (int i = 0; i < positions.Count - 1; i++)
            {
                length += Vector3.Distance(positions[i], positions[i + 1]);
            }
            return length;
        }

        private void Update()
        {
            if (animateLine && lineRenderer.positionCount > 0 && lineMaterial != null)
            {
                // Scroll the texture offset for moving animation (negative for forward direction)
                animationOffset -= scrollSpeed * Time.deltaTime;
                lineMaterial.mainTextureOffset = new Vector2(animationOffset, 0f);
            }
        }

        public void ClearPath()
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        public void SetPathColor(Color color)
        {
            lineColor = color;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                if (lineMaterial != null)
                    lineMaterial.color = color;
            }
        }

        public void SetLineWidth(float width)
        {
            lineWidth = width;
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }
        }

        public void ToggleAnimation(bool enabled)
        {
            animateLine = enabled;
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                if (lineMaterial.mainTexture != null)
                    Destroy(lineMaterial.mainTexture);
                Destroy(lineMaterial);
            }
        }

        // Editor helper - call this from inspector button or context menu
        [ContextMenu("Refresh Path Now")]
        private void RefreshPathFromContextMenu()
        {
            if (Application.isPlaying)
            {
                InitializeAndDrawPath();
            }
        }
    }
}