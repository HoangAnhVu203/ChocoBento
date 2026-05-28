using System;
using UnityEngine;

public sealed class RewardManager : Singleton<RewardManager>
{
    private const string RemoveForceAdsKey = "ChocoBento.RemoveForceAds";
    private const string RemoveAllAdsKey = "ChocoBento.RemoveAllAds";

    public event Action RewardsClaimed;

    public bool RemoveForceAds => PlayerPrefs.GetInt(RemoveForceAdsKey, 0) == 1 || RemoveAllAds;
    public bool RemoveAllAds => PlayerPrefs.GetInt(RemoveAllAdsKey, 0) == 1;

    public bool ClaimPackage(ShopPackageConfig packageConfig)
    {
        if (packageConfig == null || packageConfig.Rewards == null || packageConfig.Rewards.Length == 0)
        {
            return false;
        }

        ClaimRewards(packageConfig.Rewards);
        LogPackageClaimed(packageConfig);
        return true;
    }

    public void ClaimRewards(RewardData[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return;
        }

        for (int i = 0; i < rewards.Length; i++)
        {
            ClaimReward(rewards[i]);
        }

        PlayerPrefs.Save();
        RewardsClaimed?.Invoke();
    }

    public void ClaimReward(RewardData reward)
    {
        if (reward == null || reward.Amount <= 0)
        {
            return;
        }

        switch (reward.Type)
        {
            case RewardType.Coin:
                CoinManager.Instance.AddCoins(reward.Amount);
                break;
            case RewardType.Hint:
                ItemManager.Instance.AddHint(reward.Amount);
                break;
            case RewardType.Knife:
                ItemManager.Instance.AddKnife(reward.Amount);
                break;
            case RewardType.BonusHeart:
                HeartManager.Instance.AddBonusHeart(reward.Amount);
                break;
            case RewardType.RemoveForceAds:
                PlayerPrefs.SetInt(RemoveForceAdsKey, 1);
                break;
            case RewardType.RemoveAllAds:
                PlayerPrefs.SetInt(RemoveAllAdsKey, 1);
                PlayerPrefs.SetInt(RemoveForceAdsKey, 1);
                break;
        }
    }

    private static void LogPackageClaimed(ShopPackageConfig packageConfig)
    {
        string rewards = FormatRewards(packageConfig.Rewards);
        Debug.Log(
            $"[RewardManager] Claimed package: id={packageConfig.PackageId}, name={packageConfig.PackageName}, price={packageConfig.PriceText}, rewards=[{rewards}]. " +
            $"Totals: coin={CoinManager.Instance.CoinCount}, heart={HeartManager.Instance.HeartCount} (normal={HeartManager.Instance.NormalHeartCount}, bonus={HeartManager.Instance.BonusHeartCount}), " +
            $"hint={ItemManager.Instance.HintCount}, knife={ItemManager.Instance.KnifeCount}, removeForceAds={RewardManager.Instance.RemoveForceAds}, removeAllAds={RewardManager.Instance.RemoveAllAds}");
    }

    private static string FormatRewards(RewardData[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(rewards[i].Type);
            builder.Append(" x");
            builder.Append(rewards[i].Amount);
        }

        return builder.ToString();
    }
}
