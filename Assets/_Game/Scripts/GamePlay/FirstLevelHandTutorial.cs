using System.Collections;
using Spine.Unity;
using UnityEngine;

public sealed class FirstLevelHandTutorial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handRoot;
    [SerializeField] private SkeletonAnimation spineHand;
    [SerializeField] private ChocolatePiece targetPiece;
    [SerializeField] private TargetTray targetTray;

    [Header("Level")]
    [SerializeField] private int tutorialLevelIndex = 0;
    [SerializeField] private bool onlyRunOnTutorialLevel = true;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveToTrayDuration = 0.65f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float repeatDelay = 0.15f;
    [SerializeField] private Vector3 chocolatePointOffset;
    [SerializeField] private Vector3 trayPointOffset;
    [SerializeField] private bool snapToChocolateOnStart = true;

    private Coroutine tutorialRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        if (ShouldSkipTutorial())
        {
            gameObject.SetActive(false);
            return;
        }

        tutorialRoutine = StartCoroutine(PlayTutorialLoop());
    }

    private void OnDisable()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }
    }

    private IEnumerator PlayTutorialLoop()
    {
        yield return WaitForTargets();

        if (IsTutorialComplete())
        {
            gameObject.SetActive(false);
            yield break;
        }

        var chocolatePoint = GetChocolatePoint();
        if (snapToChocolateOnStart)
        {
            handRoot.position = chocolatePoint;
        }
        SetHandAlpha(1f);

        while (!IsTutorialComplete())
        {
            chocolatePoint = GetChocolatePoint();
            var trayPoint = GetTrayPoint();

            yield return MoveHand(handRoot.position, trayPoint, moveToTrayDuration);
            if (IsTutorialComplete())
            {
                break;
            }

            yield return FadeHand(1f, 0f, fadeDuration);
            handRoot.position = GetChocolatePoint();
            yield return FadeHand(0f, 1f, fadeDuration);

            if (repeatDelay > 0f)
            {
                yield return new WaitForSeconds(repeatDelay);
            }
        }

        gameObject.SetActive(false);
    }

    private IEnumerator WaitForTargets()
    {
        while (targetPiece == null || targetTray == null)
        {
            ResolveReferences();
            yield return null;
        }
    }

    private IEnumerator MoveHand(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f || (from - to).sqrMagnitude <= 0.0001f)
        {
            handRoot.position = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && !IsTutorialComplete())
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            handRoot.position = Vector3.LerpUnclamped(from, to, EaseInOutCubic(time));
            yield return null;
        }

        handRoot.position = to;
    }

    private Vector3 GetChocolatePoint()
    {
        if (targetPiece == null)
        {
            return handRoot.position;
        }

        return targetPiece.GetWorldBounds().center + chocolatePointOffset;
    }

    private Vector3 GetTrayPoint()
    {
        if (targetTray == null)
        {
            return handRoot.position;
        }

        return GetTransformBoundsCenter(targetTray.transform) + trayPointOffset;
    }

    private Vector3 GetTransformBoundsCenter(Transform root)
    {
        if (root == null)
        {
            return handRoot.position;
        }

        bool hasBounds = false;
        Bounds bounds = default;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        var colliders = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        return hasBounds ? bounds.center : root.position;
    }

    private bool IsTutorialComplete()
    {
        return targetPiece == null || targetPiece.IsInTargetTray;
    }

    private bool ShouldSkipTutorial()
    {
        return onlyRunOnTutorialLevel
            && LevelManager.Instance != null
            && LevelManager.Instance.CurrentLevelIndex != tutorialLevelIndex;
    }

    private void ResolveReferences()
    {
        if (handRoot == null)
        {
            handRoot = transform;
        }

        if (spineHand == null)
        {
            spineHand = GetComponent<SkeletonAnimation>();
        }

        if (targetPiece == null)
        {
            targetPiece = FindFirstAvailablePiece();
        }

        if (targetTray == null)
        {
            targetTray = TargetTray.Instance != null ? TargetTray.Instance : FindFirstObjectByType<TargetTray>();
        }
    }

    private IEnumerator FadeHand(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetHandAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && !IsTutorialComplete())
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            SetHandAlpha(Mathf.Lerp(from, to, time));
            yield return null;
        }

        SetHandAlpha(to);
    }

    private void SetHandAlpha(float alpha)
    {
        if (spineHand == null)
        {
            return;
        }

        var color = spineHand.Skeleton.GetColor();
        color.a = alpha;
        spineHand.Skeleton.SetColor(color);
    }

    private ChocolatePiece FindFirstAvailablePiece()
    {
        var pieces = FindObjectsByType<ChocolatePiece>(FindObjectsSortMode.None);
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && !pieces[i].IsInTargetTray)
            {
                return pieces[i];
            }
        }

        return null;
    }

    private float EaseInOutCubic(float time)
    {
        return time < 0.5f
            ? 4f * time * time * time
            : 1f - Mathf.Pow(-2f * time + 2f, 3f) * 0.5f;
    }
}
