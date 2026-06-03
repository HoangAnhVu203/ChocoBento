using System;
using UnityEngine;

public enum KnifeCutDirection
{
    Vertical,
    Horizontal
}

public sealed class KnifeDrag : MonoBehaviour, IDraggableGameplay
{
    [SerializeField] private KnifeCutDirection cutDirection = KnifeCutDirection.Vertical;
    [SerializeField, Min(1)] private int bladeLengthInCells = 4;
    [SerializeField] private bool snapToCutLineOnStart = true;

    private Vector3 dragOffset;
    private ChocolatePiece hoveredPiece;
    private int cutIndex = -1;
    private int cutStartIndex;
    private int cutLength;
    private bool isDragging;

    public event Action DragStarted;
    public event Action DragEnded;

    public KnifeCutDirection CutDirection => cutDirection;
    public ChocolatePiece HoveredPiece => hoveredPiece;
    public int CutIndex => cutIndex;
    public int CutStartIndex => cutStartIndex;
    public int CutLength => cutLength;
    public int BladeLengthInCells => bladeLengthInCells;
    public bool IsDragging => isDragging;
    public bool HasValidCut => hoveredPiece != null && cutIndex > 0 && cutLength > 0;

    private void Awake()
    {
        GuessDirectionFromRotation();
    }

    private void Start()
    {
        if (snapToCutLineOnStart)
        {
            RefreshCutData(true);
        }
    }

    public void BeginDrag(Vector3 pointerWorldPosition)
    {
        isDragging = true;
        dragOffset = transform.position - pointerWorldPosition;
        GameplayLevelController.Instance?.SetSelectedKnife(this);
        AudioManager.Instance.PlayKnifeLift();
        DragStarted?.Invoke();
    }

    public void Drag(Vector3 pointerWorldPosition)
    {
        var desired = pointerWorldPosition + dragOffset;

        if (GridManager.Instance == null)
        {
            transform.position = desired;
            cutIndex = -1;
            cutLength = 0;
            GameplayLevelController.Instance?.SetCanCut(false);
            return;
        }

        transform.position = desired;

        hoveredPiece = GameplayLevelController.Instance != null
            ? GameplayLevelController.Instance.FindPieceForKnife(cutDirection, transform.position, bladeLengthInCells)
            : null;

        if (hoveredPiece == null)
        {
            cutIndex = -1;
            cutLength = 0;
            GameplayLevelController.Instance?.SetCanCut(false);
            return;
        }

        if (!GameplayLevelController.Instance.TryGetKnifeCutData(hoveredPiece, cutDirection, transform.position, bladeLengthInCells, out cutIndex, out cutStartIndex, out cutLength))
        {
            cutIndex = -1;
            cutLength = 0;
            GameplayLevelController.Instance?.SetCanCut(false);
            return;
        }

        GameplayLevelController.Instance?.SetCanCut(HasValidCut);
    }

    public void EndDrag(Vector3 pointerWorldPosition)
    {
        isDragging = false;
        RefreshCutData(true);
        GameplayLevelController.Instance?.SetSelectedKnife(this);
        GameplayLevelController.Instance?.SetCanCut(HasValidCut);
        AudioManager.Instance.PlayKnifeDrop();
        DragEnded?.Invoke();
    }

    public bool RefreshCutData(bool snapPosition = false)
    {
        if (GridManager.Instance == null || GameplayLevelController.Instance == null)
        {
            hoveredPiece = null;
            cutIndex = -1;
            cutLength = 0;
            return false;
        }

        if (snapPosition)
        {
            transform.position = cutDirection == KnifeCutDirection.Vertical
                ? GridManager.Instance.SnapWorldToVerticalCutLine(transform.position, bladeLengthInCells)
                : GridManager.Instance.SnapWorldToHorizontalCutLine(transform.position, bladeLengthInCells);
        }

        hoveredPiece = GameplayLevelController.Instance.FindPieceForKnife(cutDirection, transform.position, bladeLengthInCells);
        if (hoveredPiece == null)
        {
            cutIndex = -1;
            cutLength = 0;
            return false;
        }

        if (!GameplayLevelController.Instance.TryGetKnifeCutData(hoveredPiece, cutDirection, transform.position, bladeLengthInCells, out cutIndex, out cutStartIndex, out cutLength))
        {
            hoveredPiece = null;
            cutIndex = -1;
            cutLength = 0;
            return false;
        }

        return HasValidCut;
    }

    private void GuessDirectionFromRotation()
    {
        var z = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.z));
        cutDirection = z > 45f && z < 135f ? KnifeCutDirection.Vertical : KnifeCutDirection.Horizontal;
    }
}
