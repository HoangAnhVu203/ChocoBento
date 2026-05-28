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

    private void OnEnable()
    {
        BindButton();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindButton();
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
            priceText.text = packageConfig.PriceText;
        }

    }

    private void HandleBuyClicked()
    {
        if (packageConfig == null)
        {
            return;
        }

        if (panelShop == null)
        {
            panelShop = GetComponentInParent<PanelShop>(true);
        }

        if (panelShop != null)
        {
            panelShop.BuyPackage(packageConfig);
            return;
        }

        bool success = RewardManager.Instance.ClaimPackage(packageConfig);
        ShowBuyResult(success);
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
}
