using UnityEngine;
using UnityEngine.UI;

public class PanelHome : UICanvas
{
    [SerializeField] private Button playButton;

    private void OnEnable()
    {
        ResolveButton();
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
    }

    private void HandlePlayClicked()
    {
        GameManager.Instance.PlayCurrentLevel();
    }

    private void ResolveButton()
    {
        if (playButton != null)
        {
            return;
        }

        playButton = GetComponentInChildren<Button>(true);
    }
}
