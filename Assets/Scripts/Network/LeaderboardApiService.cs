using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderboardApiService : MonoBehaviour
{
    private const string GATEWAY_URL = "http://localhost:8000";

    public static LeaderboardApiService Instance { get; private set; }

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

    // ─────────────────────────────────────────
    // HELPER — authenticated GET request
    // ─────────────────────────────────────────
    private UnityWebRequest BuildGetRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.CurrentSessionId))
        {
            request.SetRequestHeader("Authorization", $"Bearer {AuthManager.Instance.CurrentSessionId}");
        }
        return request;
    }

    // ─────────────────────────────────────────
    // GET LEADERBOARD
    // ─────────────────────────────────────────

    /// <summary>
    /// Fetches the top N scores for a board.
    /// Called when loading the Leaderboards scene.
    /// boardId: "global", "weekly", "daily"
    /// mode: "simple", "time_attack", "survival"
    /// </summary>
    public IEnumerator GetLeaderboard(
        string boardId,
        string mode,
        int limit,
        Action<LeaderboardResponse> onSuccess,
        Action<string> onError)
    {
        string url = $"{GATEWAY_URL}/leaderboard/api/scores/{boardId}?mode={mode}&limit={limit}";

        using (UnityWebRequest request = BuildGetRequest(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"📋 Leaderboard [{boardId}/{mode}] fetched.");
                onSuccess?.Invoke(JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text));
            }
            else
            {
                Debug.LogError("Failed to fetch leaderboard: " + request.error);
                onError?.Invoke(request.error);
            }
        }
    }

    // ─────────────────────────────────────────
    // GET PLAYER RANK
    // ─────────────────────────────────────────

    /// <summary>
    /// Fetches a player's rank on a specific board.
    /// Returns null via onSuccess if the player hasn't scored yet (404).
    /// </summary>
    public IEnumerator GetPlayerRank(
        string boardId,
        string firebaseUid,
        string mode,
        Action<PlayerRankResponse> onSuccess,
        Action<string> onError)
    {
        string url = $"{GATEWAY_URL}/leaderboard/api/scores/{boardId}/rank/{firebaseUid}?mode={mode}";

        using (UnityWebRequest request = BuildGetRequest(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"🏆 Player rank on [{boardId}/{mode}] fetched.");
                onSuccess?.Invoke(JsonUtility.FromJson<PlayerRankResponse>(request.downloadHandler.text));
            }
            else if (request.responseCode == 404)
            {
                // Player has no score yet — not an error, just return null
                Debug.Log($"Player has no score on [{boardId}/{mode}] yet.");
                onSuccess?.Invoke(null);
            }
            else
            {
                Debug.LogError("Failed to fetch player rank: " + request.error);
                onError?.Invoke(request.error);
            }
        }
    }
}
