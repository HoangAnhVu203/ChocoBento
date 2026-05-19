using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelHome : UICanvas
{
    [SerializeField] private Button playButton;
    [SerializeField] private Text coinText;
    [SerializeField] private ScrollRect levelScrollView;
    [SerializeField] private Transform levelItemContent;
    [SerializeField] private HomeLevelItemUI levelItemPrefab;
    [SerializeField, Range(0f, 1f)] private float currentLevelViewportY = 0.32f;

    private readonly List<HomeLevelItemUI> spawnedLevelItems = new();
    private Coroutine alignCurrentLevelCoroutine;

    private void OnEnable()
    {
        ResolveButton();
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

        CoinManager.Instance.CoinChanged -= RefreshCoinText;
        StopAlignCurrentLevelCoroutine();
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

        var buttons = GetComponentsInChildren<Button>(true);
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

    //Call Panel
    public void OpenSetting() => UIManager.Instance.OpenUI<PanelSetting>();

    public void RefreshLevelItems()
    {
        PopulateLevelItems();
    }

    private void PopulateLevelItems()
    {
        ResolveLevelListReferences();
        ClearSpawnedLevelItems();

        if (levelItemContent == null || levelItemPrefab == null || LevelManager.Instance == null)
        {
            return;
        }

        levelItemPrefab.gameObject.SetActive(false);

        var levelCount = LevelManager.Instance.LevelCount;
        var currentLevelIndex = LevelManager.Instance.CurrentLevelIndex;
        RectTransform currentLevelItem = null;
        for (int i = levelCount - 1; i >= 0; i--)
        {
            var item = Instantiate(levelItemPrefab, levelItemContent);
            item.gameObject.SetActive(true);
            item.Setup(i, currentLevelIndex, LevelManager.Instance.GetBestStars(i), HandleLevelItemClicked);
            spawnedLevelItems.Add(item);

            if (i == currentLevelIndex)
            {
                currentLevelItem = item.transform as RectTransform;
            }
        }

        AlignCurrentLevelToViewport(currentLevelItem);
    }

    private void AlignCurrentLevelToViewport(RectTransform currentLevelItem)
    {
        StopAlignCurrentLevelCoroutine();
        if (currentLevelItem == null || levelScrollView == null)
        {
            return;
        }

        alignCurrentLevelCoroutine = StartCoroutine(AlignCurrentLevelRoutine(currentLevelItem));
    }

    private IEnumerator AlignCurrentLevelRoutine(RectTransform currentLevelItem)
    {
        yield return null;
        yield return null;

        if (currentLevelItem == null || levelScrollView == null || levelScrollView.content == null)
        {
            yield break;
        }

        var viewport = levelScrollView.viewport != null ? levelScrollView.viewport : levelScrollView.transform as RectTransform;
        var content = levelScrollView.content;
        if (viewport == null || content == null)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        var viewportRect = viewport.rect;
        var viewportBottom = -viewportRect.height * viewport.pivot.y;
        var desiredY = viewportBottom + viewportRect.height * currentLevelViewportY;
        var itemCenterY = viewport.InverseTransformPoint(currentLevelItem.TransformPoint(currentLevelItem.rect.center)).y;
        var anchoredPosition = content.anchoredPosition;
        anchoredPosition.y += desiredY - itemCenterY;
        content.anchoredPosition = anchoredPosition;
        levelScrollView.velocity = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        alignCurrentLevelCoroutine = null;
    }

    private void StopAlignCurrentLevelCoroutine()
    {
        if (alignCurrentLevelCoroutine == null)
        {
            return;
        }

        StopCoroutine(alignCurrentLevelCoroutine);
        alignCurrentLevelCoroutine = null;
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
            var transforms = GetComponentsInChildren<Transform>(true);
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
