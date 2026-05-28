using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class PlayerAuthService : MonoBehaviour
{
    public static PlayerAuthService Instance { get; private set; }

    public string CurrentUserId => currentUser != null ? currentUser.UserId : string.Empty;
    public bool IsSignedIn { get; private set; }

    public event Action<FirebaseUser> OnSignedIn;

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private LeaderBoardService leaderBoardService;
    private bool isInitializing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        leaderBoardService = GetComponent<LeaderBoardService>() ?? FindFirstObjectByType<LeaderBoardService>();
    }

    public void Initialize()
    {
        if (IsSignedIn)
        {
            leaderBoardService?.InitializeAfterSignIn();
            return;
        }

        if (isInitializing)
        {
            return;
        }

        if (FirebaseBootstrap.Instance == null || !FirebaseBootstrap.Instance.IsInitialized)
        {
            Debug.LogWarning("[PlayerAuthService] Firebase is not initialized yet.");
            return;
        }

        isInitializing = true;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            CompleteSignIn(auth.CurrentUser);
            return;
        }

        Task signInTask = auth.SignInAnonymouslyAsync();
        signInTask.ContinueWithOnMainThread(OnAnonymousSignInCompleted);
    }

    private void OnAnonymousSignInCompleted(Task task)
    {
        isInitializing = false;

        if (task.IsCanceled)
        {
            Debug.LogError("[PlayerAuthService] Anonymous sign-in was canceled.");
            return;
        }

        if (task.IsFaulted)
        {
            Debug.LogError($"[PlayerAuthService] Anonymous sign-in failed: {task.Exception}");
            return;
        }

        CompleteSignIn(auth?.CurrentUser);
    }

    private void CompleteSignIn(FirebaseUser user)
    {
        currentUser = user ?? auth?.CurrentUser;
        IsSignedIn = currentUser != null;
        isInitializing = false;

        if (!IsSignedIn)
        {
            Debug.LogError("[PlayerAuthService] Firebase user is null after sign-in.");
            return;
        }

        Debug.Log($"[PlayerAuthService] Anonymous sign-in success. UID: {currentUser.UserId}");
        OnSignedIn?.Invoke(currentUser);

        leaderBoardService ??= FindFirstObjectByType<LeaderBoardService>();
        leaderBoardService?.InitializeAfterSignIn();
    }
}
