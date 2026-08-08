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
            // Hide the daily challenge panel ONLY if it is not sharing a panel with GameManager
            GameManager gm = FindObjectOfType<GameManager>();
            bool isShared = false;
            
            if (gm != null && dailyChallengePanel != null)
            {
                if (dailyChallengePanel == gm.classicPanel || 
                    dailyChallengePanel == gm.timeAttackPanel || 
                    dailyChallengePanel == gm.survivalPanel)
                {
                    isShared = true;
                }
            }

            // Only disable it if it's a dedicated Daily Challenge panel
            if (!isShared && dailyChallengePanel != null)
            {
                dailyChallengePanel.SetActive(false);
            }
            
            // NOTE: Removed the gm.classicNextButton hiding from here!
            
            this.enabled = false;
            return;
        }

        // Setup UI
        if (dailyChallengePanel != null) dailyChallengePanel.SetActive(true);
        
        // NEW: Hide the Next button if it exists in the shared panel
        GameManager gmRef = FindObjectOfType<GameManager>();
        if (gmRef != null && gmRef.classicNextButton != null)
        {
            gmRef.classicNextButton.gameObject.SetActive(false);
        }

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
