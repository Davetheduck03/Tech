using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
	/// <summary>
	/// Visualizes cached A* paths using a dotted, animated line effect.
	/// Supports drawing a single path (by index) or all precomputed paths at once.
	/// </summary>
	public class PathVisualizer : MonoBehaviour
	{
		[Header("Path Selection")]
		[Tooltip("Draw every path in the Astar cache instead of a single start→end pair")]
		[SerializeField] private bool drawAllPaths = false;
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

		// Primary line renderer (used for single-path mode or the first path in all-path mode)
		private LineRenderer lineRenderer;
		private Material lineMaterial;

		// Extra line renderers spawned for additional paths when drawAllPaths = true
		private List<LineRenderer> extraLineRenderers = new List<LineRenderer>();
		private List<Material> extraMaterials = new List<Material>();

		private float animationOffset = 0f;
		private PathNode currentStartNode;
		private PathNode currentEndNode;

		#region Unity Lifecycle

		private void Start()
		{
			SetupLineRenderer();

			if (Astar.Instance != null)
				Astar.Instance.OnPathsRecalculated += OnPathRecalculated;

			if (showOnStart)
				Invoke(nameof(InitializeAndDrawPath), 0.5f);
		}

		private void OnEnable()
		{
			if (autoUpdateOnPathChange)
				PathNodeGenerator.OnGridGenerated += OnPathsGenerated;
		}

		private void OnDisable()
		{
			if (autoUpdateOnPathChange)
				PathNodeGenerator.OnGridGenerated -= OnPathsGenerated;

			if (Astar.Instance != null)
				Astar.Instance.OnPathsRecalculated -= OnPathRecalculated;
		}

		private void Update()
		{
			if (!animateLine) return;

			animationOffset -= scrollSpeed * Time.deltaTime;

			// Animate primary renderer
			if (lineRenderer.positionCount > 0 && lineMaterial != null)
				lineMaterial.mainTextureOffset = new Vector2(animationOffset, 0f);

			// Animate extra renderers
			for (int i = 0; i < extraMaterials.Count; i++)
			{
				if (extraMaterials[i] != null)
					extraMaterials[i].mainTextureOffset = new Vector2(animationOffset, 0f);
			}
		}

		private void OnDestroy()
		{
			DestroyLineMaterial(lineMaterial);
			DestroyExtraRenderers();
		}

		#endregion

		#region Path Drawing

		private void OnPathsGenerated()
		{
			Invoke(nameof(InitializeAndDrawPath), 0.6f);
		}

		private void OnPathRecalculated()
		{
			Invoke(nameof(RefreshPath), 0.1f);
		}

		private void InitializeAndDrawPath()
		{
			if (!drawAllPaths)
			{
				// Resolve single start/end nodes by index
				if (NodeGetter.nodeValue.ContainsKey(startNodeType) &&
					NodeGetter.nodeValue[startNodeType].Count > startNodeIndex)
					currentStartNode = NodeGetter.nodeValue[startNodeType][startNodeIndex];

				if (NodeGetter.nodeValue.ContainsKey(endNodeType) &&
					NodeGetter.nodeValue[endNodeType].Count > endNodeIndex)
					currentEndNode = NodeGetter.nodeValue[endNodeType][endNodeIndex];

				if (currentStartNode == null || currentEndNode == null)
				{
					Debug.LogWarning("PathVisualizer: Could not find start or end nodes!");
					return;
				}
			}

			RefreshPath();
		}

		/// <summary>
		/// Redraw from cache. Works for both single-path and all-paths modes.
		/// </summary>
		public void RefreshPath()
		{
			if (Astar.Instance == null)
			{
				Debug.LogError("PathVisualizer: Astar.Instance is null!");
				return;
			}

			if (drawAllPaths)
			{
				DrawAllCachedPaths();
			}
			else
			{
				DrawSinglePath();
			}
		}

		// ── Single path ───────────────────────────────────────────────────────

		private void DrawSinglePath()
		{
			ClearExtraRenderers();

			if (currentStartNode == null || currentEndNode == null)
			{
				Debug.LogWarning("PathVisualizer: Start or end node is null!");
				ClearPath();
				return;
			}

			var key = (currentStartNode, currentEndNode);
			if (Astar.Instance.generatedPathCache.TryGetValue(key, out List<PathNode> path))
			{
				ApplyPathToRenderer(lineRenderer, lineMaterial, path);
				Debug.Log($"PathVisualizer: Drawing cached path with {path.Count} nodes");
			}
			else
			{
				Debug.LogWarning($"PathVisualizer: No cached path for {currentStartNode.name} → {currentEndNode.name}");
				ClearPath();
			}
		}

		// ── All paths ─────────────────────────────────────────────────────────

		private void DrawAllCachedPaths()
		{
			var cache = Astar.Instance.generatedPathCache;

			if (cache.Count == 0)
			{
				Debug.LogWarning("PathVisualizer: Cache is empty, nothing to draw.");
				ClearPath();
				ClearExtraRenderers();
				return;
			}

			// Ensure we have enough line renderers (primary + extras)
			EnsureRendererCount(cache.Count);

			int index = 0;
			foreach (var kvp in cache)
			{
				LineRenderer lr = index == 0 ? lineRenderer : extraLineRenderers[index - 1];
				Material mat = index == 0 ? lineMaterial : extraMaterials[index - 1];
				ApplyPathToRenderer(lr, mat, kvp.Value);
				index++;
			}

			Debug.Log($"PathVisualizer: Drew {cache.Count} cached paths");
		}

		/// <summary>
		/// Make sure we have exactly <paramref name="count"/> renderers available
		/// (primary + extras).
		/// </summary>
		private void EnsureRendererCount(int count)
		{
			int needed = count - 1; // primary covers the first path

			// Add missing renderers
			while (extraLineRenderers.Count < needed)
			{
				GameObject go = new GameObject($"PathVisualizer_Extra_{extraLineRenderers.Count}");
				go.transform.SetParent(transform, false);

				LineRenderer lr = go.AddComponent<LineRenderer>();
				Material mat = CreateLineMaterial();

				ConfigureLineRenderer(lr, mat);

				extraLineRenderers.Add(lr);
				extraMaterials.Add(mat);
			}

			// Hide surplus renderers
			for (int i = needed; i < extraLineRenderers.Count; i++)
				extraLineRenderers[i].positionCount = 0;
		}

		// ── Shared helpers ────────────────────────────────────────────────────

		private void ApplyPathToRenderer(LineRenderer lr, Material mat, List<PathNode> path)
		{
			if (path == null || path.Count < 2)
			{
				lr.positionCount = 0;
				return;
			}

			List<Vector3> rawPositions = new List<Vector3>();
			foreach (var node in path)
			{
				if (node != null)
					rawPositions.Add(node.transform.position + Vector3.up * heightOffset);
			}

			List<Vector3> smooth = CreateSmoothPath(rawPositions);
			lr.positionCount = smooth.Count;
			lr.SetPositions(smooth.ToArray());

			float totalLength = CalculatePathLength(rawPositions);
			float tilingFactor = totalLength / (dashLength + gapLength);
			mat.mainTextureScale = new Vector2(tilingFactor, 1f);
		}

		#endregion

		#region Line Renderer Setup

		private void SetupLineRenderer()
		{
			lineRenderer = GetComponent<LineRenderer>();
			if (lineRenderer == null)
				lineRenderer = gameObject.AddComponent<LineRenderer>();

			lineMaterial = CreateLineMaterial();
			ConfigureLineRenderer(lineRenderer, lineMaterial);
		}

		private Material CreateLineMaterial()
		{
			Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
			Material mat = new Material(shader);
			mat.color = lineColor;
			CreateDashTexture(mat);
			return mat;
		}

		private void ConfigureLineRenderer(LineRenderer lr, Material mat)
		{
			lr.startWidth = lineWidth;
			lr.endWidth = lineWidth;
			lr.material = mat;
			lr.startColor = lineColor;
			lr.endColor = lineColor;
			lr.numCornerVertices = 5;
			lr.numCapVertices = 5;
			lr.useWorldSpace = true;
			lr.textureMode = LineTextureMode.Tile;
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			lr.sortingOrder = 1000;
		}

		private void CreateDashTexture(Material mat)
		{
			int width = 64;
			int height = 4;
			Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
			tex.wrapMode = TextureWrapMode.Repeat;

			float dashRatio = dashLength / (dashLength + gapLength);
			int dashPixels = Mathf.RoundToInt(width * dashRatio);

			for (int x = 0; x < width; x++)
			{
				Color c = x < dashPixels ? Color.white : Color.clear;
				for (int y = 0; y < height; y++)
					tex.SetPixel(x, y, c);
			}

			tex.Apply();
			mat.mainTexture = tex;
		}

		#endregion

		#region Cleanup

		public void ClearPath()
		{
			if (lineRenderer != null)
				lineRenderer.positionCount = 0;
		}

		private void ClearExtraRenderers()
		{
			foreach (var lr in extraLineRenderers)
				if (lr != null) lr.positionCount = 0;
		}

		private void DestroyExtraRenderers()
		{
			foreach (var mat in extraMaterials)
				DestroyLineMaterial(mat);

			extraMaterials.Clear();

			foreach (var lr in extraLineRenderers)
				if (lr != null) Destroy(lr.gameObject);

			extraLineRenderers.Clear();
		}

		private void DestroyLineMaterial(Material mat)
		{
			if (mat == null) return;
			if (mat.mainTexture != null) Destroy(mat.mainTexture);
			Destroy(mat);
		}

		#endregion

		#region Math Helpers

		private List<Vector3> CreateSmoothPath(List<Vector3> waypoints)
		{
			List<Vector3> smooth = new List<Vector3>();

			for (int i = 0; i < waypoints.Count - 1; i++)
			{
				Vector3 start = waypoints[i];
				Vector3 end = waypoints[i + 1];
				int points = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(start, end) * pointsPerUnit));

				for (int j = 0; j < points; j++)
					smooth.Add(Vector3.Lerp(start, end, j / (float)points));
			}

			smooth.Add(waypoints[waypoints.Count - 1]);
			return smooth;
		}

		private float CalculatePathLength(List<Vector3> positions)
		{
			float length = 0f;
			for (int i = 0; i < positions.Count - 1; i++)
				length += Vector3.Distance(positions[i], positions[i + 1]);
			return length;
		}

		#endregion

		#region Public API

		public void SetPathToVisualize(int startIdx, int endIdx)
		{
			startNodeIndex = startIdx;
			endNodeIndex = endIdx;
			drawAllPaths = false;
			InitializeAndDrawPath();
		}

		public void SetPathToVisualize(PathNode startNode, PathNode endNode)
		{
			currentStartNode = startNode;
			currentEndNode = endNode;
			drawAllPaths = false;
			RefreshPath();
		}

		public void SetDrawAllPaths(bool value)
		{
			drawAllPaths = value;
			RefreshPath();
		}

		public void SetLineColor(Color color)
		{
			lineColor = color;
			lineRenderer.startColor = color;
			lineRenderer.endColor = color;
			if (lineMaterial != null) lineMaterial.color = color;
		}

		public void SetLineWidth(float width)
		{
			lineWidth = width;
			lineRenderer.startWidth = width;
			lineRenderer.endWidth = width;
		}

		public void ToggleAnimation(bool enabled) => animateLine = enabled;

		[ContextMenu("Refresh Path Now")]
		private void RefreshPathFromContextMenu()
		{
			if (Application.isPlaying) InitializeAndDrawPath();
		}

		#endregion
	}
}