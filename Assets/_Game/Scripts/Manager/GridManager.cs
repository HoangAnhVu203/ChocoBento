using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class GridManager : Singleton<GridManager>
{
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap backgroundTilemap;

    public Grid Grid => grid;
    public float CellSize => grid != null ? grid.cellSize.x : 0.5f;

    private void Awake()
    {
        if (grid == null)
        {
            grid = GetComponent<Grid>();
        }

        if (backgroundTilemap == null)
        {
            backgroundTilemap = FindFirstObjectByType<Tilemap>();
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        if (backgroundTilemap != null)
        {
            return backgroundTilemap.WorldToCell(worldPosition);
        }

        return grid.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        if (backgroundTilemap != null)
        {
            return backgroundTilemap.GetCellCenterWorld(cell);
        }

        return grid.GetCellCenterWorld(cell);
    }

    public Vector3 SnapWorldToCell(Vector3 worldPosition)
    {
        return CellToWorldCenter(WorldToCell(worldPosition));
    }

    public Vector3 SnapWorldToVerticalCutLine(Vector3 worldPosition, int bladeLengthInCells = 1)
    {
        var cellSize = CellSize;
        var cellCenterOrigin = CellToWorldCenter(Vector3Int.zero);
        var gridOrigin = cellCenterOrigin - new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);

        var lineIndex = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellSize);
        var snapYToCellBoundary = bladeLengthInCells % 2 == 0;

        var x = gridOrigin.x + lineIndex * cellSize;
        var y = snapYToCellBoundary
            ? gridOrigin.y + Mathf.RoundToInt((worldPosition.y - gridOrigin.y) / cellSize) * cellSize
            : cellCenterOrigin.y + Mathf.RoundToInt((worldPosition.y - cellCenterOrigin.y) / cellSize) * cellSize;
        return new Vector3(x, y, worldPosition.z);
    }

    public Vector3 SnapWorldToHorizontalCutLine(Vector3 worldPosition, int bladeLengthInCells = 1)
    {
        var cellSize = CellSize;
        var cellCenterOrigin = CellToWorldCenter(Vector3Int.zero);
        var gridOrigin = cellCenterOrigin - new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);

        var lineIndex = Mathf.RoundToInt((worldPosition.y - gridOrigin.y) / cellSize);
        var snapXToCellBoundary = bladeLengthInCells % 2 == 0;

        var x = snapXToCellBoundary
            ? gridOrigin.x + Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellSize) * cellSize
            : cellCenterOrigin.x + Mathf.RoundToInt((worldPosition.x - cellCenterOrigin.x) / cellSize) * cellSize;
        var y = gridOrigin.y + lineIndex * cellSize;
        return new Vector3(x, y, worldPosition.z);
    }
}
