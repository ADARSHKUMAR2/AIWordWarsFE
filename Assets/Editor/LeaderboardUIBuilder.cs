using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUIBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Leaderboard UI")]
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

        // 2. Leaderboard Panel
        GameObject panelObj = CreateUIObject("LeaderboardPanel", safeArea);
        SetFullScreen(panelObj);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.98f, 0.98f, 0.98f); // Off-white bg
        
        // Ensure it starts hidden
        panelObj.SetActive(false);

        // 3. Top Header Bar
        GameObject header = CreateUIObject("Header", panelObj.transform);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0, 200);
        
        GameObject titleObj = CreateUIObject("Title", header.transform);
        SetFullScreen(titleObj);
        TextMeshProUGUI title = AddText(titleObj, "Weekly leaderboard", 70, Color.black, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;

        Button backBtn = BuildButton(header.transform, "BackButton", "Back", 200, 100);
        RectTransform backRt = backBtn.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0, 0.5f);
        backRt.anchorMax = new Vector2(0, 0.5f);
        backRt.pivot = new Vector2(0, 0.5f);
        backRt.anchoredPosition = new Vector2(150, 0);

        // 4. ScrollView for the List
        GameObject scrollView = CreateUIObject("ScrollView", panelObj.transform);
        RectTransform scrollRt = scrollView.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(50, 50); // Left, Bottom padding
        scrollRt.offsetMax = new Vector2(-50, -200); // Right, Top padding (leave room for header)
        
        scrollView.AddComponent<Image>().color = Color.clear;
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 50f;

        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        SetFullScreen(viewport);
        viewport.AddComponent<Image>().color = Color.white;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 30;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // 5. Generate a Prefab for LeaderboardRow
        GameObject rowPrefab = BuildLeaderboardRow();

        // 6. Connect to MainMenuController
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
        if (controller != null)
        {
            controller.leaderboardPanel = panelObj;
            
            // Automatically add back button listener
            UnityEditor.Events.UnityEventTools.AddPersistentListener(backBtn.onClick, controller.CloseLeaderboard);
            
            // Generate some dummy rows so it looks like the screenshot
            for (int i = 1; i <= 5; i++)
            {
                GameObject row = PrefabUtility.InstantiatePrefab(rowPrefab, content.transform) as GameObject;
                LeaderboardRow rowScript = row.GetComponent<LeaderboardRow>();
                if (rowScript != null)
                {
                    rowScript.Populate(i, $"Player {i}", 5000 - (i * 200));
                }
            }
        }

        Selection.activeGameObject = panelObj;
        Debug.Log("Leaderboard UI Generated! (Row prefab created at Assets/LeaderboardRowPrefab.prefab)");
    }

    private static GameObject BuildLeaderboardRow()
    {
        GameObject row = CreateUIObject("LeaderboardRow", null);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 160;

        Image bg = row.AddComponent<Image>();
        bg.color = Color.white; // Make it look like a card

        LeaderboardRow rowScript = row.AddComponent<LeaderboardRow>();

        // Layout Group for horizontal alignment
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(40, 40, 0, 0);
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Rank Circle
        GameObject rankObj = CreateUIObject("Rank", row.transform);
        rankObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
        rankObj.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f); // Light grey circle
        GameObject rankTextObj = CreateUIObject("RankText", rankObj.transform);
        SetFullScreen(rankTextObj);
        rowScript.rankText = AddText(rankTextObj, "1", 45, Color.black, TextAlignmentOptions.Center);
        rowScript.rankText.fontStyle = FontStyles.Bold;

        // Profile Image (User Icon)
        GameObject profileObj = CreateUIObject("ProfileImage", row.transform);
        profileObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
        profileObj.AddComponent<Image>().color = Color.gray; // Placeholder for user avatar

        // Name
        GameObject nameObj = CreateUIObject("Name", row.transform);
        LayoutElement nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1; // Take up remaining space
        rowScript.nameText = AddText(nameObj, "wordwizard", 50, Color.black, TextAlignmentOptions.Left);
        rowScript.nameText.fontStyle = FontStyles.Bold;
        rowScript.nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);

        // Score
        GameObject scoreObj = CreateUIObject("Score", row.transform);
        scoreObj.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 100);
        rowScript.scoreText = AddText(scoreObj, "4820", 55, Color.black, TextAlignmentOptions.Right);
        rowScript.scoreText.fontStyle = FontStyles.Bold;

        // Save as prefab
        string path = "Assets/LeaderboardRowPrefab.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, path);
        DestroyImmediate(row); // Remove the loose object from scene
        
        return prefab;
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
}