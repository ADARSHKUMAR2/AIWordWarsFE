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

        // 2. Safe Area & Background
        GameObject safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        SetFullScreen(safeArea);
        safeArea.AddComponent<SafeArea>();

        GameObject bgObj = CreateUIObject("Background", safeArea.transform);
        SetFullScreen(bgObj);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.95f, 0.95f, 0.95f); // Very light grey bg

        // 3. Title
        GameObject titleObj = CreateUIObject("Title", safeArea.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -150);
        titleRt.sizeDelta = new Vector2(800, 150);
        TextMeshProUGUI titleText = AddText(titleObj, "Ai Word Wars", 70, Color.black, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Normal; // Based on screenshot

        // 4. Main Panel (Grey Box)
        GameObject panelObj = CreateUIObject("LoginPanel", safeArea.transform);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = new Vector2(0, -50);
        panelRt.sizeDelta = new Vector2(800, 1300);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.9f, 0.9f, 0.9f); // Slightly darker grey for the container

        // Layout group for the panel
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(100, 100, 100, 100);
        vlg.spacing = 60;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        // 5. Build Form Elements
        TMP_InputField emailInput = BuildInputField(panelObj.transform, "EmailInput", "Enter email....");
        TMP_InputField passwordInput = BuildInputField(panelObj.transform, "PasswordInput", "Enter password....", true);
        
        // Add a bit of extra spacing before buttons
        CreateSpacer(panelObj.transform, 40);

        Button loginBtn = BuildButton(panelObj.transform, "LoginButton", "Login", 350, 120);
        Button registerBtn = BuildButton(panelObj.transform, "RegisterButton", "Register", 350, 120);

        CreateSpacer(panelObj.transform, 20);

        // Status Text
        GameObject statusObj = CreateUIObject("StatusText", panelObj.transform);
        statusObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
        TextMeshProUGUI statusText = AddText(statusObj, "Status???", 50, Color.white, TextAlignmentOptions.Center);
        
        CreateSpacer(panelObj.transform, 20);

        Button googleBtn = BuildButton(panelObj.transform, "GoogleLoginButton", "Sign In With Google", 450, 120);

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
        Debug.Log("Login UI Generated Successfully! Check the scene for 'LoginCanvas'.");
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
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
        inputObj.AddComponent<Image>().color = Color.white;
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Text Area (where text is actually typed)
        GameObject textArea = CreateUIObject("Text Area", inputObj.transform);
        RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.sizeDelta = new Vector2(-40, -20); // padding
        textAreaRt.anchoredPosition = Vector2.zero;
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject placeholder = CreateUIObject("Placeholder", textArea.transform);
        SetFullScreen(placeholder);
        TextMeshProUGUI pText = AddText(placeholder, placeholderText, 45, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
        pText.fontStyle = FontStyles.Italic;
        
        // Input Text
        GameObject textObj = CreateUIObject("Text", textArea.transform);
        SetFullScreen(textObj);
        TextMeshProUGUI tText = AddText(textObj, "", 45, Color.black, TextAlignmentOptions.Left);

        inputField.textViewport = textAreaRt;
        inputField.textComponent = tText;
        inputField.placeholder = pText;

        if (isPassword)
        {
            inputField.inputType = TMP_InputField.InputType.Password;
        }

        return inputField;
    }

    private static Button BuildButton(Transform parent, string name, string label, float width, float height)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        btnObj.AddComponent<Image>().color = Color.white;
        
        Button btn = btnObj.AddComponent<Button>();
        
        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        SetFullScreen(textObj);
        AddText(textObj, label, 35, Color.black, TextAlignmentOptions.Center);
        
        return btn;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateUIObject("Spacer", parent);
        spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(100, height);
    }
}