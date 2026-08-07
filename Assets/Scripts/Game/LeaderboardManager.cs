using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform entriesContainer;      // Parent object for score row prefabs
    public GameObject entryRowPrefab;       // Prefab: has Rank, Name, Score TMP texts
    public TextMeshProUGUI statusText;      // Shows "Loading..." or "No scores yet"
    public Button closeButton;  

    [Header("Settings")]
    public string boardId = "global";
    public string mode = "simple";
    public int limit = 20;

    private MainMenuController menuController;


    private void Awake()
    {
        menuController = FindObjectOfType<MainMenuController>();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void Start()
    {
        LoadLeaderboard();
    }

    private void OnCloseClicked()
    {
        if (menuController != null)
            menuController.CloseLeaderboard();
    }

    public void LoadLeaderboard()
    {
        if (statusText) statusText.text = "Loading...";
        // Clear previous entries
        foreach (Transform child in entriesContainer)
            Destroy(child.gameObject);

        StartCoroutine(LeaderboardApiService.Instance.GetLeaderboard(boardId, mode, limit, OnLoaded, OnError));
    }

    private void OnLoaded(LeaderboardResponse response)
    {
        string json = JsonUtility.ToJson(response, true); // 'true' makes it pretty-print
        Debug.Log("Leaderboard loaded successfully. JSON:\n" + json);

        // Clear existing rows
        foreach (Transform child in entriesContainer)
            Destroy(child.gameObject);

        if (response == null || response.entries == null || response.entries.Length == 0)
        {
            if (statusText) statusText.text = "No scores yet. Be the first!";
            return;
        }

        if (statusText) statusText.text = "";

        foreach (var entry in response.entries)
        {
            GameObject row = Instantiate(entryRowPrefab, entriesContainer);
            
            LeaderboardRow rowScript = row.GetComponent<LeaderboardRow>();
            if (rowScript != null)
                rowScript.Populate(entry.rank, entry.display_name, entry.score);
        }
    }

    private void OnError(string error)
    {
        Debug.LogError("Leaderboard load failed: " + error);
        if (statusText) statusText.text = "Failed to load leaderboard.";
    }
}
