using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using WordWars.Models;

public class GameApiService : MonoBehaviour
{
    private string GatewayUrl => AppConfig.Instance.ApiUrl;

    public static GameApiService Instance { get; private set; }

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
    // HELPER — builds an authenticated POST request
    // ─────────────────────────────────────────
    private UnityWebRequest BuildPostRequest(string url, string jsonBody)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.CurrentSessionId))
        {
            request.SetRequestHeader("Authorization", $"Bearer {AuthManager.Instance.CurrentSessionId}");
        }
        return request;
    }

    // ─────────────────────────────────────────
    // CLASSIC MODE
    // ─────────────────────────────────────────

    public IEnumerator GetNewPuzzle(NewPuzzleRequest puzzleRequest, Action<PuzzleResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/game/api/puzzle/new";
        string jsonBody = JsonUtility.ToJson(puzzleRequest);

        using (UnityWebRequest request = BuildPostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Fetched new puzzle: " + request.downloadHandler.text);
                onSuccess?.Invoke(JsonUtility.FromJson<PuzzleResponse>(request.downloadHandler.text));
            }
            else
            {
                Debug.LogError("Failed to fetch puzzle: " + request.error);
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator SolvePuzzle(SolveRequest solveRequest, Action<SolveResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/game/api/puzzle/solve";
        string jsonBody = JsonUtility.ToJson(solveRequest);

        using (UnityWebRequest request = BuildPostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Solved puzzle: " + request.downloadHandler.text);
                onSuccess?.Invoke(JsonUtility.FromJson<SolveResponse>(request.downloadHandler.text));
            }
            else
            {
                Debug.LogError("Failed to solve puzzle: " + request.error);
                onError?.Invoke(request.error);
            }
        }
    }

    // ─────────────────────────────────────────
    // TIME ATTACK MODE
    // ─────────────────────────────────────────

    public IEnumerator GetNewTimeAttackPuzzle(NewPuzzleRequest puzzleRequest, Action<TimeAttackPuzzleResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/game/api/time-attack/puzzle/new";
        string jsonBody = JsonUtility.ToJson(puzzleRequest);

        using (UnityWebRequest request = BuildPostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(JsonUtility.FromJson<TimeAttackPuzzleResponse>(request.downloadHandler.text));
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator SolveTimeAttackPuzzle(TimeAttackSolveRequest solveRequest, Action<TimeAttackSolveResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/game/api/time-attack/puzzle/solve";
        string jsonBody = JsonUtility.ToJson(solveRequest);

        using (UnityWebRequest request = BuildPostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(JsonUtility.FromJson<TimeAttackSolveResponse>(request.downloadHandler.text));
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    // ─────────────────────────────────────────
    // SURVIVAL MODE
    // ─────────────────────────────────────────

    public IEnumerator GetNewSurvivalPuzzle(NewPuzzleRequest puzzleRequest, Action<SurvivalPuzzleResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/game/api/survival/puzzle/new";
        string jsonBody = JsonUtility.ToJson(puzzleRequest);

        using (UnityWebRequest request = BuildPostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(JsonUtility.FromJson<SurvivalPuzzleResponse>(request.downloadHandler.text));
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator SolveSurvivalPuzzle(SurvivalSolveRequest solveRequest, Action<SurvivalSolveResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/game/api/survival/puzzle/solve";
        string jsonBody = JsonUtility.ToJson(solveRequest);

        using (UnityWebRequest request = BuildPostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(JsonUtility.FromJson<SurvivalSolveResponse>(request.downloadHandler.text));
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }
}
