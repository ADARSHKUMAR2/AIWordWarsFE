using System;
using System.Collections;
using Firebase;
using Firebase.Auth;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    public bool IsLoggedIn => currentUser != null;
    public string CurrentUserUID => currentUser?.UserId;

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

        // Send token to backend
        yield return ApiService.Instance.LoginWithFirebase(idToken, onSuccess, onError);
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
        if (auth != null && currentUser != null)
        {
            auth.SignOut();
            currentUser = null;
            Debug.Log("✅ Signed out");
        }
    }
}
