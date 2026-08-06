/*
 * Shop UI Controller - Example
 * 
 * Shows how to integrate IAPManager and ShopApiService with your UI
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordWars.IAP;
using WordWars.Network;

namespace WordWars.UI
{
    public class ShopUIController : MonoBehaviour
    {
        [Header("UI References")]
        public Button buyHints10Button;
        public Button buyHints50Button;
        public Button buyRemoveAdsButton;
        public Button buyBattlePassButton;

        [Header("Price Labels")]
        public TextMeshProUGUI hints10PriceText;
        public TextMeshProUGUI hints50PriceText;
        public TextMeshProUGUI removeAdsPriceText;
        public TextMeshProUGUI battlePassPriceText;

        [Header("Inventory Display")]
        public TextMeshProUGUI hintsCountText;
        public GameObject adsRemovedIndicator;

        private string userId;

        void Start()
        {
            
            userId = AuthManager.Instance.CurrentUserUID;

            // Setup button listeners
            buyHints10Button.onClick.AddListener(() => BuyProduct(IAPManager.HINTS_10));
            buyHints50Button.onClick.AddListener(() => BuyProduct(IAPManager.HINTS_50));
            buyRemoveAdsButton.onClick.AddListener(() => BuyProduct(IAPManager.REMOVE_ADS));
            buyBattlePassButton.onClick.AddListener(() => BuyProduct(IAPManager.BATTLE_PASS_PREMIUM));

            // Listen to updated IAP events
            IAPManager.Instance.StoreInitializedEvent += UpdatePrices;
            IAPManager.Instance.PurchaseSuccessEvent += OnPurchaseSuccess;
            IAPManager.Instance.PurchaseFailedEvent += OnPurchaseFailed;

            // Load inventory
            RefreshInventory();
        }

        void UpdatePrices()
        {
            // Display localized prices
            hints10PriceText.text = IAPManager.Instance.GetProductPrice(IAPManager.HINTS_10);
            hints50PriceText.text = IAPManager.Instance.GetProductPrice(IAPManager.HINTS_50);
            removeAdsPriceText.text = IAPManager.Instance.GetProductPrice(IAPManager.REMOVE_ADS);
            battlePassPriceText.text = IAPManager.Instance.GetProductPrice(IAPManager.BATTLE_PASS_PREMIUM);
        }

        void BuyProduct(string productId)
        {
            Debug.Log($"User clicked buy: {productId}");
            IAPManager.Instance.BuyProduct(productId);
        }

        void OnPurchaseSuccess(string productName)
        {
            Debug.Log($"✅ Purchase successful: {productName}");
            ShowMessage($"Purchase successful! You received: {productName}");
            RefreshInventory();
        }

        void OnPurchaseFailed(string error)
        {
            Debug.LogError($"❌ Purchase failed: {error}");
            ShowMessage($"Purchase failed: {error}");
        }

        void RefreshInventory()
        {
            StartCoroutine(ShopApiService.Instance.GetInventory(
                userId,
                onSuccess: (inventory) =>
                {
                    hintsCountText.text = $"Hints: {inventory.hints}";
                    if(adsRemovedIndicator != null)
                        adsRemovedIndicator.SetActive(inventory.remove_ads);

                    Debug.Log($"📦 Inventory loaded: {inventory.hints} hints");
                },
                onError: (error) =>
                {
                    Debug.LogError($"Failed to load inventory: {error}");
                }
            ));
        }

        void ShowMessage(string message)
        {
            // Implement your own UI notification system here (e.g., Toast, popup)
            Debug.Log($"💬 {message}");
        }

        void OnDestroy()
        {
            // Cleanup updated event listeners
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.StoreInitializedEvent -= UpdatePrices;
                IAPManager.Instance.PurchaseSuccessEvent -= OnPurchaseSuccess;
                IAPManager.Instance.PurchaseFailedEvent -= OnPurchaseFailed;
            }
        }
    }
}
