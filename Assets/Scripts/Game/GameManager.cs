using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameMode { Classic, TimeAttack, Survival }
    public GameMode currentMode;

    [Header("Common UI")]
    public TextMeshProUGUI scrambledWordText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI feedbackText;
    public TMP_InputField answerInputField;
    public Button submitButton;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public Button quitButton;

    [Header("Mode Specific Panels")]
    public GameObject classicPanel;
    public GameObject timeAttackPanel;
    public GameObject survivalPanel;

    [Header("Classic UI")]
    public TextMeshProUGUI classicScoreText;
    public Button classicNextButton;

    [Header("Time Attack UI")]
    public TextMeshProUGUI timeAttackScoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI comboText;

    [Header("Survival UI")]
    public TextMeshProUGUI survivalScoreText;
    public TextMeshProUGUI heartsText;

    [Header("Settings")]
    public int difficulty = 5;

    [Header("Progression HUD")]
    public TextMeshProUGUI xpText;         // e.g. "XP: 250 / 500"
    public TextMeshProUGUI coinsText;      // e.g. "💰 120"
    public TextMeshProUGUI levelText;      // e.g. "Lv.3"
    public GameObject levelUpPanel;  

    // State Variables
    private string currentPuzzleId;
    private float puzzleStartTime;
    private int totalScore = 0;
    
    // Time Attack specific
    private float sessionStartTime;
    private float sessionDuration = 60f;
    private int currentCombo = 1;
    
    // Survival specific
    private int currentHearts = 3;
    private float timeLimitPerWord = 30f;

    private bool isPlaying = false;
    private bool isWaitingForNetwork = false;

    private void Start()
    {
        // 1. Read which mode the user selected in the Main Menu
        string savedMode = PlayerPrefs.GetString("SelectedGameMode", "Classic");
        if (System.Enum.TryParse(savedMode, out GameMode parsedMode))
        {
            currentMode = parsedMode;
        }

        // 2. Setup initial UI state
        gameOverPanel.SetActive(false);
        submitButton.interactable = false;
        submitButton.onClick.AddListener(OnSubmitClicked);
        quitButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
        
        if(classicNextButton != null) 
            classicNextButton.onClick.AddListener(LoadPuzzle);

        SetupModePanels();
        UpdateProgressionHUD();
        
        // 3. Start the game loop
        isPlaying = true;
        if (currentMode == GameMode.TimeAttack) sessionStartTime = Time.time;
        
        LoadPuzzle();
    }

    private void Update()
    {
        if (!isPlaying || isWaitingForNetwork) return;

        if (currentMode == GameMode.TimeAttack)
        {
            float elapsed = Time.time - sessionStartTime;
            float remaining = sessionDuration - elapsed;

            if (remaining <= 0)
            {
                remaining = 0;
                EndGame();
            }

            timerText.text = $"⏳ {Mathf.CeilToInt(remaining)}s";
            timerText.color = remaining <= 10f ? Color.red : Color.white;
        }
        else if (currentMode == GameMode.Survival && !string.IsNullOrEmpty(currentPuzzleId))
        {
            float elapsed = Time.time - puzzleStartTime;
            if (elapsed >= timeLimitPerWord)
            {
                // Timeout in survival costs a heart
                submitButton.interactable = false;
                feedbackText.text = "⏰ Too slow!";
                SubmitAnswer(""); // Submit empty to trigger backend penalty
            }
        }
    }

    private void SetupModePanels()
    {
        classicPanel.SetActive(currentMode == GameMode.Classic);
        timeAttackPanel.SetActive(currentMode == GameMode.TimeAttack);
        survivalPanel.SetActive(currentMode == GameMode.Survival);

        UpdateScoreUI();
        if (currentMode == GameMode.TimeAttack) UpdateComboUI();
        if (currentMode == GameMode.Survival) UpdateHeartsUI();
    }

    // ─────────────────────────────────────────
    // FETCHING PUZZLES
    // ─────────────────────────────────────────
    private void LoadPuzzle()
    {
        if (!isPlaying || isWaitingForNetwork) return;
        
        isWaitingForNetwork = true;
        submitButton.interactable = false;
        if (classicNextButton != null) classicNextButton.interactable = false;
        
        scrambledWordText.text = "Loading...";
        hintText.text = "";
        answerInputField.text = "";
        currentPuzzleId = "";

        string userId = AuthManager.Instance?.CurrentUserUID ?? "guest";

        if (currentMode == GameMode.Classic)
        {
            var req = new NewPuzzleRequest { difficulty = difficulty, mode = "simple", user_id = userId };
            StartCoroutine(GameApiService.Instance.GetNewPuzzle(req, res => HandlePuzzleLoaded(res.puzzle_id, res.scrambled, res.hint), OnError));
        }
        else if (currentMode == GameMode.TimeAttack)
        {
            var req = new NewPuzzleRequest { difficulty = difficulty, mode = "time_attack", user_id = userId };
            StartCoroutine(GameApiService.Instance.GetNewTimeAttackPuzzle(req, res => {
                sessionDuration = res.session_duration;
                HandlePuzzleLoaded(res.puzzle_id, res.scrambled, res.hint);
            }, OnError));
        }
        else if (currentMode == GameMode.Survival)
        {
            var req = new NewPuzzleRequest { difficulty = difficulty, mode = "survival", user_id = userId };
            StartCoroutine(GameApiService.Instance.GetNewSurvivalPuzzle(req, res => {
                timeLimitPerWord = res.time_limit_per_word;
                if (currentHearts <= 0) currentHearts = res.starting_hearts;
                UpdateHeartsUI();
                HandlePuzzleLoaded(res.puzzle_id, res.scrambled, res.hint);
            }, OnError));
        }
    }

    private void HandlePuzzleLoaded(string pId, string scrambled, string hint)
    {
        isWaitingForNetwork = false;
        if (!isPlaying) return;

        currentPuzzleId = pId;
        scrambledWordText.text = scrambled.ToUpper();
        hintText.text = "Hint: " + hint;
        feedbackText.text = currentMode == GameMode.TimeAttack ? "Go!" : "Solve it!";

        puzzleStartTime = Time.time;
        submitButton.interactable = true;
        
        answerInputField.Select();
        answerInputField.ActivateInputField();
    }

    // ─────────────────────────────────────────
    // SUBMITTING ANSWERS
    // ─────────────────────────────────────────
    private void OnSubmitClicked()
    {
        string answer = answerInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(answer)) return;

        submitButton.interactable = false;
        feedbackText.text = "Checking...";
        SubmitAnswer(answer);
    }

    private void SubmitAnswer(string answer)
    {
        isWaitingForNetwork = true;
        float timeTaken = Time.time - puzzleStartTime;
        string userId = AuthManager.Instance?.CurrentUserUID ?? "guest";

        if (currentMode == GameMode.Classic)
        {
            var req = new SolveRequest { puzzle_id = currentPuzzleId, answer = answer, time_taken = timeTaken, user_id = userId };
            StartCoroutine(GameApiService.Instance.SolvePuzzle(req, res => {
                if (res.correct) totalScore += res.score;

                string fb = res.correct
                    ? $"<color=green>Correct!</color> +{res.score}"
                    : $"<color=red>Incorrect.</color> Word: {res.correct_answer}";

                if (res.correct && res.xp_earned > 0)
                    fb += $"\n<color=yellow>+{res.xp_earned} XP  💰+{res.coins_earned}</color>";

                feedbackText.text = fb;

                UpdateScoreUI();
                if (res.progression != null)
                    ShowProgressionFeedback(res.progression);

                if (classicNextButton != null) classicNextButton.interactable = true;
                isWaitingForNetwork = false;
            }, OnError));
        }
        else if (currentMode == GameMode.TimeAttack)
        {
            var req = new TimeAttackSolveRequest { puzzle_id = currentPuzzleId, answer = answer, time_taken = timeTaken, current_combo = currentCombo, session_elapsed = Time.time - sessionStartTime, user_id = userId };
            StartCoroutine(GameApiService.Instance.SolveTimeAttackPuzzle(req, res => {
                if (res.session_expired) { EndGame(); return; }
                currentCombo = res.new_combo;
                totalScore += res.score;
                feedbackText.text = res.correct ? $"<color=green>✅ +{res.score} {(currentCombo > 1 ? $"x{currentCombo}" : "")}</color>" : $"<color=red>❌ {res.correct_answer}</color> Combo reset!";
                UpdateScoreUI();
                UpdateComboUI();
                isWaitingForNetwork = false;
                Invoke(nameof(LoadPuzzle), 0.8f); // Auto advance
            }, OnError));
        }
        else if (currentMode == GameMode.Survival)
        {
            var req = new SurvivalSolveRequest { puzzle_id = currentPuzzleId, answer = answer, time_taken = timeTaken, current_hearts = currentHearts, user_id = userId };
            StartCoroutine(GameApiService.Instance.SolveSurvivalPuzzle(req, res => {
                currentHearts = res.hearts_remaining;
                totalScore += res.score;
                UpdateHeartsUI();
                UpdateScoreUI();
                
                if (res.game_over) { EndGame(); return; }
                
                feedbackText.text = res.correct ? $"<color=green>✅ +{res.score}</color>" : $"<color=red>❌ {res.correct_answer}</color>";
                isWaitingForNetwork = false;
                Invoke(nameof(LoadPuzzle), 1.0f); // Auto advance
            }, OnError));
        }
    }

    // ─────────────────────────────────────────
    // PROGRESSION FEEDBACK
    // ─────────────────────────────────────────
    
    /// <summary>
    /// Called after a correct solve to update the HUD with live XP/coin/level data.
    /// Also triggers the level-up panel if the player leveled up.
    /// </summary>
    private void ShowProgressionFeedback(ProgressionData progression)
    {
        // Update the live HUD
        UpdateProgressionHUD(progression.xp, progression.coins, progression.level, progression.xp_to_next_level);

        // Show level-up panel if player leveled up
        if (progression.leveled_up && levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
            // Auto-hide after 2.5 seconds
            Invoke(nameof(HideLevelUpPanel), 2.5f);
            Debug.Log($"🎉 Level Up! {progression.old_level} → {progression.level}");
        }
    }

    private void HideLevelUpPanel()
    {
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    /// <summary>
    /// Reads current values from AuthManager (on scene start) or takes live values from a solve response.
    /// </summary>
    private void UpdateProgressionHUD(int xp = -1, int coins = -1, int level = -1, int xpToNext = -1)
    {
        // If no live values passed, read from AuthManager cache (set at login)
        if (xp == -1 && AuthManager.Instance != null)
        {
            xp = AuthManager.Instance.CurrentXP;
            coins = AuthManager.Instance.CurrentCoins;
            level = AuthManager.Instance.CurrentLevel;
            xpToNext = AuthManager.Instance.CurrentXPToNext;
        }

        if (xpText != null)
            xpText.text = xpToNext > 0 ? $"XP: {xp} / {xpToNext}" : $"XP: {xp} (MAX)";

        if (coinsText != null)
            coinsText.text = $"💰 {coins}";

        if (levelText != null)
            levelText.text = $"Lv.{level}";
    }

    // ─────────────────────────────────────────
    // UI UPDATES & GAME OVER
    // ─────────────────────────────────────────
    private void OnError(string error)
    {
        isWaitingForNetwork = false;
        feedbackText.text = $"<color=red>Error:</color> {error}";
        submitButton.interactable = true;
    }

    private void EndGame()
    {
        isPlaying = false;
        submitButton.interactable = false;
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Game Over!\nFinal Score: {totalScore}";
        if(currentMode == GameMode.TimeAttack) timerText.text = "⏳ 0s";
    }

    private void UpdateScoreUI()
    {
        if (currentMode == GameMode.Classic && classicScoreText) classicScoreText.text = $"Score: {totalScore}";
        if (currentMode == GameMode.TimeAttack && timeAttackScoreText) timeAttackScoreText.text = $"Score: {totalScore}";
        if (currentMode == GameMode.Survival && survivalScoreText) survivalScoreText.text = $"Score: {totalScore}";
    }

    private void UpdateComboUI()
    {
        if (comboText) comboText.text = currentCombo > 1 ? $"🔥 x{currentCombo}" : "";
    }

    private void UpdateHeartsUI()
    {
        if (!heartsText) return;
        string h = "";
        for (int i = 0; i < currentHearts; i++) h += "❤️";
        for (int i = currentHearts; i < 3; i++) h += "🖤";
        heartsText.text = h;
    }
}
