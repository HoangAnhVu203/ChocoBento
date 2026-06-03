using System.Collections.Generic;
using UnityEngine;

public sealed class ChocolatePiece : MonoBehaviour, IDraggableGameplay
{
    [SerializeField] private Vector2Int sizeInCells = new(5, 4);
    [SerializeField] private bool snapToGridOnRelease = true;

    private readonly List<Transform> cells = new();
    private Vector3 dragOffset;
    private Vector3 lastValidPosition;
    private ChocolateDragLiftVisual dragLiftVisual;
    private ChocolatePlacementPreview placementPreview;
    private ParticleSystem[] dropParticles;

    public Vector2Int SizeInCells => sizeInCells;
    public IReadOnlyList<Transform> Cells => cells;
    public bool IsInTargetTray { get; private set; }

    public IReadOnlyList<Transform> RefreshedCells
    {
        get
        {
            RefreshCells();
            return cells;
        }
    }

    private void Awake()
    {
        dragLiftVisual = GetComponent<ChocolateDragLiftVisual>();
        if (dragLiftVisual == null)
        {
            dragLiftVisual = gameObject.AddComponent<ChocolateDragLiftVisual>();
        }

        placementPreview = GetComponent<ChocolatePlacementPreview>();
        if (placementPreview == null)
        {
            placementPreview = gameObject.AddComponent<ChocolatePlacementPreview>();
        }

        RefreshCells(true);
        RefreshDropParticles();
        EnsureCollider();
        lastValidPosition = transform.position;
    }

    private void OnTransformChildrenChanged()
    {
        RefreshCells();
        EnsureCollider();
    }

    public void Configure(Vector2Int newSize, bool rebuildCellOffsets = true)
    {
        sizeInCells = newSize;
        RefreshCells(rebuildCellOffsets);
        EnsureCollider();
    }

    public void BeginDrag(Vector3 pointerWorldPosition)
    {
        GameplayLevelController.Instance?.SetSelectedPiece(this);
        SetInTargetTray(false);
        ClearStrawberryMismatchMarkers();
        dragLiftVisual?.Lift();
        placementPreview?.Show(this);
        AudioManager.Instance.PlayChocolateLift();
        dragOffset = transform.position - pointerWorldPosition;
    }

    public void Drag(Vector3 pointerWorldPosition)
    {
        transform.position = pointerWorldPosition + dragOffset;
        placementPreview?.UpdatePreview();
    }

    public void EndDrag(Vector3 pointerWorldPosition)
    {
        if (!snapToGridOnRelease || GridManager.Instance == null)
        {
            placementPreview?.Hide();
            dragLiftVisual?.Drop();
            AudioManager.Instance.PlayChocolateDrop();
            return;
        }

        placementPreview?.Hide();
        var previousValidPosition = lastValidPosition;
        transform.position = GridManager.Instance.SnapWorldToCell(transform.position);
        GameplayLevelController.Instance?.ResolvePieceOverlap(this);

        var acceptedByTray = false;
        var rejectedByTray = false;
        if (TargetTray.Instance != null)
        {
            acceptedByTray = TargetTray.Instance.TryAccept(this);
            rejectedByTray = TargetTray.Instance.LastPlacementRejected;
        }

        if (rejectedByTray)
        {
            transform.position = previousValidPosition;
        }
        else
        {
            lastValidPosition = transform.position;
        }

        SetInTargetTray(acceptedByTray);
        dragLiftVisual?.Drop();
        AudioManager.Instance.PlayChocolateDrop();
        PlayDropParticles();
        GameplayLevelController.Instance?.LogTargetTrayProgress();
    }

    public bool ContainsWorldPoint(Vector3 worldPosition)
    {
        var bounds = GetWorldBounds();
        return bounds.Contains(new Vector3(worldPosition.x, worldPosition.y, bounds.center.z));
    }

    public Bounds GetWorldBounds()
    {
        RefreshCells();

        if (cells.Count == 0)
        {
            var cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 0.5f;
            return new Bounds(transform.position, Vector3.one * cellSize);
        }

        var bounds = new Bounds(cells[0].position, Vector3.zero);
        for (int i = 0; i < cells.Count; i++)
        {
            bounds.Encapsulate(cells[i].position);
        }

        var padding = GridManager.Instance != null ? GridManager.Instance.CellSize : 0.5f;
        bounds.Expand(padding);
        return bounds;
    }

    public void ReturnToLastValidPosition()
    {
        transform.position = lastValidPosition;
    }

    public void SetInTargetTray(bool isInTargetTray)
    {
        IsInTargetTray = isInTargetTray;
    }

    public List<Vector3Int> GetOccupiedCellsAt(Vector3Int anchorCell)
    {
        RefreshCells();

        var occupied = new List<Vector3Int>();
        if (GridManager.Instance == null)
        {
            return occupied;
        }

        var currentAnchor = GridManager.Instance.WorldToCell(transform.position);
        for (int i = 0; i < cells.Count; i++)
        {
            var offset = GetCellOffset(cells[i]);
            occupied.Add(anchorCell + offset);
        }

        return occupied;
    }

    public List<Vector3Int> GetOccupiedCells()
    {
        if (GridManager.Instance == null)
        {
            return new List<Vector3Int>();
        }

        return GetOccupiedCellsAt(GridManager.Instance.WorldToCell(transform.position));
    }

    public void GetOccupiedCellBounds(out Vector3Int minCell, out Vector3Int maxCell)
    {
        RefreshCells();

        minCell = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        maxCell = new Vector3Int(int.MinValue, int.MinValue, 0);

        if (GridManager.Instance == null || cells.Count == 0)
        {
            var fallback = GridManager.Instance != null ? GridManager.Instance.WorldToCell(transform.position) : Vector3Int.zero;
            minCell = fallback;
            maxCell = fallback;
            return;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = GetCell(cells[i]);
            minCell.x = Mathf.Min(minCell.x, cell.x);
            minCell.y = Mathf.Min(minCell.y, cell.y);
            maxCell.x = Mathf.Max(maxCell.x, cell.x);
            maxCell.y = Mathf.Max(maxCell.y, cell.y);
        }
    }

    public Vector3Int GetCell(Transform cell)
    {
        if (GridManager.Instance == null)
        {
            return Vector3Int.RoundToInt(cell.localPosition / 0.5f);
        }

        return GridManager.Instance.WorldToCell(transform.position) + GetCellOffset(cell);
    }

    public Vector3Int GetCellOffset(Transform cell)
    {
        if (GridManager.Instance == null)
        {
            return Vector3Int.RoundToInt(cell.localPosition / 0.5f);
        }

        var visual = GetOrAddCutVisual(cell);
        if (!visual.HasCellOffset)
        {
            visual.CellOffset = GridManager.Instance.WorldToCell(cell.position) - GridManager.Instance.WorldToCell(transform.position);
            visual.HasCellOffset = true;
        }

        return visual.CellOffset;
    }

    public bool IsStrawberryCell(Transform cell)
    {
        return cell != null
            && (cell.CompareTag("Strawberry") || cell.name.IndexOf("Strawberry", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public void SetStrawberryMismatchMarkerVisible(Transform cell, bool visible)
    {
        var marker = FindChildByName(cell, "X");
        if (marker != null)
        {
            marker.gameObject.SetActive(visible);
        }
    }

    public void ClearStrawberryMismatchMarkers()
    {
        RefreshCells();
        for (int i = 0; i < cells.Count; i++)
        {
            SetStrawberryMismatchMarkerVisible(cells[i], false);
        }
    }

    private void RefreshCells(bool rebuildCellOffsets = false)
    {
        cells.Clear();

        var uniqueCells = new Dictionary<Vector3Int, Transform>();
        var anchorCell = GridManager.Instance != null ? GridManager.Instance.WorldToCell(transform.position) : Vector3Int.zero;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf && child.GetComponent<ParticleSystem>() == null)
            {
                var visual = GetOrAddCutVisual(child);
                if (rebuildCellOffsets || !visual.HasCellOffset)
                {
                    visual.CellOffset = GridManager.Instance != null
                        ? GridManager.Instance.WorldToCell(child.position) - anchorCell
                        : Vector3Int.RoundToInt(child.localPosition / 0.5f);
                    visual.HasCellOffset = true;
                }

                if (uniqueCells.TryGetValue(visual.CellOffset, out var existingCell))
                {
                    if (IsStrawberryCell(child) && !IsStrawberryCell(existingCell))
                    {
                        uniqueCells[visual.CellOffset] = child;
                    }

                    continue;
                }

                uniqueCells.Add(visual.CellOffset, child);
            }
        }

        foreach (var cell in uniqueCells.Values)
        {
            cells.Add(cell);
        }

        if (cells.Count == 0)
        {
            return;
        }

        var min = Vector2Int.one * int.MaxValue;
        var max = Vector2Int.one * int.MinValue;
        foreach (var cell in cells)
        {
            var localCell = GetCellOffset(cell);

            min.x = Mathf.Min(min.x, localCell.x);
            min.y = Mathf.Min(min.y, localCell.y);
            max.x = Mathf.Max(max.x, localCell.x);
            max.y = Mathf.Max(max.y, localCell.y);
        }

        sizeInCells = new Vector2Int(max.x - min.x + 1, max.y - min.y + 1);
    }

    private static ChocolateCellCutVisual GetOrAddCutVisual(Transform cell)
    {
        if (!cell.TryGetComponent(out ChocolateCellCutVisual visual))
        {
            visual = cell.gameObject.AddComponent<ChocolateCellCutVisual>();
        }

        return visual;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            var result = FindChildByName(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void EnsureCollider()
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        var bounds = GetLocalBounds();
        collider.offset = bounds.center;
        collider.size = bounds.size;
    }

    private void RefreshDropParticles()
    {
        dropParticles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void PlayDropParticles()
    {
        if (dropParticles == null || dropParticles.Length == 0)
        {
            RefreshDropParticles();
        }

        if (dropParticles == null)
        {
            return;
        }

        for (int i = 0; i < dropParticles.Length; i++)
        {
            if (dropParticles[i] == null)
            {
                continue;
            }

            dropParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dropParticles[i].Play(true);
        }
    }

    private Bounds GetLocalBounds()
    {
        RefreshCells();

        if (cells.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one * 0.5f);
        }

        var bounds = new Bounds(transform.InverseTransformPoint(cells[0].position), Vector3.zero);
        for (int i = 0; i < cells.Count; i++)
        {
            bounds.Encapsulate(transform.InverseTransformPoint(cells[i].position));
        }

        var padding = GridManager.Instance != null ? GridManager.Instance.CellSize : 0.5f;
        bounds.Expand(padding);
        return bounds;
    }
}
