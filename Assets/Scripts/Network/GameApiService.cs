using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameApiService : MonoBehaviour
{
    private const string GATEWAY_URL = "http://localhost:8000"; // Change to your deployed URL in production

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

    // POST /game/api/puzzle/new
    public IEnumerator GetNewPuzzle(NewPuzzleRequest puzzleRequest, Action<PuzzleResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GATEWAY_URL}/game/api/puzzle/new";
        string jsonBody = JsonUtility.ToJson(puzzleRequest);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Fetched new puzzle: " + request.downloadHandler.text);
                PuzzleResponse response = JsonUtility.FromJson<PuzzleResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                Debug.LogError("Failed to fetch puzzle: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text);
                onError?.Invoke(request.error);
            }
        }
    }

    // POST /game/api/puzzle/solve
    public IEnumerator SolvePuzzle(SolveRequest solveRequest, Action<SolveResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GATEWAY_URL}/game/api/puzzle/solve";
        string jsonBody = JsonUtility.ToJson(solveRequest);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Solved puzzle: " + request.downloadHandler.text);
                SolveResponse response = JsonUtility.FromJson<SolveResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                Debug.LogError("Failed to solve puzzle: " + request.error);
                Debug.LogError("Response body: " + request.downloadHandler.text);
                onError?.Invoke(request.error);
            }
        }
    }

    // --- DATA MODELS ---

    [Serializable]
    public class NewPuzzleRequest
    {
        public int difficulty;
        public string mode;
        public string category;
    }

    [Serializable]
    public class PuzzleResponse
    {
        public string puzzle_id;
        public string scrambled;
        public int difficulty;
        public string hint;
        public string mode;
        public string category;
    }

    [Serializable]
    public class SolveRequest
    {
        public string puzzle_id;
        public string answer;
        public float time_taken;
        public string user_id;
    }

    [Serializable]
    public class SolveResponse
    {
        public bool correct;
        public float time_taken;
        public int score;
        public string feedback;
        public string correct_answer;
    }
}
