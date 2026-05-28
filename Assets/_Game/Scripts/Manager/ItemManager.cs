using System;
using UnityEngine;

public sealed class ItemManager : Singleton<ItemManager>
{
    private const string HintCountKey = "ChocoBento.HintItemUses";
    private const string KnifeCountKey = "ChocoBento.AddKnifeItemUses";

    [SerializeField, Min(0)] private int initialHintCount = 1;
    [SerializeField, Min(0)] private int initialKnifeCount = 2;

    public event Action<int> HintChanged;
    public event Action<int> KnifeChanged;

    public int HintCount
    {
        get
        {
            EnsureSaveExists();
            return Mathf.Max(0, PlayerPrefs.GetInt(HintCountKey, initialHintCount));
        }
    }

    public int KnifeCount
    {
        get
        {
            EnsureSaveExists();
            return Mathf.Max(0, PlayerPrefs.GetInt(KnifeCountKey, initialKnifeCount));
        }
    }

    private void Awake()
    {
        EnsureSaveExists();
        PublishState();
    }

    public void AddHint(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetHintCount(HintCount + amount);
    }

    public bool TryUseHint(int amount = 1)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (HintCount < amount)
        {
            PublishState();
            return false;
        }

        SetHintCount(HintCount - amount);
        return true;
    }

    public void AddKnife(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetKnifeCount(KnifeCount + amount);
    }

    public bool TryUseKnife(int amount = 1)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (KnifeCount < amount)
        {
            PublishState();
            return false;
        }

        SetKnifeCount(KnifeCount - amount);
        return true;
    }

    public void RefreshItemDisplay()
    {
        EnsureSaveExists();
        PublishState();
    }

    private void SetHintCount(int count)
    {
        PlayerPrefs.SetInt(HintCountKey, Mathf.Max(0, count));
        PlayerPrefs.Save();
        PublishState();
    }

    private void SetKnifeCount(int count)
    {
        PlayerPrefs.SetInt(KnifeCountKey, Mathf.Max(0, count));
        PlayerPrefs.Save();
        PublishState();
    }

    private void EnsureSaveExists()
    {
        if (!PlayerPrefs.HasKey(HintCountKey))
        {
            PlayerPrefs.SetInt(HintCountKey, Mathf.Max(0, initialHintCount));
        }

        if (!PlayerPrefs.HasKey(KnifeCountKey))
        {
            PlayerPrefs.SetInt(KnifeCountKey, Mathf.Max(0, initialKnifeCount));
        }

        PlayerPrefs.Save();
    }

    private void PublishState()
    {
        HintChanged?.Invoke(HintCount);
        KnifeChanged?.Invoke(KnifeCount);
    }
}
