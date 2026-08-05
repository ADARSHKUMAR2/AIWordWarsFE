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

    private void Start()
    {
        classicModeButton.onClick.AddListener(() => StartGameMode("Classic"));
        timeAttackButton.onClick.AddListener(() => StartGameMode("TimeAttack"));
        survivalButton.onClick.AddListener(() => StartGameMode("Survival"));
        
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
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

    private void OnLogoutClicked()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.SignOut();
        }
        
        // Send them back to the Login screen
        SceneManager.LoadScene("LoginScene"); // Make sure this matches your login scene name!
    }
}
