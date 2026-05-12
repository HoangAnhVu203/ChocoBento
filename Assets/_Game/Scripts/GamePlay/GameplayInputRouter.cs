using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameplayInputRouter : MonoBehaviour
{
    [SerializeField, Min(0f)] private float pointerHitRadius = 0.18f;

    private IDraggableGameplay activeDrag;

    private void Update()
    {
        if (WasPressedThisFrame())
        {
            var pointerWorldPosition = PointerWorldPosition();
            activeDrag = FindBestDraggable(pointerWorldPosition);
            activeDrag?.BeginDrag(pointerWorldPosition);
        }

        if (IsPressed())
        {
            activeDrag?.Drag(PointerWorldPosition());
        }

        if (WasReleasedThisFrame())
        {
            activeDrag?.EndDrag(PointerWorldPosition());
            activeDrag = null;
        }
    }

    private static Vector3 PointerWorldPosition()
    {
        var screen = PointerScreenPosition();
        var camera = Camera.main;
        screen.z = camera != null ? -camera.transform.position.z : 10f;
        return camera != null ? camera.ScreenToWorldPoint(screen) : Vector3.zero;
    }

    private static Vector3 PointerScreenPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private static bool WasPressedThisFrame()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
               || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
    }

    private static bool IsPressed()
    {
        return (Mouse.current != null && Mouse.current.leftButton.isPressed)
               || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
    }

    private static bool WasReleasedThisFrame()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
               || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame);
    }

    private IDraggableGameplay FindBestDraggable(Vector3 pointerWorldPosition)
    {
        if (pointerHitRadius <= 0f)
        {
            var hit = Physics2D.OverlapPoint(pointerWorldPosition);
            return hit != null ? hit.GetComponentInParent<IDraggableGameplay>() : null;
        }

        var hits = Physics2D.OverlapCircleAll(pointerWorldPosition, pointerHitRadius);
        return FindClosestDraggable<KnifeDrag>(hits, pointerWorldPosition)
            ?? FindClosestDraggable<ChocolatePiece>(hits, pointerWorldPosition)
            ?? FindClosestDraggable<IDraggableGameplay>(hits, pointerWorldPosition);
    }

    private static IDraggableGameplay FindClosestDraggable<T>(Collider2D[] hits, Vector3 pointerWorldPosition)
    {
        IDraggableGameplay bestDrag = null;
        var bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            var drag = hit.GetComponentInParent<IDraggableGameplay>();
            if (drag == null || drag is not T)
            {
                continue;
            }

            var closestPoint = hit.ClosestPoint(pointerWorldPosition);
            var distance = ((Vector2)pointerWorldPosition - closestPoint).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDrag = drag;
            }
        }

        return bestDrag;
    }
}
