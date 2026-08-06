using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class DailyChallengeApiService : MonoBehaviour
{
    private readonly string baseUrl = "http://127.0.0.1:8000/daily-challenge/api/daily-challenge"; 

    public static DailyChallengeApiService Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public IEnumerator GetTodaysChallenge(Action<DailyChallengeData> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/today";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            
            // Append auth token if needed
            if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.CurrentSessionId))
            {
                request.SetRequestHeader("Authorization", $"Bearer {AuthManager.Instance.CurrentSessionId}");
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Fetched daily challenge: " + request.downloadHandler.text);
                DailyChallengeData response = JsonUtility.FromJson<DailyChallengeData>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                Debug.LogError("Failed to fetch daily challenge: " + request.error);
                onError?.Invoke(request.error);
            }
        }
    }
}
