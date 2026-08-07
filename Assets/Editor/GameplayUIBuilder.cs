using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class GameplayUIBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Gameplay UI")]
    public static void GenerateUI()
    {
        // 0. Ensure EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            #if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            #endif
        }

        // 1. Create Canvas
        GameObject canvasGo = new GameObject("GameplayCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. Safe Area & Background
        GameObject safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        SetFullScreen(safeArea);
        safeArea.AddComponent<SafeArea>();

        GameObject bgObj = CreateUIObject("Background", safeArea.transform);
        SetFullScreen(bgObj);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.95f, 0.96f, 0.98f); // Soft light blueish-grey background

        // --- HUD / Top Bar ---
        GameObject topBar = CreateUIObject("TopBar", safeArea.transform);
        RectTransform topBarRt = topBar.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 1);
        topBarRt.anchorMax = new Vector2(1, 1);
        topBarRt.pivot = new Vector2(0.5f, 1);
        topBarRt.anchoredPosition = Vector2.zero;
        topBarRt.sizeDelta = new Vector2(0, 150);
        
        Button quitBtn = BuildButton(topBar.transform, "QuitButton", "Quit", 200, 100);
        quitBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        quitBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
        quitBtn.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        quitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 0);

        // Progression HUD
        GameObject progHud = CreateUIObject("ProgressionHUDPanel", topBar.transform);
        RectTransform progRt = progHud.GetComponent<RectTransform>();
        progRt.anchorMin = new Vector2(1, 0.5f);
        progRt.anchorMax = new Vector2(1, 0.5f);
        progRt.pivot = new Vector2(1, 0.5f);
        progRt.anchoredPosition = new Vector2(-50, 0);
        progRt.sizeDelta = new Vector2(600, 100);
        
        HorizontalLayoutGroup progHlg = progHud.AddComponent<HorizontalLayoutGroup>();
        progHlg.childAlignment = TextAnchor.MiddleRight;
        progHlg.spacing = 30;
        progHlg.childControlWidth = false;
        progHlg.childControlHeight = false;

        TextMeshProUGUI levelText = AddText(CreateUIObject("LevelsText", progHud.transform), "Lv.3", 45, Color.black, TextAlignmentOptions.Right);
        levelText.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 80);
        levelText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI xpText = AddText(CreateUIObject("XpText", progHud.transform), "XP: 250/500", 40, Color.black, TextAlignmentOptions.Right);
        xpText.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 80);

        TextMeshProUGUI coinsText = AddText(CreateUIObject("CoinsText", progHud.transform), "120 Coins", 45, new Color(0.8f, 0.6f, 0f), TextAlignmentOptions.Right);
        coinsText.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 80);
        coinsText.fontStyle = FontStyles.Bold;

        // --- Mode Specific Panels (Header area below TopBar) ---
        GameObject modeContainer = CreateUIObject("ModeContainer", safeArea.transform);
        RectTransform mcRt = modeContainer.GetComponent<RectTransform>();
        mcRt.anchorMin = new Vector2(0, 1);
        mcRt.anchorMax = new Vector2(1, 1);
        mcRt.pivot = new Vector2(0.5f, 1);
        mcRt.anchoredPosition = new Vector2(0, -180);
        mcRt.sizeDelta = new Vector2(0, 150);

        // Classic Panel
        GameObject classicPanel = CreateUIObject("ClassicPanel", modeContainer.transform);
        SetFullScreen(classicPanel);
        TextMeshProUGUI classicScore = AddText(CreateUIObject("ScoreText", classicPanel.transform), "Score: 0", 60, Color.black, TextAlignmentOptions.Center);
        SetFullScreen(classicScore.gameObject);
        classicScore.fontStyle = FontStyles.Bold;
        Button classicNextBtn = BuildButton(classicPanel.transform, "NextButton", "Next >", 250, 100);
        classicNextBtn.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
        classicNextBtn.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        classicNextBtn.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
        classicNextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, 0);

        // Time Attack Panel
        GameObject timeAttackPanel = CreateUIObject("TimeAttackPanel", modeContainer.transform);
        SetFullScreen(timeAttackPanel);
        TextMeshProUGUI taScore = AddText(CreateUIObject("ScoreText", timeAttackPanel.transform), "Score: 0", 50, Color.black, TextAlignmentOptions.Left);
        taScore.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        taScore.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
        taScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 0);
        taScore.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 80);
        
        TextMeshProUGUI taTimer = AddText(CreateUIObject("TimerText", timeAttackPanel.transform), "01:00", 70, Color.red, TextAlignmentOptions.Center);
        SetFullScreen(taTimer.gameObject);
        taTimer.fontStyle = FontStyles.Bold;
        
        TextMeshProUGUI taCombo = AddText(CreateUIObject("ComboText", timeAttackPanel.transform), "Combo x1", 50, new Color(1f, 0.5f, 0f), TextAlignmentOptions.Right);
        taCombo.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
        taCombo.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        taCombo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, 0);
        taCombo.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 80);

        // Survival Panel
        GameObject survivalPanel = CreateUIObject("SurvivalPanel", modeContainer.transform);
        SetFullScreen(survivalPanel);
        TextMeshProUGUI survScore = AddText(CreateUIObject("ScoreText", survivalPanel.transform), "Score: 0", 60, Color.black, TextAlignmentOptions.Left);
        survScore.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        survScore.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
        survScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 0);
        survScore.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
        
        TextMeshProUGUI survHearts = AddText(CreateUIObject("HeartsText", survivalPanel.transform), "❤️❤️❤️", 60, Color.red, TextAlignmentOptions.Right);
        survHearts.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
        survHearts.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        survHearts.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, 0);
        survHearts.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);

        // --- Multiplayer Specific Panel ---
        GameObject multiplayerPanel = CreateUIObject("MultiplayerPanel", modeContainer.transform);
        SetFullScreen(multiplayerPanel);
        
        TextMeshProUGUI mpStatus = AddText(CreateUIObject("MpStatusText", multiplayerPanel.transform), "Waiting for opponent...", 50, Color.black, TextAlignmentOptions.Center);
        mpStatus.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        mpStatus.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        mpStatus.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        mpStatus.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
        mpStatus.fontStyle = FontStyles.Bold;

        // --- Common Panel (Main Game Area) ---
        GameObject commonPanel = CreateUIObject("CommonPanel", safeArea.transform);
        RectTransform commonRt = commonPanel.GetComponent<RectTransform>();
        commonRt.anchorMin = new Vector2(0.5f, 0.5f);
        commonRt.anchorMax = new Vector2(0.5f, 0.5f);
        commonRt.pivot = new Vector2(0.5f, 0.5f);
        commonRt.anchoredPosition = new Vector2(0, -100);
        commonRt.sizeDelta = new Vector2(900, 1000);
        
        Image cpBg = commonPanel.AddComponent<Image>();
        cpBg.color = Color.white; // White card for main gameplay

        VerticalLayoutGroup cpVlg = commonPanel.AddComponent<VerticalLayoutGroup>();
        cpVlg.padding = new RectOffset(50, 50, 80, 50);
        cpVlg.spacing = 50;
        cpVlg.childAlignment = TextAnchor.UpperCenter;
        cpVlg.childControlWidth = true;
        cpVlg.childControlHeight = false;

        TextMeshProUGUI catText = AddText(CreateUIObject("CategoryText", commonPanel.transform), "Category: ANIMALS", 45, Color.gray, TextAlignmentOptions.Center);
        catText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 60);

        TextMeshProUGUI scramText = AddText(CreateUIObject("ScrambledWordText", commonPanel.transform), "G T I R E", 130, Color.black, TextAlignmentOptions.Center);
        scramText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 180);
        scramText.fontStyle = FontStyles.Bold;
        scramText.characterSpacing = 20;

        TextMeshProUGUI hintText = AddText(CreateUIObject("HintText", commonPanel.transform), "Hint: Has stripes.", 45, new Color(0.3f, 0.6f, 0.9f), TextAlignmentOptions.Center);
        hintText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
        hintText.fontStyle = FontStyles.Italic;

        TMP_InputField answerInput = BuildInputField(commonPanel.transform, "AnswerInputField", "Type answer...");
        answerInput.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 150);

        Button submitBtn = BuildBigButton(commonPanel.transform, "SubmitButton", "Submit", new Color(1f, 0.4f, 0.3f));
        submitBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 150);

        TextMeshProUGUI feedbackText = AddText(CreateUIObject("FeedbackText", commonPanel.transform), "", 50, Color.red, TextAlignmentOptions.Center);
        feedbackText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
        feedbackText.fontStyle = FontStyles.Bold;

        // --- Overlays ---
        // Game Over Panel
        GameObject goPanel = CreateUIObject("GameOverPanel", safeArea.transform);
        SetFullScreen(goPanel);
        goPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        
        GameObject goBox = CreateUIObject("Box", goPanel.transform);
        goBox.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
        goBox.AddComponent<Image>().color = Color.white;
        VerticalLayoutGroup goVlg = goBox.AddComponent<VerticalLayoutGroup>();
        goVlg.childAlignment = TextAnchor.MiddleCenter;
        goVlg.spacing = 50;

        AddText(CreateUIObject("Title", goBox.transform), "Game Over!", 80, Color.black, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        TextMeshProUGUI finalScoreText = AddText(CreateUIObject("FinalScoreText", goBox.transform), "Final Score: 1500", 60, new Color(0.2f, 0.6f, 1f), TextAlignmentOptions.Center);
        Button returnMainBtn = BuildBigButton(goBox.transform, "ReturnButton", "Main Menu", new Color(0.9f, 0.9f, 0.9f));
        
        // Level Up Panel
        GameObject luPanel = CreateUIObject("LevelUpPanel", safeArea.transform);
        SetFullScreen(luPanel);
        luPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        GameObject luBox = CreateUIObject("Box", luPanel.transform);
        luBox.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 500);
        luBox.AddComponent<Image>().color = Color.white;
        VerticalLayoutGroup luVlg = luBox.AddComponent<VerticalLayoutGroup>();
        luVlg.childAlignment = TextAnchor.MiddleCenter;
        luVlg.spacing = 50;
        AddText(CreateUIObject("Title", luBox.transform), "Level Up!", 90, new Color(1f, 0.8f, 0f), TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        Button luCloseBtn = BuildBigButton(luBox.transform, "CloseButton", "Awesome!", new Color(0.9f, 0.9f, 0.9f));

        // Multiplayer Result Panel
        GameObject mpResultPanel = CreateUIObject("MultiplayerResultPanel", safeArea.transform);
        SetFullScreen(mpResultPanel);
        mpResultPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        GameObject mpResultBox = CreateUIObject("Box", mpResultPanel.transform);
        mpResultBox.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
        mpResultBox.AddComponent<Image>().color = Color.white;
        VerticalLayoutGroup mpVlg = mpResultBox.AddComponent<VerticalLayoutGroup>();
        mpVlg.childAlignment = TextAnchor.MiddleCenter;
        mpVlg.spacing = 50;
        AddText(CreateUIObject("Title", mpResultBox.transform), "Match Finished", 80, Color.black, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        TextMeshProUGUI mpResultText = AddText(CreateUIObject("MpResultText", mpResultBox.transform), "You Won!", 70, new Color(0.2f, 0.8f, 0.2f), TextAlignmentOptions.Center);
        Button mpPlayAgainBtn = BuildBigButton(mpResultBox.transform, "PlayAgainButton", "Play Again", new Color(0.9f, 0.9f, 0.9f));

        goPanel.SetActive(false);
        luPanel.SetActive(false);
        mpResultPanel.SetActive(false);
        timeAttackPanel.SetActive(false);
        survivalPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        classicPanel.SetActive(true); // Default to classic

        // --- Wiring ---
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
        }

        gm.scrambledWordText = scramText;
        gm.hintText = hintText;
        gm.feedbackText = feedbackText;
        gm.answerInputField = answerInput;
        gm.submitButton = submitBtn;
        gm.gameOverPanel = goPanel;
        gm.finalScoreText = finalScoreText;
        gm.quitButton = quitBtn;
        
        gm.classicPanel = classicPanel;
        gm.timeAttackPanel = timeAttackPanel;
        gm.survivalPanel = survivalPanel;
        
        gm.classicScoreText = classicScore;
        gm.classicNextButton = classicNextBtn;
        
        gm.timeAttackScoreText = taScore;
        gm.timerText = taTimer;
        gm.comboText = taCombo;
        
        gm.survivalScoreText = survScore;
        gm.heartsText = survHearts;
        
        gm.xpText = xpText;
        gm.coinsText = coinsText;
        gm.levelText = levelText;
        gm.levelUpPanel = luPanel;

        // Wire MultiplayerGameManager
        MultiplayerGameManager mpManager = Object.FindFirstObjectByType<MultiplayerGameManager>();
        if (mpManager == null)
        {
            GameObject mpObj = new GameObject("MultiplayerGameManager");
            mpManager = mpObj.AddComponent<MultiplayerGameManager>();
        }

        mpManager.scrambledWordText = scramText;
        mpManager.hintText = hintText;
        mpManager.feedbackText = feedbackText;
        mpManager.answerInputField = answerInput;
        mpManager.submitButton = submitBtn;
        mpManager.quitButton = quitBtn;

        mpManager.classicPanel = classicPanel;
        mpManager.timeAttackPanel = timeAttackPanel;
        mpManager.survivalPanel = survivalPanel;
        mpManager.gameOverPanel = goPanel;

        mpManager.mpStatusText = mpStatus;
        mpManager.mpResultPanel = mpResultPanel;
        mpManager.mpResultText = mpResultText;
        mpManager.mpPlayAgainButton = mpPlayAgainBtn;

        Selection.activeGameObject = canvasGo;
        Debug.Log("Gameplay UI Generated Successfully! Check 'GameplayCanvas'.");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetFullScreen(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static TextMeshProUGUI AddText(GameObject go, string text, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static Button BuildButton(Transform parent, string name, string label, float width, float height)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        btnObj.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
        
        Button btn = btnObj.AddComponent<Button>();
        
        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        SetFullScreen(textObj);
        AddText(textObj, label, 35, Color.black, TextAlignmentOptions.Center);
        
        return btn;
    }

    private static Button BuildBigButton(Transform parent, string name, string label, Color color)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
        btnObj.AddComponent<Image>().color = color;
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = CreateUIObject("Text", btnObj.transform);
        SetFullScreen(txtObj);
        TextMeshProUGUI tmp = AddText(txtObj, label, 55, Color.white, TextAlignmentOptions.Center);
        tmp.fontStyle = FontStyles.Bold;

        return btn;
    }

    private static TMP_InputField BuildInputField(Transform parent, string name, string placeholderText)
    {
        GameObject inputObj = CreateUIObject(name, parent);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
        inputObj.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        GameObject textArea = CreateUIObject("Text Area", inputObj.transform);
        RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.sizeDelta = new Vector2(-40, -20);
        textAreaRt.anchoredPosition = Vector2.zero;
        textArea.AddComponent<RectMask2D>();

        GameObject placeholder = CreateUIObject("Placeholder", textArea.transform);
        SetFullScreen(placeholder);
        TextMeshProUGUI pText = AddText(placeholder, placeholderText, 60, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        pText.fontStyle = FontStyles.Italic;
        
        GameObject textObj = CreateUIObject("Text", textArea.transform);
        SetFullScreen(textObj);
        TextMeshProUGUI tText = AddText(textObj, "", 60, Color.black, TextAlignmentOptions.Center);
        tText.fontStyle = FontStyles.Bold;

        inputField.textViewport = textAreaRt;
        inputField.textComponent = tText;
        inputField.placeholder = pText;

        return inputField;
    }
}