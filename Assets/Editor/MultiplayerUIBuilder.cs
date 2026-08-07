using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUIBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Multiplayer UI")]
    public static void GenerateUI()
    {
        // 1. Find or create MainMenuCanvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.name != "MainMenuCanvas")
        {
            Debug.LogError("Please run 'Generate Main Menu UI' first, or ensure MainMenuCanvas is in the scene.");
            return;
        }

        Transform safeArea = canvas.transform.Find("SafeArea");
        if (safeArea == null) safeArea = canvas.transform;

        // 2. Main Multiplayer Panel container
        GameObject rootPanel = CreateUIObject("MultiplayerPanel", safeArea);
        SetFullScreen(rootPanel);
        Image bg = rootPanel.AddComponent<Image>();
        bg.color = new Color(0.98f, 0.98f, 0.98f); // Off-white bg
        rootPanel.SetActive(false); // Hide by default

        // 3. Lobby Main Panel
        GameObject lobbyMainPanel = BuildLobbyMainPanel(rootPanel.transform);
        
        // 4. Create Panel
        GameObject createPanel = BuildCreatePanel(rootPanel.transform);
        createPanel.SetActive(false);

        // 5. Join Panel
        GameObject joinPanel = BuildJoinPanel(rootPanel.transform);
        joinPanel.SetActive(false);

        // 6. Connect to MultiplayerLobbyManager & MainMenuController
        MainMenuController mainController = Object.FindFirstObjectByType<MainMenuController>();
        
        MultiplayerLobbyManager mpManager = rootPanel.AddComponent<MultiplayerLobbyManager>();
        mpManager.mainMenuController = mainController;
        
        // Panels
        mpManager.lobbyMainPanel = lobbyMainPanel;
        mpManager.createPanel = createPanel;
        mpManager.joinPanel = joinPanel;

        // Lobby Main references
        mpManager.createRoomButton = lobbyMainPanel.transform.Find("CreateRoomButton").GetComponent<Button>();
        mpManager.joinRoomButton = lobbyMainPanel.transform.Find("JoinRoomButton").GetComponent<Button>();
        mpManager.backButton = lobbyMainPanel.transform.Find("Header/BackButton").GetComponent<Button>();

        // Create Panel references
        mpManager.roomCodeDisplay = createPanel.transform.Find("RoomCodeDisplay").GetComponent<TextMeshProUGUI>();
        mpManager.createStatusText = createPanel.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
        mpManager.difficultySlider = createPanel.transform.Find("DifficultySlider").GetComponent<Slider>();
        mpManager.cancelCreateButton = createPanel.transform.Find("Header/CancelButton").GetComponent<Button>();

        // Join Panel references
        mpManager.joinRoomInput = joinPanel.transform.Find("JoinInput").GetComponent<TMP_InputField>();
        mpManager.confirmJoinButton = joinPanel.transform.Find("ConfirmButton").GetComponent<Button>();
        mpManager.joinStatusText = joinPanel.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
        mpManager.cancelJoinButton = joinPanel.transform.Find("Header/CancelButton").GetComponent<Button>();

        if (mainController != null)
        {
            mainController.multiplayerPanel = rootPanel;
            mainController.multiplayerLobbyManager = mpManager;
        }

        Selection.activeGameObject = rootPanel;
        Debug.Log("Multiplayer UI Generated Successfully!");
    }

    private static GameObject BuildLobbyMainPanel(Transform parent)
    {
        GameObject panel = CreateUIObject("LobbyMainPanel", parent);
        SetFullScreen(panel);

        // Header
        GameObject header = BuildHeader(panel.transform, "Multiplayer Lobby", "BackButton", "Back");

        // Layout container for buttons
        GameObject container = CreateUIObject("Container", panel.transform);
        RectTransform cRt = container.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(800, 600);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 80;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;

        BuildBigButton(container.transform, "CreateRoomButton", "Create Room", new Color(0.8f, 1f, 0.8f));
        BuildBigButton(container.transform, "JoinRoomButton", "Join Room", new Color(1f, 0.8f, 0.8f));

        return panel;
    }

    private static GameObject BuildCreatePanel(Transform parent)
    {
        GameObject panel = CreateUIObject("CreatePanel", parent);
        SetFullScreen(panel);

        BuildHeader(panel.transform, "Create Room", "CancelButton", "Cancel");

        // Container
        GameObject container = CreateUIObject("Container", panel.transform);
        RectTransform cRt = container.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = new Vector2(0, 100);
        cRt.sizeDelta = new Vector2(800, 800);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 50;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // Room Code Label
        GameObject codeLabelObj = CreateUIObject("CodeLabel", container.transform);
        codeLabelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 60);
        AddText(codeLabelObj, "Your Room Code:", 50, Color.gray, TextAlignmentOptions.Center);

        // Room Code Display
        GameObject codeObj = CreateUIObject("RoomCodeDisplay", container.transform);
        codeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 150);
        TextMeshProUGUI codeTmp = AddText(codeObj, "----", 120, Color.black, TextAlignmentOptions.Center);
        codeTmp.fontStyle = FontStyles.Bold;

        // Spacer
        GameObject spacer = CreateUIObject("Spacer", container.transform);
        spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

        // Difficulty Label
        GameObject diffLabel = CreateUIObject("DiffLabel", container.transform);
        diffLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 60);
        AddText(diffLabel, "Difficulty Level:", 50, Color.gray, TextAlignmentOptions.Center);

        // Slider
        GameObject sliderObj = CreateUIObject("DifficultySlider", container.transform);
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);
        Slider slider = sliderObj.AddComponent<Slider>();
        
        GameObject bgObj = CreateUIObject("Background", sliderObj.transform);
        SetFullScreen(bgObj);
        bgObj.AddComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
        
        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
        SetFullScreen(fillArea);
        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        SetFullScreen(fill);
        fill.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
        SetFullScreen(handleArea);
        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        handle.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 0);
        handle.AddComponent<Image>().color = Color.white;

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.minValue = 1;
        slider.maxValue = 10;
        slider.wholeNumbers = true;
        slider.value = 5;

        // Status Text
        GameObject statusObj = CreateUIObject("StatusText", container.transform);
        statusObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
        AddText(statusObj, "Creating room...", 45, Color.black, TextAlignmentOptions.Center);

        return panel;
    }

    private static GameObject BuildJoinPanel(Transform parent)
    {
        GameObject panel = CreateUIObject("JoinPanel", parent);
        SetFullScreen(panel);

        BuildHeader(panel.transform, "Join Room", "CancelButton", "Cancel");

        // Container
        GameObject container = CreateUIObject("Container", panel.transform);
        RectTransform cRt = container.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = new Vector2(0, 100);
        cRt.sizeDelta = new Vector2(800, 800);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 80;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // Label
        GameObject labelObj = CreateUIObject("Label", container.transform);
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 60);
        AddText(labelObj, "Enter Room Code:", 50, Color.gray, TextAlignmentOptions.Center);

        // Input Field
        TMP_InputField input = BuildInputField(container.transform, "JoinInput", "e.g. AB12CD");
        input.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);

        // Join Button
        BuildBigButton(container.transform, "ConfirmButton", "Join", new Color(1f, 0.8f, 0.8f));

        // Status Text
        GameObject statusObj = CreateUIObject("StatusText", container.transform);
        statusObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
        AddText(statusObj, "", 45, Color.red, TextAlignmentOptions.Center);

        return panel;
    }

    private static GameObject BuildHeader(Transform parent, string titleStr, string btnName, string btnText)
    {
        GameObject header = CreateUIObject("Header", parent);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0, 200);
        
        GameObject titleObj = CreateUIObject("Title", header.transform);
        SetFullScreen(titleObj);
        TextMeshProUGUI title = AddText(titleObj, titleStr, 70, Color.black, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;

        GameObject btnObj = CreateUIObject(btnName, header.transform);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0, 0.5f);
        btnRt.anchorMax = new Vector2(0, 0.5f);
        btnRt.pivot = new Vector2(0, 0.5f);
        btnRt.anchoredPosition = new Vector2(150, 0);
        btnRt.sizeDelta = new Vector2(200, 100);
        btnObj.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
        btnObj.AddComponent<Button>();

        GameObject txtObj = CreateUIObject("Text", btnObj.transform);
        SetFullScreen(txtObj);
        AddText(txtObj, btnText, 35, Color.black, TextAlignmentOptions.Center);

        return header;
    }

    private static Button BuildBigButton(Transform parent, string name, string label, Color color)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
        btnObj.AddComponent<Image>().color = color;
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = CreateUIObject("Text", btnObj.transform);
        SetFullScreen(txtObj);
        TextMeshProUGUI tmp = AddText(txtObj, label, 55, Color.black, TextAlignmentOptions.Center);
        tmp.fontStyle = FontStyles.Bold;

        return btn;
    }

    private static TMP_InputField BuildInputField(Transform parent, string name, string placeholderText)
    {
        GameObject inputObj = CreateUIObject(name, parent);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
        inputObj.AddComponent<Image>().color = Color.white;
        
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

        inputField.textViewport = textAreaRt;
        inputField.textComponent = tText;
        inputField.placeholder = pText;

        return inputField;
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
}