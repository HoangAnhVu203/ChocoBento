using UnityEngine;
using UnityEngine.UI;

public class PanelLose : UICanvas
{
    [SerializeField] private Button replayButton;
    [SerializeField] private Button homeButton;

    private void OnEnable()
    {
        ResolveButtons();
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(HandleReplayClicked);
            replayButton.onClick.AddListener(HandleReplayClicked);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(HandleHomeClicked);
            homeButton.onClick.AddListener(HandleHomeClicked);
        }
    }

    private void OnDisable()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(HandleReplayClicked);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(HandleHomeClicked);
        }
    }

    private void HandleReplayClicked()
    {
        GameManager.Instance.ReplayLevel();
    }

    private void HandleHomeClicked()
    {
        GameManager.Instance.GoHome();
    }

    private void ResolveButtons()
    {
        if (replayButton != null && homeButton != null)
        {
            return;
        }

        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.IndexOf("Home", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                homeButton = buttons[i];
            }
            else if (buttons[i].name.IndexOf("Try", System.StringComparison.OrdinalIgnoreCase) >= 0
                     || buttons[i].name.IndexOf("Replay", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                replayButton = buttons[i];
            }
        }

        if (replayButton == null && buttons.Length > 0)
        {
            replayButton = buttons[0];
        }

        if (homeButton == null && buttons.Length > 1)
        {
            homeButton = buttons[1];
        }
    }
}
