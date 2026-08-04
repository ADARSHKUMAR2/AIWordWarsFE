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

    [Serializable]
    public class UserProfile
    {
        public string firebase_uid;
        public string display_name;
        public string email;
        public string photo_url;
        public int coins;
        public int xp;
        public int level;
    }
}
    