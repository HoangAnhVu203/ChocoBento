using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelDoneLevel : UICanvas
{
    [SerializeField] private RectTransform firstImage;
    [SerializeField] private RectTransform secondImage;
    [SerializeField] private Vector3 firstImageStartScale = Vector3.one;
    [SerializeField] private Vector3 firstImageEndScale = new(30f, 30f, 1f);
    [SerializeField] private Vector3 secondImageStartScale = Vector3.one;
    [SerializeField] private Vector3 secondImageEndScale = new(7f, 7f, 1f);
    [SerializeField, Min(0.01f)] private float imageScaleDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float trayMoveDuration = 0.5f;
    [SerializeField, Min(0f)] private float finishDelay = 1f;
    [SerializeField] private Vector3 trayCenterScreenOffset;

    private Coroutine sequenceCoroutine;
    private Action onComplete;
    private readonly List<SpriteRenderer> hiddenWorldRenderers = new();

    public void Play(Action completed)
    {
        onComplete = completed;
        ResolveImages();
        RestoreHiddenWorldRenderers();

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (secondImage != null)
        {
            secondImage.localScale = secondImageStartScale;
            secondImage.gameObject.SetActive(false);
        }

        if (firstImage != null)
        {
            firstImage.gameObject.SetActive(true);
            firstImage.localScale = firstImageStartScale;
            yield return ScaleRoutine(firstImage, firstImageStartScale, firstImageEndScale, imageScaleDuration);
        }

        var trayRoot = CreateAnimatedTrayUiRoot();
        if (trayRoot != null)
        {
            yield return MoveTrayToCenterRoutine(trayRoot);
        }

        if (secondImage != null)
        {
            secondImage.gameObject.SetActive(true);
            secondImage.localScale = secondImageStartScale;
            yield return ScaleRoutine(secondImage, secondImageStartScale, secondImageEndScale, imageScaleDuration);
        }

        if (finishDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(finishDelay);
        }

        sequenceCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator ScaleRoutine(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(from, to, EaseOutCubic(t));
            yield return null;
        }

        target.localScale = to;
    }

    private IEnumerator MoveTrayToCenterRoutine(RectTransform trayRoot)
    {
        var start = trayRoot.anchoredPosition;
        var target = new Vector2(trayCenterScreenOffset.x, trayCenterScreenOffset.y);

        var elapsed = 0f;
        while (elapsed < trayMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / trayMoveDuration);
            trayRoot.anchoredPosition = Vector2.LerpUnclamped(start, target, EaseOutCubic(t));
            yield return null;
        }

        trayRoot.anchoredPosition = target;
    }

    private RectTransform CreateAnimatedTrayUiRoot()
    {
        var targetTray = TargetTray.Instance;
        if (targetTray == null)
        {
            return null;
        }

        var animatedObjects = new List<Transform> { targetTray.transform };
        var pieces = FindObjectsByType<ChocolatePiece>(FindObjectsSortMode.None);
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && pieces[i].IsInTargetTray)
            {
                animatedObjects.Add(pieces[i].transform);
            }
        }

        var spriteRenderers = CollectSpriteRenderers(animatedObjects);
        if (spriteRenderers.Count == 0)
        {
            return null;
        }

        var panelRect = transform as RectTransform;
        if (panelRect == null)
        {
            return null;
        }

        var root = new GameObject("DoneLevelTrayUiRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(panelRect, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.localScale = Vector3.one;
        root.SetAsLastSibling();

        var center = GetUiBoundsCenter(spriteRenderers, panelRect);
        root.anchoredPosition = center;

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            var source = spriteRenderers[i];
            if (source == null || source.sprite == null)
            {
                continue;
            }

            var image = new GameObject(source.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var imageRect = image.GetComponent<RectTransform>();
            imageRect.SetParent(root, false);
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = WorldToPanelPoint(source.bounds.center, panelRect) - center;
            imageRect.sizeDelta = GetRendererUiSize(source, panelRect);
            imageRect.localRotation = Quaternion.identity;
            imageRect.localScale = Vector3.one;

            var imageComponent = image.GetComponent<Image>();
            imageComponent.sprite = source.sprite;
            imageComponent.color = source.color;
            imageComponent.preserveAspect = false;

            hiddenWorldRenderers.Add(source);
            source.enabled = false;
        }

        return root;
    }

    private List<SpriteRenderer> CollectSpriteRenderers(List<Transform> transforms)
    {
        var renderers = new List<SpriteRenderer>();
        var uniqueRenderers = new HashSet<SpriteRenderer>();

        for (int i = 0; i < transforms.Count; i++)
        {
            if (transforms[i] == null)
            {
                continue;
            }

            var childRenderers = transforms[i].GetComponentsInChildren<SpriteRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < childRenderers.Length; rendererIndex++)
            {
                var renderer = childRenderers[rendererIndex];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null)
                {
                    continue;
                }

                if (uniqueRenderers.Add(renderer))
                {
                    renderers.Add(renderer);
                }
            }
        }

        renderers.Sort((left, right) =>
        {
            var layerCompare = left.sortingLayerID.CompareTo(right.sortingLayerID);
            return layerCompare != 0 ? layerCompare : left.sortingOrder.CompareTo(right.sortingOrder);
        });

        return renderers;
    }

    private Vector2 GetUiBoundsCenter(List<SpriteRenderer> renderers, RectTransform panelRect)
    {
        var hasBounds = false;
        var min = Vector2.zero;
        var max = Vector2.zero;

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            var bounds = renderers[i].bounds;
            var corners = new[]
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.center.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.center.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.center.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.center.z)
            };

            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                var point = WorldToPanelPoint(corners[cornerIndex], panelRect);
                if (!hasBounds)
                {
                    min = point;
                    max = point;
                    hasBounds = true;
                }
                else
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
        }

        return hasBounds ? (min + max) * 0.5f : Vector2.zero;
    }

    private Vector2 GetRendererUiSize(SpriteRenderer renderer, RectTransform panelRect)
    {
        var bounds = renderer.bounds;
        var min = WorldToPanelPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z), panelRect);
        var max = WorldToPanelPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.center.z), panelRect);
        return new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
    }

    private Vector2 WorldToPanelPoint(Vector3 worldPosition, RectTransform panelRect)
    {
        var camera = Camera.main;
        var canvas = GetComponentInParent<Canvas>();
        var canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPoint, canvasCamera, out var localPoint);
        return localPoint;
    }

    private void ResolveImages()
    {
        if (firstImage != null && secondImage != null)
        {
            return;
        }

        var images = GetComponentsInChildren<Image>(true);
        var found = 0;
        for (int i = 0; i < images.Length; i++)
        {
            var rect = images[i].transform as RectTransform;
            if (rect == null || rect == transform)
            {
                continue;
            }

            if (found == 0 && firstImage == null)
            {
                firstImage = rect;
                found++;
            }
            else if (found <= 1 && secondImage == null && rect != firstImage)
            {
                secondImage = rect;
                found++;
            }
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        var inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private void RestoreHiddenWorldRenderers()
    {
        for (int i = 0; i < hiddenWorldRenderers.Count; i++)
        {
            if (hiddenWorldRenderers[i] != null)
            {
                hiddenWorldRenderers[i].enabled = true;
            }
        }

        hiddenWorldRenderers.Clear();
    }

    private void OnDisable()
    {
        RestoreHiddenWorldRenderers();
    }
}
