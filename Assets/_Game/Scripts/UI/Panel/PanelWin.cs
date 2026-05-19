using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelWin : UICanvas
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button bonusButton;
    [SerializeField] private Text nextRewardText;
    [SerializeField] private Text bonusRewardText;
    [SerializeField] private Text coinText;
    [SerializeField] private GameObject[] starRoots;
    [SerializeField] private RandomObjectSpawner[] randomObjectSpawners;
    [SerializeField] private RectTransform fxRoot;
    [SerializeField] private RectTransform coinFlyPrefab;
    [SerializeField] private RectTransform nextCoinFlyFrom;
    [SerializeField] private RectTransform bonusCoinFlyFrom;
    [SerializeField] private RectTransform coinFlyTo;
    [SerializeField, Min(1)] private int coinFlyCount = 12;
    [SerializeField, Min(0f)] private float nextLevelDelayAfterCoinFly = 1f;
    [SerializeField, Min(0.01f)] private float openScaleDuration = 0.5f;
    [Header("Win Decoration")]
    [SerializeField] private RectTransform[] bigHornRoots;
    [SerializeField] private RectTransform[] smallHornRoots;
    [SerializeField] private RectTransform chocoRoot;
    [SerializeField] private ParticleSystem[] bigHornVfx;
    [SerializeField] private ParticleSystem[] smallHornVfx;
    [SerializeField, Min(0.01f)] private float decorationScaleDuration = 0.22f;
    [SerializeField, Min(0f)] private float decorationStepDelay = 0.05f;

    private bool rewardClaimed;
    private Coroutine openScaleCoroutine;
    private Coroutine decorationCoroutine;

    private void OnEnable()
    {
        rewardClaimed = false;
        ResolveButtons();
        SetRewardButtonsInteractable(true);
        ResolveStarRoots();
        ResolveRandomObjectSpawners();
        BindCoinText();
        RefreshWinStars();
        RefreshRewardTexts();
        PrepareWinDecorations();
        PlayOpenScaleAnimation();
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextClicked);
            nextButton.onClick.AddListener(HandleNextClicked);
        }

        if (bonusButton != null)
        {
            bonusButton.onClick.RemoveListener(HandleBonusClicked);
            bonusButton.onClick.AddListener(HandleBonusClicked);
        }

    }

    private void OnDisable()
    {
        if (openScaleCoroutine != null)
        {
            StopCoroutine(openScaleCoroutine);
            openScaleCoroutine = null;
        }

        if (decorationCoroutine != null)
        {
            StopCoroutine(decorationCoroutine);
            decorationCoroutine = null;
        }

        transform.localScale = Vector3.one;
        RestoreWinDecorations();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        if (bonusButton != null)
        {
            bonusButton.onClick.RemoveListener(HandleBonusClicked);
        }

        CoinManager.Instance.CoinChanged -= RefreshCoinText;
    }

    private void PlayOpenScaleAnimation()
    {
        if (openScaleCoroutine != null)
        {
            StopCoroutine(openScaleCoroutine);
        }

        openScaleCoroutine = StartCoroutine(OpenScaleRoutine());
    }

    private IEnumerator OpenScaleRoutine()
    {
        transform.localScale = Vector3.zero;

        var elapsed = 0f;
        while (elapsed < openScaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / openScaleDuration);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(t));
            yield return null;
        }

        transform.localScale = Vector3.one;
        openScaleCoroutine = null;
        PlayWinDecorationAnimation();
    }

    private void PlayWinDecorationAnimation()
    {
        if (decorationCoroutine != null)
        {
            StopCoroutine(decorationCoroutine);
        }

        decorationCoroutine = StartCoroutine(WinDecorationRoutine());
    }

    private IEnumerator WinDecorationRoutine()
    {
        yield return ScaleGroupRoutine(bigHornRoots, decorationScaleDuration);
        PlayBigHornVfx();

        if (decorationStepDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(decorationStepDelay);
        }

        yield return ScaleGroupRoutine(smallHornRoots, decorationScaleDuration);
        PlaySmallHornVfx();

        if (decorationStepDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(decorationStepDelay);
        }

        if (chocoRoot != null)
        {
            yield return ScaleRectRoutine(chocoRoot, Vector3.zero, Vector3.one, decorationScaleDuration);
        }

        PlayRandomObjectSpawners();
        decorationCoroutine = null;
    }

    private IEnumerator ScaleGroupRoutine(RectTransform[] roots, float duration)
    {
        if (roots == null || roots.Length == 0)
        {
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var scale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(t));
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                {
                    roots[i].localScale = scale;
                }
            }

            yield return null;
        }

        SetGroupScale(roots, Vector3.one);
    }

    private IEnumerator ScaleRectRoutine(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(from, to, EaseOutBack(t));
            yield return null;
        }

        if (target != null)
        {
            target.localScale = to;
        }
    }

    private void PrepareWinDecorations()
    {
        SetGroupBottomPivot(bigHornRoots);
        SetGroupBottomPivot(smallHornRoots);
        SetBottomPivot(chocoRoot);
        SetGroupScale(bigHornRoots, Vector3.zero);
        SetGroupScale(smallHornRoots, Vector3.zero);
        if (chocoRoot != null)
        {
            chocoRoot.localScale = Vector3.zero;
        }

        StopBigHornVfx();
        StopSmallHornVfx();
    }

    private void RestoreWinDecorations()
    {
        SetGroupScale(bigHornRoots, Vector3.one);
        SetGroupScale(smallHornRoots, Vector3.one);
        if (chocoRoot != null)
        {
            chocoRoot.localScale = Vector3.one;
        }

        StopBigHornVfx();
        StopSmallHornVfx();
    }

    private void SetGroupScale(RectTransform[] roots, Vector3 scale)
    {
        if (roots == null)
        {
            return;
        }

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
            {
                roots[i].localScale = scale;
            }
        }
    }

    private void SetGroupBottomPivot(RectTransform[] roots)
    {
        if (roots == null)
        {
            return;
        }

        for (int i = 0; i < roots.Length; i++)
        {
            SetBottomPivot(roots[i]);
        }
    }

    private void SetBottomPivot(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        SetPivotKeepPosition(rect, new Vector2(0.5f, 0f));
    }

    private void SetPivotKeepPosition(RectTransform rect, Vector2 pivot)
    {
        var size = rect.rect.size;
        var deltaPivot = rect.pivot - pivot;
        var deltaPosition = new Vector3(deltaPivot.x * size.x * rect.localScale.x, deltaPivot.y * size.y * rect.localScale.y, 0f);
        rect.pivot = pivot;
        rect.localPosition -= deltaPosition;
    }

    private void PlayBigHornVfx()
    {
        if (bigHornVfx == null)
        {
            return;
        }

        for (int i = 0; i < bigHornVfx.Length; i++)
        {
            if (bigHornVfx[i] == null)
            {
                continue;
            }

            bigHornVfx[i].gameObject.SetActive(true);
            bigHornVfx[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bigHornVfx[i].Play(true);
        }
    }

    private void StopBigHornVfx()
    {
        if (bigHornVfx == null)
        {
            return;
        }

        for (int i = 0; i < bigHornVfx.Length; i++)
        {
            if (bigHornVfx[i] != null)
            {
                bigHornVfx[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void PlaySmallHornVfx()
    {
        PlayParticleGroup(smallHornVfx);
    }

    private void StopSmallHornVfx()
    {
        StopParticleGroup(smallHornVfx);
    }

    private void PlayParticleGroup(ParticleSystem[] particles)
    {
        if (particles == null)
        {
            return;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
            {
                continue;
            }

            particles[i].gameObject.SetActive(true);
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play(true);
        }
    }

    private void StopParticleGroup(ParticleSystem[] particles)
    {
        if (particles == null)
        {
            return;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void HandleNextClicked()
    {
        ClaimRewardAndPlayNext(GetWinRewardCoins(), nextCoinFlyFrom);
    }

    private void HandleBonusClicked()
    {
        ClaimRewardAndPlayNext(GetWinRewardCoins() * 2, bonusCoinFlyFrom);
    }

    private void ClaimRewardAndPlayNext(int coinReward, RectTransform fxFrom)
    {
        if (rewardClaimed)
        {
            return;
        }

        rewardClaimed = true;
        SetRewardButtonsInteractable(false);
        StartCoroutine(ClaimRewardRoutine(coinReward, fxFrom));
    }

    private IEnumerator ClaimRewardRoutine(int coinReward, RectTransform fxFrom)
    {
        var fx = PlayCoinFlyFx(fxFrom);
        if (fx != null)
        {
            yield return new WaitForSecondsRealtime(fx.TotalDuration);
        }

        CoinManager.Instance.AddCoins(coinReward);

        if (nextLevelDelayAfterCoinFly > 0f)
        {
            yield return new WaitForSecondsRealtime(nextLevelDelayAfterCoinFly);
        }

        GameManager.Instance.PlayNextLevel();
    }

    private int GetWinRewardCoins()
    {
        var starCount = Mathf.Clamp(GameManager.Instance.LastWinStarCount, 0, 3);
        return starCount switch
        {
            3 => 60,
            2 => 40,
            1 => 20,
            _ => 0
        };
    }

    private void RefreshRewardTexts()
    {
        ResolveRewardTexts();

        var rewardCoins = GetWinRewardCoins();
        if (nextRewardText != null)
        {
            nextRewardText.text = rewardCoins.ToString();
        }

        if (bonusRewardText != null)
        {
            bonusRewardText.text = (rewardCoins * 2).ToString();
        }
    }

    private CoinFlyFX PlayCoinFlyFx(RectTransform fxFrom)
    {
        var fxTo = coinFlyTo != null ? coinFlyTo : coinText != null ? coinText.rectTransform : null;
        fxFrom = fxFrom != null ? fxFrom : nextButton != null ? nextButton.transform as RectTransform : null;
        if (fxFrom == null || fxTo == null)
        {
            return null;
        }

        var fx = FindFirstObjectByType<CoinFlyFX>();
        if (fx == null)
        {
            var go = new GameObject("CoinFlyFX_Auto");
            fx = go.AddComponent<CoinFlyFX>();
        }

        if (fxRoot == null)
        {
            fxRoot = ResolveFxRoot();
        }

        if (fxRoot != null)
        {
            fx.SetRoot(fxRoot);
        }

        fx.SetPrefab(coinFlyPrefab);
        fx.Play(fxFrom, fxTo, coinFlyPrefab, coinFlyCount);
        return fx;
    }

    private RectTransform ResolveFxRoot()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return null;
        }

        var root = new GameObject("FXRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();
        return root;
    }

    private void SetRewardButtonsInteractable(bool interactable)
    {
        if (nextButton != null)
        {
            nextButton.interactable = interactable;
        }

        if (bonusButton != null)
        {
            bonusButton.interactable = interactable;
        }
    }

    private void BindCoinText()
    {
        CoinManager.Instance.CoinChanged -= RefreshCoinText;
        CoinManager.Instance.CoinChanged += RefreshCoinText;
        RefreshCoinText(CoinManager.Instance.CoinCount);
    }

    public void RefreshCoinText()
    {
        RefreshCoinText(CoinManager.Instance.CoinCount);
    }

    private void RefreshCoinText(int coinCount)
    {
        if (coinText != null)
        {
            coinText.text = coinCount.ToString();
        }
    }

    private void ResolveButtons()
    {
        if (nextButton != null && bonusButton != null)
        {
            return;
        }

        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.Equals("Btn60coin", System.StringComparison.OrdinalIgnoreCase))
            {
                nextButton = buttons[i];
            }
            else if (buttons[i].name.Equals("Btn120coin", System.StringComparison.OrdinalIgnoreCase))
            {
                bonusButton = buttons[i];
            }
        }

        if (nextButton == null && buttons.Length > 0)
        {
            nextButton = buttons[0];
        }

        if (bonusButton == null && buttons.Length > 1)
        {
            bonusButton = buttons[1];
        }
    }

    private void ResolveRewardTexts()
    {
        if (nextRewardText == null && nextButton != null)
        {
            nextRewardText = nextButton.GetComponentInChildren<Text>(true);
        }

        if (bonusRewardText == null && bonusButton != null)
        {
            bonusRewardText = bonusButton.GetComponentInChildren<Text>(true);
        }
    }

    private void ResolveRandomObjectSpawners()
    {
        if (randomObjectSpawners != null && randomObjectSpawners.Length > 0)
        {
            return;
        }

        randomObjectSpawners = GetComponentsInChildren<RandomObjectSpawner>(true);
    }

    private void ResolveStarRoots()
    {
        if (starRoots != null && starRoots.Length > 0)
        {
            return;
        }

        starRoots = new GameObject[3];
        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.Equals("Star1", System.StringComparison.OrdinalIgnoreCase))
            {
                starRoots[0] = transforms[i].gameObject;
            }
            else if (transforms[i].name.Equals("Star2", System.StringComparison.OrdinalIgnoreCase))
            {
                starRoots[1] = transforms[i].gameObject;
            }
            else if (transforms[i].name.Equals("Star3", System.StringComparison.OrdinalIgnoreCase))
            {
                starRoots[2] = transforms[i].gameObject;
            }
        }
    }

    private void RefreshWinStars()
    {
        if (starRoots == null)
        {
            return;
        }

        var starCount = Mathf.Clamp(GameManager.Instance.LastWinStarCount, 0, starRoots.Length);
        for (int i = 0; i < starRoots.Length; i++)
        {
            if (starRoots[i] != null)
            {
                starRoots[i].SetActive(i < starCount);
            }
        }
    }

    private void PlayRandomObjectSpawners()
    {
        if (randomObjectSpawners == null)
        {
            return;
        }

        for (int i = 0; i < randomObjectSpawners.Length; i++)
        {
            if (randomObjectSpawners[i] != null && randomObjectSpawners[i].gameObject.activeInHierarchy)
            {
                randomObjectSpawners[i].RandomShow();
            }
        }
    }
}
