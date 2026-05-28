using System.Collections;
using Spine.Unity;
using UnityEngine;

public sealed class ThirdLevelKnifeTutorial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handRoot;
    [SerializeField] private SkeletonAnimation spineHand;
    [SerializeField] private KnifeDrag targetKnife;
    [SerializeField] private ChocolatePiece targetPiece;
    [SerializeField] private Transform cutPointOverride;

    [Header("Level")]
    [SerializeField] private int tutorialLevelIndex = 2;
    [SerializeField] private string tutorialLevelName = "Level_3";
    [SerializeField] private bool onlyRunOnTutorialLevel = true;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveToCutDuration = 0.65f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.4f;
    [SerializeField, Min(0f)] private float repeatDelay = 0.15f;
    [SerializeField] private Vector3 knifePointOffset;
    [SerializeField] private Vector3 cutPointOffset;
    [SerializeField] private Vector3 cutButtonOffset;
    [SerializeField] private bool snapToKnifeOnStart = true;

    [Header("Animation")]
    [SerializeField] private string cutButtonIdleAnimation = "idle";

    private Coroutine tutorialRoutine;
    private PanelGameplay gameplayPanel;
    private bool knifeWasDragged;
    private bool tutorialComplete;

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

        ResolveReferences();
        Subscribe();
        tutorialRoutine = StartCoroutine(PlayTutorial());
    }

    private void OnDisable()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        Unsubscribe();
    }

    private IEnumerator PlayTutorial()
    {
        yield return WaitForTargets();

        if (tutorialComplete)
        {
            gameObject.SetActive(false);
            yield break;
        }

        if (snapToKnifeOnStart)
        {
            handRoot.position = GetKnifePoint();
        }

        SetHandAlpha(1f);

        while (!tutorialComplete && !knifeWasDragged)
        {
            var knifePoint = GetKnifePoint();
            var cutPoint = GetCutPoint();

            handRoot.position = knifePoint;
            SetHandAlpha(1f);

            yield return MoveHand(knifePoint, cutPoint, moveToCutDuration, () => tutorialComplete || knifeWasDragged);
            if (tutorialComplete || knifeWasDragged)
            {
                break;
            }

            yield return FadeHand(1f, 0f, fadeDuration, () => tutorialComplete || knifeWasDragged);
            handRoot.position = GetKnifePoint();
            yield return FadeHand(0f, 1f, fadeDuration, () => tutorialComplete || knifeWasDragged);

            if (repeatDelay > 0f)
            {
                yield return new WaitForSeconds(repeatDelay);
            }
        }

        SetHandAlpha(0f);

        while (!tutorialComplete && !IsKnifeReadyToCut())
        {
            yield return null;
        }

        if (tutorialComplete)
        {
            gameObject.SetActive(false);
            yield break;
        }

        PlayIdleAnimation();
        SetHandAlpha(1f);

        while (!tutorialComplete)
        {
            handRoot.position = GetCutButtonPoint();
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator WaitForTargets()
    {
        while (!tutorialComplete && (targetKnife == null || targetPiece == null || gameplayPanel == null))
        {
            ResolveReferences();
            Subscribe();
            yield return null;
        }
    }

    private IEnumerator MoveHand(Vector3 from, Vector3 to, float duration, System.Func<bool> shouldStop)
    {
        if (duration <= 0f || (from - to).sqrMagnitude <= 0.0001f)
        {
            handRoot.position = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && !shouldStop())
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            handRoot.position = Vector3.LerpUnclamped(from, to, EaseInOutCubic(time));
            yield return null;
        }

        if (!shouldStop())
        {
            handRoot.position = to;
        }
    }

    private IEnumerator FadeHand(float from, float to, float duration, System.Func<bool> shouldStop)
    {
        if (duration <= 0f)
        {
            SetHandAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && !shouldStop())
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            SetHandAlpha(Mathf.Lerp(from, to, time));
            yield return null;
        }

        if (!shouldStop())
        {
            SetHandAlpha(to);
        }
    }

    private Vector3 GetKnifePoint()
    {
        return targetKnife != null ? targetKnife.transform.position + knifePointOffset : handRoot.position;
    }

    private Vector3 GetCutPoint()
    {
        if (cutPointOverride != null)
        {
            return cutPointOverride.position + cutPointOffset;
        }

        if (targetPiece == null || GridManager.Instance == null)
        {
            return handRoot.position;
        }

        targetPiece.GetOccupiedCellBounds(out var minCell, out var maxCell);
        var cellSize = GridManager.Instance.CellSize;
        var center = targetPiece.GetWorldBounds().center;

        if (targetKnife != null && targetKnife.CutDirection == KnifeCutDirection.Horizontal)
        {
            var bottomBoundaryY = GridManager.Instance.CellToWorldCenter(minCell).y - cellSize * 0.5f;
            return new Vector3(center.x, bottomBoundaryY + cellSize, center.z) + cutPointOffset;
        }

        var leftBoundaryX = GridManager.Instance.CellToWorldCenter(minCell).x - cellSize * 0.5f;
        return new Vector3(leftBoundaryX + cellSize, center.y, center.z) + cutPointOffset;
    }

    private Vector3 GetCutButtonPoint()
    {
        ResolveReferences();
        var cutButton = gameplayPanel != null ? gameplayPanel.CutButtonTransform as RectTransform : null;
        if (cutButton == null)
        {
            return handRoot.position;
        }

        var canvas = cutButton.GetComponentInParent<Canvas>();
        var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, cutButton.position);
        var camera = Camera.main;
        if (camera == null)
        {
            return cutButton.position + cutButtonOffset;
        }

        screenPoint.z = -camera.transform.position.z;
        return camera.ScreenToWorldPoint(screenPoint) + cutButtonOffset;
    }

    private bool IsKnifeReadyToCut()
    {
        return targetKnife != null && !targetKnife.IsDragging && targetKnife.RefreshCutData(false);
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

        if (targetKnife == null)
        {
            targetKnife = FindFirstObjectByType<KnifeDrag>();
        }

        if (targetPiece == null)
        {
            targetPiece = FindFirstObjectByType<ChocolatePiece>();
        }

        if (gameplayPanel == null)
        {
            gameplayPanel = PanelGameplay.Instance != null ? PanelGameplay.Instance : FindFirstObjectByType<PanelGameplay>();
        }
    }

    private void Subscribe()
    {
        if (targetKnife != null)
        {
            targetKnife.DragStarted -= HandleKnifeDragStarted;
            targetKnife.DragStarted += HandleKnifeDragStarted;
        }

        if (gameplayPanel != null)
        {
            gameplayPanel.CutApplied -= HandleCutApplied;
            gameplayPanel.CutApplied += HandleCutApplied;
        }
    }

    private void Unsubscribe()
    {
        if (targetKnife != null)
        {
            targetKnife.DragStarted -= HandleKnifeDragStarted;
        }

        if (gameplayPanel != null)
        {
            gameplayPanel.CutApplied -= HandleCutApplied;
        }
    }

    private void HandleKnifeDragStarted()
    {
        knifeWasDragged = true;
    }

    private void HandleCutApplied()
    {
        tutorialComplete = true;
    }

    private bool ShouldSkipTutorial()
    {
        if (!onlyRunOnTutorialLevel || LevelManager.Instance == null)
        {
            return false;
        }

        if (LevelManager.Instance.CurrentLevelIndex == tutorialLevelIndex)
        {
            return false;
        }

        return string.IsNullOrEmpty(tutorialLevelName) || transform.root.name != tutorialLevelName;
    }

    private void PlayIdleAnimation()
    {
        if (spineHand == null || string.IsNullOrEmpty(cutButtonIdleAnimation))
        {
            return;
        }

        if (spineHand.Skeleton.Data.FindAnimation(cutButtonIdleAnimation) == null)
        {
            return;
        }

        spineHand.AnimationState.SetAnimation(0, cutButtonIdleAnimation, true);
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

    private float EaseInOutCubic(float time)
    {
        return time < 0.5f
            ? 4f * time * time * time
            : 1f - Mathf.Pow(-2f * time + 2f, 3f) * 0.5f;
    }
}
