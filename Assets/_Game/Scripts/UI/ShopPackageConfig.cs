using UnityEngine;

public enum ShopPackageType
{
    Package,
    CoinPack
}

[CreateAssetMenu(fileName = "Shop Package", menuName = "ChocoBento/Shop Package")]
public sealed class ShopPackageConfig : ScriptableObject
{
    [SerializeField] private string packageId;
    [SerializeField] private string packageName;
    [SerializeField] private string description;
    [SerializeField] private ShopPackageType packageType;
    [SerializeField] private string productId;
    [SerializeField] private string priceText;
    [SerializeField] private RewardData[] rewards;

    public string PackageId => packageId;
    public string PackageName => packageName;
    public string Description => description;
    public ShopPackageType PackageType => packageType;
    public string ProductId => productId;
    public string PriceText => priceText;
    public RewardData[] Rewards => rewards;

    public static ShopPackageConfig CreateRuntime(
        string packageId,
        string packageName,
        string description,
        ShopPackageType packageType,
        string productId,
        string priceText,
        RewardData[] rewards)
    {
        var config = CreateInstance<ShopPackageConfig>();
        config.packageId = packageId;
        config.packageName = packageName;
        config.description = description;
        config.packageType = packageType;
        config.productId = productId;
        config.priceText = priceText;
        config.rewards = rewards;
        return config;
    }
}
