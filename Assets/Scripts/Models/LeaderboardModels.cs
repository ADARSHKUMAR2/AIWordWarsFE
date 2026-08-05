// ─────────────────────────────────────────
// LEADERBOARD DATA MODELS
// ─────────────────────────────────────────
using System;

[Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string firebase_uid;
    public string display_name;
    public string photo_url;
    public int score;
    public string submitted_at;
}

[Serializable]
public class LeaderboardResponse
{
    public string board_id;
    public string mode;
    public LeaderboardEntry[] entries;  // Array of top N players
}

[Serializable]
public class PlayerRankResponse
{
    public string firebase_uid;
    public int rank;
    public int score;
    public string board_id;
    public string mode;
}

[Serializable]
public class LeaderboardScoreResult
{
    public string firebase_uid;
    public string board_id;
    public string mode;
    public int score;
    public bool is_personal_best;
    public string submitted_at;
}