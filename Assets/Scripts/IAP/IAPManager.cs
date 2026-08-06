/*
 * IAP Manager - Unity In-App Purchase Integration
 * 
 * This manages all IAP operations using Unity IAP (Unity Purchasing)
 */

using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using WordWars.Network;
using WordWars.Models;

namespace WordWars.IAP
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        private static IAPManager _instance;
        public static IAPManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("IAPManager");
                    _instance = go.AddComponent<IAPManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;

        // Product IDs - must match Google Play Console exactly
        public const string HINTS_10 = "com.wordwars.hints_10";
        public const string HINTS_50 = "com.wordwars.hints_50";
        public const string REMOVE_ADS = "com.wordwars.remove_ads";
        public const string BATTLE_PASS_PREMIUM = "com.wordwars.battlepass_premium";

        // Events for UI to listen to (Renamed to avoid clashing with interface methods)
        public event Action<string> PurchaseSuccessEvent;
        public event Action<string> PurchaseFailedEvent;
        public event Action StoreInitializedEvent;

        private bool isInitialized = false;

        void Start()
        {
            InitializePurchasing();
        }

        /// <summary>
        /// Initialize Unity IAP
        /// </summary>
        private void InitializePurchasing()
        {
            if (isInitialized)
            {
                Debug.Log("IAP already initialized");
                return;
            }

            Debug.Log("🛒 Initializing Unity IAP...");

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add products to the catalog
            builder.AddProduct(HINTS_10, ProductType.Consumable);
            builder.AddProduct(HINTS_50, ProductType.Consumable);
            builder.AddProduct(REMOVE_ADS, ProductType.NonConsumable);
            builder.AddProduct(BATTLE_PASS_PREMIUM, ProductType.NonConsumable);

            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Called when Unity IAP initialization succeeds
        /// </summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("✅ Unity IAP initialized successfully");

            storeController = controller;
            storeExtensionProvider = extensions;
            isInitialized = true;

            StoreInitializedEvent?.Invoke();

            foreach (var product in controller.products.all)
            {
                Debug.Log($"📦 Product: {product.definition.id} - {product.metadata.localizedPriceString}");
            }
        }

        /// <summary>
        /// Called when Unity IAP initialization fails (Legacy)
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"❌ IAP initialization failed: {error}");
            PurchaseFailedEvent?.Invoke($"Store initialization failed: {error}");
        }

        /// <summary>
        /// Called when Unity IAP initialization fails (Detailed)
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"❌ IAP initialization failed: {error} - {message}");
            PurchaseFailedEvent?.Invoke($"Store initialization failed: {message}");
        }

        /// <summary>
        /// Initiate a purchase
        /// </summary>
        public void BuyProduct(string productId)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Cannot purchase - IAP not initialized");
                PurchaseFailedEvent?.Invoke("Store not ready");
                return;
            }

            Debug.Log($"🛒 Initiating purchase: {productId}");
            Product product = storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"💳 Purchasing: {product.definition.id}");
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError($"❌ Product not available: {productId}");
                PurchaseFailedEvent?.Invoke("Product not available");
            }
        }

        /// <summary>
        /// Called when a purchase completes
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Debug.Log($"✅ Purchase completed: {args.purchasedProduct.definition.id}");

            // Extract Google Play purchase token
            var wrapper = JsonUtility.FromJson<GooglePurchaseWrapper>(args.purchasedProduct.receipt);
            var googleReceipt = JsonUtility.FromJson<GooglePlayReceipt>(wrapper.Payload);
            var tokenData = JsonUtility.FromJson<GooglePlayPurchaseData>(googleReceipt.json);
            
            string productId = args.purchasedProduct.definition.id;
            string actualToken = tokenData.purchaseToken;

            Debug.Log($"📝 Purchase Token: {actualToken}");

            // Note: Assuming AuthManager exists in your project. Adjust if you get user ID differently.
            string userId = AuthManager.Instance.CurrentUserUID; 

            StartCoroutine(ShopApiService.Instance.ValidatePurchase(
                userId,
                productId,
                actualToken,
                onSuccess: (response) =>
                {
                    Debug.Log($"✅ Backend validated purchase: {response.product_name}");
                    PurchaseSuccessEvent?.Invoke(response.product_name);
                },
                onError: (error) =>
                {
                    Debug.LogError($"❌ Backend validation failed: {error}");
                    PurchaseFailedEvent?.Invoke(error);
                }
            ));

            return PurchaseProcessingResult.Complete;
        }

        /// <summary>
        /// Called when a purchase fails (Required by IStoreListener)
        /// </summary>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"❌ Purchase failed: {product.definition.id} - {failureReason}");
            PurchaseFailedEvent?.Invoke($"Purchase failed: {failureReason}");
        }

        /// <summary>
        /// Called when a purchase fails with detailed info (Required by IDetailedStoreListener)
        /// </summary>
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError($"❌ Purchase failed: {product.definition.id} - {failureDescription.reason}");
            PurchaseFailedEvent?.Invoke($"Purchase failed: {failureDescription.message}");
        }

        /// <summary>
        /// Get the localized price of a product
        /// </summary>
        public string GetProductPrice(string productId)
        {
            if (!isInitialized || storeController == null)
                return "...";

            Product product = storeController.products.WithID(productId);
            if (product != null)
                return product.metadata.localizedPriceString;

            return "N/A";
        }

        /// <summary>
        /// Check if a product is available for purchase
        /// </summary>
        public bool IsProductAvailable(string productId)
        {
            if (!isInitialized || storeController == null)
                return false;

            Product product = storeController.products.WithID(productId);
            return product != null && product.availableToPurchase;
        }

        // Helper classes for parsing Google Play receipts
        [Serializable]
        private class GooglePurchaseWrapper
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        [Serializable]
        private class GooglePlayReceipt
        {
            public string json;
            public string signature;
        }

        [Serializable]
        private class GooglePlayPurchaseData
        {
            public string orderId;
            public string packageName;
            public string productId;
            public string purchaseTime;
            public int purchaseState;
            public string purchaseToken;
        }
    }
}
