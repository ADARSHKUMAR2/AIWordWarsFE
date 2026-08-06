using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

/// <summary>
/// Manages the WebSocket connection to the multiplayer-session-service.
/// Uses NativeWebSocket (https://github.com/endel/NativeWebSocket) which
/// works correctly in Unity Editor, macOS, iOS, Android, and WebGL.
///
/// Connect URL: ws://localhost:8007/ws/{ROOM_CODE}?user_id={FIREBASE_UID}
/// </summary>
public class MultiplayerWebSocketService : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static MultiplayerWebSocketService Instance { get; private set; }

    // ── Events (fired on Unity's main thread via NativeWebSocket's dispatcher) ──
    public event Action<string> OnWaiting;
    public event Action<WsGameStart> OnGameStart;
    public event Action<WsAnswerResult> OnAnswerResult;
    public event Action<WsGameOver> OnGameOver;
    public event Action OnOpponentDisconnected;
    public event Action<string> OnError;

    // ── Internal state ───────────────────────────────────────────────────────
    private WebSocket _ws;
    private bool _isConnected = false;

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

    private void Update()
    {
        // CRITICAL: NativeWebSocket requires this to dispatch messages on the main thread
        if (_ws != null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _ws.DispatchMessageQueue();
#endif
        }
    }

    // ── PUBLIC API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Connect to the multiplayer session WebSocket.
    /// wsUrl example: "ws://localhost:8007/ws/AB12CD"
    /// userId: the Firebase UID — appended as ?user_id= query param
    /// </summary>
    public async Task Connect(string wsUrl, string userId)
    {
        if (_isConnected)
        {
            Debug.LogWarning("[WS] Already connected.");
            return;
        }

        // Append user_id as query param
        if (!wsUrl.Contains("user_id="))
            wsUrl += $"?user_id={userId}";

        Debug.Log($"[WS] Connecting to: {wsUrl}");

        _ws = new WebSocket(wsUrl);

        _ws.OnOpen += () =>
        {
            _isConnected = true;
            Debug.Log("[WS] ✅ Connected!");
        };

        _ws.OnError += (err) =>
        {
            Debug.LogError($"[WS] ❌ Error: {err}");
            _isConnected = false;
            OnError?.Invoke(err);
        };

        _ws.OnClose += (code) =>
        {
            _isConnected = false;
            Debug.Log($"[WS] 🔌 Closed. Code: {code}");
        };

        _ws.OnMessage += (bytes) =>
        {
            string text = Encoding.UTF8.GetString(bytes);
            Debug.Log($"[WS] 📨 Received: {text}");
            HandleMessage(text);
        };

        // This actually initiates the TCP connection
        await _ws.Connect();
        Debug.Log("[WS] Await Connect() returned.");
    }

    /// <summary>
    /// Send the player's answer to the server.
    /// </summary>
    public async Task SubmitAnswer(string answer, float timeTaken)
    {
        if (_ws == null || _ws.State != WebSocketState.Open)
        {
            Debug.LogWarning("[WS] Cannot send — not connected.");
            return;
        }

        string msg = $"{{\"type\":\"submit_answer\",\"answer\":\"{answer}\",\"time_taken\":{timeTaken}}}";
        Debug.Log($"[WS] Sending answer: {msg}");
        await _ws.SendText(msg);
    }

    /// <summary>
    /// Gracefully close the WebSocket connection.
    /// </summary>
    public async Task Disconnect()
    {
        if (_ws == null) return;
        _isConnected = false;
        try
        {
            await _ws.Close();
        }
        catch { }
        _ws = null;
        Debug.Log("[WS] Disconnected.");
    }

    // ── PRIVATE: Handle incoming messages ────────────────────────────────────

    private void HandleMessage(string json)
    {
        // Parse the type field first
        var baseMsg = JsonUtility.FromJson<WsMessage>(json);
        if (baseMsg == null) return;

        Debug.Log($"[WS] Message type: {baseMsg.type}");

        switch (baseMsg.type)
        {
            case "waiting":
                OnWaiting?.Invoke(baseMsg.message);
                break;

            case "game_start":
                var gameStart = JsonUtility.FromJson<WsGameStart>(json);
                OnGameStart?.Invoke(gameStart);
                break;

            case "answer_result":
                var ansResult = JsonUtility.FromJson<WsAnswerResult>(json);
                OnAnswerResult?.Invoke(ansResult);
                break;

            case "game_over":
                var gameOver = JsonUtility.FromJson<WsGameOver>(json);
                OnGameOver?.Invoke(gameOver);
                break;

            case "opponent_disconnected":
                OnOpponentDisconnected?.Invoke();
                break;

            case "error":
                OnError?.Invoke(baseMsg.message);
                break;

            case "pong":
                Debug.Log("[WS] Pong received.");
                break;

            default:
                Debug.LogWarning($"[WS] Unknown message type: {baseMsg.type}");
                break;
        }
    }

    private void OnDestroy()
    {
        _ = Disconnect();
    }
}
