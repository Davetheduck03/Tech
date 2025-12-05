using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathLineRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Color lineColor = new Color(0f, 1f, 1f, 0.8f);
        [SerializeField] private float heightOffset = 0.2f;
        
        [Header("Dotted Pattern")]
        [SerializeField] private float dashLength = 0.5f;
        [SerializeField] private float gapLength = 0.3f;
        [SerializeField] private int pointsPerSegment = 20;
        
        [Header("Animation")]
        [SerializeField] private float animationSpeed = 1f;
        [SerializeField] private bool animate = true;
        
        private LineRenderer lineRenderer;
        private Material lineMaterial;
        private float textureOffset = 0f;
        
        private void Awake()
        {
            SetupLineRenderer();
        }
        
        private void SetupLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.alignment = LineAlignment.TransformZ;
            
            // Create material with dashed shader
            CreateDashedMaterial();
        }
        
        private void CreateDashedMaterial()
        {
            // Use Unity's built-in Particles/Standard Unlit shader or create a custom one
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
                
            lineMaterial = new Material(shader);
            lineMaterial.color = lineColor;
            
            // Enable transparency
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_ZWrite", 0);
            lineMaterial.DisableKeyword("_ALPHATEST_ON");
            lineMaterial.EnableKeyword("_ALPHABLEND_ON");
            lineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            lineMaterial.renderQueue = 3000;
            
            lineRenderer.material = lineMaterial;
        }
        
        public void DrawPath(List<PathNode> path)
        {
            if (path == null || path.Count < 2)
            {
                lineRenderer.positionCount = 0;
                return;
            }
            
            List<Vector3> positions = new List<Vector3>();
            
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] != null)
                {
                    Vector3 pos = path[i].transform.position + Vector3.up * heightOffset;
                    positions.Add(pos);
                }
            }
            
            // Generate dotted line positions
            List<Vector3> dottedPositions = GenerateDottedLine(positions);
            
            lineRenderer.positionCount = dottedPositions.Count;
            lineRenderer.SetPositions(dottedPositions.ToArray());
            
            // Calculate texture tiling for proper dash appearance
            float totalLength = CalculateTotalLength(positions);
            float tilingFactor = totalLength / (dashLength + gapLength);
            lineMaterial.mainTextureScale = new Vector2(tilingFactor, 1f);
        }
        
        private List<Vector3> GenerateDottedLine(List<Vector3> waypoints)
        {
            List<Vector3> dottedPoints = new List<Vector3>();
            
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 start = waypoints[i];
                Vector3 end = waypoints[i + 1];
                float segmentLength = Vector3.Distance(start, end);
                
                float currentDistance = 0f;
                bool isDash = true;
                
                while (currentDistance < segmentLength)
                {
                    float t = currentDistance / segmentLength;
                    Vector3 point = Vector3.Lerp(start, end, t);
                    
                    if (isDash)
                    {
                        // Add dash segment
                        float dashEnd = Mathf.Min(currentDistance + dashLength, segmentLength);
                        int dashPoints = Mathf.CeilToInt((dashEnd - currentDistance) / dashLength * pointsPerSegment);
                        
                        for (int j = 0; j <= dashPoints; j++)
                        {
                            float dashT = Mathf.Lerp(currentDistance, dashEnd, j / (float)dashPoints) / segmentLength;
                            dottedPoints.Add(Vector3.Lerp(start, end, dashT));
                        }
                        
                        currentDistance += dashLength;
                    }
                    else
                    {
                        // Skip gap - add a duplicate point to create break
                        if (dottedPoints.Count > 0)
                        {
                            dottedPoints.Add(dottedPoints[dottedPoints.Count - 1]);
                            dottedPoints.Add(Vector3.Lerp(start, end, Mathf.Min((currentDistance + gapLength) / segmentLength, 1f)));
                        }
                        currentDistance += gapLength;
                    }
                    
                    isDash = !isDash;
                }
            }
            
            return dottedPoints;
        }
        
        private float CalculateTotalLength(List<Vector3> positions)
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
            if (animate && lineMaterial != null)
            {
                textureOffset += animationSpeed * Time.deltaTime;
                lineMaterial.mainTextureOffset = new Vector2(textureOffset, 0f);
            }
        }
        
        public void ClearPath()
        {
            lineRenderer.positionCount = 0;
        }
        
        public void SetLineColor(Color color)
        {
            lineColor = color;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            if (lineMaterial != null)
                lineMaterial.color = color;
        }
        
        public void SetLineWidth(float width)
        {
            lineWidth = width;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
        
        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = speed;
        }
        
        private void OnDestroy()
        {
            if (lineMaterial != null)
                Destroy(lineMaterial);
        }
    }
}