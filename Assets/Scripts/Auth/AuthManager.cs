using System;
using System.Collections;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    public bool IsLoggedIn => currentUser != null;
    public string CurrentUserUID 
    {
        get 
        {
            if (currentUser != null) return currentUser.UserId;
            string savedUid = PlayerPrefs.GetString("firebase_uid", "");
            return string.IsNullOrEmpty(savedUid) ? null : savedUid;
        }
    }

    public string CurrentSessionId { get; private set; }

    // ── Cached progression stats (set at login) ──
    public int CurrentXP { get; private set; }
    public int CurrentCoins { get; private set; }
    public int CurrentLevel { get; private set; }
    public int CurrentXPToNext { get; private set; }
    public string CurrentDisplayName { get; private set; }  
    public string CurrentPhotoUrl { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeFirebase();
        CheckExistingSession();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("✅ Firebase initialized successfully");
            }
            else
            {
                Debug.LogError($"❌ Firebase initialization failed: {task.Result}");
            }
        });
    }

    private void CheckExistingSession()
    {
        string savedSession = PlayerPrefs.GetString("session_id", "");
        if (!string.IsNullOrEmpty(savedSession))
        {
            Debug.Log("🔍 Found saved session, validating...");
            CurrentSessionId = savedSession;
            
            StartCoroutine(ApiService.Instance.ValidateSession(savedSession, (isValid) => {
                if (isValid)
                {
                    Debug.Log("✅ Session Valid!");
                    string savedUid = PlayerPrefs.GetString("firebase_uid", "");
                    if (!string.IsNullOrEmpty(savedUid))
                    {
                        // Fetch progression first, THEN load scene inside the callback
                        StartCoroutine(ApiService.Instance.GetUserProfile(savedUid, (profile) => {
                            CurrentXP = profile.xp;
                            CurrentCoins = profile.coins;
                            CurrentLevel = profile.level;
                            CurrentXPToNext = profile.xp_to_next_level;
                            CurrentDisplayName = profile.display_name;  
                            CurrentPhotoUrl = profile.photo_url;    
                            Debug.Log($"✅ Progression restored: Lv.{CurrentLevel}, XP={CurrentXP}, Coins={CurrentCoins}");
                            SceneManager.LoadScene("MainMenuScene"); // ← only here
                        }, (err) => {
                            Debug.LogWarning($"Could not fetch progression on resume: {err}");
                            SceneManager.LoadScene("MainMenuScene"); // ← fallback, still load
                        }));
                        // NO SceneManager.LoadScene here — let the coroutine handle it
                    }
                    else
                    {
                        // No saved UID (edge case), just go to menu
                        SceneManager.LoadScene("MainMenuScene");
                    }
                }
                else
                {
                    Debug.Log("❌ Session Invalid. Clearing.");
                    SignOut();
                }
            }));
        }
    }

    // Sign in with email and password
    public void SignInWithEmail(string email, string password, Action<UserProfile> onSuccess, Action<string> onError)
    {
        StartCoroutine(SignInWithEmailCoroutine(email, password, onSuccess, onError));
    }

    private IEnumerator SignInWithEmailCoroutine(string email, string password, Action<UserProfile> onSuccess, Action<string> onError)
    {
        var signInTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signInTask.IsCompleted);

        if (signInTask.Exception != null)
        {
            Debug.LogError("Firebase sign-in failed: " + signInTask.Exception);
            onError?.Invoke(signInTask.Exception.Message);
            yield break;
        }

        currentUser = signInTask.Result.User;
        Debug.Log($"✅ Firebase sign-in successful: {currentUser.Email}");

        // Get the ID token
        var tokenTask = currentUser.TokenAsync(false);
        yield return new WaitUntil(() => tokenTask.IsCompleted);

        if (tokenTask.Exception != null)
        {
            Debug.LogError("Failed to get ID token: " + tokenTask.Exception);
            onError?.Invoke(tokenTask.Exception.Message);
            yield break;
        }

        string idToken = tokenTask.Result;
        Debug.Log("✅ Got Firebase ID token");

        Action<UserProfile> wrappedSuccess = (profile) => 
        {
            CurrentSessionId = profile.session_id;
            CurrentXP = profile.xp;
            CurrentCoins = profile.coins;
            CurrentLevel = profile.level;
            CurrentXPToNext = profile.xp_to_next_level;
            CurrentDisplayName = profile.display_name; 
            CurrentPhotoUrl = profile.photo_url;    

            PlayerPrefs.SetString("session_id", profile.session_id);
            PlayerPrefs.SetString("firebase_uid", profile.firebase_uid);  // ← save UID

            PlayerPrefs.Save();
            Debug.Log($"✅ Session saved locally: {CurrentSessionId}");
            
            onSuccess?.Invoke(profile);
        };

        // Send token to backend
        yield return ApiService.Instance.LoginWithFirebase(idToken, wrappedSuccess, onError);
    }

    // Register with email and password
    public void RegisterWithEmail(string email, string password, Action<UserProfile> onSuccess, Action<string> onError)
    {
        StartCoroutine(RegisterWithEmailCoroutine(email, password, onSuccess, onError));
    }

    private IEnumerator RegisterWithEmailCoroutine(string email, string password, Action<UserProfile> onSuccess, Action<string> onError)
    {
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError("Firebase registration failed: " + registerTask.Exception);
            onError?.Invoke(registerTask.Exception.Message);
            yield break;
        }

        currentUser = registerTask.Result.User;
        Debug.Log($"✅ Firebase registration successful: {currentUser.Email}");

        // After registration, sign in
        yield return SignInWithEmailCoroutine(email, password, onSuccess, onError);
    }

    // Sign out
    public void SignOut()
    {
        // 1. Sign out of Firebase
        if (auth != null && currentUser != null)
        {
            auth.SignOut();
            currentUser = null;
        }

        // 2. Call backend to invalidate session (Fire and forget, or yield it)
        if (!string.IsNullOrEmpty(CurrentSessionId))
        {
            // Call backend to invalidate session
            StartCoroutine(ApiService.Instance.Logout(CurrentSessionId));
        }

        // 3. Clear local session data
        CurrentSessionId = null;
        CurrentXP = 0;
        CurrentCoins = 0;
        CurrentLevel = 1;
        CurrentXPToNext = 0;
        PlayerPrefs.DeleteKey("session_id");
        PlayerPrefs.DeleteKey("firebase_uid");  
        PlayerPrefs.Save();
        
        Debug.Log("✅ Signed out completely and cleared local session.");
    }

    // Sign in with Google
    
}
