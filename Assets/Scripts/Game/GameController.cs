using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scrambledWordText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public TMP_InputField answerInputField;
    public Button submitButton;
    public Button nextPuzzleButton;

    [Header("Game Settings")]
    public int currentDifficulty = 5;
    public string gameMode = "simple";

    // Internal State
    private string currentPuzzleId;
    private float puzzleStartTime;
    private int totalScore = 0;

    private void Start()
    {
        // Disable buttons until the first puzzle is fully loaded from the backend
        submitButton.interactable = false;
        nextPuzzleButton.interactable = false;

        // Setup button click events
        submitButton.onClick.AddListener(OnSubmitClicked);
        nextPuzzleButton.onClick.AddListener(LoadNewPuzzle);

        // Update initial score UI
        UpdateScoreUI();

        // Automatically fetch the first puzzle
        LoadNewPuzzle();
    }

    public void LoadNewPuzzle()
    {
        feedbackText.text = "Loading puzzle...";
        scrambledWordText.text = "";
        hintText.text = "";
        answerInputField.text = "";
        
        submitButton.interactable = false;
        nextPuzzleButton.interactable = false;

        // Prepare request data
        GameApiService.NewPuzzleRequest request = new GameApiService.NewPuzzleRequest
        {
            difficulty = currentDifficulty,
            mode = gameMode,
            category = "" // Leave empty for random category
        };

        // Call the backend API
        StartCoroutine(GameApiService.Instance.GetNewPuzzle(request, OnPuzzleLoaded, OnError));
    }

    private void OnPuzzleLoaded(GameApiService.PuzzleResponse response)
    {
        currentPuzzleId = response.puzzle_id;
        scrambledWordText.text = response.scrambled.ToUpper();
        hintText.text = "Hint: " + response.hint;
        feedbackText.text = "Can you guess the word?";
        
        puzzleStartTime = Time.time;
        submitButton.interactable = true;
        
        // Automatically select the input field so the user can start typing immediately
        answerInputField.Select();
        answerInputField.ActivateInputField();
    }

    private void OnSubmitClicked()
    {
        string playerAnswer = answerInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(playerAnswer)) return;

        submitButton.interactable = false;
        feedbackText.text = "Checking answer...";

        float timeTaken = Time.time - puzzleStartTime;
        
        // Prepare submit data
        GameApiService.SolveRequest request = new GameApiService.SolveRequest
        {
            puzzle_id = currentPuzzleId,
            answer = playerAnswer,
            time_taken = timeTaken,
            // TODO: If you are saving user login data (e.g. in AuthManager), replace "guest" with their real UID.
            user_id = "guest" 
        };

        // Call the backend API
        StartCoroutine(GameApiService.Instance.SolvePuzzle(request, OnAnswerResult, OnError));
    }

    private void OnAnswerResult(GameApiService.SolveResponse response)
    {
        if (response.correct)
        {
            feedbackText.text = $"<color=green>Correct!</color> +{response.score} points";
            totalScore += response.score;
            UpdateScoreUI();
        }
        else
        {
            feedbackText.text = $"<color=red>Incorrect.</color> The word was: {response.correct_answer}";
        }

        // Enable the Next button so the player can proceed
        nextPuzzleButton.interactable = true;
    }

    private void OnError(string errorMsg)
    {
        feedbackText.text = $"<color=red>Network Error:</color> {errorMsg}";
        
        // Re-enable buttons so the user can try again
        submitButton.interactable = true;
        nextPuzzleButton.interactable = true;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }
    }
}
