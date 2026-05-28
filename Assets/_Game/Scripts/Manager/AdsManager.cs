using System;
using UnityEngine;

public enum AdsPlacement
{
    Unknown,
    LevelStart,
    LevelComplete,
    LevelFail,
    Replay,
    Home,
    Shop,
    Rewarded
}

public sealed class AdsManager : Singleton<AdsManager>
{
    public event Action<AdsPlacement> ForceAdBlocked;
    public event Action<AdsPlacement> BannerBlocked;
    public event Action<AdsPlacement> RewardedAdCompleted;
    public event Action<AdsPlacement> RewardedAdFailed;

    public bool CanShowForceAds => RewardManager.Instance == null || !RewardManager.Instance.RemoveForceAds;
    public bool CanShowBannerAds => RewardManager.Instance == null || !RewardManager.Instance.RemoveAllAds;
    public bool CanShowRewardedAds => true;

    private void OnEnable()
    {
        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.RewardsClaimed -= HandleRewardsClaimed;
            RewardManager.Instance.RewardsClaimed += HandleRewardsClaimed;
        }
    }

    private void OnDisable()
    {
        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.RewardsClaimed -= HandleRewardsClaimed;
        }
    }

    public bool ShowForceAd(AdsPlacement placement = AdsPlacement.Unknown)
    {
        if (!CanShowForceAds)
        {
            ForceAdBlocked?.Invoke(placement);
            Debug.Log($"[AdsManager] Force ad blocked by remove ads. placement={placement}");
            return false;
        }

        ShowForceAdInternal(placement);
        return true;
    }

    public bool ShowInterstitial(AdsPlacement placement = AdsPlacement.Unknown)
    {
        return ShowForceAd(placement);
    }

    public bool ShowBanner(AdsPlacement placement = AdsPlacement.Unknown)
    {
        if (!CanShowBannerAds)
        {
            HideBanner();
            BannerBlocked?.Invoke(placement);
            Debug.Log($"[AdsManager] Banner ad blocked by remove all ads. placement={placement}");
            return false;
        }

        ShowBannerInternal(placement);
        return true;
    }

    public void HideBanner()
    {
        HideBannerInternal();
    }

    public bool ShowRewardedAd(Action<bool> onCompleted = null, AdsPlacement placement = AdsPlacement.Rewarded)
    {
        if (!CanShowRewardedAds)
        {
            RewardedAdFailed?.Invoke(placement);
            onCompleted?.Invoke(false);
            return false;
        }

        ShowRewardedAdInternal(placement, success =>
        {
            if (success)
            {
                RewardedAdCompleted?.Invoke(placement);
            }
            else
            {
                RewardedAdFailed?.Invoke(placement);
            }

            onCompleted?.Invoke(success);
        });

        return true;
    }

    private void HandleRewardsClaimed()
    {
        if (RewardManager.Instance != null && RewardManager.Instance.RemoveAllAds)
        {
            HideBanner();
        }
    }

    private void ShowForceAdInternal(AdsPlacement placement)
    {
        Debug.Log($"[AdsManager] Show force ad placeholder. placement={placement}");
        // TODO: Call the real interstitial/force ad SDK here.
    }

    private void ShowBannerInternal(AdsPlacement placement)
    {
        Debug.Log($"[AdsManager] Show banner placeholder. placement={placement}");
        // TODO: Call the real banner ad SDK here.
    }

    private void HideBannerInternal()
    {
        Debug.Log("[AdsManager] Hide banner placeholder.");
        // TODO: Hide the real banner ad SDK here.
    }

    private void ShowRewardedAdInternal(AdsPlacement placement, Action<bool> onCompleted)
    {
        Debug.Log($"[AdsManager] Show rewarded ad placeholder. placement={placement}");
        // TODO: Call the real rewarded ad SDK here and invoke callback with the SDK result.
        onCompleted?.Invoke(true);
    }
}
