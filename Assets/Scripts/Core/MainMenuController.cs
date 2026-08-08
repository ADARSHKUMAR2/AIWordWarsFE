using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DateTime = System.DateTime;
public class MainMenuController : MonoBehaviour
{
    [Header("Mode Buttons")]
    public Button classicModeButton;
    public Button timeAttackButton;
    public Button survivalButton;
    public Button multiplayerButton; 
    
    [Header("Other UI")]
    public Button logoutButton;
    public Button leaderboardButton; 

    [Header("Panels")]
    public GameObject mainMenuPanel;    
    public GameObject leaderboardPanel;
    public GameObject multiplayerPanel;

    [Header("Leaderboard")]
    public LeaderboardManager leaderboardManager;
    [Header("Multiplayer")]
    public MultiplayerLobbyManager multiplayerLobbyManager;

    [Header("Daily Challenge UI")]
    public Button dailyChallengeButton;
    public TextMeshProUGUI dailyChallengeButtonText; 

    [Header("Profile UI")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI rankText;

    private void Start()
    {
        UpdateProfileUI();
        
        classicModeButton.onClick.AddListener(() => StartGameMode("Classic"));
        timeAttackButton.onClick.AddListener(() => StartGameMode("TimeAttack"));
        survivalButton.onClick.AddListener(() => StartGameMode("Survival"));
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
        
        if (multiplayerButton != null)
            multiplayerButton.onClick.AddListener(OpenMultiplayerLobby);
        
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(OpenLeaderboard);


        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (multiplayerPanel != null)
            multiplayerPanel.SetActive(false);  

         // Setup Daily Challenge Button
        if (dailyChallengeButton != null)
        {
            CheckDailyChallengeStatus();
            dailyChallengeButton.onClick.AddListener(() => StartGameMode("DailyChallenge"));
        }
        
    }

    private void UpdateProfileUI()
    {
        if (AuthManager.Instance != null)
        {
            if (coinsText != null) coinsText.text = $"<sprite name=\"coin\">{AuthManager.Instance.CurrentCoins}";
            if (xpText != null) xpText.text = $"<sprite name=\"gem\">{AuthManager.Instance.CurrentXP}";
            if (levelText != null) levelText.text = $"<sprite name=\"badge\">{AuthManager.Instance.CurrentLevel}";
        }
        FetchAndDisplayRank();
    }

    private void FetchAndDisplayRank()
    {
        if (rankText == null) return;
        
        string uid = AuthManager.Instance.CurrentUserUID;
        if (string.IsNullOrEmpty(uid))
        {
            rankText.text = "Rank: -";
            return;
        }

        rankText.text = "Rank: ..."; // Loading state

        // Using "global" board and "simple" mode as defaults. Change if needed.
        StartCoroutine(LeaderboardApiService.Instance.GetPlayerRank("global", uid, "simple", 
            onSuccess: (rankResponse) => 
            {
                if (rankResponse != null)
                {
                    rankText.text = $"Rank: {rankResponse.rank}";
                }
                else
                {
                    // This happens if the API returns 404 (user has no score yet)
                    rankText.text = "Rank: Unranked";
                }
            }, 
            onError: (errorMsg) => 
            {
                Debug.LogWarning($"Failed to get rank for main menu: {errorMsg}");
                rankText.text = "Rank: N/A";
            }
        ));
    }

    private void CheckDailyChallengeStatus()
    {
        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastPlayed = PlayerPrefs.GetString("LastDailyChallengeCompletedDate", "");

        if (todayDate == lastPlayed)
        {
            dailyChallengeButton.interactable = false;
            if (dailyChallengeButtonText != null) dailyChallengeButtonText.text = "Come back tomorrow!";
        }
        else
        {
            dailyChallengeButton.interactable = true;
            if (dailyChallengeButtonText != null) dailyChallengeButtonText.text = "Play Daily Challenge";
        }
    }


    private void StartGameMode(string modeName)
    {
        Debug.Log($"Selected Mode: {modeName}. Loading GameScene...");
        
        // Save the chosen mode so GameManager knows which panels to activate
        PlayerPrefs.SetString("SelectedGameMode", modeName);
        PlayerPrefs.Save();
        
        // Always load the single unified GameScene
        SceneManager.LoadScene("GameplayScene"); 
    }

    private void OpenLeaderboard()
    {
        // Hide main menu, show leaderboard panel
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);

        // Trigger a fresh data load every time it's opened
        if (leaderboardManager != null)
            leaderboardManager.LoadLeaderboard();
    }

    public void CloseLeaderboard()
    {
        // Show main menu, hide leaderboard panel
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        //testing ad display on leaderboard close
        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowInterstitialAd();
        }

    }

    // ── MULTIPLAYER LOBBY
    private void OpenMultiplayerLobby()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (multiplayerPanel != null) multiplayerPanel.SetActive(true);

        // Reset the lobby UI to its default state every time it's opened
        if (multiplayerLobbyManager != null)
            multiplayerLobbyManager.ResetToMainPanel();
    }

    public void CloseMultiplayerLobby()
    {
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    private void OnLogoutClicked()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.SignOut();
        }
        
        // Send them back to the Login screen
        SceneManager.LoadScene("LoginScene"); 
    }
}
