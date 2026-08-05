using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Mode Buttons")]
    public Button classicModeButton;
    public Button timeAttackButton;
    public Button survivalButton;
    
    [Header("Other UI")]
    public Button logoutButton;
    public Button leaderboardButton; 

    [Header("Panels")]
    public GameObject mainMenuPanel;    
    public GameObject leaderboardPanel;
    [Header("Leaderboard")]
    public LeaderboardManager leaderboardManager;

    private void Start()
    {
        classicModeButton.onClick.AddListener(() => StartGameMode("Classic"));
        timeAttackButton.onClick.AddListener(() => StartGameMode("TimeAttack"));
        survivalButton.onClick.AddListener(() => StartGameMode("Survival"));
        
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
        
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(OpenLeaderboard);


        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
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
