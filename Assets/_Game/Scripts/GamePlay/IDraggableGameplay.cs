using UnityEngine;

public interface IDraggableGameplay
{
    void BeginDrag(Vector3 pointerWorldPosition);
    void Drag(Vector3 pointerWorldPosition);
    void EndDrag(Vector3 pointerWorldPosition);
}
