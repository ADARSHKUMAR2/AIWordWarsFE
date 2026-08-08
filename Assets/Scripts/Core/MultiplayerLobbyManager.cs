using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordWars.Models;

/// <summary>
/// Controls the Multiplayer Lobby PANEL inside MainMenuScene.
///
/// Inspector wiring (all inside your MultiplayerPanel GameObject):
///   MainMenuController    → MainMenuController (the parent controller, for Back button)
///
///   [Main sub-panel]
///   CreateRoomButton      → Button
///   JoinRoomButton        → Button  (shows the Join sub-panel)
///   BackButton            → Button  (returns to main menu)
///
///   [Create sub-panel]
///   RoomCodeDisplay       → TextMeshProUGUI  (shows generated code like "AB12CD")
///   CreateStatusText      → TextMeshProUGUI  (shows "Waiting for opponent...")
///   DifficultySlider      → Slider  (optional, 1-10)
///   CancelCreateButton    → Button  (goes back to lobby main panel)
///
///   [Join sub-panel]
///   JoinRoomInput         → TMP_InputField  (player types the code)
///   ConfirmJoinButton     → Button
///   JoinStatusText        → TextMeshProUGUI
///   CancelJoinButton      → Button  (goes back to lobby main panel)
/// </summary>
public class MultiplayerLobbyManager : MonoBehaviour
{
    [Header("Back to Main Menu")]
    public MainMenuController mainMenuController;

    [Header("Sub-panels")]
    public GameObject lobbyMainPanel;    // Shows Create / Join options
    public GameObject createPanel;       // Shows room code + waiting status
    public GameObject joinPanel;         // Shows input field to enter code

    [Header("Lobby Main Panel UI")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button backButton;

    [Header("Create Panel UI")]
    public TextMeshProUGUI roomCodeDisplay;
    public TextMeshProUGUI createStatusText;
    public Slider difficultySlider;
    public Button cancelCreateButton;

    [Header("Join Panel UI")]
    public TMP_InputField joinRoomInput;
    public Button confirmJoinButton;
    public TextMeshProUGUI joinStatusText;
    public Button cancelJoinButton;

    // Internal state
    private string _currentRoomCode;
    private bool _isPolling = false;

    private void Awake()
    {
        // Wire buttons
        if (createRoomButton) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRoomButton)   joinRoomButton.onClick.AddListener(ShowJoinPanel);
        if (backButton)       backButton.onClick.AddListener(OnBackClicked);

        if (cancelCreateButton) cancelCreateButton.onClick.AddListener(OnCancelCreate);
        if (cancelJoinButton)   cancelJoinButton.onClick.AddListener(ShowLobbyMain);
        if (confirmJoinButton)  confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);

        if (difficultySlider != null)
        {
            difficultySlider.minValue = 1;
            difficultySlider.maxValue = 10;
            difficultySlider.value = 5;
        }
    }

    // ── CALLED BY MainMenuController WHEN PANEL OPENS ────────────────────────

    public void ResetToMainPanel()
    {
        _isPolling = false;
        _currentRoomCode = null;
        if (joinRoomInput) joinRoomInput.text = "";
        ShowLobbyMain();
    }

    // ── NAVIGATION ────────────────────────────────────────────────────────────

    private void ShowLobbyMain()
    {
        lobbyMainPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
    }

    private void ShowJoinPanel()
    {
        lobbyMainPanel.SetActive(false);
        createPanel.SetActive(false);
        joinPanel.SetActive(true);
        SetJoinStatus("");
    }

    private void OnBackClicked()
    {
        _isPolling = false;
        if (mainMenuController != null)
            mainMenuController.CloseMultiplayerLobby();
    }

    private void OnCancelCreate()
    {
        Debug.Log("[Lobby] Cancel Create clicked.");
        _isPolling = false;
        ShowLobbyMain();
    }

    // ── CREATE ROOM ──────────────────────────────────────────────────────────

    private void OnCreateRoomClicked()
    {
        string userId = AuthManager.Instance?.CurrentUserUID;
        Debug.Log($"[Lobby] OnCreateRoomClicked. UserID: {userId}");

        if (string.IsNullOrEmpty(userId)) { SetCreateStatus("Error: Not logged in!"); return; }

        lobbyMainPanel.SetActive(false);
        createPanel.SetActive(true);
        joinPanel.SetActive(false);

        int difficulty = difficultySlider != null ? (int)difficultySlider.value : 5;
        roomCodeDisplay.text = "Creating...";
        SetCreateStatus("Please wait...");
        createRoomButton.interactable = false;

        Debug.Log($"[Lobby] Calling API to create room. Diff: {difficulty}");

        StartCoroutine(MultiplayerApiService.Instance.CreateRoom(
            userId,
            difficulty,
            onSuccess: (response) =>
            {
                Debug.Log($"[Lobby] Create Room SUCCESS. Code: {response.room_code}");
                _currentRoomCode = response.room_code;
                roomCodeDisplay.text = $"<size=200%><b>{response.room_code}</b></size>";
                SetCreateStatus("Share this code with your opponent.\nWaiting for them to join...");
                createRoomButton.interactable = true;

                if (!_isPolling)
                {
                    Debug.Log($"[Lobby] Starting poll for room: {response.room_code}");
                    _isPolling = true;
                    StartCoroutine(PollForOpponent(response.room_code));
                }
            },
            onError: (err) =>
            {
                SetCreateStatus($"Failed to create room: {err}");
                createRoomButton.interactable = true;
                ShowLobbyMain();
            }
        ));
    }
    private IEnumerator PollForOpponent(string roomCode)
    {
        Debug.Log($"[Lobby] Polling started for {roomCode}");
        
        while (_isPolling)
        {
            Debug.Log($"[Lobby] Polling /api/match/{roomCode}...");
            yield return StartCoroutine(MultiplayerApiService.Instance.PollRoomStatus(
                roomCode,
                onSuccess: (status) =>
                {
                    Debug.Log($"[Lobby] Poll success. Raw status: '{status.status}'");
                    string cleanStatus = status.status.Trim().ToLower();
                    
                    if (cleanStatus == "ready")
                    {
                        Debug.Log($"[Lobby] 🟢 ROOM READY! Host is preparing to load scene...");
                        _isPolling = false;
                        SetCreateStatus("Opponent joined! Starting game...");
                        
                        string wsUrl = $"{AppConfig.Instance.WsUrl}/ws/{roomCode}";

                        PlayerPrefs.SetString("mp_ws_url", wsUrl);
                        PlayerPrefs.SetString("mp_room_code", roomCode);
                        PlayerPrefs.Save();
                        
                        Debug.Log($"[Lobby] Host saved prefs. WS: {wsUrl}. Invoking GoToGame_Delayed in 0.8s");
                        Invoke(nameof(GoToGame_Delayed), 0.8f);
                    }
                },
                onError: (err) =>
                {
                    Debug.LogError($"[Lobby] Poll failed: {err}");
                    _isPolling = false;
                    SetCreateStatus("Room expired. Please try again.");
                    Invoke(nameof(ShowLobbyMain), 2f);
                }
            ));

            // Wait 0.5s AFTER the HTTP request finishes before polling again
            if (_isPolling)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        Debug.Log($"[Lobby] Polling stopped for {roomCode}");
    }

    // ── JOIN ROOM ────────────────────────────────────────────────────────────

    private void OnConfirmJoinClicked()
    {
        string code = joinRoomInput.text.Trim().ToUpper();
        Debug.Log($"[Lobby] OnConfirmJoinClicked. Code entered: {code}");

        if (string.IsNullOrEmpty(code) || code.Length < 4)
        {
            SetJoinStatus("Please enter a valid room code.");
            return;
        }

        string userId = AuthManager.Instance?.CurrentUserUID;
        if (string.IsNullOrEmpty(userId)) { SetJoinStatus("Error: Not logged in!"); return; }

        SetJoinStatus("Joining room...");
        confirmJoinButton.interactable = false;

        Debug.Log($"[Lobby] Calling API to join room {code}...");

        StartCoroutine(MultiplayerApiService.Instance.JoinRoom(
            code,
            userId,
            onSuccess: (response) =>
            {
                Debug.Log($"[Lobby] 🟢 Join Room SUCCESS. WS URL: {response.ws_url}");
                
                SetJoinStatus("Joined! Connecting to game...");
                PlayerPrefs.SetString("mp_ws_url", response.ws_url);
                PlayerPrefs.SetString("mp_room_code", response.room_code);
                PlayerPrefs.Save();
                Invoke(nameof(GoToGame_Delayed), 0.8f);
            },
            onError: (err) =>
            {
                Debug.LogError($"[Lobby] Join Room FAILED: {err}");
                SetJoinStatus($"Failed to join: {err}");
                confirmJoinButton.interactable = true;
            }
        ));
    }

    // ── START GAME ───────────────────────────────────────────────────────────

    // Called via Invoke after a short delay so the player sees "Starting..." message
    private void GoToGame_Delayed()
    {
        Debug.Log("[Lobby] GoToGame_Delayed executed! Setting PlayerPrefs and loading GameplayScene...");
        PlayerPrefs.SetString("SelectedGameMode", "Multiplayer");
        PlayerPrefs.SetInt("mp_is_multiplayer", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameplayScene");
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private void SetCreateStatus(string msg)
    {
        if (createStatusText != null) createStatusText.text = msg;
    }

    private void SetJoinStatus(string msg)
    {
        if (joinStatusText != null) joinStatusText.text = msg;
    }

    private void OnDestroy()
    {
        _isPolling = false;
    }
}
