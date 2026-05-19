using UnityEngine;
using System;

public sealed class CoinManager : Singleton<CoinManager>
{
    private const string CoinKey = "ChocoBento.CoinCount";

    public int CoinCount => PlayerPrefs.GetInt(CoinKey, 0);
    public event Action<int> CoinChanged;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCoins(Mathf.Max(0, CoinCount) + amount);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (CoinCount < amount)
        {
            return false;
        }

        SetCoins(CoinCount - amount);
        return true;
    }

    public void RefreshCoinDisplay()
    {
        CoinChanged?.Invoke(CoinCount);
    }

    private void SetCoins(int coinCount)
    {
        coinCount = Mathf.Max(0, coinCount);
        PlayerPrefs.SetInt(CoinKey, coinCount);
        PlayerPrefs.Save();
        CoinChanged?.Invoke(coinCount);
    }
}
