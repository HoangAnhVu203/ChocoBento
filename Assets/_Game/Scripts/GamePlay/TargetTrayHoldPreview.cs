using System.Collections.Generic;
using UnityEngine;

public sealed class TargetTrayHoldPreview : MonoBehaviour
{
    [SerializeField] private Color dashColor = new(0.05f, 1f, 0.18f, 1f);
    [SerializeField] private Color fillColor = new(0.34f, 0.86f, 0.3f, 0.65f);
    [SerializeField] private string slotNameContains = "TraySlot";
    [SerializeField] private float fillPadding = 0.04f;
    [SerializeField] private float dashLineWidth = 0.0525f;
    [SerializeField] private float dashLength = 0.16f;
    [SerializeField] private float dashGap = 0.1f;
    [SerializeField] private float dashSpeed = 0.35f;
    [SerializeField] private int fillSortingOrder = 8;
    [SerializeField] private int dashSortingOrder = 10;

    private readonly List<DashedEdge> dashedEdges = new();
    private readonly List<HoldBlock> holdBlocks = new();
    private GameObject previewRoot;
    private Material lineMaterial;
    private Sprite fillSprite;
    private Texture2D fillTexture;
    private ChocolatePiece[] pieces = System.Array.Empty<ChocolatePiece>();

    private void Start()
    {
        BuildPreview();
    }

    private void Update()
    {
        if (previewRoot == null)
        {
            BuildPreview();
        }

        UpdateDashLines();
        UpdateFillVisibility();
    }

    private void OnDestroy()
    {
        if (previewRoot != null)
        {
            Destroy(previewRoot);
        }

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

    private void BuildPreview()
    {
        if (GridManager.Instance == null || !TryGetSlotBounds(out var minCell, out var maxCell))
        {
            return;
        }

        previewRoot = new GameObject("TargetTrayHoldPreview");
        previewRoot.transform.SetParent(transform, false);
        previewRoot.transform.localPosition = Vector3.zero;

        float cellSize = GridManager.Instance.CellSize;
        float left = GridManager.Instance.CellToWorldCenter(minCell).x - cellSize * 0.5f;
        float right = GridManager.Instance.CellToWorldCenter(maxCell).x + cellSize * 0.5f;
        float bottom = GridManager.Instance.CellToWorldCenter(minCell).y - cellSize * 0.5f;
        float top = GridManager.Instance.CellToWorldCenter(maxCell).y + cellSize * 0.5f;

        for (float rowBottom = bottom; rowBottom < top - 0.001f; rowBottom += cellSize)
        {
            float rowTop = Mathf.Min(rowBottom + cellSize, top);
            AddHoldBlock(left, right, rowBottom, rowTop);
        }
    }

    private bool TryGetSlotBounds(out Vector3Int minCell, out Vector3Int maxCell)
    {
        minCell = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        maxCell = new Vector3Int(int.MinValue, int.MinValue, 0);

        var children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == transform || !children[i].name.Contains(slotNameContains))
            {
                continue;
            }

            var cell = GridManager.Instance.WorldToCell(children[i].position);
            minCell.x = Mathf.Min(minCell.x, cell.x);
            minCell.y = Mathf.Min(minCell.y, cell.y);
            maxCell.x = Mathf.Max(maxCell.x, cell.x);
            maxCell.y = Mathf.Max(maxCell.y, cell.y);
        }

        return minCell.x != int.MaxValue;
    }

    private void AddFill(float left, float right, float bottom, float top)
    {
        var fillObject = new GameObject("HoldFill");
        fillObject.transform.SetParent(previewRoot.transform, false);
        fillObject.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        fillObject.transform.localScale = new Vector3(Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom), 1f);

        var renderer = fillObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFillSprite();
        renderer.color = fillColor;
        renderer.sortingOrder = fillSortingOrder;

        var center = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        var size = new Vector3(Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom), 1f);
        holdBlocks.Add(new HoldBlock(renderer, new Bounds(center, size)));
    }

    private void AddHoldBlock(float left, float right, float bottom, float top)
    {
        AddFill(left + fillPadding, right - fillPadding, bottom + fillPadding, top - fillPadding);
        AddDashedEdge(new Vector3(left, bottom, 0f), new Vector3(right, bottom, 0f));
        AddDashedEdge(new Vector3(right, bottom, 0f), new Vector3(right, top, 0f));
        AddDashedEdge(new Vector3(right, top, 0f), new Vector3(left, top, 0f));
        AddDashedEdge(new Vector3(left, top, 0f), new Vector3(left, bottom, 0f));
    }

    private void UpdateFillVisibility()
    {
        if (holdBlocks.Count == 0)
        {
            return;
        }

        pieces = FindObjectsByType<ChocolatePiece>(FindObjectsSortMode.None);
        for (int i = 0; i < holdBlocks.Count; i++)
        {
            var block = holdBlocks[i];
            if (block.Fill != null)
            {
                block.Fill.enabled = !IsBlockCoveredByChocolate(block.Bounds);
            }
        }
    }

    private bool IsBlockCoveredByChocolate(Bounds blockBounds)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && BoundsOverlap2D(blockBounds, GetPieceCellBounds(pieces[i])))
            {
                return true;
            }
        }

        return false;
    }

    private Bounds GetPieceCellBounds(ChocolatePiece piece)
    {
        var cells = piece.RefreshedCells;
        if (cells.Count == 0)
        {
            return new Bounds(piece.transform.position, Vector3.zero);
        }

        var bounds = new Bounds(cells[0].position, Vector3.zero);
        for (int i = 1; i < cells.Count; i++)
        {
            bounds.Encapsulate(cells[i].position);
        }

        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 0.5f;
        bounds.Expand(new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1f));
        return bounds;
    }

    private bool BoundsOverlap2D(Bounds a, Bounds b)
    {
        return a.min.x < b.max.x
            && a.max.x > b.min.x
            && a.min.y < b.max.y
            && a.max.y > b.min.y;
    }

    private void AddDashedEdge(Vector3 start, Vector3 end)
    {
        float edgeLength = Vector3.Distance(start, end);
        int dashCount = Mathf.CeilToInt(edgeLength / Mathf.Max(0.01f, dashLength + dashGap)) + 2;
        var lines = new List<LineRenderer>();

        for (int i = 0; i < dashCount; i++)
        {
            var lineObject = new GameObject("HoldDash");
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

    private readonly struct HoldBlock
    {
        public readonly SpriteRenderer Fill;
        public readonly Bounds Bounds;

        public HoldBlock(SpriteRenderer fill, Bounds bounds)
        {
            Fill = fill;
            Bounds = bounds;
        }
    }
}
