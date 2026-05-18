using System.Collections.Generic;
using UnityEngine;

public sealed class GameplayLevelController : MonoBehaviour
{
    public static GameplayLevelController Instance { get; private set; }

    [SerializeField] private PanelGameplay gameplayPanel;
    [SerializeField] private KnifeDrag selectedKnife;
    [SerializeField] private ChocolatePiece selectedPiece;
    [SerializeField, Range(0.7f, 1f)] private float cutEdgeCellScale = 0.84f;
    [SerializeField] private ParticleSystem cutChocoParticlePrefab;
    [SerializeField, Min(0.1f)] private float cutChocoParticleCleanupDelay = 2f;

    private readonly List<ChocolatePiece> pieces = new();
    private readonly Dictionary<ChocolatePiece, List<CutMark>> cutMarksByPiece = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshLevelObjects();
        SetCanCut(false);
    }

    public void RefreshLevelObjects()
    {
        pieces.Clear();
        pieces.AddRange(FindObjectsByType<ChocolatePiece>(FindObjectsSortMode.None));
        cutMarksByPiece.Clear();
        selectedKnife = null;
        selectedPiece = null;

        gameplayPanel = PanelGameplay.Instance != null ? PanelGameplay.Instance : FindFirstObjectByType<PanelGameplay>();
    }

    public void SetSelectedKnife(KnifeDrag knife)
    {
        selectedKnife = knife;
    }

    public void SetSelectedPiece(ChocolatePiece piece)
    {
        selectedPiece = piece;
    }

    public void SetCanCut(bool canCut)
    {
        if (gameplayPanel == null)
        {
            gameplayPanel = PanelGameplay.Instance != null ? PanelGameplay.Instance : FindFirstObjectByType<PanelGameplay>();
        }

        if (gameplayPanel != null)
        {
            gameplayPanel.SetCanCut(canCut || HasAnyValidKnife());
        }
    }

    public ChocolatePiece FindPieceAt(Vector3 worldPosition)
    {
        pieces.RemoveAll(piece => piece == null);

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i].ContainsWorldPoint(worldPosition))
            {
                selectedPiece = pieces[i];
                return pieces[i];
            }
        }

        return null;
    }

    public ChocolatePiece FindPieceForKnife(KnifeCutDirection direction, Vector3 knifePosition, int bladeLengthInCells)
    {
        pieces.RemoveAll(piece => piece == null);

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (TryGetKnifeCutData(pieces[i], direction, knifePosition, bladeLengthInCells, out _, out _, out _))
            {
                return pieces[i];
            }
        }

        return null;
    }

    public List<KnifeCutTarget> FindCutTargetsForKnife(KnifeCutDirection direction, Vector3 knifePosition, int bladeLengthInCells)
    {
        pieces.RemoveAll(piece => piece == null);

        var targets = new List<KnifeCutTarget>();
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (TryGetKnifeCutData(pieces[i], direction, knifePosition, bladeLengthInCells, out var lineIndex, out var segmentStart, out var segmentLength))
            {
                targets.Add(new KnifeCutTarget(pieces[i], lineIndex, segmentStart, segmentLength));
            }
        }

        return targets;
    }

    public bool TryGetKnifeCutData(
        ChocolatePiece piece,
        KnifeCutDirection direction,
        Vector3 knifePosition,
        int bladeLengthInCells,
        out int lineIndex,
        out int segmentStart,
        out int segmentLength)
    {
        lineIndex = -1;
        segmentStart = 0;
        segmentLength = 0;

        if (piece == null || GridManager.Instance == null)
        {
            return false;
        }

        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);
        var cellSize = GridManager.Instance.CellSize;

        if (direction == KnifeCutDirection.Vertical)
        {
            var width = maxCell.x - minCell.x + 1;
            var height = maxCell.y - minCell.y + 1;
            var leftBoundaryX = GridManager.Instance.CellToWorldCenter(minCell).x - cellSize * 0.5f;
            lineIndex = Mathf.RoundToInt((knifePosition.x - leftBoundaryX) / cellSize);

            if (lineIndex <= 0 || lineIndex >= width)
            {
                return false;
            }

            var bottomCellCenterY = GridManager.Instance.CellToWorldCenter(minCell).y;
            var centerIndex = Mathf.RoundToInt((knifePosition.y - bottomCellCenterY) / cellSize);
            CalculateSegmentIntersection(centerIndex, bladeLengthInCells, height, out segmentStart, out segmentLength);
            return segmentLength > 0 && SegmentHasUncutUnit(piece, direction, lineIndex, segmentStart, segmentLength);
        }

        var pieceWidth = maxCell.x - minCell.x + 1;
        var pieceHeight = maxCell.y - minCell.y + 1;
        var bottomBoundaryY = GridManager.Instance.CellToWorldCenter(minCell).y - cellSize * 0.5f;
        lineIndex = Mathf.RoundToInt((knifePosition.y - bottomBoundaryY) / cellSize);

        if (lineIndex <= 0 || lineIndex >= pieceHeight)
        {
            return false;
        }

        var leftCenterX = GridManager.Instance.CellToWorldCenter(minCell).x;
        var horizontalCenterIndex = Mathf.RoundToInt((knifePosition.x - leftCenterX) / cellSize);
        CalculateSegmentIntersection(horizontalCenterIndex, bladeLengthInCells, pieceWidth, out segmentStart, out segmentLength);
        return segmentLength > 0 && SegmentHasUncutUnit(piece, direction, lineIndex, segmentStart, segmentLength);
    }

    private static void CalculateSegmentIntersection(int centerIndex, int bladeLengthInCells, int availableLength, out int segmentStart, out int segmentLength)
    {
        var rawStart = centerIndex - bladeLengthInCells / 2;
        var rawEnd = rawStart + bladeLengthInCells;

        segmentStart = Mathf.Clamp(rawStart, 0, availableLength);
        var segmentEnd = Mathf.Clamp(rawEnd, 0, availableLength);
        segmentLength = Mathf.Max(0, segmentEnd - segmentStart);
    }

    public void ResolvePieceOverlap(ChocolatePiece movingPiece)
    {
        if (GridManager.Instance == null || movingPiece == null)
        {
            return;
        }

        pieces.RemoveAll(piece => piece == null);
        if (!pieces.Contains(movingPiece))
        {
            pieces.Add(movingPiece);
        }

        var desiredAnchor = GridManager.Instance.WorldToCell(movingPiece.transform.position);
        var resolvedAnchor = FindNearestFreeAnchor(movingPiece, desiredAnchor);
        movingPiece.transform.position = GridManager.Instance.CellToWorldCenter(resolvedAnchor);
    }

    public bool IsPlacementFree(ChocolatePiece movingPiece, Vector3Int candidateAnchor)
    {
        if (movingPiece == null)
        {
            return false;
        }

        pieces.RemoveAll(piece => piece == null);
        if (!pieces.Contains(movingPiece))
        {
            pieces.Add(movingPiece);
        }

        return !WouldOverlap(movingPiece, candidateAnchor);
    }

    public void LogTargetTrayProgress()
    {
        pieces.RemoveAll(piece => piece == null);

        if (TargetTray.Instance != null)
        {
            TargetTray.Instance.RefreshMismatchMarkers(pieces);
            var filledSlots = TargetTray.Instance.CountFilledSlots(pieces);
            var slotCount = TargetTray.Instance.SlotCount;

            Debug.Log($"[TargetTray] Tray slots filled: {filledSlots}/{slotCount}");

            if (slotCount > 0 && filledSlots == slotCount)
            {
                Debug.Log("[TargetTray] All tray slots are filled.");
                GameManager.Instance.WinLevel();
            }
        }

        var placedCount = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].IsInTargetTray)
            {
                placedCount++;
            }
        }

        Debug.Log($"[TargetTray] Choco pieces placed: {placedCount}/{pieces.Count}");

        if (pieces.Count > 0 && placedCount == pieces.Count)
        {
            Debug.Log("[TargetTray] All choco pieces are in the tray.");
            GameManager.Instance.WinLevel();
        }
    }

    private Vector3Int FindNearestFreeAnchor(ChocolatePiece movingPiece, Vector3Int desiredAnchor)
    {
        if (!WouldOverlap(movingPiece, desiredAnchor))
        {
            return desiredAnchor;
        }

        const int maxSearchRadius = 8;
        var directions = new[]
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0),
        };

        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                var candidate = desiredAnchor + directions[i] * radius;
                if (!WouldOverlap(movingPiece, candidate))
                {
                    return candidate;
                }
            }

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    var candidate = desiredAnchor + new Vector3Int(x, y, 0);
                    if (!WouldOverlap(movingPiece, candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return desiredAnchor;
    }

    private bool WouldOverlap(ChocolatePiece movingPiece, Vector3Int candidateAnchor)
    {
        var movingCells = movingPiece.GetOccupiedCellsAt(candidateAnchor);
        for (int i = 0; i < pieces.Count; i++)
        {
            var otherPiece = pieces[i];
            if (otherPiece == null || otherPiece == movingPiece)
            {
                continue;
            }

            var otherCells = otherPiece.GetOccupiedCells();
            for (int movingIndex = 0; movingIndex < movingCells.Count; movingIndex++)
            {
                for (int otherIndex = 0; otherIndex < otherCells.Count; otherIndex++)
                {
                    if (movingCells[movingIndex] == otherCells[otherIndex])
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public int CutSelectedPiece()
    {
        var knives = FindObjectsByType<KnifeDrag>(FindObjectsSortMode.None);
        var appliedCutCount = 0;

        for (int i = 0; i < knives.Length; i++)
        {
            if (!knives[i].RefreshCutData(false))
            {
                continue;
            }

            var piece = knives[i].HoveredPiece;
            if (piece == null)
            {
                continue;
            }

            var targets = FindCutTargetsForKnife(knives[i].CutDirection, knives[i].transform.position, knives[i].BladeLengthInCells);
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                var target = targets[targetIndex];
                if (target.Piece == null)
                {
                    continue;
                }

                var cutApplied = knives[i].CutDirection == KnifeCutDirection.Vertical
                    ? RegisterVerticalCut(target.Piece, target.LineIndex, target.SegmentStart, target.SegmentLength)
                    : RegisterHorizontalCut(target.Piece, target.LineIndex, target.SegmentStart, target.SegmentLength);

                if (cutApplied)
                {
                    appliedCutCount++;
                }
            }
        }

        if (appliedCutCount == 0)
        {
            SetCanCut(false);
            return 0;
        }

        SetCanCut(HasAnyValidKnife());
        return appliedCutCount;
    }

    private bool HasAnyValidKnife()
    {
        var knives = FindObjectsByType<KnifeDrag>(FindObjectsSortMode.None);
        for (int i = 0; i < knives.Length; i++)
        {
            if (knives[i] != null && knives[i].RefreshCutData(false))
            {
                return true;
            }
        }

        return false;
    }

    private bool RegisterVerticalCut(ChocolatePiece piece, int cutIndex, int segmentStart, int segmentLength)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);
        var width = maxCell.x - minCell.x + 1;
        if (cutIndex <= 0 || cutIndex >= width)
        {
            return false;
        }

        if (!SegmentHasUncutUnit(piece, KnifeCutDirection.Vertical, cutIndex, segmentStart, segmentLength))
        {
            return false;
        }

        AddCutMark(piece, new CutMark(KnifeCutDirection.Vertical, cutIndex, segmentStart, segmentLength));
        ApplyPartialCutInset(piece, KnifeCutDirection.Vertical, minCell, maxCell, cutIndex, segmentStart, segmentLength);
        PlayCutChocoParticle(piece, KnifeCutDirection.Vertical, minCell, cutIndex, segmentStart, segmentLength);
        SplitDisconnectedComponents(piece);
        return true;
    }

    private bool RegisterHorizontalCut(ChocolatePiece piece, int cutIndex, int segmentStart, int segmentLength)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);
        var height = maxCell.y - minCell.y + 1;
        if (cutIndex <= 0 || cutIndex >= height)
        {
            return false;
        }

        if (!SegmentHasUncutUnit(piece, KnifeCutDirection.Horizontal, cutIndex, segmentStart, segmentLength))
        {
            return false;
        }

        AddCutMark(piece, new CutMark(KnifeCutDirection.Horizontal, cutIndex, segmentStart, segmentLength));
        ApplyPartialCutInset(piece, KnifeCutDirection.Horizontal, minCell, maxCell, cutIndex, segmentStart, segmentLength);
        PlayCutChocoParticle(piece, KnifeCutDirection.Horizontal, minCell, cutIndex, segmentStart, segmentLength);
        SplitDisconnectedComponents(piece);
        return true;
    }

    private void AddCutMark(ChocolatePiece piece, CutMark mark)
    {
        if (!cutMarksByPiece.TryGetValue(piece, out var marks))
        {
            marks = new List<CutMark>();
            cutMarksByPiece.Add(piece, marks);
        }

        marks.Add(mark);
    }

    private bool SegmentHasUncutUnit(ChocolatePiece piece, KnifeCutDirection direction, int lineIndex, int segmentStart, int segmentLength)
    {
        if (segmentLength <= 0)
        {
            return false;
        }

        for (int i = segmentStart; i < segmentStart + segmentLength; i++)
        {
            if (!IsCutUnitMarked(piece, direction, lineIndex, i))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCutUnitMarked(ChocolatePiece piece, KnifeCutDirection direction, int lineIndex, int unitIndex)
    {
        if (!cutMarksByPiece.TryGetValue(piece, out var marks))
        {
            return false;
        }

        for (int i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            if (mark.Direction == direction
                && mark.LineIndex == lineIndex
                && unitIndex >= mark.StartIndex
                && unitIndex < mark.StartIndex + mark.Length)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayCutChocoParticle(
        ChocolatePiece piece,
        KnifeCutDirection direction,
        Vector3Int minCell,
        int lineIndex,
        int segmentStart,
        int segmentLength)
    {
        if (cutChocoParticlePrefab == null || GridManager.Instance == null || piece == null || segmentLength <= 0)
        {
            return;
        }

        var position = GetCutSegmentCenter(direction, minCell, lineIndex, segmentStart, segmentLength);
        var particle = Instantiate(cutChocoParticlePrefab, position, cutChocoParticlePrefab.transform.rotation);
        particle.transform.SetParent(transform, true);
        BringCutParticleToFront(particle);

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        Destroy(particle.gameObject, GetParticleCleanupDelay(particle));
    }

    private static Vector3 GetCutSegmentCenter(
        KnifeCutDirection direction,
        Vector3Int minCell,
        int lineIndex,
        int segmentStart,
        int segmentLength)
    {
        var cellSize = GridManager.Instance.CellSize;
        var minCenter = GridManager.Instance.CellToWorldCenter(minCell);

        if (direction == KnifeCutDirection.Vertical)
        {
            return new Vector3(
                minCenter.x + (lineIndex - 0.5f) * cellSize,
                minCenter.y + (segmentStart + segmentLength * 0.5f - 0.5f) * cellSize,
                0f);
        }

        return new Vector3(
            minCenter.x + (segmentStart + segmentLength * 0.5f - 0.5f) * cellSize,
            minCenter.y + (lineIndex - 0.5f) * cellSize,
            0f);
    }

    private static void BringCutParticleToFront(ParticleSystem particle)
    {
        var renderers = particle.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = Mathf.Max(renderers[i].sortingOrder, 10);
        }
    }

    private float GetParticleCleanupDelay(ParticleSystem particle)
    {
        if (particle == null)
        {
            return cutChocoParticleCleanupDelay;
        }

        var cleanupDelay = cutChocoParticleCleanupDelay;
        var particles = particle.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            cleanupDelay = Mathf.Max(cleanupDelay, main.duration + main.startLifetime.constantMax);
        }

        return cleanupDelay;
    }

    private void SplitDisconnectedComponents(ChocolatePiece source)
    {
        if (source == null || GridManager.Instance == null || !cutMarksByPiece.TryGetValue(source, out var sourceMarks))
        {
            return;
        }

        source.GetOccupiedCellBounds(out var sourceMinCell, out _);

        var cellMap = new Dictionary<Vector3Int, Transform>();
        var sourceCells = source.RefreshedCells;
        for (int i = 0; i < sourceCells.Count; i++)
        {
            var child = sourceCells[i];
            cellMap[source.GetCell(child)] = child;
        }

        var components = FindConnectedComponents(source, cellMap, sourceMinCell);
        if (components.Count <= 1)
        {
            return;
        }

        var newPieces = new List<ChocolatePiece>();
        var pieceParent = source.transform.parent;
        for (int i = 0; i < components.Count; i++)
        {
            GetComponentCellBounds(source, components[i], out var componentMinCell, out var componentMaxCell);
            var root = CreatePieceRoot($"{source.name}_Part{i + 1}", GridManager.Instance.CellToWorldCenter(componentMinCell), pieceParent);
            for (int cellIndex = 0; cellIndex < components[i].Count; cellIndex++)
            {
                var child = components[i][cellIndex];
                var logicCell = source.GetCell(child);
                child.SetParent(root.transform, true);
                var visual = GetOrAddCutVisual(child);
                visual.CellOffset = logicCell - componentMinCell;
                visual.HasCellOffset = true;
                child.localPosition = new Vector3(
                    visual.CellOffset.x * GridManager.Instance.CellSize,
                    visual.CellOffset.y * GridManager.Instance.CellSize,
                    child.localPosition.z);
                visual.BaseLocalPosition = child.localPosition;
                if (!visual.HasBaseTransform)
                {
                    visual.BaseLocalScale = child.localScale;
                }
                visual.HasBaseTransform = true;
                ApplyCellCutVisual(child, visual, GridManager.Instance.CellSize * (1f - cutEdgeCellScale) * 0.5f);
            }

            var piece = root.AddComponent<ChocolatePiece>();
            piece.Configure(new Vector2Int(componentMaxCell.x - componentMinCell.x + 1, componentMaxCell.y - componentMinCell.y + 1), false);
            newPieces.Add(piece);
        }

        pieces.Remove(source);
        pieces.AddRange(newPieces);

        TransferCutMarksToNewPieces(sourceMarks, sourceMinCell, newPieces);
        cutMarksByPiece.Remove(source);
        Destroy(source.gameObject);
    }

    private List<List<Transform>> FindConnectedComponents(ChocolatePiece source, Dictionary<Vector3Int, Transform> cellMap, Vector3Int minCell)
    {
        var components = new List<List<Transform>>();
        var visited = new HashSet<Vector3Int>();
        var directions = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

        foreach (var entry in cellMap)
        {
            if (visited.Contains(entry.Key))
            {
                continue;
            }

            var component = new List<Transform>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(entry.Key);
            visited.Add(entry.Key);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(cellMap[current]);

                for (int i = 0; i < directions.Length; i++)
                {
                    var next = current + directions[i];
                    if (visited.Contains(next) || !cellMap.ContainsKey(next) || IsEdgeCut(source, minCell, current, next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            components.Add(component);
        }

        return components;
    }

    private bool IsEdgeCut(ChocolatePiece piece, Vector3Int minCell, Vector3Int a, Vector3Int b)
    {
        if (!cutMarksByPiece.TryGetValue(piece, out var marks))
        {
            return false;
        }

        for (int i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            if (a.x != b.x && mark.Direction == KnifeCutDirection.Vertical)
            {
                var leftCellX = Mathf.Min(a.x, b.x);
                var lineIndex = leftCellX - minCell.x + 1;
                var rowIndex = a.y - minCell.y;
                if (mark.LineIndex == lineIndex && rowIndex >= mark.StartIndex && rowIndex < mark.StartIndex + mark.Length)
                {
                    return true;
                }
            }
            else if (a.y != b.y && mark.Direction == KnifeCutDirection.Horizontal)
            {
                var bottomCellY = Mathf.Min(a.y, b.y);
                var lineIndex = bottomCellY - minCell.y + 1;
                var columnIndex = a.x - minCell.x;
                if (mark.LineIndex == lineIndex && columnIndex >= mark.StartIndex && columnIndex < mark.StartIndex + mark.Length)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void GetComponentCellBounds(ChocolatePiece source, List<Transform> component, out Vector3Int minCell, out Vector3Int maxCell)
    {
        minCell = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        maxCell = new Vector3Int(int.MinValue, int.MinValue, 0);

        if (GridManager.Instance == null || component.Count == 0)
        {
            minCell = Vector3Int.zero;
            maxCell = Vector3Int.zero;
            return;
        }

        for (int i = 0; i < component.Count; i++)
        {
            var cell = source.GetCell(component[i]);
            minCell.x = Mathf.Min(minCell.x, cell.x);
            minCell.y = Mathf.Min(minCell.y, cell.y);
            maxCell.x = Mathf.Max(maxCell.x, cell.x);
            maxCell.y = Mathf.Max(maxCell.y, cell.y);
        }
    }

    private void TransferCutMarksToNewPieces(List<CutMark> sourceMarks, Vector3Int sourceMinCell, List<ChocolatePiece> newPieces)
    {
        for (int i = 0; i < newPieces.Count; i++)
        {
            var newPiece = newPieces[i];
            newPiece.GetOccupiedCellBounds(out var minCell, out var maxCell);
            var width = maxCell.x - minCell.x + 1;
            var height = maxCell.y - minCell.y + 1;

            for (int markIndex = 0; markIndex < sourceMarks.Count; markIndex++)
            {
                var mark = sourceMarks[markIndex];
                if (mark.Direction == KnifeCutDirection.Vertical)
                {
                    var worldLineX = sourceMinCell.x + mark.LineIndex;
                    var newLineIndex = worldLineX - minCell.x;
                    if (newLineIndex <= 0 || newLineIndex >= width)
                    {
                        continue;
                    }

                    var worldStartY = sourceMinCell.y + mark.StartIndex;
                    var worldEndY = sourceMinCell.y + mark.StartIndex + mark.Length;
                    var newStart = Mathf.Clamp(worldStartY - minCell.y, 0, height);
                    var newEnd = Mathf.Clamp(worldEndY - minCell.y, 0, height);
                    if (newEnd > newStart)
                    {
                        AddCutMark(newPiece, new CutMark(mark.Direction, newLineIndex, newStart, newEnd - newStart));
                    }
                }
                else
                {
                    var worldLineY = sourceMinCell.y + mark.LineIndex;
                    var newLineIndex = worldLineY - minCell.y;
                    if (newLineIndex <= 0 || newLineIndex >= height)
                    {
                        continue;
                    }

                    var worldStartX = sourceMinCell.x + mark.StartIndex;
                    var worldEndX = sourceMinCell.x + mark.StartIndex + mark.Length;
                    var newStart = Mathf.Clamp(worldStartX - minCell.x, 0, width);
                    var newEnd = Mathf.Clamp(worldEndX - minCell.x, 0, width);
                    if (newEnd > newStart)
                    {
                        AddCutMark(newPiece, new CutMark(mark.Direction, newLineIndex, newStart, newEnd - newStart));
                    }
                }
            }
        }
    }

    private void ApplyPartialCutInset(
        ChocolatePiece piece,
        KnifeCutDirection direction,
        Vector3Int minCell,
        Vector3Int maxCell,
        int lineIndex,
        int segmentStart,
        int segmentLength)
    {
        if (GridManager.Instance == null || piece == null)
        {
            return;
        }

        var inset = GridManager.Instance.CellSize * (1f - cutEdgeCellScale) * 0.5f;
        var segmentEnd = segmentStart + segmentLength;

        var pieceCells = piece.RefreshedCells;
        for (int i = 0; i < pieceCells.Count; i++)
        {
            var child = pieceCells[i];
            var cell = piece.GetCell(child);
            var visual = GetOrAddCutVisual(child);
            var changed = false;

            if (direction == KnifeCutDirection.Vertical)
            {
                var rowIndex = cell.y - minCell.y;
                if (rowIndex < segmentStart || rowIndex >= segmentEnd)
                {
                    continue;
                }

                var leftCutCellX = minCell.x + lineIndex - 1;
                var rightCutCellX = minCell.x + lineIndex;
                if (cell.x == leftCutCellX && !visual.RightInset)
                {
                    visual.RightInset = true;
                    changed = true;
                }
                else if (cell.x == rightCutCellX && !visual.LeftInset)
                {
                    visual.LeftInset = true;
                    changed = true;
                }
            }
            else
            {
                var columnIndex = cell.x - minCell.x;
                if (columnIndex < segmentStart || columnIndex >= segmentEnd)
                {
                    continue;
                }

                var bottomCutCellY = minCell.y + lineIndex - 1;
                var topCutCellY = minCell.y + lineIndex;
                if (cell.y == bottomCutCellY && !visual.TopInset)
                {
                    visual.TopInset = true;
                    changed = true;
                }
                else if (cell.y == topCutCellY && !visual.BottomInset)
                {
                    visual.BottomInset = true;
                    changed = true;
                }
            }

            if (changed)
            {
                ApplyCellCutVisual(child, visual, inset);
            }
        }
    }

    private void ApplyCellCutVisual(Transform cell, ChocolateCellCutVisual visual, float inset)
    {
        var horizontalCuts = (visual.LeftInset ? 1 : 0) + (visual.RightInset ? 1 : 0);
        var verticalCuts = (visual.BottomInset ? 1 : 0) + (visual.TopInset ? 1 : 0);
        var shrinkPerSide = 1f - cutEdgeCellScale;

        var scale = visual.BaseLocalScale;
        scale.x *= Mathf.Max(0.1f, 1f - shrinkPerSide * horizontalCuts);
        scale.y *= Mathf.Max(0.1f, 1f - shrinkPerSide * verticalCuts);

        var position = visual.BaseLocalPosition;
        if (visual.LeftInset && !visual.RightInset)
        {
            position.x += inset;
        }
        else if (visual.RightInset && !visual.LeftInset)
        {
            position.x -= inset;
        }

        if (visual.BottomInset && !visual.TopInset)
        {
            position.y += inset;
        }
        else if (visual.TopInset && !visual.BottomInset)
        {
            position.y -= inset;
        }

        cell.localScale = scale;
        cell.localPosition = position;
    }

    private static ChocolateCellCutVisual GetOrAddCutVisual(Transform cell)
    {
        if (!cell.TryGetComponent(out ChocolateCellCutVisual visual))
        {
            visual = cell.gameObject.AddComponent<ChocolateCellCutVisual>();
        }

        if (!visual.HasBaseTransform)
        {
            visual.BaseLocalPosition = cell.localPosition;
            visual.BaseLocalScale = cell.localScale;
            visual.HasBaseTransform = true;
        }

        return visual;
    }

    private GameObject CreatePieceRoot(string name, Vector3 position, Transform parent)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, true);
        root.transform.position = GridManager.Instance != null ? GridManager.Instance.SnapWorldToCell(position) : position;
        return root;
    }

    private static void NormalizePieceRoot(Transform root)
    {
        if (GridManager.Instance == null || root.childCount == 0)
        {
            return;
        }

        var childWorldPositions = new List<Vector3>();
        var minCell = new Vector3Int(int.MaxValue, int.MaxValue, 0);

        foreach (Transform child in root)
        {
            childWorldPositions.Add(child.position);
            var cell = GridManager.Instance.WorldToCell(child.position);
            minCell.x = Mathf.Min(minCell.x, cell.x);
            minCell.y = Mathf.Min(minCell.y, cell.y);
        }

        root.position = GridManager.Instance.CellToWorldCenter(minCell);

        var index = 0;
        foreach (Transform child in root)
        {
            child.position = childWorldPositions[index];
            index++;
        }
    }

    private readonly struct CutMark
    {
        public readonly KnifeCutDirection Direction;
        public readonly int LineIndex;
        public readonly int StartIndex;
        public readonly int Length;

        public CutMark(KnifeCutDirection direction, int lineIndex, int startIndex, int length)
        {
            Direction = direction;
            LineIndex = lineIndex;
            StartIndex = startIndex;
            Length = length;
        }
    }

    public readonly struct KnifeCutTarget
    {
        public readonly ChocolatePiece Piece;
        public readonly int LineIndex;
        public readonly int SegmentStart;
        public readonly int SegmentLength;

        public KnifeCutTarget(ChocolatePiece piece, int lineIndex, int segmentStart, int segmentLength)
        {
            Piece = piece;
            LineIndex = lineIndex;
            SegmentStart = segmentStart;
            SegmentLength = segmentLength;
        }
    }

}
