using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        statusText.text = "";
    }

    private void OnLoginClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please enter email and password", Color.red);
            return;
        }

        ShowStatus("Logging in...", Color.yellow);
        loginButton.interactable = false;
        registerButton.interactable = false;

        AuthManager.Instance.SignInWithEmail(email, password, OnLoginSuccess, OnLoginError);
    }

    private void OnRegisterClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please enter email and password", Color.red);
            return;
        }

        if (password.Length < 6)
        {
            ShowStatus("Password must be at least 6 characters", Color.red);
            return;
        }

        ShowStatus("Registering...", Color.yellow);
        loginButton.interactable = false;
        registerButton.interactable = false;

        AuthManager.Instance.RegisterWithEmail(email, password, OnLoginSuccess, OnLoginError);
    }

    private void OnLoginSuccess(ApiService.UserProfile profile)
    {
        ShowStatus($"Welcome, {profile.display_name ?? profile.email}!", Color.green);
        Debug.Log($"User Profile: Coins={profile.coins}, XP={profile.xp}, Level={profile.level}");
        
        // Navigate to main menu after 1 second
        Invoke(nameof(LoadMainMenu), 1f);
    }

    private void OnLoginError(string error)
    {
        ShowStatus($"Login failed: {error}", Color.red);
        loginButton.interactable = true;
        registerButton.interactable = true;
    }

    private void ShowStatus(string message, Color color)
    {
        statusText.text = message;
        statusText.color = color;
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
