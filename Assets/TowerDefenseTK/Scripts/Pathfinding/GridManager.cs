using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
	public static GridManager Instance;

	[Header("Grid Settings")]
	public int width = 10;
	public int height = 10;
	public float cellSize = 1f;

	[Header("Grid Line Settings")]
	public bool drawGridInGame = true;
	public Color lineColor = new Color(1f, 1f, 1f, 0.4f);
	[Tooltip("Raise this if lines clip into your ground plane.")]
	public float lineHeightOffset = 0.05f;

	private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

	// GL drawing needs a plain unlit material — created once, never destroyed
	private Material glMaterial;

	private Vector3 Origin => transform.position;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	private void Awake()
	{
		Instance = this;
		GenerateGrid();
		CreateGLMaterial();
	}

	private void CreateGLMaterial()
	{
		// Standard hidden shader that works in every render pipeline
		Shader shader = Shader.Find("Hidden/Internal-Colored");
		if (shader == null)
		{
			// Fallback — exists in all Unity versions
			shader = Shader.Find("Sprites/Default");
		}

		glMaterial = new Material(shader);
		glMaterial.hideFlags = HideFlags.HideAndDontSave;
		// Turn off depth write so lines draw on top of the ground
		glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		glMaterial.SetInt("_ZWrite", 0);
	}

	// OnPostRender is called by the camera after it finishes rendering.
	// This must be on a script attached to the camera, OR we use Camera.onPostRender.
	// We register via the callback so this script can live on any GameObject.
	private void OnEnable()
	{
		Camera.onPostRender += DrawGL;
	}

	private void OnDisable()
	{
		Camera.onPostRender -= DrawGL;
	}

	private void OnDestroy()
	{
		if (glMaterial != null)
			DestroyImmediate(glMaterial);
	}

	// ── GL Drawing ────────────────────────────────────────────────────────────

	private void DrawGL(Camera cam)
	{
		if (!drawGridInGame) return;
		if (glMaterial == null) return;
		if (!Application.isPlaying) return;

		// Only draw for the main camera (skip reflection probes, shadow maps etc.)
		if (cam != Camera.main) return;

		glMaterial.SetPass(0);

		GL.PushMatrix();
		GL.MultMatrix(Matrix4x4.identity); // world space

		GL.Begin(GL.LINES);
		GL.Color(lineColor);

		float totalW = width * cellSize;
		float totalH = height * cellSize;
		float y = Origin.y + lineHeightOffset;

		// Vertical lines (run along Z axis)
		for (int x = 0; x <= width; x++)
		{
			float xPos = Origin.x + x * cellSize;
			GL.Vertex3(xPos, y, Origin.z);
			GL.Vertex3(xPos, y, Origin.z + totalH);
		}

		// Horizontal lines (run along X axis)
		for (int z = 0; z <= height; z++)
		{
			float zPos = Origin.z + z * cellSize;
			GL.Vertex3(Origin.x, y, zPos);
			GL.Vertex3(Origin.x + totalW, y, zPos);
		}

		GL.End();
		GL.PopMatrix();
	}

	// ── Grid Data ─────────────────────────────────────────────────────────────

	public Dictionary<Vector2Int, GridNode> GetAllNodes() => grid;

	private void GenerateGrid()
	{
		grid.Clear();

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				Vector2Int coords = new Vector2Int(x, y);
				grid[coords] = new GridNode(coords, GridToWorld(coords));
			}
		}
	}

	public Vector3 GridToWorld(Vector2Int coords)
	{
		return Origin + new Vector3(coords.x * cellSize, 0f, coords.y * cellSize);
	}

	public bool TryGetNode(Vector2Int coords, out GridNode node) =>
		grid.TryGetValue(coords, out node);

	public Vector2Int WorldToGrid(Vector3 world)
	{
		Vector3 local = world - Origin;
		return new Vector2Int(
			Mathf.FloorToInt(local.x / cellSize),
			Mathf.FloorToInt(local.z / cellSize)
		);
	}

	public bool IsCellOccupied(Vector2Int coords) =>
		grid.ContainsKey(coords) && grid[coords].occupied;

	public void SetCellOccupied(Vector2Int coords, bool value)
	{
		if (grid.ContainsKey(coords))
			grid[coords].occupied = value;
	}

	// ── Scene-view Gizmos ─────────────────────────────────────────────────────

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0f, 1f, 0f, 0.25f);

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				Vector3 bl = Origin + new Vector3(x * cellSize, 0.01f, y * cellSize);
				Vector3 br = bl + new Vector3(cellSize, 0f, 0f);
				Vector3 tr = bl + new Vector3(cellSize, 0f, cellSize);
				Vector3 tl = bl + new Vector3(0f, 0f, cellSize);

				Gizmos.DrawLine(bl, br);
				Gizmos.DrawLine(br, tr);
				Gizmos.DrawLine(tr, tl);
				Gizmos.DrawLine(tl, bl);
			}
		}
	}
}