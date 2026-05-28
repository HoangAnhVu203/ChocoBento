using UnityEngine;

public class PanelShop : UICanvas
{
    [SerializeField] private bool grantRewardsDirectly = true;

    public bool BuyPackage(ShopPackageConfig packageConfig)
    {
        if (packageConfig == null)
        {
            ShowBuyResult(false);
            return false;
        }

        if (grantRewardsDirectly)
        {
            bool success = RewardManager.Instance.ClaimPackage(packageConfig);
            ShowBuyResult(success);
            return success;
        }

        Debug.Log($"[PanelShop] Purchase requested: {packageConfig.ProductId} ({packageConfig.PackageId})");
        ShowBuyResult(false);
        return false;
    }

    private static void ShowBuyResult(bool success)
    {
        var panelResult = UIManager.Instance.OpenUI<PanelResult>();
        panelResult.ShowBuyResult(success);
    }
}
