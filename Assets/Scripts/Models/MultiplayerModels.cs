using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────
// REQUEST MODELS  (Unity → Backend)
// ─────────────────────────────────────────────────────────────────

[Serializable]
public class CreateRoomRequest
{
    public string user_id;
    public int difficulty = 5;
}

[Serializable]
public class JoinRoomRequest
{
    public string room_code;
    public string user_id;
}

// ─────────────────────────────────────────────────────────────────
// RESPONSE MODELS  (Backend → Unity, REST)
// ─────────────────────────────────────────────────────────────────

[Serializable]
public class CreateRoomResponse
{
    public string room_code;
    public string status;
    public int difficulty;
    public string message;
}

[Serializable]
public class JoinRoomResponse
{
    public string room_code;
    public string status;
    public int difficulty;
    public string ws_url;
    public string message;
}

[Serializable]
public class RoomStatusResponse
{
    public string room_code;
    public string status;       // "waiting" | "ready" | "in_game" | "finished"
    public int difficulty;
    public string host_uid;
    public string guest_uid;
}

// ─────────────────────────────────────────────────────────────────
// WEBSOCKET MESSAGE MODELS  (Backend → Unity, WebSocket)
// ─────────────────────────────────────────────────────────────────

/// <summary>
/// Base wrapper — we first deserialize just the "type" field,
/// then deserialize the full payload based on type.
/// </summary>
[Serializable]
public class WsMessage
{
    public string type;     // waiting | game_start | next_question | question_result | answer_result | game_over | ...
    public string message;  // human-readable string (used by "waiting", "error", "opponent_disconnected")
}

[Serializable]
public class WsGameStart
{
    public string type;         // "game_start"
    public string puzzle_id;
    public string scrambled;    // e.g. "P Z L E U Z"
    public int difficulty;
    public string hint;
    public string category;
    public int time_limit;      // seconds
    public int question_number;
    public int total_questions;
    public WsPlayerScore[] score_entries;
    public string[] players;    // array of user_ids
}

[Serializable]
public class WsNextQuestion : WsGameStart { }

[Serializable]
public class WsPlayerScore
{
    public string user_id;
    public int score;
}

[Serializable]
public class WsQuestionResult
{
    public string type;
    public int question_number;
    public string winner_id;    // Empty/null when the question timed out
    public string correct_word;
    public WsPlayerScore[] score_entries;
}

[Serializable]
public class WsAnswerResult
{
    public string type;         // "answer_result"
    public bool correct;
    public string message;
}

[Serializable]
public class WsGameOver
{
    public string type;         // "game_over"
    public string winner_id;
    public string loser_id;
    public string your_result;  // "win" | "lose" | "draw"
    public WsPlayerScore[] score_entries;
    public WsQuestionHistory[] question_results;
}

[Serializable]
public class WsQuestionHistory
{
    public int question_number;
    public string winner_id;
}
