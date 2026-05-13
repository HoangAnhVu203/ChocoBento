using System.Collections.Generic;
using UnityEngine;

public sealed class ChocolatePlacementPreview : MonoBehaviour
{
    [SerializeField] private Color validColor = new(0.05f, 1f, 0.18f, 1f);
    [SerializeField] private Color invalidColor = new(1f, 0.05f, 0.05f, 1f);
    [SerializeField] private float lineWidth = 0.0525f;
    [SerializeField] private float dashLength = 0.16f;
    [SerializeField] private float dashGap = 0.1f;
    [SerializeField] private float dashSpeed = 0.35f;
    [SerializeField] private int sortingOrder = 10;

    private readonly List<DashedEdge> dashedEdges = new();
    private GameObject previewRoot;
    private ChocolatePiece piece;
    private Material lineMaterial;
    private Color currentColor;

    private void OnDestroy()
    {
        DestroyPreviewRoot();
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }

    public void Show(ChocolatePiece targetPiece)
    {
        piece = targetPiece;
        BuildPreview();
        if (previewRoot != null)
        {
            previewRoot.SetActive(true);
        }

        UpdatePreview();
    }

    public void UpdatePreview()
    {
        if (piece == null || GridManager.Instance == null || previewRoot == null)
        {
            return;
        }

        var desiredAnchor = GridManager.Instance.WorldToCell(piece.transform.position);
        var previewAnchor = desiredAnchor;
        var isValid = GameplayLevelController.Instance == null
            || GameplayLevelController.Instance.IsPlacementFree(piece, desiredAnchor);

        if (TargetTray.Instance != null
            && TargetTray.Instance.TryGetPreviewPlacement(piece, desiredAnchor, out var trayAnchor, out var trayPlacementValid))
        {
            previewAnchor = trayAnchor;
            isValid = trayPlacementValid
                && (GameplayLevelController.Instance == null || GameplayLevelController.Instance.IsPlacementFree(piece, trayAnchor));
        }

        previewRoot.transform.position = GridManager.Instance.CellToWorldCenter(previewAnchor);
        currentColor = isValid ? validColor : invalidColor;
        UpdateDashLines(currentColor);
    }

    public void Hide()
    {
        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }

    private void BuildPreview()
    {
        DestroyPreviewRoot();
        if (piece == null)
        {
            return;
        }

        previewRoot = new GameObject($"{piece.name}_PlacementPreview");
        dashedEdges.Clear();

        var cellSize = GridManager.Instance.CellSize;
        var cells = piece.RefreshedCells;
        var occupiedOffsets = new HashSet<Vector3Int>();
        for (int i = 0; i < cells.Count; i++)
        {
            occupiedOffsets.Add(piece.GetCellOffset(cells[i]));
        }

        foreach (var offset in occupiedOffsets)
        {
            AddBoundaryEdgeIfNeeded(occupiedOffsets, offset, Vector3Int.left, new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, 0.5f), cellSize);
            AddBoundaryEdgeIfNeeded(occupiedOffsets, offset, Vector3Int.right, new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), cellSize);
            AddBoundaryEdgeIfNeeded(occupiedOffsets, offset, Vector3Int.down, new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), cellSize);
            AddBoundaryEdgeIfNeeded(occupiedOffsets, offset, Vector3Int.up, new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), cellSize);
        }
    }

    private void SetColor(Color color)
    {
        for (int edgeIndex = 0; edgeIndex < dashedEdges.Count; edgeIndex++)
        {
            var lines = dashedEdges[edgeIndex].Lines;
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                if (lines[lineIndex] != null)
                {
                    lines[lineIndex].startColor = color;
                    lines[lineIndex].endColor = color;
                }
            }
        }
    }

    private void UpdateDashLines(Color color)
    {
        SetColor(color);

        var cycleLength = Mathf.Max(0.01f, dashLength + dashGap);
        var phase = Mathf.Repeat(Time.time * dashSpeed, cycleLength);
        for (int edgeIndex = 0; edgeIndex < dashedEdges.Count; edgeIndex++)
        {
            var edge = dashedEdges[edgeIndex];
            var edgeVector = edge.End - edge.Start;
            var edgeLength = edgeVector.magnitude;
            if (edgeLength <= 0f)
            {
                continue;
            }

            var direction = edgeVector / edgeLength;
            var dashStart = -phase;
            for (int lineIndex = 0; lineIndex < edge.Lines.Count; lineIndex++)
            {
                var line = edge.Lines[lineIndex];
                if (line == null)
                {
                    continue;
                }

                var startDistance = dashStart + lineIndex * cycleLength;
                var endDistance = startDistance + dashLength;
                var clippedStart = Mathf.Clamp(startDistance, 0f, edgeLength);
                var clippedEnd = Mathf.Clamp(endDistance, 0f, edgeLength);
                var isVisible = clippedEnd > clippedStart;

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

    private void AddBoundaryEdgeIfNeeded(
        HashSet<Vector3Int> occupiedOffsets,
        Vector3Int cellOffset,
        Vector3Int neighborDirection,
        Vector2 localStart,
        Vector2 localEnd,
        float cellSize)
    {
        if (occupiedOffsets.Contains(cellOffset + neighborDirection))
        {
            return;
        }

        var cellCenter = new Vector3(cellOffset.x * cellSize, cellOffset.y * cellSize, 0f);
        var start = cellCenter + new Vector3(localStart.x * cellSize, localStart.y * cellSize, 0f);
        var end = cellCenter + new Vector3(localEnd.x * cellSize, localEnd.y * cellSize, 0f);
        var edgeLength = Vector3.Distance(start, end);
        var dashCount = Mathf.CeilToInt(edgeLength / Mathf.Max(0.01f, dashLength + dashGap)) + 2;
        var lines = new List<LineRenderer>();

        for (int i = 0; i < dashCount; i++)
        {
            var edgeObject = new GameObject("PreviewDash");
            edgeObject.transform.SetParent(previewRoot.transform, false);

            var line = edgeObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = sortingOrder;
            line.material = GetLineMaterial();
            line.startColor = validColor;
            line.endColor = validColor;
            lines.Add(line);
        }

        dashedEdges.Add(new DashedEdge(start, end, lines));
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return lineMaterial;
    }

    private void DestroyPreviewRoot()
    {
        if (previewRoot == null)
        {
            return;
        }

        Destroy(previewRoot);
        previewRoot = null;
        dashedEdges.Clear();
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
