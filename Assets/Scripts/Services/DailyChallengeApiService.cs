using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using WordWars.Models;

public class DailyChallengeApiService : MonoBehaviour
{
    private string BaseUrl => $"{AppConfig.Instance.ApiUrl}/daily-challenge/api/daily-challenge"; 

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
        string url = $"{BaseUrl}/today";
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
