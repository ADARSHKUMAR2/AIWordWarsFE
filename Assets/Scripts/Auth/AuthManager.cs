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
    public string CurrentUserUID => currentUser?.UserId;

    public string CurrentSessionId { get; private set; }

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
            
            // Call ApiService to validate session
            StartCoroutine(ApiService.Instance.ValidateSession(savedSession, (isValid) => {
                if(isValid) {
                    Debug.Log("✅ Session Valid! Proceed to Game.");
                    SceneManager.LoadScene("MainMenuScene");
                } else {
                    Debug.Log("❌ Session Invalid. Clearing.");
                    SignOut(); // Clear the bad session locally
                }
            }));
        }
    }
    
    // Sign in with email and password
    public void SignInWithEmail(string email, string password, Action<ApiService.UserProfile> onSuccess, Action<string> onError)
    {
        StartCoroutine(SignInWithEmailCoroutine(email, password, onSuccess, onError));
    }

    private IEnumerator SignInWithEmailCoroutine(string email, string password, Action<ApiService.UserProfile> onSuccess, Action<string> onError)
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

        Action<ApiService.UserProfile> wrappedSuccess = (profile) => 
        {
            CurrentSessionId = profile.session_id;
            PlayerPrefs.SetString("session_id", profile.session_id);
            PlayerPrefs.Save();
            Debug.Log($"✅ Session saved locally: {CurrentSessionId}");
            
            onSuccess?.Invoke(profile);
        };

        // Send token to backend
        yield return ApiService.Instance.LoginWithFirebase(idToken, wrappedSuccess, onError);
    }

    // Register with email and password
    public void RegisterWithEmail(string email, string password, Action<ApiService.UserProfile> onSuccess, Action<string> onError)
    {
        StartCoroutine(RegisterWithEmailCoroutine(email, password, onSuccess, onError));
    }

    private IEnumerator RegisterWithEmailCoroutine(string email, string password, Action<ApiService.UserProfile> onSuccess, Action<string> onError)
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
        PlayerPrefs.DeleteKey("session_id");
        PlayerPrefs.Save();
        
        Debug.Log("✅ Signed out completely and cleared local session.");
    }

    // Sign in with Google
    
}
