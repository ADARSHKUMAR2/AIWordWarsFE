# AI WordWars - Unity Client 🎮

The frontend client for AI WordWars, built in Unity 6. It features a streamlined Single-Scene architecture for gameplay, automatic session persistence, and seamless communication with the Python microservices backend.

## 🏗️ Architecture

*   **Single-Scene Game Loop:** Instead of loading different scenes for different game modes, the core gameplay happens in a unified `GameScene`. The `GameManager.cs` dynamically toggles specific UI panels (Timer, Hearts, Combo) based on the mode selected in the Main Menu.
*   **Session Persistence:** On successful Firebase login, the backend provides a Redis `session_id`. `AuthManager.cs` stores this in `PlayerPrefs` and performs an auto-login (`/session/validate`) on app startup.
*   **Network Layer:** Separated into `ApiService.cs` (Auth operations) and `GameApiService.cs` (Gameplay operations). The network layer automatically injects the `Authorization: Bearer <session_id>` header into all protected API calls.

## 🗂️ Project Structure

*   `Assets/Scripts/Auth/`: Contains `AuthManager.cs` (Firebase + Redis session logic) and `LoginUI.cs`.
*   `Assets/Scripts/Network/`: Contains all HTTP `UnityWebRequest` logic and strongly-typed data models (`GameModels.cs`, `LoginModels.cs`).
*   `Assets/Scripts/Game/`: Contains `MainMenuController.cs` and the unified `GameManager.cs` that handles Classic, Time Attack, and Survival logic.

## 🗺️ Scene Flow

1.  **`LoginScene`:** Firebase Auth UI. Skipped automatically if a valid Redis session exists in `PlayerPrefs`.
2.  **`MainMenuScene`:** User selects between Classic, Time Attack, or Survival. Selection is saved to `PlayerPrefs` before loading the game.
3.  **`GameScene`:** The core game loop. `GameManager.cs` reads the selected mode, sets up the HUD, and begins fetching AI-generated puzzles from the backend.

## 🚀 Setup Instructions

1.  **Dependencies:** Ensure the **Firebase Unity SDK** (Auth) and **Newtonsoft.Json** packages are installed.
2.  **Build Settings:** Ensure the scenes are added to `File -> Build Settings` in this exact order:
    *   0: LoginScene
    *   1: MainMenuScene
    *   2: GameScene
3.  **Backend Connection:** Ensure `GATEWAY_URL` in `ApiService.cs` and `GameApiService.cs` points to your running FastAPI gateway (e.g., `http://localhost:8000`).