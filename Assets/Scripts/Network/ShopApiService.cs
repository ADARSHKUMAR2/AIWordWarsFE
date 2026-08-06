/*
 * Shop API Service
 * 
 * Handles all HTTP requests to the monetization service
 * 
 * Endpoints:
 * - POST /shop/api/purchase/validate - Validate IAP purchase
 * - GET /shop/api/inventory/{userId} - Get user inventory
 * - POST /shop/api/inventory/consume_hint - Use a hint
 * - GET /shop/api/battlepass/{userId} - Get battle pass status
 * - POST /shop/api/battlepass/claim - Claim battle pass reward
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using WordWars.Models;

namespace WordWars.Network
{
    public class ShopApiService : MonoBehaviour
    {
        private static ShopApiService _instance;
        public static ShopApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ShopApiService");
                    _instance = go.AddComponent<ShopApiService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // API Gateway URL (change for production)
        private const string BASE_URL = "http://localhost:8000/shop/api";

        /// <summary>
        /// Validate a purchase with the backend
        /// Called after Unity IAP completes a purchase
        /// </summary>
        public IEnumerator ValidatePurchase(
            string userId,
            string productId,
            string purchaseToken,
            Action<PurchaseValidationResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{BASE_URL}/purchase/validate";

            // Create request body
            var requestBody = new
            {
                user_id = userId,
                product_id = productId,
                purchase_token = purchaseToken
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            // Create request
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                Debug.Log($"📤 Validating purchase: {productId}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"✅ Purchase validated: {responseText}");

                    var response = JsonUtility.FromJson<PurchaseValidationResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    string error = $"Purchase validation failed: {request.error}";
                    Debug.LogError($"❌ {error}");
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>
        /// Get user's inventory
        /// </summary>
        public IEnumerator GetInventory(
            string userId,
            Action<InventoryResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{BASE_URL}/inventory/{userId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<InventoryResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// Consume one hint from inventory
        /// Called when player uses a hint during gameplay
        /// </summary>
        public IEnumerator ConsumeHint(
            string userId,
            Action<int> onSuccess,  // Returns remaining hints
            Action<string> onError)
        {
            string url = $"{BASE_URL}/inventory/consume_hint";

            var requestBody = new { user_id = userId };
            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Parse response to get remaining hints
                    var response = JsonUtility.FromJson<ConsumeHintResponse>(
                        request.downloadHandler.text
                    );
                    onSuccess?.Invoke(response.remaining_hints);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// Get battle pass status
        /// </summary>
        public IEnumerator GetBattlePass(
            string userId,
            string seasonId,
            Action<BattlePassResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{BASE_URL}/battlepass/{userId}?season_id={seasonId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<BattlePassResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// Claim a battle pass reward
        /// </summary>
        public IEnumerator ClaimBattlePassReward(
            string userId,
            string seasonId,
            int tier,
            string rewardType,  // "free" or "premium"
            Action<ClaimRewardResponse> onSuccess,
            Action<string> onError)
        {
            string url = $"{BASE_URL}/battlepass/claim";

            var requestBody = new
            {
                user_id = userId,
                season_id = seasonId,
                tier = tier,
                reward_type = rewardType
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<ClaimRewardResponse>(
                        request.downloadHandler.text
                    );
                    onSuccess?.Invoke(response);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        // Helper response class
        [Serializable]
        private class ConsumeHintResponse
        {
            public bool success;
            public int remaining_hints;
        }
    }
}
