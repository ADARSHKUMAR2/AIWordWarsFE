using System;

// ─────────────────────────────────────────
// SHARED DATA MODELS
// ─────────────────────────────────────────

[Serializable]
public class NewPuzzleRequest
{
    public int difficulty = 5;
    public string mode = "simple";
    public string category;
    public string user_id;
}

[Serializable]
public class PuzzleResponse
{
    public string puzzle_id;
    public string scrambled;
    public int difficulty;
    public string hint;
    public string mode;
    public string category;
}

[Serializable]
public class SolveRequest
{
    public string puzzle_id;
    public string answer;
    public float time_taken;
    public string user_id;
}

[Serializable]
public class SolveResponse
{
    public bool correct;
    public float time_taken;
    public int score;
    public string feedback;
    public string correct_answer;
}

// ─────────────────────────────────────────
// TIME ATTACK DATA MODELS
// ─────────────────────────────────────────

[Serializable]
public class TimeAttackPuzzleResponse
{
    public string puzzle_id;
    public string scrambled;
    public int difficulty;
    public string hint;
    public string mode;
    public string category;
    public int session_duration; // Total session time (60s)
}

[Serializable]
public class TimeAttackSolveRequest
{
    public string puzzle_id;
    public string answer;
    public float time_taken;
    public int current_combo;       // Current combo Unity is tracking
    public float session_elapsed;   // How many seconds have passed in the 60s window
    public string user_id;
}

[Serializable]
public class TimeAttackSolveResponse
{
    public bool correct;
    public int score;
    public int new_combo;           // Backend sends back the new combo for Unity to save
    public bool session_expired;
    public string feedback;
    public string correct_answer;
}

// ─────────────────────────────────────────
// SURVIVAL DATA MODELS
// ─────────────────────────────────────────

[Serializable]
public class SurvivalPuzzleResponse
{
    public string puzzle_id;
    public string scrambled;
    public int difficulty;
    public string hint;
    public string mode;
    public string category;
    public int starting_hearts;     // How many hearts the game starts with (3)
    public int time_limit_per_word; // Max seconds per word (30s)
}

[Serializable]
public class SurvivalSolveRequest
{
    public string puzzle_id;
    public string answer;
    public float time_taken;
    public int current_hearts;      // Current hearts Unity is tracking
    public string user_id;
}

[Serializable]
public class SurvivalSolveResponse
{
    public bool correct;
    public int score;
    public int hearts_remaining;    // Backend sends back new heart count
    public bool game_over;
    public string feedback;
    public string correct_answer;
}
