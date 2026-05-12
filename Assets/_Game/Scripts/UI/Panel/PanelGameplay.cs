using UnityEngine;
using UnityEngine.UI;

public class PanelGameplay : UICanvas
{
    public new static PanelGameplay Instance { get; private set; }

    [SerializeField] private Button cutButton;
    [SerializeField] private Image cutButtonImage;
    [SerializeField] private Sprite cutEnabledSprite;
    [SerializeField] private Sprite cutDisabledSprite;

    private void OnEnable()
    {
        Instance = this;
        BindCutButton();
        SetCanCut(false);
    }

    private void OnDisable()
    {
        if (cutButton != null)
        {
            cutButton.onClick.RemoveListener(HandleCutClicked);
        }
    }

    public void SetCanCut(bool canCut)
    {
        ResolveCutButton();
        if (cutButton != null)
        {
            cutButton.interactable = canCut;
        }

        ResolveCutButtonImage();
        if (cutButtonImage != null)
        {
            var sprite = canCut ? cutEnabledSprite : cutDisabledSprite;
            if (sprite != null)
            {
                cutButtonImage.sprite = sprite;
            }
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

    private void ResolveCutButton()
    {
        if (cutButton != null)
        {
            return;
        }

        cutButton = GetComponentInChildren<Button>(true);
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

    private void HandleCutClicked()
    {
        GameplayLevelController.Instance?.CutSelectedPiece();
    }
}
