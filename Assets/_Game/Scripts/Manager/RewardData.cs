using System;
using UnityEngine;

public enum RewardType
{
    Coin,
    Hint,
    Knife,
    BonusHeart,
    RemoveForceAds,
    RemoveAllAds
}

[Serializable]
public sealed class RewardData
{
    [SerializeField] private RewardType type;
    [SerializeField, Min(0)] private int amount;

    public RewardType Type => type;
    public int Amount => amount;

    public RewardData(RewardType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
