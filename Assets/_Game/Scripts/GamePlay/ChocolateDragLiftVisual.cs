using System.Collections.Generic;
using UnityEngine;

public sealed class ChocolateDragLiftVisual : MonoBehaviour
{
    [SerializeField] private float liftedScale = 1.06f;
    [SerializeField] private Vector3 liftedOffset = new(0f, 0.08f, 0f);
    [SerializeField] private Vector3 liftedShadowOffset = new(-0.035f, -0.035f, 0f);
    [SerializeField] private int liftedSortingOrderBoost = 20;
    [SerializeField] private string strawberryJiggleNameContains = "StrawberryImage";
    [SerializeField, Min(0f)] private float strawberryJiggleDistance = 0.09f;
    [SerializeField, Min(0f)] private float strawberryJiggleVelocityStrength = 0.07f;
    [SerializeField, Min(0f)] private float strawberryJiggleSpring = 36f;
    [SerializeField, Min(0f)] private float strawberryJiggleDamping = 8f;
    [SerializeField, Min(0f)] private float strawberryJiggleTilt = 24f;
    [SerializeField, Min(0f)] private float strawberryJiggleSquash = 0.14f;
    [SerializeField, Min(0f)] private float strawberryJiggleForceSpeed = 5.5f;
    [SerializeField, Min(0.001f)] private float strawberryJiggleVelocitySmoothTime = 0.11f;
    [SerializeField, Min(0f)] private float strawberryJiggleSettleThreshold = 0.002f;

    private readonly List<SpriteRendererState> rendererStates = new();
    private readonly List<TransformState> childStates = new();
    private readonly List<TransformState> shadowStates = new();
    private readonly List<StrawberryJiggleState> strawberryJiggleStates = new();
    private Vector3 baseScale;
    private Vector3 lastWorldPosition;
    private Vector2 smoothedLocalVelocity;
    private Vector2 smoothedLocalVelocityRef;
    private bool isLifted;
    private bool isSettlingStrawberries;

    private void Awake()
    {
        CaptureBaseTransform();
    }

    public void Lift()
    {
        if (isLifted)
        {
            return;
        }

        CaptureBaseTransform();
        CaptureRendererStates();
        CaptureChildStates();
        CaptureShadowStates();
        CaptureStrawberryJiggleStates();

        transform.localScale = baseScale * liftedScale;
        for (int i = 0; i < childStates.Count; i++)
        {
            if (childStates[i].Transform != null)
            {
                childStates[i].Transform.localPosition = childStates[i].LocalPosition + liftedOffset;
            }
        }

        for (int i = 0; i < shadowStates.Count; i++)
        {
            if (shadowStates[i].Transform != null)
            {
                shadowStates[i].Transform.localPosition = shadowStates[i].LocalPosition - liftedOffset + liftedShadowOffset;
            }
        }

        for (int i = 0; i < rendererStates.Count; i++)
        {
            if (rendererStates[i].Renderer != null)
            {
                rendererStates[i].Renderer.sortingOrder = rendererStates[i].SortingOrder + liftedSortingOrderBoost;
            }
        }

        isLifted = true;
        isSettlingStrawberries = false;
        smoothedLocalVelocity = Vector2.zero;
        smoothedLocalVelocityRef = Vector2.zero;
        lastWorldPosition = transform.position;
    }

    public void Drop()
    {
        if (!isLifted)
        {
            return;
        }

        transform.localScale = baseScale;
        for (int i = 0; i < childStates.Count; i++)
        {
            if (childStates[i].Transform != null)
            {
                childStates[i].Transform.localPosition = childStates[i].LocalPosition;
            }
        }

        for (int i = 0; i < shadowStates.Count; i++)
        {
            if (shadowStates[i].Transform != null)
            {
                shadowStates[i].Transform.localPosition = shadowStates[i].LocalPosition;
            }
        }

        for (int i = 0; i < rendererStates.Count; i++)
        {
            if (rendererStates[i].Renderer != null)
            {
                rendererStates[i].Renderer.sortingOrder = rendererStates[i].SortingOrder;
            }
        }

        RestoreStrawberryJiggleStates();
        isLifted = false;
        isSettlingStrawberries = false;
    }

    private void LateUpdate()
    {
        if (!isLifted && !isSettlingStrawberries)
        {
            return;
        }

        var deltaTime = Time.deltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            lastWorldPosition = transform.position;
            return;
        }

        var worldVelocity = isLifted ? (transform.position - lastWorldPosition) / deltaTime : Vector3.zero;
        if (isLifted)
        {
            lastWorldPosition = transform.position;
        }

        UpdateStrawberryJiggle(worldVelocity, deltaTime);
    }

    private void CaptureBaseTransform()
    {
        baseScale = transform.localScale;
    }

    private void CaptureRendererStates()
    {
        rendererStates.Clear();
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsShadowTransform(renderers[i].transform))
            {
                continue;
            }

            rendererStates.Add(new SpriteRendererState(renderers[i], renderers[i].sortingOrder));
        }
    }

    private void CaptureChildStates()
    {
        childStates.Clear();
        foreach (Transform child in transform)
        {
            childStates.Add(new TransformState(child, child.localPosition));
        }
    }

    private void CaptureShadowStates()
    {
        shadowStates.Clear();
        var children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != transform && IsShadowTransform(children[i]))
            {
                shadowStates.Add(new TransformState(children[i], children[i].localPosition));
            }
        }
    }

    private void CaptureStrawberryJiggleStates()
    {
        strawberryJiggleStates.Clear();

        if (string.IsNullOrWhiteSpace(strawberryJiggleNameContains))
        {
            return;
        }

        var children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            if (child == transform
                || child.name.IndexOf(strawberryJiggleNameContains, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            strawberryJiggleStates.Add(new StrawberryJiggleState(
                child,
                child.localPosition,
                child.localScale,
                child.localRotation));
        }
    }

    private void UpdateStrawberryJiggle(Vector3 worldVelocity, float deltaTime)
    {
        if (strawberryJiggleStates.Count == 0)
        {
            return;
        }

        var localVelocity = transform.InverseTransformVector(worldVelocity);
        var targetLocalVelocity = isLifted ? new Vector2(localVelocity.x, localVelocity.y) : Vector2.zero;
        smoothedLocalVelocity = Vector2.SmoothDamp(
            smoothedLocalVelocity,
            targetLocalVelocity,
            ref smoothedLocalVelocityRef,
            strawberryJiggleVelocitySmoothTime,
            Mathf.Infinity,
            deltaTime);

        var localVelocity2D = smoothedLocalVelocity;
        var forceAmount = Mathf.Clamp01(localVelocity2D.magnitude / Mathf.Max(strawberryJiggleForceSpeed, 0.001f));
        var windPull = localVelocity2D.sqrMagnitude > 0.0001f
            ? -localVelocity2D.normalized * strawberryJiggleDistance * forceAmount
            : Vector2.zero;
        var targetOffset = new Vector3(
            windPull.x - localVelocity2D.x * strawberryJiggleVelocityStrength,
            windPull.y - localVelocity2D.y * strawberryJiggleVelocityStrength,
            0f);
        targetOffset = Vector3.ClampMagnitude(targetOffset, strawberryJiggleDistance);
        targetOffset.z = 0f;

        var hasVisibleMotion = false;
        for (int i = 0; i < strawberryJiggleStates.Count; i++)
        {
            var state = strawberryJiggleStates[i];
            if (state.Transform == null)
            {
                continue;
            }

            var displacement = state.Offset - targetOffset;
            var acceleration = (-strawberryJiggleSpring * displacement) - (strawberryJiggleDamping * state.Velocity);
            state.Velocity += acceleration * deltaTime;
            state.Offset += state.Velocity * deltaTime;
            state.Offset = Vector3.ClampMagnitude(state.Offset, strawberryJiggleDistance);
            state.Offset.z = 0f;
            hasVisibleMotion |= state.Offset.sqrMagnitude > strawberryJiggleSettleThreshold * strawberryJiggleSettleThreshold
                || state.Velocity.sqrMagnitude > strawberryJiggleSettleThreshold * strawberryJiggleSettleThreshold;

            var jiggleAmount = strawberryJiggleDistance > Mathf.Epsilon
                ? Mathf.Clamp01(state.Offset.magnitude / strawberryJiggleDistance)
                : 0f;
            var tiltFromOffset = Mathf.Clamp(-state.Offset.x / Mathf.Max(strawberryJiggleDistance, 0.001f), -1f, 1f);
            var tiltFromVerticalDrag = Mathf.Clamp(state.Offset.y / Mathf.Max(strawberryJiggleDistance, 0.001f), -1f, 1f) * 0.35f;
            var tilt = (tiltFromOffset + tiltFromVerticalDrag) * strawberryJiggleTilt;
            var squash = strawberryJiggleSquash * jiggleAmount;
            var direction = state.Offset.sqrMagnitude > 0.0001f
                ? new Vector2(state.Offset.x, state.Offset.y).normalized
                : Vector2.zero;
            var stretchX = Mathf.Abs(direction.x) * squash;
            var stretchY = Mathf.Abs(direction.y) * squash;
            var compressX = stretchY * 0.55f;
            var compressY = stretchX * 0.55f;
            var scaleX = 1f + stretchX - compressX;
            var scaleY = 1f + stretchY - compressY;

            state.Transform.localPosition = state.BaseLocalPosition + state.Offset;
            state.Transform.localRotation = state.BaseLocalRotation * Quaternion.Euler(0f, 0f, tilt);
            state.Transform.localScale = new Vector3(
                state.BaseLocalScale.x * Mathf.Max(0.2f, scaleX),
                state.BaseLocalScale.y * Mathf.Max(0.2f, scaleY),
                state.BaseLocalScale.z);

            strawberryJiggleStates[i] = state;
        }

        if (!isLifted && isSettlingStrawberries && !hasVisibleMotion && smoothedLocalVelocity.sqrMagnitude <= 0.0001f)
        {
            RestoreStrawberryJiggleStates();
            isSettlingStrawberries = false;
        }
    }

    private void RestoreStrawberryJiggleStates()
    {
        for (int i = 0; i < strawberryJiggleStates.Count; i++)
        {
            var state = strawberryJiggleStates[i];
            if (state.Transform == null)
            {
                continue;
            }

            state.Transform.localPosition = state.BaseLocalPosition;
            state.Transform.localScale = state.BaseLocalScale;
            state.Transform.localRotation = state.BaseLocalRotation;
        }

        strawberryJiggleStates.Clear();
    }

    private static bool IsShadowTransform(Transform target)
    {
        while (target != null)
        {
            if (target.name.IndexOf("Shadow", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    private readonly struct SpriteRendererState
    {
        public readonly SpriteRenderer Renderer;
        public readonly int SortingOrder;

        public SpriteRendererState(SpriteRenderer renderer, int sortingOrder)
        {
            Renderer = renderer;
            SortingOrder = sortingOrder;
        }
    }

    private readonly struct TransformState
    {
        public readonly Transform Transform;
        public readonly Vector3 LocalPosition;

        public TransformState(Transform transform, Vector3 localPosition)
        {
            Transform = transform;
            LocalPosition = localPosition;
        }
    }

    private struct StrawberryJiggleState
    {
        public readonly Transform Transform;
        public readonly Vector3 BaseLocalPosition;
        public readonly Vector3 BaseLocalScale;
        public readonly Quaternion BaseLocalRotation;
        public Vector3 Offset;
        public Vector3 Velocity;

        public StrawberryJiggleState(
            Transform transform,
            Vector3 baseLocalPosition,
            Vector3 baseLocalScale,
            Quaternion baseLocalRotation)
        {
            Transform = transform;
            BaseLocalPosition = baseLocalPosition;
            BaseLocalScale = baseLocalScale;
            BaseLocalRotation = baseLocalRotation;
            Offset = Vector3.zero;
            Velocity = Vector3.zero;
        }
    }
}
