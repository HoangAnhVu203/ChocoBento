using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelHome : UICanvas
{
    [SerializeField] private Button playButton;
    [SerializeField] private Text coinText;

    [Header("Level Scroll")]
    [SerializeField] private ScrollRect levelScrollView;
    [SerializeField] private Transform levelItemContent;
    [SerializeField] private HomeLevelItemUI levelItemPrefab;

    private readonly List<HomeLevelItemUI> spawnedLevelItems = new();
    private Coroutine scrollToCurrentLevelCoroutine;

    private void OnEnable()
    {
        ResolveButton();
        ResolveLevelListReferences();

        SetupHorizontalScroll();
        PopulateLevelItems();
        BindCoinText();

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
            playButton.onClick.AddListener(HandlePlayClicked);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CoinChanged -= RefreshCoinText;
        }

        StopScrollToCurrentLevelCoroutine();
        ClearSpawnedLevelItems();
    }

    private void HandlePlayClicked()
    {
        GameManager.Instance.PlayCurrentLevel();
    }

    private void HandleLevelItemClicked(int levelIndex)
    {
        LevelManager.Instance.PlayLevel(levelIndex);
    }

    private void ResolveButton()
    {
        if (playButton != null)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                playButton = buttons[i];
                return;
            }
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (levelItemContent == null || !buttons[i].transform.IsChildOf(levelItemContent))
            {
                playButton = buttons[i];
                return;
            }
        }
    }

    private void BindCoinText()
    {
        if (CoinManager.Instance == null)
        {
            return;
        }

        CoinManager.Instance.CoinChanged -= RefreshCoinText;
        CoinManager.Instance.CoinChanged += RefreshCoinText;

        RefreshCoinText(CoinManager.Instance.CoinCount);
    }

    public void RefreshCoinText()
    {
        if (CoinManager.Instance != null)
        {
            RefreshCoinText(CoinManager.Instance.CoinCount);
        }
    }

    private void RefreshCoinText(int coinCount)
    {
        if (coinText != null)
        {
            coinText.text = coinCount.ToString();
        }
    }

    public void OpenSetting()
    {
        var panelSetting = UIManager.Instance.OpenUI<PanelSetting>();
        panelSetting.SetOpenedFromHome(true);
    }

    public void RefreshLevelItems()
    {
        PopulateLevelItems();
    }

    private void PopulateLevelItems()
    {
        ResolveLevelListReferences();
        SetupHorizontalScroll();
        ClearSpawnedLevelItems();

        if (levelItemContent == null || levelItemPrefab == null || LevelManager.Instance == null)
        {
            return;
        }

        levelItemPrefab.gameObject.SetActive(false);

        int levelCount = LevelManager.Instance.LevelCount;
        int currentLevelIndex = LevelManager.Instance.CurrentLevelIndex;

        // Level đi từ trái sang phải: 0, 1, 2, 3...
        for (int i = 0; i < levelCount; i++)
        {
            HomeLevelItemUI item = Instantiate(levelItemPrefab, levelItemContent);
            item.gameObject.SetActive(true);

            item.Setup(
                i,
                currentLevelIndex,
                LevelManager.Instance.GetBestStars(i),
                HandleLevelItemClicked
            );

            spawnedLevelItems.Add(item);
        }

        StartScrollToCurrentLevel(currentLevelIndex);
    }

    private void SetupHorizontalScroll()
    {
        if (levelScrollView == null)
        {
            return;
        }

        levelScrollView.horizontal = true;
        levelScrollView.vertical = false;
        levelScrollView.velocity = Vector2.zero;
        levelScrollView.verticalNormalizedPosition = 1f;
    }

    private void StartScrollToCurrentLevel(int currentLevelIndex)
    {
        StopScrollToCurrentLevelCoroutine();
        scrollToCurrentLevelCoroutine = StartCoroutine(ScrollToCurrentLevelRoutine(currentLevelIndex));
    }

    private IEnumerator ScrollToCurrentLevelRoutine(int currentLevelIndex)
    {
        yield return null;
        yield return null;

        ScrollToCurrentLevel(currentLevelIndex);
        scrollToCurrentLevelCoroutine = null;
    }

    private void ScrollToCurrentLevel(int currentLevelIndex)
    {
        if (levelScrollView == null ||
            levelScrollView.content == null ||
            spawnedLevelItems.Count == 0)
        {
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= spawnedLevelItems.Count)
        {
            return;
        }

        RectTransform content = levelScrollView.content;
        RectTransform viewport = levelScrollView.viewport;

        if (viewport == null)
        {
            viewport = levelScrollView.transform as RectTransform;
        }

        if (viewport == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform target = spawnedLevelItems[currentLevelIndex].transform as RectTransform;

        if (target == null)
        {
            return;
        }

        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);
        Vector3 viewportLocalCenter = viewport.InverseTransformPoint(targetWorldCenter);

        float viewportCenterX = viewport.rect.width * (0.5f - viewport.pivot.x);
        float offsetX = viewportLocalCenter.x - viewportCenterX;

        Vector2 anchoredPosition = content.anchoredPosition;
        anchoredPosition.x -= offsetX;

        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        float minX = Mathf.Min(0f, viewportWidth - contentWidth);
        float maxX = 0f;

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);

        // Không tác động trục Y
        anchoredPosition.y = content.anchoredPosition.y;

        content.anchoredPosition = anchoredPosition;

        levelScrollView.velocity = Vector2.zero;
        levelScrollView.verticalNormalizedPosition = 1f;

        Canvas.ForceUpdateCanvases();
    }

    private void StopScrollToCurrentLevelCoroutine()
    {
        if (scrollToCurrentLevelCoroutine == null)
        {
            return;
        }

        StopCoroutine(scrollToCurrentLevelCoroutine);
        scrollToCurrentLevelCoroutine = null;
    }

    private void ClearSpawnedLevelItems()
    {
        for (int i = 0; i < spawnedLevelItems.Count; i++)
        {
            if (spawnedLevelItems[i] != null)
            {
                spawnedLevelItems[i].gameObject.SetActive(false);
                Destroy(spawnedLevelItems[i].gameObject);
            }
        }

        spawnedLevelItems.Clear();
    }

    private void ResolveLevelListReferences()
    {
        if (levelScrollView == null)
        {
            levelScrollView = GetComponentInChildren<ScrollRect>(true);
        }

        if (levelItemContent == null && levelScrollView != null)
        {
            levelItemContent = levelScrollView.content;
        }

        if (levelItemContent == null)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.Equals("Content", System.StringComparison.OrdinalIgnoreCase))
                {
                    levelItemContent = transforms[i];
                    break;
                }
            }
        }

        if (levelItemPrefab == null && levelItemContent != null)
        {
            levelItemPrefab = levelItemContent.GetComponentInChildren<HomeLevelItemUI>(true);
        }
    }
}
