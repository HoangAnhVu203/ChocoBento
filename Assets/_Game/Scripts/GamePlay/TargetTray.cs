using System.Collections.Generic;
using UnityEngine;

public sealed class TargetTray : MonoBehaviour
{
    public static TargetTray Instance { get; private set; }

    [SerializeField] private float acceptDistance = 0.35f;
    [SerializeField] private string fillSlotNameContains = "TraySlot";
    [SerializeField] private string blockedCellNameContains = "Rock";
    [SerializeField] private string overflowCellNameContains = "OverflowSlot";
    [SerializeField] private string strawberryCellNameContains = "Strawberry";
    [SerializeField] private bool allowOverflowThroughOpenEdges;
    [SerializeField] private bool openLeft;
    [SerializeField] private bool openRight;
    [SerializeField] private bool openTop;
    [SerializeField] private bool openBottom;

    private readonly List<Vector3Int> fillableSlotCells = new();
    private readonly HashSet<Vector3Int> allSlotCells = new();
    private readonly HashSet<Vector3Int> blockedCells = new();
    private readonly HashSet<Vector3Int> overflowCells = new();
    private readonly HashSet<Vector3Int> strawberrySlotCells = new();
    private readonly Dictionary<Vector3Int, Transform> strawberryMismatchMarkers = new();
    private Vector3Int trayMinCell;
    private Vector3Int trayMaxCell;
    private bool hasTrayBounds;

    public bool LastPlacementRejected { get; private set; }

    public int SlotCount
    {
        get
        {
            CacheSlots();
            return fillableSlotCells.Count;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CacheSlots();
    }

    public bool TryAccept(ChocolatePiece piece)
    {
        LastPlacementRejected = false;

        if (GridManager.Instance == null)
        {
            return false;
        }

        CacheSlots();
        if (fillableSlotCells.Count == 0)
        {
            return false;
        }

        var pieceCell = GridManager.Instance.WorldToCell(piece.transform.position);
        if (!TryFindBestPlacementAnchor(piece, pieceCell, out var placementAnchor))
        {
            LastPlacementRejected = IsNearTrayArea(piece, pieceCell);
            if (LastPlacementRejected)
            {
                Debug.Log("[TargetTray] Placement rejected: choco overlaps a blocked or closed tray cell.");
            }

            return false;
        }

        var snappedPosition = GridManager.Instance.CellToWorldCenter(placementAnchor);
        piece.transform.position = snappedPosition;
        RefreshStrawberryMismatchMarkersForPiece(piece);
        return true;
    }

    public void RefreshMismatchMarkers(IEnumerable<ChocolatePiece> pieces)
    {
        CacheSlots();
        ClearStrawberryMismatchMarkers();

        foreach (var piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            piece.ClearStrawberryMismatchMarkers();
            if (piece.IsInTargetTray)
            {
                RefreshStrawberryMismatchMarkersForPiece(piece);
            }
        }
    }

    public int CountFilledSlots(IEnumerable<ChocolatePiece> pieces)
    {
        if (GridManager.Instance == null)
        {
            return 0;
        }

        CacheSlots();

        var filledSlots = new HashSet<Vector3Int>();
        foreach (var piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            if (!IsStrawberryPlacementValid(piece, GridManager.Instance.WorldToCell(piece.transform.position), false))
            {
                continue;
            }

            var occupiedCells = piece.GetOccupiedCells();
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                if (allSlotCells.Contains(occupiedCells[i]) && !blockedCells.Contains(occupiedCells[i]))
                {
                    filledSlots.Add(occupiedCells[i]);
                }
            }
        }

        return filledSlots.Count;
    }

    private bool TryFindBestPlacementAnchor(ChocolatePiece piece, Vector3Int desiredAnchor, out Vector3Int placementAnchor)
    {
        placementAnchor = desiredAnchor;

        var offsets = GetPieceCellOffsets(piece);
        if (offsets.Count == 0)
        {
            return false;
        }

        var candidateAnchors = new HashSet<Vector3Int>();
        for (int slotIndex = 0; slotIndex < fillableSlotCells.Count; slotIndex++)
        {
            for (int offsetIndex = 0; offsetIndex < offsets.Count; offsetIndex++)
            {
                candidateAnchors.Add(fillableSlotCells[slotIndex] - offsets[offsetIndex]);
            }
        }

        foreach (var overflowCell in overflowCells)
        {
            for (int offsetIndex = 0; offsetIndex < offsets.Count; offsetIndex++)
            {
                candidateAnchors.Add(overflowCell - offsets[offsetIndex]);
            }
        }

        var bestDistance = float.MaxValue;
        foreach (var candidateAnchor in candidateAnchors)
        {
            if (!CanPlaceAt(piece, candidateAnchor, out var coversFillableSlot) || !coversFillableSlot)
            {
                continue;
            }

            var snappedPosition = GridManager.Instance.CellToWorldCenter(candidateAnchor);
            var distance = Vector3.Distance(piece.transform.position, snappedPosition);
            if (distance > acceptDistance || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            placementAnchor = candidateAnchor;
        }

        return bestDistance < float.MaxValue;
    }

    private List<Vector3Int> GetPieceCellOffsets(ChocolatePiece piece)
    {
        var offsets = new List<Vector3Int>();
        var cells = piece.RefreshedCells;
        for (int i = 0; i < cells.Count; i++)
        {
            offsets.Add(piece.GetCellOffset(cells[i]));
        }

        return offsets;
    }

    private bool IsNearTrayArea(ChocolatePiece piece, Vector3Int anchorCell)
    {
        var occupiedCells = piece.GetOccupiedCellsAt(anchorCell);
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            if (allSlotCells.Contains(occupiedCells[i])
                || blockedCells.Contains(occupiedCells[i])
                || overflowCells.Contains(occupiedCells[i])
                || IsOpenEdgeOverflowCell(occupiedCells[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanPlaceAt(ChocolatePiece piece, Vector3Int anchorCell, out bool coversFillableSlot)
    {
        coversFillableSlot = false;

        var occupiedCells = piece.GetOccupiedCellsAt(anchorCell);
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            if (blockedCells.Contains(occupiedCells[i]))
            {
                return false;
            }

            if (allSlotCells.Contains(occupiedCells[i]))
            {
                coversFillableSlot = true;
                continue;
            }

            if (overflowCells.Contains(occupiedCells[i]))
            {
                continue;
            }

            if (IsOpenEdgeOverflowCell(occupiedCells[i]))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void RefreshStrawberryMismatchMarkersForPiece(ChocolatePiece piece)
    {
        IsStrawberryPlacementValid(piece, GridManager.Instance.WorldToCell(piece.transform.position), true);
    }

    private bool IsStrawberryPlacementValid(ChocolatePiece piece, Vector3Int anchorCell, bool showMarkers)
    {
        var isValid = true;
        var cells = piece.RefreshedCells;

        for (int i = 0; i < cells.Count; i++)
        {
            var occupiedCell = anchorCell + piece.GetCellOffset(cells[i]);
            var hasPieceStrawberry = piece.IsStrawberryCell(cells[i]);
            var requiresStrawberry = strawberrySlotCells.Contains(occupiedCell);

            if (hasPieceStrawberry && !requiresStrawberry)
            {
                isValid = false;
                if (showMarkers)
                {
                    piece.SetStrawberryMismatchMarkerVisible(cells[i], true);
                }
            }

            if (!hasPieceStrawberry && requiresStrawberry)
            {
                isValid = false;
                if (showMarkers && strawberryMismatchMarkers.TryGetValue(occupiedCell, out var marker))
                {
                    marker.gameObject.SetActive(true);
                }
            }
        }

        return isValid;
    }

    private void CacheSlots()
    {
        fillableSlotCells.Clear();
        allSlotCells.Clear();
        blockedCells.Clear();
        overflowCells.Clear();
        strawberrySlotCells.Clear();
        strawberryMismatchMarkers.Clear();
        hasTrayBounds = false;

        if (GridManager.Instance == null)
        {
            return;
        }

        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            var cell = GridManager.Instance.WorldToCell(child.position);
            if (IsBlockedCellObject(child))
            {
                blockedCells.Add(cell);
            }
            else if (IsOverflowCellObject(child))
            {
                overflowCells.Add(cell);
            }
            else if (IsFillableCellObject(child))
            {
                allSlotCells.Add(cell);
                if (IsStrawberryCellObject(child))
                {
                    strawberrySlotCells.Add(cell);
                    var marker = FindChildByName(child, "X");
                    if (marker != null)
                    {
                        strawberryMismatchMarkers[cell] = marker;
                    }
                }
            }
        }

        foreach (var cell in allSlotCells)
        {
            if (!blockedCells.Contains(cell))
            {
                fillableSlotCells.Add(cell);
            }
        }

        CacheTrayBounds();
    }

    private bool IsBlockedCellObject(Transform child)
    {
        return !string.IsNullOrEmpty(blockedCellNameContains)
            && child.name.IndexOf(blockedCellNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsOverflowCellObject(Transform child)
    {
        return !string.IsNullOrEmpty(overflowCellNameContains)
            && child.name.IndexOf(overflowCellNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsFillableCellObject(Transform child)
    {
        return IsStrawberryCellObject(child)
            || (!string.IsNullOrEmpty(fillSlotNameContains)
                && child.name.IndexOf(fillSlotNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
            || child.name.IndexOf("FillSlot", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsStrawberryCellObject(Transform child)
    {
        return child.CompareTag("Strawberry")
            || (!string.IsNullOrEmpty(strawberryCellNameContains)
                && child.name.IndexOf(strawberryCellNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void CacheTrayBounds()
    {
        if (allSlotCells.Count == 0)
        {
            return;
        }

        trayMinCell = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        trayMaxCell = new Vector3Int(int.MinValue, int.MinValue, 0);

        foreach (var cell in allSlotCells)
        {
            trayMinCell.x = Mathf.Min(trayMinCell.x, cell.x);
            trayMinCell.y = Mathf.Min(trayMinCell.y, cell.y);
            trayMaxCell.x = Mathf.Max(trayMaxCell.x, cell.x);
            trayMaxCell.y = Mathf.Max(trayMaxCell.y, cell.y);
        }

        hasTrayBounds = true;
    }

    private bool IsOpenEdgeOverflowCell(Vector3Int cell)
    {
        if (!allowOverflowThroughOpenEdges || !hasTrayBounds)
        {
            return false;
        }

        var isOutside = false;
        if (cell.x < trayMinCell.x)
        {
            if (!openLeft)
            {
                return false;
            }

            isOutside = true;
        }

        if (cell.x > trayMaxCell.x)
        {
            if (!openRight)
            {
                return false;
            }

            isOutside = true;
        }

        if (cell.y > trayMaxCell.y)
        {
            if (!openTop)
            {
                return false;
            }

            isOutside = true;
        }

        if (cell.y < trayMinCell.y)
        {
            if (!openBottom)
            {
                return false;
            }

            isOutside = true;
        }

        return isOutside;
    }

    private void ClearStrawberryMismatchMarkers()
    {
        foreach (var marker in strawberryMismatchMarkers.Values)
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }
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
}
