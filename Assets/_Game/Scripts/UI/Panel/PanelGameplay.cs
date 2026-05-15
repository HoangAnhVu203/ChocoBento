using UnityEngine;
using UnityEngine.UI;

public class PanelGameplay : UICanvas
{
    public new static PanelGameplay Instance { get; private set; }

    [Header("Cut Rule Temp")]
    [SerializeField, Range(0, 2)] private int startKnifeCount = 2;
    [SerializeField, Range(0, 3)] private int startStarCount = 3;

    [SerializeField] private Button cutButton;
    [SerializeField] private Image cutButtonImage;
    [SerializeField] private Button addKnifeButton;
    [SerializeField] private Text levelText;
    [SerializeField] private Sprite cutEnabledSprite;
    [SerializeField] private Sprite cutDisabledSprite;
    [SerializeField] private Transform knifeRoot;
    [SerializeField] private Image[] knifeImages;
    [SerializeField] private Sprite knifeAvailableSprite;
    [SerializeField] private Sprite knifeUsedSprite;
    [SerializeField] private Transform starRoot;
    [SerializeField] private Image[] starImages;
    [SerializeField] private Sprite starAvailableSprite;
    [SerializeField] private Sprite starUsedSprite;

    private int remainingKnifeCount;
    private int remainingStarCount;
    private bool currentCanCut;

    public int RemainingStarCount => remainingStarCount;

    private void OnEnable()
    {
        Instance = this;
        BindCutButton();
        BindAddKnifeButton();
        ResolveHudImages();
        ResetTemporaryRule();
        RefreshLevelText();
        SetCanCut(false);
    }

    private void OnDisable()
    {
        if (cutButton != null)
        {
            cutButton.onClick.RemoveListener(HandleCutClicked);
        }

        if (addKnifeButton != null)
        {
            addKnifeButton.onClick.RemoveListener(HandleAddKnifeClicked);
        }
    }

    public void SetCanCut(bool canCut)
    {
        currentCanCut = canCut;
        ResolveCutButton();
        if (cutButton != null)
        {
            cutButton.interactable = canCut && HasCutResource();
        }

        ResolveCutButtonImage();
        if (cutButtonImage != null)
        {
            var sprite = canCut && HasCutResource() ? cutEnabledSprite : cutDisabledSprite;
            if (sprite != null)
            {
                cutButtonImage.sprite = sprite;
            }
        }
    }

    public void ResetTemporaryRule()
    {
        ResolveHudImages();
        remainingKnifeCount = Mathf.Clamp(startKnifeCount, 0, knifeImages.Length);
        remainingStarCount = Mathf.Clamp(startStarCount, 0, starImages.Length);
        RefreshKnifeImages();
        RefreshStarImages();
    }

    public void SetTemporaryRule(int knifeCount, int starCount)
    {
        ResolveHudImages();
        startKnifeCount = Mathf.Clamp(knifeCount, 0, knifeImages.Length);
        startStarCount = Mathf.Clamp(starCount, 0, starImages.Length);
        ResetTemporaryRule();
        RefreshLevelText();
        SetCanCut(currentCanCut);
    }

    public void SetLevelText(int levelNumber)
    {
        ResolveLevelText();
        if (levelText != null)
        {
            levelText.text = $"Level {Mathf.Max(1, levelNumber)}";
        }
    }

    private void BindCutButton()
    {
        ResolveCutButton();
        if (cutButton == null)
        {
            return;
        }

        cutButton.onClick.RemoveListener(HandleCutClicked);
        cutButton.onClick.AddListener(HandleCutClicked);
    }

    private void BindAddKnifeButton()
    {
        ResolveAddKnifeButton();
        if (addKnifeButton == null)
        {
            return;
        }

        addKnifeButton.onClick.RemoveListener(HandleAddKnifeClicked);
        addKnifeButton.onClick.AddListener(HandleAddKnifeClicked);
    }

    private void ResolveCutButton()
    {
        if (cutButton != null)
        {
            return;
        }

        cutButton = GetComponentInChildren<Button>(true);
    }

    private void ResolveAddKnifeButton()
    {
        if (addKnifeButton != null)
        {
            return;
        }

        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.Equals("AddKnifeBtn", System.StringComparison.OrdinalIgnoreCase))
            {
                addKnifeButton = buttons[i];
                return;
            }
        }
    }

    private void ResolveCutButtonImage()
    {
        if (cutButtonImage != null)
        {
            return;
        }

        ResolveCutButton();
        if (cutButton != null)
        {
            cutButtonImage = cutButton.GetComponent<Image>();
        }
    }

    private void ResolveLevelText()
    {
        if (levelText != null)
        {
            return;
        }

        var texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name.Equals("LevelTxt", System.StringComparison.OrdinalIgnoreCase))
            {
                levelText = texts[i];
                return;
            }
        }
    }

    private void RefreshLevelText()
    {
        var levelNumber = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelIndex + 1 : 1;
        SetLevelText(levelNumber);
    }

    private void HandleCutClicked()
    {
        if (!currentCanCut || !HasCutResource())
        {
            SetCanCut(currentCanCut);
            return;
        }

        var appliedCutCount = GameplayLevelController.Instance != null
            ? GameplayLevelController.Instance.CutSelectedPiece()
            : 0;

        if (appliedCutCount <= 0)
        {
            SetCanCut(false);
            return;
        }

        ConsumeTemporaryCutResource();
        SetCanCut(currentCanCut);
    }

    private void HandleAddKnifeClicked()
    {
        if (remainingKnifeCount >= startKnifeCount)
        {
            return;
        }

        remainingKnifeCount++;
        RefreshKnifeImages();
        SetCanCut(currentCanCut);
    }

    private void ConsumeTemporaryCutResource()
    {
        if (remainingKnifeCount > 0)
        {
            remainingKnifeCount--;
            RefreshKnifeImages();
            return;
        }

        if (remainingStarCount > 0)
        {
            remainingStarCount--;
            RefreshStarImages();
            if (remainingStarCount <= 0)
            {
                GameManager.Instance.LoseLevel();
            }
        }
    }

    private bool HasCutResource()
    {
        return remainingKnifeCount > 0 || remainingStarCount > 0;
    }

    private void ResolveHudImages()
    {
        ResolveImageRoot(ref knifeRoot, "KnifeRoot");
        ResolveImageRoot(ref starRoot, "StarRoot");

        if (knifeImages == null || knifeImages.Length == 0)
        {
            knifeImages = GetDirectChildImages(knifeRoot);
        }

        if (starImages == null || starImages.Length == 0)
        {
            starImages = GetDirectChildImages(starRoot);
        }

        if (knifeAvailableSprite == null && knifeImages.Length > 0)
        {
            knifeAvailableSprite = knifeImages[0].sprite;
        }

        if (starAvailableSprite == null && starImages.Length > 0)
        {
            starAvailableSprite = starImages[0].sprite;
        }
    }

    private void RefreshKnifeImages()
    {
        ResolveHudImages();
        for (int i = 0; i < knifeImages.Length; i++)
        {
            if (knifeImages[i] == null)
            {
                continue;
            }

            knifeImages[i].gameObject.SetActive(i < startKnifeCount);
            var isAvailable = i < remainingKnifeCount;
            ApplyResourceImageState(knifeImages[i], isAvailable, knifeAvailableSprite, knifeUsedSprite);
        }
    }

    private void RefreshStarImages()
    {
        ResolveHudImages();
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
            {
                continue;
            }

            var isAvailable = i < remainingStarCount;
            ApplyResourceImageState(starImages[i], isAvailable, starAvailableSprite, starUsedSprite);
        }
    }

    private static void ApplyResourceImageState(Image image, bool isAvailable, Sprite availableSprite, Sprite usedSprite)
    {
        var sprite = isAvailable ? availableSprite : usedSprite;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            return;
        }

        image.color = isAvailable ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }

    private void ResolveImageRoot(ref Transform root, string rootName)
    {
        if (root != null)
        {
            return;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.Equals(rootName, System.StringComparison.OrdinalIgnoreCase))
            {
                root = transforms[i];
                return;
            }
        }
    }

    private static Image[] GetDirectChildImages(Transform root)
    {
        if (root == null)
        {
            return System.Array.Empty<Image>();
        }

        var images = new System.Collections.Generic.List<Image>();
        foreach (Transform child in root)
        {
            if (child.TryGetComponent(out Image image))
            {
                images.Add(image);
            }
        }

        return images.ToArray();
    }
}
