using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class DailyChallengeManager : MonoBehaviour
{
    [Header("Reuse Gameplay UI")]
    public GameObject dailyChallengePanel; 
    public TextMeshProUGUI scrambledWordText;
    public TextMeshProUGUI hintText;
    public TMP_InputField answerInputField;
    public Button submitButton;
    public Button quitButton;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI categoryText; // Add a text field for category somewhere

    private DailyChallengeData currentChallenge;

    private void Start()
    {
        // Only run if the selected mode is DailyChallenge
        string savedMode = PlayerPrefs.GetString("SelectedGameMode", "Classic");
        if (savedMode != "DailyChallenge")
        {
            // 🟢 FIX: Hide the daily challenge panel if we aren't playing it
            if (dailyChallengePanel != null) dailyChallengePanel.SetActive(false);
            this.enabled = false;
            return;
        }

        // Setup UI
        if (dailyChallengePanel != null) dailyChallengePanel.SetActive(true);
        submitButton.onClick.AddListener(OnSubmitAnswer);
        quitButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        LoadDailyChallenge();
    }

    private void LoadDailyChallenge()
    {
        submitButton.interactable = false;
        scrambledWordText.text = "Loading...";
        if (hintText != null) hintText.text = "";
        feedbackText.text = "";
        
        StartCoroutine(DailyChallengeApiService.Instance.GetTodaysChallenge(
            onSuccess: (challengeData) => 
            {
                currentChallenge = challengeData;
                scrambledWordText.text = currentChallenge.scrambled.ToUpper();
                if (hintText != null) hintText.text = "Hint: " + currentChallenge.hint;
                if (categoryText != null) categoryText.text = "Category: " + currentChallenge.category;
                submitButton.interactable = true;
            },
            onError: (errorMsg) => 
            {
                scrambledWordText.text = "Failed to load.";
                feedbackText.text = errorMsg;
            }
        ));
    }

    private void OnSubmitAnswer()
    {
        if (currentChallenge == null) return;

        string playerAnswer = answerInputField.text.Trim().ToUpper();
        string correctAnswer = currentChallenge.word.ToUpper();

        if (playerAnswer == correctAnswer)
        {
            // Win
            feedbackText.color = Color.green;
            feedbackText.text = "Correct! +50 Coins!";
            submitButton.interactable = false;
            
            // Save completion status so they can't play again today
            PlayerPrefs.SetString("LastDailyChallengeCompletedDate", currentChallenge.date);
            PlayerPrefs.Save();

            // Give rewards via ShopApiService (if implemented)
            
            // Return to main menu after delay
            Invoke(nameof(ReturnToMenu), 2.5f);
        }
        else
        {
            // Wrong
            feedbackText.color = Color.red;
            feedbackText.text = "Incorrect! Try again.";
            answerInputField.text = ""; 
        }
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
