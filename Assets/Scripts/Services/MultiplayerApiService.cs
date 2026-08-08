using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using WordWars.Models;

/// <summary>
/// Handles REST calls to the matchmaking-service for creating and joining rooms.
/// WebSocket connection is handled separately by MultiplayerWebSocketService.
/// </summary>
public class MultiplayerApiService : MonoBehaviour
{
    private string GatewayUrl => AppConfig.Instance.ApiUrl;

    public static MultiplayerApiService Instance { get; private set; }

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

    // ─────────────────────────────────────────────────────────────────
    // POST /matchmaking/api/match/create
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Player 1 creates a room. Returns the room code to display to the player.
    /// </summary>
    public IEnumerator CreateRoom(string userId, int difficulty, Action<CreateRoomResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/matchmaking/api/match/create";

        CreateRoomRequest payload = new CreateRoomRequest
        {
            user_id = userId,
            difficulty = difficulty
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response);
        }
        else
        {
            onError?.Invoke(request.downloadHandler.text);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /matchmaking/api/match/join
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Player 2 joins using the room code. Returns the WebSocket URL.
    /// </summary>
    public IEnumerator JoinRoom(string roomCode, string userId, Action<JoinRoomResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/matchmaking/api/match/join";

        JoinRoomRequest payload = new JoinRoomRequest
        {
            room_code = roomCode.ToUpper(),
            user_id = userId
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<JoinRoomResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response);
        }
        else
        {
            onError?.Invoke(request.downloadHandler.text);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /matchmaking/api/match/{room_code}  — Poll room status
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Player 1 polls this to detect when Player 2 has joined (status becomes "ready").
    /// </summary>
    public IEnumerator PollRoomStatus(string roomCode, Action<RoomStatusResponse> onSuccess, Action<string> onError)
    {
        string url = $"{GatewayUrl}/matchmaking/api/match/{roomCode.ToUpper()}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<RoomStatusResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response);
        }
        else
        {
            onError?.Invoke(request.downloadHandler.text);
        }
    }
}
