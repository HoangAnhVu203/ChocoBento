using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class RemoveAdsManager : MonoBehaviour, IDetailedStoreListener
{
    public static RemoveAdsManager Instance;

    [Header("Packages")]
    [SerializeField] private ShopPackageConfig starterPackage;
    [SerializeField] private ShopPackageConfig[] shopPackages;

    [Header("UI")]
    [SerializeField] private RemoveAdsPanelUI panelUI;

    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    public event Action PricesUpdated;
    public event Action<string, bool> PurchaseFinished;

    public string CurrentPrice { get; private set; }

    public bool IsAdsRemoved =>
        RewardManager.Instance != null && RewardManager.Instance.RemoveAllAds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        if (storeController != null)
            return;

        var builder = ConfigurationBuilder.Instance(
            StandardPurchasingModule.Instance()
        );

        AddProduct(builder, starterPackage);

        if (shopPackages != null)
        {
            for (int i = 0; i < shopPackages.Length; i++)
            {
                AddProduct(builder, shopPackages[i]);
            }
        }

        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyRemoveAds()
    {
        BuyPackage(starterPackage);
    }

    public void BuyPackage(ShopPackageConfig packageConfig)
    {
        if (packageConfig == null)
        {
            panelUI?.ShowPopup("No package found.");
            ShowBuyResult(false);
            return;
        }

        string storeProductId = packageConfig.StoreProductId;

        if (storeController == null)
        {
            panelUI?.ShowPopup("The store is not yet available.");
            ShowBuyResult(false);
            PurchaseFinished?.Invoke(storeProductId, false);
            return;
        }

        Product product = storeController.products.WithID(storeProductId);

        if (product != null && product.availableToPurchase)
        {
            storeController.InitiatePurchase(product);
        }
        else
        {
            panelUI?.ShowPopup("No product found.");
            ShowBuyResult(false);
            PurchaseFinished?.Invoke(storeProductId, false);
        }
    }

    public bool TryGetLocalizedPrice(ShopPackageConfig packageConfig, out string price)
    {
        price = null;
        if (storeController == null || packageConfig == null)
        {
            return false;
        }

        Product product = storeController.products.WithID(packageConfig.StoreProductId);
        if (product == null || product.metadata == null)
        {
            return false;
        }

        price = product.metadata.localizedPriceString;
        return !string.IsNullOrEmpty(price);
    }

#if UNITY_IOS
    public void RestorePurchase()
    {
        if (extensionProvider == null)
            return;

        var apple = extensionProvider.GetExtension<IAppleExtensions>();

 apple.RestoreTransactions((result, message) =>
{
    Debug.Log("Restore Result: " + result);
    Debug.Log("Restore Message: " + message);

    if (result)
    {
        panelUI?.ShowPopup("Restore successfully");
    }
    else
    {
        panelUI?.ShowPopup("No transactions to recover.");
    }
});
    }
#endif

    public void OnInitialized(
        IStoreController controller,
        IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;

        Product product = starterPackage != null
            ? storeController.products.WithID(starterPackage.StoreProductId)
            : null;

        if (product != null)
        {
            CurrentPrice = product.metadata.localizedPriceString;
        }

        Debug.Log("IAP Initialized");
        PricesUpdated?.Invoke();
    }

    public void OnInitializeFailed(
        InitializationFailureReason error)
    {
        Debug.LogError("IAP Init Failed: " + error);

        panelUI?.ShowPopup("Store creation failed");
    }

    public void OnInitializeFailed(
        InitializationFailureReason error,
        string message)
    {
        Debug.LogError($"IAP Init Failed: {error} - {message}");

        panelUI?.ShowPopup("Store creation failed");
    }

    public PurchaseProcessingResult ProcessPurchase(
        PurchaseEventArgs args)
    {
        string purchasedProductId = args.purchasedProduct.definition.id;
        ShopPackageConfig purchasedPackage = FindPackage(purchasedProductId);

        if (purchasedPackage != null)
        {
            bool claimed = RewardManager.Instance != null
                && RewardManager.Instance.ClaimPackage(purchasedPackage);

            panelUI?.ShowPopup(claimed
                ? "Purchase successful - Ads turned off"
                : "Purchase successful");

            ShowBuyResult(claimed);
            PurchaseFinished?.Invoke(purchasedProductId, claimed);

            Debug.Log($"IAP Purchased: {purchasedProductId}");
        }
        else
        {
            ShowBuyResult(false);
            PurchaseFinished?.Invoke(purchasedProductId, false);
            Debug.LogError($"IAP Purchased but no package config found: {purchasedProductId}");
        }

        panelUI?.OnPurchaseFinished();

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(
        Product product,
        PurchaseFailureReason failureReason)
    {
        Debug.LogError(
            $"Purchase Failed: {product.definition.id} - {failureReason}"
        );

        panelUI?.ShowPopup("Buy failed");

        panelUI?.OnPurchaseFinished();
        ShowBuyResult(false);
        PurchaseFinished?.Invoke(product.definition.id, false);
    }

    public void OnPurchaseFailed(
        Product product,
        PurchaseFailureDescription failureDescription)
    {
        Debug.LogError(
            $"Purchase Failed: {failureDescription.reason} - {failureDescription.message}"
        );

        panelUI?.ShowPopup("Buy failed");

        panelUI?.OnPurchaseFinished();
        ShowBuyResult(false);
        PurchaseFinished?.Invoke(product.definition.id, false);
    }

    private static void AddProduct(ConfigurationBuilder builder, ShopPackageConfig packageConfig)
    {
        if (builder == null || packageConfig == null || string.IsNullOrEmpty(packageConfig.StoreProductId))
        {
            return;
        }

        ProductType productType = packageConfig.PackageType == ShopPackageType.CoinPack
            ? ProductType.Consumable
            : ProductType.NonConsumable;

        builder.AddProduct(packageConfig.StoreProductId, productType);
    }

    private ShopPackageConfig FindPackage(string storeProductId)
    {
        if (starterPackage != null && starterPackage.StoreProductId == storeProductId)
        {
            return starterPackage;
        }

        if (shopPackages == null)
        {
            return null;
        }

        for (int i = 0; i < shopPackages.Length; i++)
        {
            if (shopPackages[i] != null && shopPackages[i].StoreProductId == storeProductId)
            {
                return shopPackages[i];
            }
        }

        return null;
    }

    private static void ShowBuyResult(bool success)
    {
        var panelResult = UIManager.Instance.OpenUI<PanelResult>();
        panelResult.ShowBuyResult(success);
    }
}
