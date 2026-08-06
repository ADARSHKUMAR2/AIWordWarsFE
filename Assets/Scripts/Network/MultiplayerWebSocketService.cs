using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages the WebSocket connection to the multiplayer-session-service.
///
/// USAGE:
///   1. Call Connect(wsUrl, userId) after matchmaking succeeds.
///   2. Subscribe to events: OnGameStart, OnAnswerResult, OnGameOver, OnOpponentDisconnected.
///   3. Call SubmitAnswer(answer, timeTaken) when the player submits.
///   4. Call Disconnect() when leaving the game.
/// </summary>
public class MultiplayerWebSocketService : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static MultiplayerWebSocketService Instance { get; private set; }

    // ── Events (fire on the main thread via _mainThreadActions) ─────────────
    public event Action<string> OnWaiting;
    public event Action<WsGameStart> OnGameStart;
    public event Action<WsAnswerResult> OnAnswerResult;
    public event Action<WsGameOver> OnGameOver;
    public event Action OnOpponentDisconnected;
    public event Action<string> OnError;

    // ── Internal state ───────────────────────────────────────────────────────
    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;
    private bool _isConnected = false;

    // Thread-safe queue to dispatch WebSocket callbacks to Unity's main thread
    private readonly System.Collections.Generic.Queue<Action> _mainThreadActions
        = new System.Collections.Generic.Queue<Action>();

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
        // Drain the action queue on Unity's main thread each frame
        lock (_mainThreadActions)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    // ── PUBLIC API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Connect to the multiplayer session WebSocket.
    /// wsUrl example: "ws://localhost:8007/ws/AB12CD?user_id=firebase_uid_here"
    /// </summary>
    public async Task Connect(string wsUrl, string userId)
    {
        if (_isConnected)
        {
            Debug.LogWarning("MultiplayerWebSocketService: Already connected.");
            return;
        }

        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        try
        {
            // Append user_id as query param if not already in url
            if (!wsUrl.Contains("user_id="))
                wsUrl += $"?user_id={userId}";

            Debug.Log($"[WS] Connecting to {wsUrl}");
            await _ws.ConnectAsync(new Uri(wsUrl), _cts.Token);
            _isConnected = true;
            Debug.Log("[WS] Connected!");

            // Start receiving in background
            _ = ReceiveLoop();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WS] Connection failed: {ex.Message}");
            Enqueue(() => OnError?.Invoke($"Connection failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Send the player's answer to the server.
    /// </summary>
    public async Task SubmitAnswer(string answer, float timeTaken)
    {
        if (!_isConnected || _ws == null) return;

        string msg = $"{{\"type\":\"submit_answer\",\"answer\":\"{answer}\",\"time_taken\":{timeTaken}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        try
        {
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cts.Token
            );
            Debug.Log($"[WS] Sent answer: {answer}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WS] Send failed: {ex.Message}");
        }
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
            _cts?.Cancel();
            if (_ws.State == WebSocketState.Open)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Leaving", CancellationToken.None);
            _ws.Dispose();
            _ws = null;
        }
        catch { /* ignore close errors */ }
        Debug.Log("[WS] Disconnected.");
    }

    // ── PRIVATE: Receive loop ────────────────────────────────────────────────

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[4096];

        while (_ws != null && _ws.State == WebSocketState.Open)
        {
            try
            {
                var segment = new ArraySegment<byte>(buffer);
                WebSocketReceiveResult result = await _ws.ReceiveAsync(segment, _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("[WS] Server closed connection.");
                    _isConnected = false;
                    break;
                }

                string text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"[WS] Received: {text}");
                HandleMessage(text);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WS] Receive error: {ex.Message}");
                break;
            }
        }

        _isConnected = false;
    }

    private void HandleMessage(string json)
    {
        // First parse just the "type" field
        var base_msg = JsonUtility.FromJson<WsMessage>(json);
        if (base_msg == null) return;

        switch (base_msg.type)
        {
            case "waiting":
                Enqueue(() => OnWaiting?.Invoke(base_msg.message));
                break;

            case "game_start":
                var gameStart = JsonUtility.FromJson<WsGameStart>(json);
                Enqueue(() => OnGameStart?.Invoke(gameStart));
                break;

            case "answer_result":
                var ansResult = JsonUtility.FromJson<WsAnswerResult>(json);
                Enqueue(() => OnAnswerResult?.Invoke(ansResult));
                break;

            case "game_over":
                var gameOver = JsonUtility.FromJson<WsGameOver>(json);
                Enqueue(() => OnGameOver?.Invoke(gameOver));
                break;

            case "opponent_disconnected":
                Enqueue(() => OnOpponentDisconnected?.Invoke());
                break;

            case "error":
                Enqueue(() => OnError?.Invoke(base_msg.message));
                break;

            case "pong":
                Debug.Log("[WS] Pong received.");
                break;
        }
    }

    private void Enqueue(Action action)
    {
        lock (_mainThreadActions)
        {
            _mainThreadActions.Enqueue(action);
        }
    }

    private void OnDestroy()
    {
        _ = Disconnect();
    }
}
