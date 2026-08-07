using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles ONLY the multiplayer (1v1 WebSocket) game flow in GameplayScene.
///
/// How it works:
///   - On Start(), checks if mp_is_multiplayer == 1 in PlayerPrefs.
///   - If YES: activates itself, disables GameManager, runs multiplayer flow.
///   - If NO:  disables itself, GameManager runs normally.
///
/// Inspector wiring (share the same UI objects as GameManager):
///   scrambledWordText  → same Text as GameManager
///   hintText           → same Text as GameManager
///   feedbackText       → same Text as GameManager
///   answerInputField   → same InputField as GameManager
///   submitButton       → same Button as GameManager
///   quitButton         → same Button as GameManager
///
///   [Multiplayer-specific]
///   mpStatusText       → TextMeshProUGUI for "Waiting..." / "Game ON!"
///   mpResultPanel      → Panel shown at end (WIN/LOSE/DRAW)
///   mpResultText       → TextMeshProUGUI inside mpResultPanel
///   mpPlayAgainButton  → Button inside mpResultPanel → goes to MainMenuScene
/// </summary>
public class MultiplayerGameManager : MonoBehaviour
{
    [Header("Shared UI (same refs as GameManager)")]
    public TextMeshProUGUI scrambledWordText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI feedbackText;
    public TMP_InputField answerInputField;
    public Button submitButton;
    public Button quitButton;

    [Header("Single-player panels to hide during multiplayer")]
    public GameObject classicPanel;
    public GameObject timeAttackPanel;
    public GameObject survivalPanel;
    public GameObject gameOverPanel;

    [Header("Multiplayer-specific UI")]
    public TextMeshProUGUI mpStatusText;    // e.g. "Waiting for opponent..." / "Game ON!"
    public GameObject mpResultPanel;        // Shown at game end
    public TextMeshProUGUI mpResultText;    // Shows WIN / LOSE / DRAW message
    public Button mpPlayAgainButton;        // Returns to MainMenuScene

    // ── Internal state ───────────────────────────────────────────────────────
    private float _puzzleStartTime;

    private void Start()
    {
        bool isMultiplayer = PlayerPrefs.GetInt("mp_is_multiplayer", 0) == 1;

        if (!isMultiplayer)
        {
            // 🟢 FIX: Explicitly hide ALL multiplayer UI elements before disabling the manager
            if (mpResultPanel != null) mpResultPanel.SetActive(false);
            
            // Also hide the status text (and its parent if you placed it in a panel)
            if (mpStatusText != null) mpStatusText.gameObject.SetActive(false);
            
            // disable this manager and let GameManager/DailyChallengeManager run
            gameObject.SetActive(false);
            return;
        }

        // ── Multiplayer session — take control ────────────────────────────────

        // Clear the flag immediately so navigating back doesn't re-trigger
        PlayerPrefs.SetInt("mp_is_multiplayer", 0);
        PlayerPrefs.Save();

        // Hide all single-player panels
        if (classicPanel)    classicPanel.SetActive(false);
        if (timeAttackPanel) timeAttackPanel.SetActive(false);
        if (survivalPanel)   survivalPanel.SetActive(false);
        if (gameOverPanel)   gameOverPanel.SetActive(false);
        if (mpResultPanel)   mpResultPanel.SetActive(false);

        // Initial UI state
        scrambledWordText.text = "Connecting...";
        hintText.text = "";
        feedbackText.text = "";
        answerInputField.text = "";
        submitButton.interactable = false;

        SetStatus("Connecting to game server...");

        // Wire buttons
        submitButton.onClick.AddListener(OnSubmitClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        if (mpPlayAgainButton != null)
            mpPlayAgainButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        // Connect WebSocket
        string wsUrl   = PlayerPrefs.GetString("mp_ws_url");
        string userId  = AuthManager.Instance?.CurrentUserUID;
        
        Debug.Log($"[MP_GameManager] WS URL: {wsUrl}, UserID: {userId}");
        
        _ = ConnectWebSocket(wsUrl, userId);
    }

    // ── WEBSOCKET CONNECTION ─────────────────────────────────────────────────

    private async Task ConnectWebSocket(string wsUrl, string userId)
    {
        Debug.Log("[MP_GameManager] Subscribing to WS events and calling Connect...");
        
        // Subscribe to events BEFORE connecting
        MultiplayerWebSocketService.Instance.OnWaiting              += SetStatus;
        MultiplayerWebSocketService.Instance.OnGameStart            += OnGameStart;
        MultiplayerWebSocketService.Instance.OnAnswerResult         += OnAnswerResult;
        MultiplayerWebSocketService.Instance.OnGameOver             += OnGameOver;
        MultiplayerWebSocketService.Instance.OnOpponentDisconnected += OnOpponentDisconnected;
        MultiplayerWebSocketService.Instance.OnError                += err => SetStatus($"⚠️ {err}");

        await MultiplayerWebSocketService.Instance.Connect(wsUrl, userId);
        Debug.Log("[MP_GameManager] Await Connect finished.");
    }

    // ── WEBSOCKET EVENT HANDLERS ─────────────────────────────────────────────

    private void OnGameStart(WsGameStart data)
    {
        Debug.Log($"[MP_GameManager] OnGameStart event fired! Scrambled: {data.scrambled}");
        
        scrambledWordText.text = data.scrambled.ToUpper();
        hintText.text = !string.IsNullOrEmpty(data.hint) ? "Hint: " + data.hint : "";
        feedbackText.text = "🆚 Solve it before your opponent!";
        SetStatus($"⏱ You have {data.time_limit}s. GO!");

        _puzzleStartTime = Time.time;
        submitButton.interactable = true;
        answerInputField.text = "";
        answerInputField.Select();
        answerInputField.ActivateInputField();
    }

    private void OnAnswerResult(WsAnswerResult data)
    {
        if (!data.correct)
        {
            feedbackText.text = "<color=red>❌ Incorrect! Try again.</color>";
            submitButton.interactable = true;
            answerInputField.text = "";
            answerInputField.Select();
            answerInputField.ActivateInputField();
        }
        // If correct, server will immediately broadcast game_over — no need to handle here
    }

    private void OnGameOver(WsGameOver data)
    {
        submitButton.interactable = false;
        answerInputField.interactable = false;

        string resultMsg;
        if (data.your_result == "win")
            resultMsg = $"<color=green><size=120%><b>🏆 YOU WIN!</b></size></color>\nSolved in <b>{data.winning_time:F1}s</b>";
        else if (data.your_result == "lose")
            resultMsg = $"<color=red><size=120%><b>😞 YOU LOSE</b></size></color>\nOpponent was faster!\nCorrect word: <b>{data.correct_word}</b>";
        else
            resultMsg = $"<color=yellow><size=120%><b>⏰ DRAW</b></size></color>\nNobody solved in time!\nCorrect word: <b>{data.correct_word}</b>";

        ShowResult(resultMsg);
        _ = MultiplayerWebSocketService.Instance.Disconnect();
    }

    private void OnOpponentDisconnected()
    {
        submitButton.interactable = false;
        ShowResult("<color=green><size=120%><b>🏆 YOU WIN!</b></size></color>\nOpponent disconnected.");
        _ = MultiplayerWebSocketService.Instance.Disconnect();
    }

    // ── SUBMIT ANSWER ────────────────────────────────────────────────────────

    private void OnSubmitClicked()
    {
        string answer = answerInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(answer)) return;

        submitButton.interactable = false;
        feedbackText.text = "Checking...";

        float timeTaken = Time.time - _puzzleStartTime;
        _ = MultiplayerWebSocketService.Instance.SubmitAnswer(answer, timeTaken);
    }

    // ── QUIT ────────────────────────────────────────────────────────────────

    private void OnQuitClicked()
    {
        _ = MultiplayerWebSocketService.Instance.Disconnect();
        SceneManager.LoadScene("MainMenuScene");
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (mpStatusText != null) mpStatusText.text = msg;
        Debug.Log($"[MP] {msg}");
    }

    private void ShowResult(string msg)
    {
        if (mpResultPanel != null)
        {
            if (mpResultText != null) mpResultText.text = msg;
            mpResultPanel.SetActive(true);
        }
        else
        {
            // Fallback if no result panel is wired
            feedbackText.text = msg;
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe to avoid ghost callbacks
        if (MultiplayerWebSocketService.Instance != null)
        {
            MultiplayerWebSocketService.Instance.OnWaiting              -= SetStatus;
            MultiplayerWebSocketService.Instance.OnGameStart            -= OnGameStart;
            MultiplayerWebSocketService.Instance.OnAnswerResult         -= OnAnswerResult;
            MultiplayerWebSocketService.Instance.OnGameOver             -= OnGameOver;
            MultiplayerWebSocketService.Instance.OnOpponentDisconnected -= OnOpponentDisconnected;
        }
    }
}
