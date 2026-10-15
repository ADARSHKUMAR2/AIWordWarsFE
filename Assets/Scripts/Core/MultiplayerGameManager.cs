using System.Threading.Tasks;
using System.Threading;
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
    public TextMeshProUGUI categoryText;

    [Header("Single-player panels to hide during multiplayer")]
    public GameObject classicPanel;
    public GameObject timeAttackPanel;
    public GameObject survivalPanel;
    public GameObject gameOverPanel;

    [Header("Multiplayer-specific UI")]
    public TextMeshProUGUI mpCountdownText;
    public TextMeshProUGUI mpStatusText;    // e.g. "Waiting for opponent..." / "Game ON!"
    public GameObject mpResultPanel;        // Shown at game end
    public TextMeshProUGUI mpResultText;    // Shows WIN / LOSE / DRAW message
    public Button mpPlayAgainButton;        // Returns to MainMenuScene

    // ── Internal state ───────────────────────────────────────────────────────
    private float _puzzleStartTime;
    private float _remainingQuestionTime;
    private bool _countdownActive;
    private bool _acceptingAnswers;
    private WsNextQuestion _pendingQuestion;
    private SynchronizationContext _unityContext;
    private string _currentUserId;

    private void Start()
    {
        _unityContext = SynchronizationContext.Current;
        bool isMultiplayer = PlayerPrefs.GetInt("mp_is_multiplayer", 0) == 1;

        if (!isMultiplayer)
        {
            // 🟢 FIX: Explicitly hide ALL multiplayer UI elements before disabling the manager
            if (mpResultPanel != null) mpResultPanel.SetActive(false);
            
            // Also hide the status text (and its parent if you placed it in a panel)
            if (mpStatusText != null) mpStatusText.gameObject.SetActive(false);
            if (mpCountdownText != null) mpCountdownText.gameObject.SetActive(false);
            
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
        _currentUserId = userId;
        
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
        MultiplayerWebSocketService.Instance.OnNextQuestion         += OnNextQuestion;
        MultiplayerWebSocketService.Instance.OnQuestionResult       += OnQuestionResult;
        MultiplayerWebSocketService.Instance.OnAnswerResult         += OnAnswerResult;
        MultiplayerWebSocketService.Instance.OnGameOver             += OnGameOver;
        MultiplayerWebSocketService.Instance.OnOpponentDisconnected += OnOpponentDisconnected;
        MultiplayerWebSocketService.Instance.OnError                += err => SetStatus($"<sprite name=\"Notification\"> {err}");

        await MultiplayerWebSocketService.Instance.Connect(wsUrl, userId);
        Debug.Log("[MP_GameManager] Await Connect finished.");
    }

    // ── WEBSOCKET EVENT HANDLERS ─────────────────────────────────────────────

    private void OnGameStart(WsGameStart data)
    {
        Debug.Log($"[MP_GameManager] OnGameStart event fired! Scrambled: {data.scrambled}");
        ShowQuestion(data);
    }

    private void OnNextQuestion(WsNextQuestion data)
    {
        _pendingQuestion = data;
        _ = ShowNextQuestionAfterResult();
    }

    private async Task ShowNextQuestionAfterResult()
    {
        await Task.Delay(1200);
        if (this == null || !isActiveAndEnabled || _pendingQuestion == null) return;
        WsNextQuestion question = _pendingQuestion;
        _pendingQuestion = null;
        _unityContext?.Post(_ =>
        {
            if (this != null && isActiveAndEnabled && question != null)
                ShowQuestion(question);
        }, null);
    }

    private void Update()
    {
        if (!_countdownActive || mpCountdownText == null) return;
        _remainingQuestionTime = Mathf.Max(0f, _remainingQuestionTime - Time.deltaTime);
        int seconds = Mathf.CeilToInt(_remainingQuestionTime);
        mpCountdownText.text = $"Time left: {seconds / 60:00}:{seconds % 60:00}";
        mpCountdownText.color = _remainingQuestionTime <= 10f ? Color.red : Color.black;
        if (_remainingQuestionTime <= 0f)
        {
            _countdownActive = false;
            _acceptingAnswers = false;
            submitButton.interactable = false;
        }
    }

    private void ShowQuestion(WsGameStart data)
    {
        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(data.category)
                ? "Category:"
                : $"Category: {data.category.ToUpperInvariant()}";
        scrambledWordText.text = data.scrambled.ToUpper();
        hintText.text = !string.IsNullOrEmpty(data.hint) ? "Hint: " + data.hint : "";
        feedbackText.text = "🆚 Solve it before your opponent!";
        string progress = $"Question {data.question_number}/{data.total_questions}  |  {FormatScores(data.score_entries)}";
        SetStatus(progress);

        _remainingQuestionTime = Mathf.Max(0f, data.time_limit);
        _countdownActive = _remainingQuestionTime > 0f;
        if (mpCountdownText != null)
        {
            mpCountdownText.gameObject.SetActive(true);
            mpCountdownText.color = Color.black;
            int seconds = Mathf.CeilToInt(_remainingQuestionTime);
            mpCountdownText.text = string.Format("Time left: {0:00}:{1:00}", seconds / 60, seconds % 60);
        }
        _puzzleStartTime = Time.time;
        _acceptingAnswers = true;
        submitButton.interactable = true;
        answerInputField.interactable = true;
        answerInputField.text = "";
        answerInputField.Select();
        answerInputField.ActivateInputField();
    }

    private void OnQuestionResult(WsQuestionResult data)
    {
        _acceptingAnswers = false;
        _countdownActive = false;
        submitButton.interactable = false;
        string winner = string.IsNullOrEmpty(data.winner_id)
            ? "No points this question."
            : data.winner_id == _currentUserId ? "You won this question!" : "Opponent won this question.";
        feedbackText.text = $"{winner} Answer: <b>{data.correct_word}</b>";
        SetStatus($"Question {data.question_number}/10  |  {FormatScores(data.score_entries)}");
    }

    private string FormatScores(WsPlayerScore[] scores)
    {
        if (scores == null || scores.Length == 0) return "Score 0 - 0";
        int mine = 0;
        int opponent = 0;
        foreach (WsPlayerScore score in scores)
        {
            if (score.user_id == _currentUserId) mine = score.score;
            else opponent = score.score;
        }
        return $"You {mine} - {opponent} Opponent";
    }

    private void OnAnswerResult(WsAnswerResult data)
    {
        if (!data.correct)
        {
            feedbackText.text = $"<color=red><sprite name=\"CrissCross\"> Incorrect! Try again.</color>";
            submitButton.interactable = true;
            answerInputField.text = "";
            answerInputField.Select();
            answerInputField.ActivateInputField();
        }
        // The server broadcasts question_result after a correct answer.
    }

    private void OnGameOver(WsGameOver data)
    {
        submitButton.interactable = false;
        answerInputField.interactable = false;
        _acceptingAnswers = false;
        _countdownActive = false;
        if (mpCountdownText != null) mpCountdownText.gameObject.SetActive(false);

        string resultMsg;
        string scoreSummary = FormatScores(data.score_entries);
        if (data.your_result == "win")
            resultMsg = $"<color=green><size=120%><b>🏆 YOU WIN THE MATCH!</b></size></color>\n{scoreSummary}";
        else if (data.your_result == "lose")
            resultMsg = $"<color=red><size=120%><b>😞 YOU LOSE THE MATCH</b></size></color>\n{scoreSummary}";
        else
            resultMsg = $"<color=yellow><size=120%><b><sprite name=\"hourglass\"> MATCH DRAW</b></size></color>\n{scoreSummary}";

        ShowResult(resultMsg);
        _ = MultiplayerWebSocketService.Instance.Disconnect();
    }

    private void OnOpponentDisconnected()
    {
        submitButton.interactable = false;
        _acceptingAnswers = false;
        _countdownActive = false;
        if (mpCountdownText != null) mpCountdownText.gameObject.SetActive(false);
        ShowResult("<color=green><size=120%><b>🏆 YOU WIN!</b></size></color>\nOpponent disconnected.");
        _ = MultiplayerWebSocketService.Instance.Disconnect();
    }

    // ── SUBMIT ANSWER ────────────────────────────────────────────────────────

    private void OnSubmitClicked()
    {
        if (!_acceptingAnswers) return;
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
            MultiplayerWebSocketService.Instance.OnNextQuestion         -= OnNextQuestion;
            MultiplayerWebSocketService.Instance.OnQuestionResult       -= OnQuestionResult;
            MultiplayerWebSocketService.Instance.OnAnswerResult         -= OnAnswerResult;
            MultiplayerWebSocketService.Instance.OnGameOver             -= OnGameOver;
            MultiplayerWebSocketService.Instance.OnOpponentDisconnected -= OnOpponentDisconnected;
        }
    }
}
