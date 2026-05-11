using System.Collections.Generic;
using UnityEngine;

public sealed class ChocolatePiece : MonoBehaviour, IDraggableGameplay
{
    [SerializeField] private Vector2Int sizeInCells = new(5, 4);
    [SerializeField] private bool snapToGridOnRelease = true;

    private readonly List<Transform> cells = new();
    private Vector3 dragOffset;
    private Vector3 lastValidPosition;

    public Vector2Int SizeInCells => sizeInCells;
    public IReadOnlyList<Transform> Cells => cells;

    private void Awake()
    {
        RefreshCells();
        EnsureCollider();
        lastValidPosition = transform.position;
    }

    private void OnTransformChildrenChanged()
    {
        RefreshCells();
        EnsureCollider();
    }

    public void Configure(Vector2Int newSize)
    {
        sizeInCells = newSize;
        RefreshCells();
        EnsureCollider();
    }

    public void BeginDrag(Vector3 pointerWorldPosition)
    {
        GameplayLevelController.Instance?.SetSelectedPiece(this);
        dragOffset = transform.position - pointerWorldPosition;
    }

    public void Drag(Vector3 pointerWorldPosition)
    {
        transform.position = pointerWorldPosition + dragOffset;
    }

    public void EndDrag(Vector3 pointerWorldPosition)
    {
        if (!snapToGridOnRelease || GridManager.Instance == null)
        {
            return;
        }

        transform.position = GridManager.Instance.SnapWorldToCell(transform.position);
        GameplayLevelController.Instance?.ResolvePieceOverlap(this);
        lastValidPosition = transform.position;
        TargetTray.Instance?.TryAccept(this);
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
            var offset = GridManager.Instance.WorldToCell(cells[i].position) - currentAnchor;
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
            var cell = GridManager.Instance.WorldToCell(cells[i].position);
            minCell.x = Mathf.Min(minCell.x, cell.x);
            minCell.y = Mathf.Min(minCell.y, cell.y);
            maxCell.x = Mathf.Max(maxCell.x, cell.x);
            maxCell.y = Mathf.Max(maxCell.y, cell.y);
        }
    }

    private void RefreshCells()
    {
        cells.Clear();

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                cells.Add(child);
            }
        }

        if (cells.Count == 0)
        {
            return;
        }

        var min = Vector2Int.one * int.MaxValue;
        var max = Vector2Int.one * int.MinValue;
        foreach (var cell in cells)
        {
            var localCell = GridManager.Instance != null
                ? GridManager.Instance.WorldToCell(cell.position) - GridManager.Instance.WorldToCell(transform.position)
                : Vector3Int.RoundToInt(cell.localPosition / 0.5f);

            min.x = Mathf.Min(min.x, localCell.x);
            min.y = Mathf.Min(min.y, localCell.y);
            max.x = Mathf.Max(max.x, localCell.x);
            max.y = Mathf.Max(max.y, localCell.y);
        }

        sizeInCells = new Vector2Int(max.x - min.x + 1, max.y - min.y + 1);
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
