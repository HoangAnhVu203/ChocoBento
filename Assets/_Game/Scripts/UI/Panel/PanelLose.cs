using UnityEngine;
using UnityEngine.UI;

public class PanelLose : UICanvas
{
    [SerializeField] private Button replayButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Text coinText;

    private void OnEnable()
    {
        ResolveButtons();
        BindCoinText();
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

        CoinManager.Instance.CoinChanged -= RefreshCoinText;
    }

    private void HandleReplayClicked()
    {
        var loseHeartPanel = UIManager.Instance.OpenUI<PanelLoseHeart>();
        loseHeartPanel.SetAction(PanelLoseHeart.LoseHeartAction.Replay);
    }

    private void HandleHomeClicked()
    {
        LevelManager.Instance.GoHome();
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

    public void GohomeBtn()
    {
        LevelManager.Instance.GoHome();
    }
}
