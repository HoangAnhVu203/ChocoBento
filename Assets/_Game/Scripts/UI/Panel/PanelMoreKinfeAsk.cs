using UnityEngine;
using UnityEngine.UI;

public class PanelMoreKinfeAsk : UICanvas
{
    [SerializeField, Min(0)] private int addKnifeCoinCost = 700;
    [SerializeField] private Button coinButton;

    private void OnEnable()
    {
        RefreshCoinButton();
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CoinChanged -= HandleCoinChanged;
            CoinManager.Instance.CoinChanged += HandleCoinChanged;
        }
    }

    private void OnDisable()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CoinChanged -= HandleCoinChanged;
        }
    }

    public void AddKnifeByCoin()
    {
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(addKnifeCoinCost))
        {
            RefreshCoinButton();
            return;
        }

        AddKnifeAndClose();
    }

    public void AddKnifeFree()
    {
        AddKnifeAndClose();
    }

    public bool CanAddKnifeByCoin()
    {
        return CoinManager.Instance != null && CoinManager.Instance.CoinCount >= addKnifeCoinCost;
    }

    private void AddKnifeAndClose()
    {
        if (PanelGameplay.Instance != null)
        {
            PanelGameplay.Instance.AddBonusKnifeCut();
        }

        UIManager.Instance.CloseUIDirectly<PanelMoreKinfeAsk>();
    }

    private void RefreshCoinButton()
    {
        ResolveCoinButton();
        if (coinButton != null)
        {
            coinButton.interactable = CanAddKnifeByCoin();
        }
    }

    private void ResolveCoinButton()
    {
        if (coinButton != null)
        {
            return;
        }

        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsCoinButton(buttons[i]))
            {
                coinButton = buttons[i];
                return;
            }
        }
    }

    private void HandleCoinChanged(int coinCount)
    {
        RefreshCoinButton();
    }

    private bool IsCoinButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        if (button.name.IndexOf("Coin", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        var texts = button.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].text.Trim() == addKnifeCoinCost.ToString())
            {
                return true;
            }
        }

        return false;
    }
}
