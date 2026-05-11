using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayLevelController : MonoBehaviour
{
    public static GameplayLevelController Instance { get; private set; }

    [SerializeField] private Button cutButton;
    [SerializeField] private KnifeDrag selectedKnife;
    [SerializeField] private ChocolatePiece selectedPiece;

    private readonly List<ChocolatePiece> pieces = new();
    private readonly Dictionary<ChocolatePiece, List<CutMark>> cutMarksByPiece = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pieces.Clear();
        pieces.AddRange(FindObjectsByType<ChocolatePiece>(FindObjectsSortMode.None));

        if (cutButton == null)
        {
            var buttonObject = GameObject.Find("Button (Legacy)");
            cutButton = buttonObject != null ? buttonObject.GetComponent<Button>() : FindFirstObjectByType<Button>();
        }

        if (cutButton != null)
        {
            cutButton.onClick.RemoveListener(CutSelectedPiece);
            cutButton.onClick.AddListener(CutSelectedPiece);
            SetCanCut(false);
        }
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
        if (cutButton != null)
        {
            cutButton.interactable = canCut;
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

    private void CutSelectedPiece()
    {
        if (selectedKnife == null || !selectedKnife.HasValidCut)
        {
            SetCanCut(false);
            return;
        }

        var piece = selectedKnife.HoveredPiece;
        if (piece == null)
        {
            SetCanCut(false);
            return;
        }

        if (selectedKnife.CutDirection == KnifeCutDirection.Vertical)
        {
            RegisterVerticalCut(piece, selectedKnife.CutIndex, selectedKnife.CutStartIndex, selectedKnife.CutLength);
        }
        else
        {
            RegisterHorizontalCut(piece, selectedKnife.CutIndex, selectedKnife.CutStartIndex, selectedKnife.CutLength);
        }

        SetCanCut(false);
    }

    private void RegisterVerticalCut(ChocolatePiece piece, int cutIndex, int segmentStart, int segmentLength)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);
        var width = maxCell.x - minCell.x + 1;
        var height = maxCell.y - minCell.y + 1;
        if (cutIndex <= 0 || cutIndex >= width)
        {
            return;
        }

        AddCutMark(piece, new CutMark(KnifeCutDirection.Vertical, cutIndex, segmentStart, segmentLength));
        if (!IsLineFullyCut(piece, KnifeCutDirection.Vertical, cutIndex, height))
        {
            return;
        }

        var splitCellX = minCell.x + cutIndex;
        SplitPiece(
            piece,
            cell => GridManager.Instance.WorldToCell(cell.position).x < splitCellX,
            new Vector2Int(cutIndex, height),
            new Vector2Int(width - cutIndex, height));
    }

    private void RegisterHorizontalCut(ChocolatePiece piece, int cutIndex, int segmentStart, int segmentLength)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);
        var width = maxCell.x - minCell.x + 1;
        var height = maxCell.y - minCell.y + 1;
        if (cutIndex <= 0 || cutIndex >= height)
        {
            return;
        }

        AddCutMark(piece, new CutMark(KnifeCutDirection.Horizontal, cutIndex, segmentStart, segmentLength));
        if (!IsLineFullyCut(piece, KnifeCutDirection.Horizontal, cutIndex, width))
        {
            return;
        }

        var splitCellY = minCell.y + cutIndex;
        SplitPiece(
            piece,
            cell => GridManager.Instance.WorldToCell(cell.position).y < splitCellY,
            new Vector2Int(width, cutIndex),
            new Vector2Int(width, height - cutIndex));
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

    private bool IsLineFullyCut(ChocolatePiece piece, KnifeCutDirection direction, int lineIndex, int requiredLength)
    {
        if (!cutMarksByPiece.TryGetValue(piece, out var marks))
        {
            return false;
        }

        var coverage = new bool[requiredLength];
        for (int i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            if (mark.Direction != direction || mark.LineIndex != lineIndex)
            {
                continue;
            }

            var end = Mathf.Min(mark.StartIndex + mark.Length, requiredLength);
            for (int index = Mathf.Max(mark.StartIndex, 0); index < end; index++)
            {
                coverage[index] = true;
            }
        }

        for (int i = 0; i < coverage.Length; i++)
        {
            if (!coverage[i])
            {
                return false;
            }
        }

        return true;
    }

    private void SplitPiece(ChocolatePiece source, System.Predicate<Transform> firstSide, Vector2Int firstSize, Vector2Int secondSize)
    {
        var firstRoot = CreatePieceRoot($"{source.name}_A", source.transform.position);
        var secondRoot = CreatePieceRoot($"{source.name}_B", source.transform.position);

        var children = new List<Transform>();
        foreach (Transform child in source.transform)
        {
            children.Add(child);
        }

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var world = child.position;
            child.SetParent(firstSide(child) ? firstRoot.transform : secondRoot.transform, true);
            child.position = world;
        }

        NormalizePieceRoot(firstRoot.transform);
        NormalizePieceRoot(secondRoot.transform);

        var firstPiece = firstRoot.AddComponent<ChocolatePiece>();
        firstPiece.Configure(firstSize);
        var secondPiece = secondRoot.AddComponent<ChocolatePiece>();
        secondPiece.Configure(secondSize);

        pieces.Remove(source);
        pieces.Add(firstPiece);
        pieces.Add(secondPiece);
        cutMarksByPiece.Remove(source);

        Destroy(source.gameObject);
    }

    private GameObject CreatePieceRoot(string name, Vector3 position)
    {
        var root = new GameObject(name);
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

    private static Vector3Int LocalCell(ChocolatePiece piece, Transform cell)
    {
        if (GridManager.Instance == null)
        {
            return Vector3Int.RoundToInt(cell.localPosition / 0.5f);
        }

        return GridManager.Instance.WorldToCell(cell.position) - GridManager.Instance.WorldToCell(piece.transform.position);
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
}
