using System.Collections.Generic;
using UnityEngine;

public sealed class TargetTray : MonoBehaviour
{
    public static TargetTray Instance { get; private set; }

    [SerializeField] private float acceptDistance = 0.35f;

    private readonly List<Vector3Int> slotCells = new();

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
        if (GridManager.Instance == null)
        {
            return false;
        }

        CacheSlots();
        if (slotCells.Count == 0)
        {
            return false;
        }

        var pieceCell = GridManager.Instance.WorldToCell(piece.transform.position);
        var nearestSlot = FindNearestSlot(pieceCell);
        var snappedPosition = GridManager.Instance.CellToWorldCenter(nearestSlot);
        var distance = Vector3.Distance(piece.transform.position, snappedPosition);

        if (distance > acceptDistance)
        {
            return false;
        }

        piece.transform.position = snappedPosition;
        return true;
    }

    private Vector3Int FindNearestSlot(Vector3Int pieceCell)
    {
        var nearest = slotCells[0];
        var bestDistance = int.MaxValue;

        for (int i = 0; i < slotCells.Count; i++)
        {
            var delta = slotCells[i] - pieceCell;
            var distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = slotCells[i];
            }
        }

        return nearest;
    }

    private void CacheSlots()
    {
        slotCells.Clear();

        if (GridManager.Instance == null)
        {
            return;
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                slotCells.Add(GridManager.Instance.WorldToCell(child.position));
            }
        }
    }
}
