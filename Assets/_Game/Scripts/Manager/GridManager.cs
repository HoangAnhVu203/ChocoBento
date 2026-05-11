using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap backgroundTilemap;

    public Grid Grid => grid;
    public float CellSize => grid != null ? grid.cellSize.x : 0.5f;

    private void Awake()
    {
        Instance = this;

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

    public Vector3 SnapWorldToVerticalCutLine(Vector3 worldPosition)
    {
        var cell = WorldToCell(worldPosition);
        var leftCenter = CellToWorldCenter(cell);
        var rightCenter = CellToWorldCenter(cell + Vector3Int.right);
        var x = (leftCenter.x + rightCenter.x) * 0.5f;
        return new Vector3(x, SnapWorldToCell(worldPosition).y, worldPosition.z);
    }

    public Vector3 SnapWorldToHorizontalCutLine(Vector3 worldPosition)
    {
        var cell = WorldToCell(worldPosition);
        var bottomCenter = CellToWorldCenter(cell);
        var topCenter = CellToWorldCenter(cell + Vector3Int.up);
        var y = (bottomCenter.y + topCenter.y) * 0.5f;
        return new Vector3(SnapWorldToCell(worldPosition).x, y, worldPosition.z);
    }
}
