using UnityEngine;
using UnityEngine.UI;

public sealed class ShopPackageItemUI : MonoBehaviour
{
    [SerializeField] private ShopPackageConfig packageConfig;
    [SerializeField] private Button buyButton;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text priceText;

    private PanelShop panelShop;
    private bool isPurchasing;

    private void OnEnable()
    {
        BindButton();
        SubscribeIap();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindButton();
        UnsubscribeIap();
    }

    public bool UsesButton(Button button)
    {
        return buyButton == button;
    }

    public void UnbindButton()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }

    public void Configure(PanelShop owner, ShopPackageConfig config)
    {
        panelShop = owner;
        packageConfig = config;
        ResolveReferences();
        BindButton();
        Refresh();
    }

    private void BindButton()
    {
        ResolveReferences();
        if (buyButton == null)
        {
            return;
        }

        buyButton.onClick.RemoveListener(HandleBuyClicked);
        buyButton.onClick.AddListener(HandleBuyClicked);
    }

    private void Refresh()
    {
        if (packageConfig == null)
        {
            return;
        }

        if (nameText != null && !string.IsNullOrEmpty(packageConfig.PackageName))
        {
            nameText.text = packageConfig.PackageName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = packageConfig.Description;
        }

        if (priceText != null && !string.IsNullOrEmpty(packageConfig.PriceText))
        {
            priceText.text = GetDisplayPrice();
        }

    }

    private void HandleBuyClicked()
    {
        if (packageConfig == null)
        {
            return;
        }

        if (isPurchasing)
        {
            return;
        }

        isPurchasing = true;
        SetButtonState(false, "Loading...");

        if (RemoveAdsManager.Instance != null)
        {
            RemoveAdsManager.Instance.BuyPackage(packageConfig);
            return;
        }

        isPurchasing = false;
        SetButtonState(true, GetDisplayPrice());
        ShowBuyResult(false);
    }

    private static void ShowBuyResult(bool success)
    {
        var panelResult = UIManager.Instance.OpenUI<PanelResult>();
        panelResult.ShowBuyResult(success);
    }

    private void ResolveReferences()
    {
        if (buyButton == null)
        {
            buyButton = GetComponent<Button>();
        }

        var texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (priceText == null && IsPriceText(texts[i].text))
            {
                priceText = texts[i];
                continue;
            }

            if (nameText == null && texts[i].name.IndexOf("Name", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                nameText = texts[i];
                continue;
            }

            if (descriptionText == null
                && (texts[i].name.IndexOf("Desc", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || texts[i].text.IndexOf("ads", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                descriptionText = texts[i];
            }
        }
    }

    private static bool IsPriceText(string text)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOf("đ", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string GetDisplayPrice()
    {
        if (RemoveAdsManager.Instance != null
            && RemoveAdsManager.Instance.TryGetLocalizedPrice(packageConfig, out string storePrice))
        {
            return storePrice;
        }

        return packageConfig != null ? packageConfig.PriceText : string.Empty;
    }

    private void SetButtonState(bool interactable, string text)
    {
        if (buyButton != null)
        {
            buyButton.interactable = interactable;
        }

        if (priceText != null)
        {
            priceText.text = text;
        }
    }

    private void SubscribeIap()
    {
        if (RemoveAdsManager.Instance == null)
        {
            return;
        }

        RemoveAdsManager.Instance.PricesUpdated -= HandlePricesUpdated;
        RemoveAdsManager.Instance.PricesUpdated += HandlePricesUpdated;
        RemoveAdsManager.Instance.PurchaseFinished -= HandlePurchaseFinished;
        RemoveAdsManager.Instance.PurchaseFinished += HandlePurchaseFinished;
    }

    private void UnsubscribeIap()
    {
        if (RemoveAdsManager.Instance == null)
        {
            return;
        }

        RemoveAdsManager.Instance.PricesUpdated -= HandlePricesUpdated;
        RemoveAdsManager.Instance.PurchaseFinished -= HandlePurchaseFinished;
    }

    private void HandlePricesUpdated()
    {
        if (!isPurchasing)
        {
            Refresh();
        }
    }

    private void HandlePurchaseFinished(string storeProductId, bool success)
    {
        if (packageConfig == null || packageConfig.StoreProductId != storeProductId)
        {
            return;
        }

        isPurchasing = false;
        SetButtonState(true, GetDisplayPrice());
    }
}
