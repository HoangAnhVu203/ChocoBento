using UnityEngine;

public enum KnifeCutDirection
{
    Vertical,
    Horizontal
}

public sealed class KnifeDrag : MonoBehaviour, IDraggableGameplay
{
    [SerializeField] private KnifeCutDirection cutDirection = KnifeCutDirection.Vertical;
    [SerializeField, Range(0.05f, 1f)] private float snapStrength = 0.35f;
    [SerializeField, Min(1)] private int bladeLengthInCells = 4;

    private Vector3 dragOffset;
    private ChocolatePiece hoveredPiece;
    private int cutIndex = -1;
    private int cutStartIndex;
    private int cutLength;

    public KnifeCutDirection CutDirection => cutDirection;
    public ChocolatePiece HoveredPiece => hoveredPiece;
    public int CutIndex => cutIndex;
    public int CutStartIndex => cutStartIndex;
    public int CutLength => cutLength;
    public bool HasValidCut => hoveredPiece != null && cutIndex > 0;

    private void Awake()
    {
        GuessDirectionFromRotation();
    }

    public void BeginDrag(Vector3 pointerWorldPosition)
    {
        dragOffset = transform.position - pointerWorldPosition;
        GameplayLevelController.Instance?.SetSelectedKnife(this);
    }

    public void Drag(Vector3 pointerWorldPosition)
    {
        var desired = pointerWorldPosition + dragOffset;
        hoveredPiece = GameplayLevelController.Instance != null
            ? GameplayLevelController.Instance.FindPieceAt(desired)
            : null;

        if (hoveredPiece == null || GridManager.Instance == null)
        {
            transform.position = desired;
            cutIndex = -1;
            GameplayLevelController.Instance?.SetCanCut(false);
            return;
        }

        if (cutDirection == KnifeCutDirection.Vertical)
        {
            var snapped = GridManager.Instance.SnapWorldToVerticalCutLine(desired);
            transform.position = Vector3.Lerp(desired, snapped, snapStrength);

            cutIndex = CalculateVerticalCutIndex(hoveredPiece, transform.position);
            CalculateVerticalCutSegment(hoveredPiece, transform.position, bladeLengthInCells, out cutStartIndex, out cutLength);
        }
        else
        {
            var snapped = GridManager.Instance.SnapWorldToHorizontalCutLine(desired);
            transform.position = Vector3.Lerp(desired, snapped, snapStrength);

            cutIndex = CalculateHorizontalCutIndex(hoveredPiece, transform.position);
            CalculateHorizontalCutSegment(hoveredPiece, transform.position, bladeLengthInCells, out cutStartIndex, out cutLength);
        }

        GameplayLevelController.Instance?.SetCanCut(HasValidCut);
    }

    public void EndDrag(Vector3 pointerWorldPosition)
    {
        GameplayLevelController.Instance?.SetSelectedKnife(this);
        GameplayLevelController.Instance?.SetCanCut(HasValidCut);
    }

    private void GuessDirectionFromRotation()
    {
        var z = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.z));
        cutDirection = z > 45f && z < 135f ? KnifeCutDirection.Vertical : KnifeCutDirection.Horizontal;
    }

    private static int CalculateVerticalCutIndex(ChocolatePiece piece, Vector3 snappedKnifePosition)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);

        var cellSize = GridManager.Instance.CellSize;
        var leftCellCenterX = GridManager.Instance.CellToWorldCenter(minCell).x;
        var distanceFromLeftCellCenter = snappedKnifePosition.x - leftCellCenterX;
        var columnsOnLeftSide = Mathf.RoundToInt(distanceFromLeftCellCenter / cellSize + 0.5f);
        var width = maxCell.x - minCell.x + 1;
        return Mathf.Clamp(columnsOnLeftSide, 1, width - 1);
    }

    private static int CalculateHorizontalCutIndex(ChocolatePiece piece, Vector3 snappedKnifePosition)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);

        var cellSize = GridManager.Instance.CellSize;
        var bottomCellCenterY = GridManager.Instance.CellToWorldCenter(minCell).y;
        var distanceFromBottomCellCenter = snappedKnifePosition.y - bottomCellCenterY;
        var rowsOnBottomSide = Mathf.RoundToInt(distanceFromBottomCellCenter / cellSize + 0.5f);
        var height = maxCell.y - minCell.y + 1;
        return Mathf.Clamp(rowsOnBottomSide, 1, height - 1);
    }

    private static void CalculateVerticalCutSegment(ChocolatePiece piece, Vector3 knifePosition, int bladeLength, out int startIndex, out int length)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);

        var height = maxCell.y - minCell.y + 1;
        length = Mathf.Clamp(bladeLength, 1, height);

        var cellSize = GridManager.Instance.CellSize;
        var bottomCellCenterY = GridManager.Instance.CellToWorldCenter(minCell).y;
        var centerIndex = Mathf.RoundToInt((knifePosition.y - bottomCellCenterY) / cellSize);
        startIndex = Mathf.Clamp(centerIndex - (length - 1) / 2, 0, height - length);
    }

    private static void CalculateHorizontalCutSegment(ChocolatePiece piece, Vector3 knifePosition, int bladeLength, out int startIndex, out int length)
    {
        piece.GetOccupiedCellBounds(out var minCell, out var maxCell);

        var width = maxCell.x - minCell.x + 1;
        length = Mathf.Clamp(bladeLength, 1, width);

        var cellSize = GridManager.Instance.CellSize;
        var leftCellCenterX = GridManager.Instance.CellToWorldCenter(minCell).x;
        var centerIndex = Mathf.RoundToInt((knifePosition.x - leftCellCenterX) / cellSize);
        startIndex = Mathf.Clamp(centerIndex - (length - 1) / 2, 0, width - length);
    }
}
