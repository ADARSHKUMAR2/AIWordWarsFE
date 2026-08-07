using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiService : MonoBehaviour
{
    private const string GATEWAY_URL = "http://localhost:8000"; // Change to your deployed URL in production

    public static ApiService Instance { get; private set; }

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

    // POST /auth/api/login
    public IEnumerator LoginWithFirebase(string idToken, Action<UserProfile> onSuccess, Action<string> onError)
    {
        string url = $"{GATEWAY_URL}/auth/api/login";
        
        string jsonBody = $"{{\"id_token\": \"{idToken}\"}}";


        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Login successful: " + request.downloadHandler.text);
                UserProfile profile = JsonUtility.FromJson<UserProfile>(request.downloadHandler.text);
                onSuccess?.Invoke(profile);
            }
            else
            {
                Debug.LogError("Login failed: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text); 
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator ValidateSession(string sessionId, Action<bool> onComplete)
    {
        string url = $"{GATEWAY_URL}/auth/api/session/validate";
        string jsonBody = $"{{\"session_id\": \"{sessionId}\"}}";

        using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Session is valid: " + request.downloadHandler.text);
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("Session is invalid or expired: " + request.error);
                onComplete?.Invoke(false);
            }
        }
    }

    // POST /auth/api/logout
    public IEnumerator Logout(string sessionId)
    {
        string url = $"{GATEWAY_URL}/auth/api/logout";
        string jsonBody = $"{{\"session_id\": \"{sessionId}\"}}";

        using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Backend logout successful.");
            }
            else
            {
                Debug.LogWarning("Backend logout failed, but clearing locally anyway: " + request.error);
            }
        }
    }

    // ---------------------------------------------
    // HELPER METHODS
    // ---------------------------------------------

    private UnityWebRequest CreatePostRequest(string url, string jsonBody)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        // If we have an active session, append it to headers for protected routes!
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.CurrentSessionId))
        {
            request.SetRequestHeader("Authorization", $"Bearer {AuthManager.Instance.CurrentSessionId}");
        }

        return request;
    }

    public IEnumerator GetUserProfile(string firebaseUid, Action<UserProfile> onSuccess, Action<string> onError)
    {
        string url = $"{GATEWAY_URL}/auth/api/me/{firebaseUid}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UserProfile profile = JsonUtility.FromJson<UserProfile>(request.downloadHandler.text);
                onSuccess?.Invoke(profile);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    private void HandleError(string context, UnityWebRequest request, Action<string> onError)
    {
        Debug.LogError($"{context}: {request.error}");
        Debug.LogError($"Response body: {request.downloadHandler.text}");
        onError?.Invoke(request.error);
    }


}
    