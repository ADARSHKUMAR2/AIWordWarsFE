using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button playButton;
    public Button quitButton;

    private void Start()
    {
        // Automatically wire up the button clicks if they are assigned in the Inspector
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void StartGame()
    {
        Debug.Log("Loading Gameplay Scene...");
        // This must match the exact name of your gameplay scene file.
        // Also, ensure BOTH scenes are added to Unity's "Build Settings"
        SceneManager.LoadScene("GameplayScene"); 
    }

    public void QuitGame()
    {
        Debug.Log("Quitting application...");
        
        // This will quit the built application. 
        // Note: Application.Quit() does not stop the Unity Editor play mode.
        Application.Quit();
    }
}
