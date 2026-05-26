using System.Collections.Generic;
using UnityEngine;

public sealed class ShapeHintPreview : MonoBehaviour
{
    [SerializeField] private Color dashColor = new(0.05f, 1f, 0.18f, 1f);
    [SerializeField] private Color fillColor = new(0.34f, 0.86f, 0.3f, 0.65f);
    [SerializeField] private float fillPadding = 0.04f;
    [SerializeField] private float dashLineWidth = 0.0525f;
    [SerializeField] private float dashLength = 0.16f;
    [SerializeField] private float dashGap = 0.1f;
    [SerializeField] private float dashSpeed = 0.35f;
    [SerializeField] private int fillSortingOrder = -5;
    [SerializeField] private int dashSortingOrder = -4;

    private readonly List<DashedEdge> dashedEdges = new();
    private GameObject previewRoot;
    private Material lineMaterial;
    private Sprite fillSprite;
    private Texture2D fillTexture;

    public bool IsShowing => previewRoot != null && previewRoot.activeSelf;

    private void Update()
    {
        UpdateDashLines();
    }

    private void OnDestroy()
    {
        Clear();

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }

        if (fillSprite != null)
        {
            Destroy(fillSprite);
        }

        if (fillTexture != null)
        {
            Destroy(fillTexture);
        }
    }

    public void Show(IReadOnlyList<Vector3Int> cells)
    {
        if (GridManager.Instance == null || cells == null || cells.Count == 0)
        {
            return;
        }

        EnsurePreviewRoot();
        previewRoot.SetActive(true);

        var uniqueCells = new HashSet<Vector3Int>();
        for (int i = 0; i < cells.Count; i++)
        {
            uniqueCells.Add(cells[i]);
        }

        foreach (var cell in uniqueCells)
        {
            AddCellFill(cell);
        }

        foreach (var cell in uniqueCells)
        {
            AddBoundaryEdges(cell, uniqueCells);
        }
    }

    public void Hide()
    {
        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }

    public void Clear()
    {
        dashedEdges.Clear();
        if (previewRoot != null)
        {
            Destroy(previewRoot);
            previewRoot = null;
        }
    }

    private void EnsurePreviewRoot()
    {
        if (previewRoot != null)
        {
            return;
        }

        previewRoot = new GameObject("ShapeHintPreview");
        previewRoot.transform.SetParent(transform, false);
        previewRoot.transform.localPosition = Vector3.zero;
    }

    private void AddCellFill(Vector3Int cell)
    {
        float cellSize = GridManager.Instance.CellSize;
        var center = GridManager.Instance.CellToWorldCenter(cell);
        float half = cellSize * 0.5f;

        AddFill(
            center.x - half + fillPadding,
            center.x + half - fillPadding,
            center.y - half + fillPadding,
            center.y + half - fillPadding);
    }

    private void AddBoundaryEdges(Vector3Int cell, HashSet<Vector3Int> cells)
    {
        float cellSize = GridManager.Instance.CellSize;
        var center = GridManager.Instance.CellToWorldCenter(cell);
        float half = cellSize * 0.5f;
        float left = center.x - half;
        float right = center.x + half;
        float bottom = center.y - half;
        float top = center.y + half;

        if (!cells.Contains(cell + Vector3Int.down))
        {
            AddDashedEdge(new Vector3(left, bottom, 0f), new Vector3(right, bottom, 0f));
        }

        if (!cells.Contains(cell + Vector3Int.right))
        {
            AddDashedEdge(new Vector3(right, bottom, 0f), new Vector3(right, top, 0f));
        }

        if (!cells.Contains(cell + Vector3Int.up))
        {
            AddDashedEdge(new Vector3(right, top, 0f), new Vector3(left, top, 0f));
        }

        if (!cells.Contains(cell + Vector3Int.left))
        {
            AddDashedEdge(new Vector3(left, top, 0f), new Vector3(left, bottom, 0f));
        }
    }

    private void AddFill(float left, float right, float bottom, float top)
    {
        var fillObject = new GameObject("HintFill");
        fillObject.transform.SetParent(previewRoot.transform, false);
        fillObject.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        fillObject.transform.localScale = new Vector3(Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom), 1f);

        var renderer = fillObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFillSprite();
        renderer.color = fillColor;
        renderer.sortingOrder = fillSortingOrder;
    }

    private void AddDashedEdge(Vector3 start, Vector3 end)
    {
        float edgeLength = Vector3.Distance(start, end);
        int dashCount = Mathf.CeilToInt(edgeLength / Mathf.Max(0.01f, dashLength + dashGap)) + 2;
        var lines = new List<LineRenderer>();

        for (int i = 0; i < dashCount; i++)
        {
            var lineObject = new GameObject("HintDash");
            lineObject.transform.SetParent(previewRoot.transform, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = dashLineWidth;
            line.endWidth = dashLineWidth;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = dashSortingOrder;
            line.material = GetLineMaterial();
            line.startColor = dashColor;
            line.endColor = dashColor;
            lines.Add(line);
        }

        dashedEdges.Add(new DashedEdge(start, end, lines));
    }

    private void UpdateDashLines()
    {
        float cycleLength = Mathf.Max(0.01f, dashLength + dashGap);
        float phase = Mathf.Repeat(Time.time * dashSpeed, cycleLength);

        for (int edgeIndex = 0; edgeIndex < dashedEdges.Count; edgeIndex++)
        {
            var edge = dashedEdges[edgeIndex];
            var edgeVector = edge.End - edge.Start;
            float edgeLength = edgeVector.magnitude;
            if (edgeLength <= 0f)
            {
                continue;
            }

            var direction = edgeVector / edgeLength;
            float dashStart = -phase;
            for (int lineIndex = 0; lineIndex < edge.Lines.Count; lineIndex++)
            {
                var line = edge.Lines[lineIndex];
                if (line == null)
                {
                    continue;
                }

                float startDistance = dashStart + lineIndex * cycleLength;
                float endDistance = startDistance + dashLength;
                float clippedStart = Mathf.Clamp(startDistance, 0f, edgeLength);
                float clippedEnd = Mathf.Clamp(endDistance, 0f, edgeLength);
                bool isVisible = clippedEnd > clippedStart;

                line.enabled = isVisible;
                if (!isVisible)
                {
                    continue;
                }

                line.SetPosition(0, edge.Start + direction * clippedStart);
                line.SetPosition(1, edge.Start + direction * clippedEnd);
            }
        }
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return lineMaterial;
    }

    private Sprite GetFillSprite()
    {
        if (fillSprite == null)
        {
            fillTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            fillTexture.SetPixel(0, 0, Color.white);
            fillTexture.Apply();
            fillSprite = Sprite.Create(fillTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        return fillSprite;
    }

    private readonly struct DashedEdge
    {
        public readonly Vector3 Start;
        public readonly Vector3 End;
        public readonly List<LineRenderer> Lines;

        public DashedEdge(Vector3 start, Vector3 end, List<LineRenderer> lines)
        {
            Start = start;
            End = end;
            Lines = lines;
        }
    }
}
