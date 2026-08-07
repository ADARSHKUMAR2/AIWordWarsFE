using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class MainMenuUIBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Main Menu UI")]
    public static void GenerateUI()
    {
        // 0. Ensure EventSystem exists
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // Check if project uses new Input System
            #if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            #endif
        }

        // 1. Create Canvas
        GameObject canvasGo = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. Safe Area
        GameObject safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        RectTransform safeAreaRect = safeArea.GetComponent<RectTransform>();
        safeAreaRect.anchorMin = Vector2.zero;
        safeAreaRect.anchorMax = Vector2.one;
        safeAreaRect.sizeDelta = Vector2.zero;
        safeArea.AddComponent<SafeArea>(); // Attach our runtime script!

        // 3. MainMenuPanel
        GameObject mainMenuPanel = CreateUIObject("MainMenuPanel", safeArea.transform);
        SetFullScreen(mainMenuPanel);
        Image bg = mainMenuPanel.AddComponent<Image>();
        bg.color = new Color(0.98f, 0.98f, 0.98f); // Off-white bg

        // 4. ScrollView
        GameObject scrollView = CreateUIObject("ScrollView", mainMenuPanel.transform);
        SetFullScreen(scrollView);
        // ScrollView needs an Image to detect raycasts for scrolling!
        scrollView.AddComponent<Image>().color = Color.clear;
        
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 50f; // Make scrolling smoother
        
        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        SetFullScreen(viewport);
        viewport.AddComponent<Image>().color = Color.white; // Unity Mask requires a solid alpha channel to make children visible!
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 2500); // Initial height
        
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(50, 50, 50, 300); // bottom padding for floating nav
        vlg.spacing = 60;
        vlg.childControlHeight = true; // This fixes the layout overlap and the red cross!
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Build Content inside ScrollView
        BuildTopBar(content.transform);
        BuildHeader(content.transform);
        Button dailyChallengeBtn = BuildDailyChallengeCard(content.transform);
        BuildPlayModesHeader(content.transform);
        Button quickPlayBtn = BuildModeCard(content.transform, "Quick Play", "Solo • 60s", new Color(1f, 0.8f, 0.8f));
        Button multiplayerBtn = BuildModeCard(content.transform, "Multiplayer", "1v1 Live", new Color(0.8f, 1f, 0.8f));
        Button leaderboardViewBtn = BuildLeaderboardPreview(content.transform);

        // 5. Floating Bottom Nav
        GameObject bottomNav = CreateUIObject("FloatingBottomNav", mainMenuPanel.transform);
        RectTransform navRect = bottomNav.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0);
        navRect.anchorMax = new Vector2(0.5f, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.anchoredPosition = new Vector2(0, 50);
        navRect.sizeDelta = new Vector2(900, 200);
        
        Image navBg = bottomNav.AddComponent<Image>();
        navBg.color = Color.white; // Pill background
        
        HorizontalLayoutGroup navHlg = bottomNav.AddComponent<HorizontalLayoutGroup>();
        navHlg.childAlignment = TextAnchor.MiddleCenter;
        navHlg.childControlWidth = true;
        navHlg.childControlHeight = true;
        navHlg.childForceExpandWidth = true;
        navHlg.childForceExpandHeight = true;
        navHlg.spacing = 20;
        navHlg.padding = new RectOffset(40, 40, 20, 20);

        Button homeBtn = BuildNavButton(bottomNav.transform, "Home");
        Button battleBtn = BuildNavButton(bottomNav.transform, "Battle");
        Button shopBtn = BuildNavButton(bottomNav.transform, "Shop");
        Button profileBtn = BuildNavButton(bottomNav.transform, "Profile");

        // 6. Controller Setup
        GameObject controllerGo = new GameObject("MainMenuManager");
        MainMenuController controller = controllerGo.AddComponent<MainMenuController>();
        
        // Link buttons
        controller.mainMenuPanel = mainMenuPanel;
        controller.dailyChallengeButton = dailyChallengeBtn;
        controller.dailyChallengeButtonText = dailyChallengeBtn.GetComponentInChildren<TextMeshProUGUI>();
        controller.classicModeButton = quickPlayBtn;
        controller.multiplayerButton = multiplayerBtn;
        controller.leaderboardButton = leaderboardViewBtn;
        
        Selection.activeGameObject = canvasGo;
        Debug.Log("Main Menu UI Generated Successfully! Check the scene for 'MainMenuCanvas'.");
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
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static void BuildTopBar(Transform parent)
    {
        GameObject topBar = CreateUIObject("TopBar", parent);
        topBar.AddComponent<LayoutElement>().minHeight = 150;
        HorizontalLayoutGroup hlg = topBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 30;
        hlg.childControlHeight = false;
        hlg.childControlWidth = false;

        GameObject profile = CreateUIObject("ProfilePic", topBar.transform);
        profile.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);
        profile.AddComponent<Image>().color = Color.gray;

        GameObject spacer = CreateUIObject("Spacer", topBar.transform);
        LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
        spacerLe.flexibleWidth = 1;

        GameObject coins = CreateUIObject("CoinsBadge", topBar.transform);
        coins.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 100);
        coins.AddComponent<Image>().color = Color.white;
        
        GameObject coinsText = CreateUIObject("CoinsText", coins.transform);
        SetFullScreen(coinsText);
        AddText(coinsText, "2.5k", 40, Color.black, TextAlignmentOptions.Center);

        GameObject gems = CreateUIObject("GemsBadge", topBar.transform);
        gems.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);
        gems.AddComponent<Image>().color = Color.white;
        
        GameObject gemsText = CreateUIObject("GemsText", gems.transform);
        SetFullScreen(gemsText);
        AddText(gemsText, "128", 40, Color.black, TextAlignmentOptions.Center);
    }

    private static void BuildHeader(Transform parent)
    {
        GameObject header = CreateUIObject("Header", parent);
        header.AddComponent<LayoutElement>().minHeight = 150;
        
        GameObject text1 = CreateUIObject("Greeting", header.transform);
        text1.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 50);
        text1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
        AddText(text1, "Good afternoon,", 45, Color.gray);

        GameObject text2 = CreateUIObject("Name", header.transform);
        text2.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 80);
        text2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
        AddText(text2, "Alex", 80, Color.black).fontStyle = FontStyles.Bold;

        GameObject rank = CreateUIObject("RankBadge", header.transform);
        RectTransform rankRt = rank.GetComponent<RectTransform>();
        rankRt.anchorMin = new Vector2(1, 0.5f);
        rankRt.anchorMax = new Vector2(1, 0.5f);
        rankRt.pivot = new Vector2(1, 0.5f);
        rankRt.anchoredPosition = new Vector2(0, 0);
        rankRt.sizeDelta = new Vector2(300, 90);
        rank.AddComponent<Image>().color = Color.white;
        
        GameObject rankText = CreateUIObject("RankText", rank.transform);
        SetFullScreen(rankText);
        AddText(rankText, "Rank #38", 45, Color.black, TextAlignmentOptions.Center);
    }

    private static Button BuildDailyChallengeCard(Transform parent)
    {
        GameObject card = CreateUIObject("DailyChallengeCard", parent);
        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 650;
        card.AddComponent<Image>().color = new Color(0.2f, 0.35f, 0.45f); // Dark blueish

        GameObject title = CreateUIObject("Title", card.transform);
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.offsetMin = new Vector2(60, 250); // Keep it above the button
        titleRt.offsetMax = new Vector2(-60, -60); // Keep it below the top edge
        
        TextMeshProUGUI tmp = AddText(title, "DAILY CHALLENGE\n<size=100><b>Guess Today's Word</b></size>", 45, Color.white);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.overflowMode = TextOverflowModes.Overflow; // Prevent cropping!

        GameObject btnGo = CreateUIObject("PlayButton", card.transform);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0, 0);
        btnRt.anchorMax = new Vector2(0, 0);
        btnRt.pivot = new Vector2(0, 0);
        btnRt.anchoredPosition = new Vector2(60, 60);
        btnRt.sizeDelta = new Vector2(450, 140);
        btnGo.AddComponent<Image>().color = new Color(1f, 0.4f, 0.3f); // Orangeish
        Button btn = btnGo.AddComponent<Button>();
        
        GameObject btnText = CreateUIObject("Text", btnGo.transform);
        SetFullScreen(btnText);
        AddText(btnText, "Play now", 55, Color.white, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        
        return btn;
    }

    private static void BuildPlayModesHeader(Transform parent)
    {
        GameObject header = CreateUIObject("PlayModesHeader", parent);
        LayoutElement le = header.AddComponent<LayoutElement>();
        le.minHeight = 80;

        GameObject title = CreateUIObject("Title", header.transform);
        SetFullScreen(title);
        AddText(title, "Play modes", 70, Color.black).fontStyle = FontStyles.Bold;

        GameObject seeAll = CreateUIObject("SeeAll", header.transform);
        RectTransform saRt = seeAll.GetComponent<RectTransform>();
        saRt.anchorMin = new Vector2(1, 0.5f);
        saRt.anchorMax = new Vector2(1, 0.5f);
        saRt.pivot = new Vector2(1, 0.5f);
        saRt.sizeDelta = new Vector2(200, 80);
        AddText(seeAll, "See all", 50, new Color(1f, 0.4f, 0.3f), TextAlignmentOptions.Right).fontStyle = FontStyles.Bold;
    }

    private static Button BuildModeCard(Transform parent, string title, string subtitle, Color iconColor)
    {
        GameObject card = CreateUIObject(title + "Card", parent);
        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 300;
        card.AddComponent<Image>().color = Color.white;
        Button btn = card.AddComponent<Button>();

        GameObject icon = CreateUIObject("Icon", card.transform);
        RectTransform iconRt = icon.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(160, 0);
        iconRt.sizeDelta = new Vector2(160, 160);
        icon.AddComponent<Image>().color = iconColor;

        GameObject textObj = CreateUIObject("Text", card.transform);
        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0, 0.5f);
        txtRt.anchorMax = new Vector2(1, 0.5f);
        txtRt.pivot = new Vector2(0, 0.5f);
        txtRt.offsetMin = new Vector2(380, -100);
        txtRt.offsetMax = new Vector2(-150, 100);
        AddText(textObj, $"<b>{title}</b>\n<size=45><color=#888888>{subtitle}</color></size>", 65, Color.black, TextAlignmentOptions.Left);

        return btn;
    }

    private static Button BuildLeaderboardPreview(Transform parent)
    {
        GameObject header = CreateUIObject("LeaderboardHeader", parent);
        LayoutElement leH = header.AddComponent<LayoutElement>();
        leH.minHeight = 80;

        GameObject title = CreateUIObject("Title", header.transform);
        SetFullScreen(title);
        AddText(title, "Weekly leaderboard", 70, Color.black).fontStyle = FontStyles.Bold;

        GameObject viewAll = CreateUIObject("View", header.transform);
        RectTransform saRt = viewAll.GetComponent<RectTransform>();
        saRt.anchorMin = new Vector2(1, 0.5f);
        saRt.anchorMax = new Vector2(1, 0.5f);
        saRt.pivot = new Vector2(1, 0.5f);
        saRt.sizeDelta = new Vector2(200, 80);
        Button btn = viewAll.AddComponent<Button>();
        AddText(viewAll, "View", 50, new Color(1f, 0.4f, 0.3f), TextAlignmentOptions.Right).fontStyle = FontStyles.Bold;
        
        GameObject card = CreateUIObject("LeaderboardCard", parent);
        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 500;
        card.AddComponent<Image>().color = Color.white;
        
        return btn;
    }

    private static Button BuildNavButton(Transform parent, string label)
    {
        GameObject btnGo = CreateUIObject(label + "Btn", parent);
        Button btn = btnGo.AddComponent<Button>();
        
        VerticalLayoutGroup vlg = btnGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        
        GameObject icon = CreateUIObject("Icon", btnGo.transform);
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
        icon.AddComponent<Image>().color = Color.gray;

        GameObject text = CreateUIObject("Label", btnGo.transform);
        AddText(text, label, 40, Color.black, TextAlignmentOptions.Center);

        return btn;
    }
}