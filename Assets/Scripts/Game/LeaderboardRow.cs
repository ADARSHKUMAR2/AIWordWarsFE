using TMPro;
using UnityEngine;

public class LeaderboardRow : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    public void Populate(int rank, string displayName, int score)
    {
        rankText.text = $"#{rank}";
        nameText.text = string.IsNullOrEmpty(displayName) ? "Anonymous" : displayName;
        scoreText.text = score.ToString("N0"); // e.g. "1,500"

        // Highlight the top 3 with gold/silver/bronze
        if (rank == 1) rankText.color = new Color(1f, 0.84f, 0f);       // Gold
        else if (rank == 2) rankText.color = new Color(0.75f, 0.75f, 0.75f); // Silver
        else if (rank == 3) rankText.color = new Color(0.8f, 0.5f, 0.2f);    // Bronze
        else rankText.color = Color.white;
    }
}
