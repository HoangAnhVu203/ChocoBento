using System;
using UnityEngine;
using UnityEngine.UI;

public class HomeLevelItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image bubble;
    [SerializeField] private Text levelText;
    [SerializeField] private Image[] starImages;

    [Header("Sprites (optional)")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite currentSprite;
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;

    [Header("Colors (fallback)")]
    [SerializeField] private Color normalColor = new Color(0.7f, 0.85f, 1f, 1f);
    [SerializeField] private Color currentColor = new Color(0.75f, 0.55f, 1f, 1f);
    [SerializeField] private Color completedColor = new Color(0.55f, 0.9f, 0.65f, 1f);
    [SerializeField] private Color filledStarColor = Color.white;
    [SerializeField] private Color emptyStarColor = new Color(1f, 1f, 1f, 0.35f);

    private int levelIndex;
    private Action<int> clicked;

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Setup(int levelIndex, int currentLevelIndex, int starCount, Action<int> onClicked)
    {
        this.levelIndex = levelIndex;
        clicked = onClicked;
        ResolveReferences();

        if (levelText != null)
        {
            levelText.text = (levelIndex + 1).ToString();
        }

        var isCurrent = levelIndex == currentLevelIndex;
        var isCompleted = starCount > 0;

        if (bubble != null)
        {
            if (isCurrent && currentSprite != null)
            {
                bubble.sprite = currentSprite;
            }
            else if (isCompleted && completedSprite != null)
            {
                bubble.sprite = completedSprite;
            }
            else if (normalSprite != null)
            {
                bubble.sprite = normalSprite;
            }
            else
            {
                bubble.color = isCurrent ? currentColor : isCompleted ? completedColor : normalColor;
            }
        }

        RefreshStars(starCount);

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
        }
    }

    private void RefreshStars(int starCount)
    {
        ResolveStars();
        starCount = Mathf.Clamp(starCount, 0, starImages.Length);

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
            {
                continue;
            }

            var isFilled = i < starCount;
            if (filledStarSprite != null && emptyStarSprite != null)
            {
                starImages[i].sprite = isFilled ? filledStarSprite : emptyStarSprite;
            }
            else
            {
                starImages[i].color = isFilled ? filledStarColor : emptyStarColor;
            }
        }
    }

    private void HandleClicked()
    {
        clicked?.Invoke(levelIndex);
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (bubble == null)
        {
            bubble = GetComponent<Image>();
        }

        ResolveStars();
    }

    private void ResolveStars()
    {
        if (starImages != null && starImages.Length > 0)
        {
            return;
        }

        var images = GetComponentsInChildren<Image>(true);
        var stars = new System.Collections.Generic.List<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i] == bubble)
            {
                continue;
            }

            if (images[i].name.IndexOf("Star", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stars.Add(images[i]);
            }
        }

        starImages = stars.ToArray();
    }
}
