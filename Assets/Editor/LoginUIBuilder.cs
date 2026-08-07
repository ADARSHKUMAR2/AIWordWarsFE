using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class LoginUIBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Login UI")]
    public static void GenerateUI()
    {
        // 0. Ensure EventSystem exists
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
        GameObject canvasGo = new GameObject("LoginCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. Safe Area & Deep Blue/Navy Background
        GameObject safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        SetFullScreen(safeArea);
        safeArea.AddComponent<SafeArea>();

        GameObject bgObj = CreateUIObject("Background", safeArea.transform);
        SetFullScreen(bgObj);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.15f); // Rich dark navy blue

        // 3. Title with Drop Shadow
        GameObject titleObj = CreateUIObject("Title", safeArea.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -250); // Move down a bit
        titleRt.sizeDelta = new Vector2(900, 200);
        
        TextMeshProUGUI titleText = AddText(titleObj, "AI Word Wars", 100, new Color(0.98f, 0.8f, 0.2f), TextAlignmentOptions.Center); // Vibrant Gold
        titleText.fontStyle = FontStyles.Bold;
        
        // Add shadow to text
        Shadow textShadow = titleObj.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0, 0, 0, 0.8f);
        textShadow.effectDistance = new Vector2(5, -5);

        // Subtitle
        GameObject subtitleObj = CreateUIObject("Subtitle", titleObj.transform);
        RectTransform subRt = subtitleObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0, 0);
        subRt.anchorMax = new Vector2(1, 0);
        subRt.pivot = new Vector2(0.5f, 1);
        subRt.anchoredPosition = new Vector2(0, -20);
        subRt.sizeDelta = new Vector2(0, 100);
        TextMeshProUGUI subText = AddText(subtitleObj, "Master the grid. Beat the AI.", 45, new Color(0.6f, 0.7f, 0.8f), TextAlignmentOptions.Center);
        subText.fontStyle = FontStyles.Italic;

        // 4. Main Panel (Frosted Glass/Semi-transparent Dark Box)
        GameObject panelObj = CreateUIObject("LoginPanel", safeArea.transform);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = new Vector2(0, -150);
        panelRt.sizeDelta = new Vector2(900, 1100);
        
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f); // Dark translucent blue

        // Drop shadow for the panel
        Shadow panelShadow = panelObj.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0, 0, 0, 0.5f);
        panelShadow.effectDistance = new Vector2(0, -10);

        // Layout group for the panel
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(80, 80, 80, 80);
        vlg.spacing = 50;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        // 5. Build Form Elements
        TMP_InputField emailInput = BuildInputField(panelObj.transform, "EmailInput", "Email Address");
        TMP_InputField passwordInput = BuildInputField(panelObj.transform, "PasswordInput", "Password", true);
        
        CreateSpacer(panelObj.transform, 20);

        // Buttons
        Button loginBtn = BuildButton(panelObj.transform, "LoginButton", "LOGIN", 740, 140, new Color(1f, 0.4f, 0.3f), Color.white); // Orange play button color
        Button registerBtn = BuildButton(panelObj.transform, "RegisterButton", "CREATE ACCOUNT", 740, 140, new Color(0.2f, 0.4f, 0.8f), Color.white); // Blue secondary button

        CreateSpacer(panelObj.transform, 10);

        // Status Text
        GameObject statusObj = CreateUIObject("StatusText", panelObj.transform);
        statusObj.GetComponent<RectTransform>().sizeDelta = new Vector2(740, 80);
        TextMeshProUGUI statusText = AddText(statusObj, "", 45, new Color(1f, 0.3f, 0.3f), TextAlignmentOptions.Center);
        statusText.fontStyle = FontStyles.Bold;
        
        CreateSpacer(panelObj.transform, 10);

        // Google Button
        Button googleBtn = BuildButton(panelObj.transform, "GoogleLoginButton", "G  SIGN IN WITH GOOGLE", 740, 130, Color.white, new Color(0.3f, 0.3f, 0.3f));
        // Add outline to make white button pop on dark bg
        Outline gOutline = googleBtn.gameObject.AddComponent<Outline>();
        gOutline.effectColor = new Color(0.8f, 0.8f, 0.8f);
        gOutline.effectDistance = new Vector2(2, -2);

        // 6. Controller Setup
        GameObject controllerGo = new GameObject("LoginManager");
        LoginUI controller = controllerGo.AddComponent<LoginUI>();
        
        // Link references
        controller.emailInput = emailInput;
        controller.passwordInput = passwordInput;
        controller.loginButton = loginBtn;
        controller.registerButton = registerBtn;
        controller.googleLoginButton = googleBtn;
        controller.statusText = statusText;
        
        Selection.activeGameObject = canvasGo;
        Debug.Log("Beautiful Login UI Generated Successfully! Check the scene for 'LoginCanvas'.");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
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

    private static TextMeshProUGUI AddText(GameObject go, string text, int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static TMP_InputField BuildInputField(Transform parent, string name, string placeholderText, bool isPassword = false)
    {
        GameObject inputObj = CreateUIObject(name, parent);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(740, 140);
        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.1f, 0.15f); // Very dark inset background
        
        // Subtle outline for input
        Outline outline = inputObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.4f, 0.6f, 0.5f);
        outline.effectDistance = new Vector2(2, -2);

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Text Area
        GameObject textArea = CreateUIObject("Text Area", inputObj.transform);
        RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.sizeDelta = new Vector2(-60, -20); // generous padding
        textAreaRt.anchoredPosition = Vector2.zero;
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject placeholder = CreateUIObject("Placeholder", textArea.transform);
        SetFullScreen(placeholder);
        TextMeshProUGUI pText = AddText(placeholder, placeholderText, 45, new Color(0.4f, 0.5f, 0.6f), TextAlignmentOptions.Left);
        
        // Input Text
        GameObject textObj = CreateUIObject("Text", textArea.transform);
        SetFullScreen(textObj);
        TextMeshProUGUI tText = AddText(textObj, "", 45, Color.white, TextAlignmentOptions.Left); // White typed text

        inputField.textViewport = textAreaRt;
        inputField.textComponent = tText;
        inputField.placeholder = pText;
        inputField.caretColor = Color.white; // Make caret visible

        if (isPassword)
        {
            inputField.inputType = TMP_InputField.InputType.Password;
        }

        return inputField;
    }

    private static Button BuildButton(Transform parent, string name, string label, float width, float height, Color bgColor, Color textColor)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        // Shadow for 3D feel
        Shadow shadow = btnObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.3f);
        shadow.effectDistance = new Vector2(0, -6);

        Button btn = btnObj.AddComponent<Button>();
        
        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        SetFullScreen(textObj);
        TextMeshProUGUI tmp = AddText(textObj, label, 45, textColor, TextAlignmentOptions.Center);
        tmp.fontStyle = FontStyles.Bold;
        
        return btn;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateUIObject("Spacer", parent);
        spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(100, height);
    }
}