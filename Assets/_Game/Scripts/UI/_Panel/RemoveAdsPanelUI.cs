using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RemoveAdsPanelUI : MonoBehaviour
{
    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;

    [SerializeField] private TextMeshProUGUI priceText;

    [SerializeField] private Button buyButton;

    [Header("Popup Message")]
    [SerializeField] private GameObject popupPanel;

    [SerializeField] private TextMeshProUGUI popupText;

    private bool isPurchasing;

    private void Start()
    {
        // Ẩn panel khi start
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // =====================================
    // BUTTON REMOVE ADS
    // =====================================
    // Click để mở popup confirm
    public void OnClick_OpenPanel()
    {
        string price = RemoveAdsManager.Instance?.CurrentPrice;

        // Hiển thị giá
        if (priceText != null)
        {
            priceText.text = string.IsNullOrEmpty(price)
                ? "Loading..."
                : $"Remove Ads\n{price}";
        }

        // Disable nút buy nếu chưa load xong
        if (buyButton != null)
        {
            buyButton.interactable = !string.IsNullOrEmpty(price);
        }

        // Hiện panel
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
    }

    // =====================================
    // BUTTON BUY
    // =====================================
    public void OnClick_Buy()
    {
        if (isPurchasing)
            return;

        isPurchasing = true;

        if (buyButton != null)
            buyButton.interactable = false;

        RemoveAdsManager.Instance?.BuyRemoveAds();

        // Đóng panel sau khi mua
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    // Gọi sau khi purchase complete/fail
    public void OnPurchaseFinished()
    {
        isPurchasing = false;

        if (buyButton != null)
            buyButton.interactable = true;
    }

    // =====================================
    // BUTTON CLOSE
    // =====================================
    public void OnClick_ClosePanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

#if UNITY_IOS
    // =====================================
    // BUTTON RESTORE
    // =====================================
    public void OnClick_RestorePurchase()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        RemoveAdsManager.Instance?.RestorePurchase();
    }
#endif

    // =====================================
    // SHOW POPUP MESSAGE
    // =====================================
    public void ShowPopup(string message)
    {
        if (popupText != null)
        {
            popupText.text = message;
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }

        CancelInvoke(nameof(HidePopup));
        Invoke(nameof(HidePopup), 2f);
    }

    private void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }
}