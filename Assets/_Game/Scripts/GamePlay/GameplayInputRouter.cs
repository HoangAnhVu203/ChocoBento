using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameplayInputRouter : MonoBehaviour
{
    private IDraggableGameplay activeDrag;

    private void Update()
    {
        if (WasPressedThisFrame())
        {
            var hit = Physics2D.OverlapPoint(PointerWorldPosition());
            activeDrag = hit != null ? hit.GetComponentInParent<IDraggableGameplay>() : null;
            activeDrag?.BeginDrag(PointerWorldPosition());
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
}
