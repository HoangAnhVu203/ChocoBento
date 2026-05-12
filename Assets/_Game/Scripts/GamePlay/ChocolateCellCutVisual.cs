using UnityEngine;

public sealed class ChocolateCellCutVisual : MonoBehaviour
{
    public bool HasCellOffset { get; set; }
    public Vector3Int CellOffset { get; set; }
    public bool HasBaseTransform { get; set; }
    public Vector3 BaseLocalPosition { get; set; }
    public Vector3 BaseLocalScale { get; set; }
    public bool LeftInset { get; set; }
    public bool RightInset { get; set; }
    public bool TopInset { get; set; }
    public bool BottomInset { get; set; }
}
