using UnityEngine;
using UnityEngine.UI;

public sealed class CoinTextUI : MonoBehaviour
{
    [SerializeField] private Text coinText;
    [SerializeField] private string prefix;
    [SerializeField] private string suffix;

    private CoinManager coinManager;

    private void OnEnable()
    {
        ResolveText();
        coinManager = CoinManager.Instance;
        coinManager.CoinChanged += Refresh;
        Refresh(coinManager.CoinCount);
    }

    private void OnDisable()
    {
        if (coinManager != null)
        {
            coinManager.CoinChanged -= Refresh;
            coinManager = null;
        }
    }

    public void Refresh()
    {
        Refresh((coinManager != null ? coinManager : CoinManager.Instance).CoinCount);
    }

    private void Refresh(int coinCount)
    {
        ResolveText();
        if (coinText != null)
        {
            coinText.text = $"{prefix}{coinCount}{suffix}";
        }
    }

    private void ResolveText()
    {
        if (coinText != null)
        {
            return;
        }

        coinText = GetComponent<Text>();
    }
}
