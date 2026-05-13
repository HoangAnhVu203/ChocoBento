using System.Collections.Generic;
using UnityEngine;

public sealed class ChocolateDragLiftVisual : MonoBehaviour
{
    [SerializeField] private float liftedScale = 1.06f;
    [SerializeField] private Vector3 liftedOffset = new(0f, 0.08f, 0f);
    [SerializeField] private Vector3 liftedShadowOffset = new(-0.035f, -0.035f, 0f);
    [SerializeField] private int liftedSortingOrderBoost = 20;

    private readonly List<SpriteRendererState> rendererStates = new();
    private readonly List<TransformState> childStates = new();
    private readonly List<TransformState> shadowStates = new();
    private Vector3 baseScale;
    private bool isLifted;

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

        isLifted = false;
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
}
