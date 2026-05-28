using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseBootstrap Instance { get; private set; }

    [SerializeField] private bool initializeOnStart = true;

    public bool IsInitialized { get; private set; }
    public DependencyStatus DependencyStatus { get; private set; } = DependencyStatus.UnavailableOther;

    private PlayerAuthService authService;
    private bool isInitializing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureFirebaseServicesExist()
    {
        if (FindFirstObjectByType<FirebaseBootstrap>() != null)
        {
            return;
        }

        GameObject services = new GameObject("Firebase Services");
        DontDestroyOnLoad(services);
        services.AddComponent<LeaderBoardService>();
        services.AddComponent<PlayerAuthService>();
        services.AddComponent<FirebaseBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        authService = GetComponent<PlayerAuthService>() ?? FindFirstObjectByType<PlayerAuthService>();
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeFirebase();
        }
    }

    public void InitializeFirebase()
    {
        if (IsInitialized || isInitializing)
        {
            return;
        }

        isInitializing = true;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(OnDependencyCheckCompleted);
    }

    private void OnDependencyCheckCompleted(Task<DependencyStatus> task)
    {
        isInitializing = false;

        if (task.IsCanceled)
        {
            Debug.LogError("[FirebaseBootstrap] Dependency check was canceled.");
            return;
        }

        if (task.IsFaulted)
        {
            Debug.LogError($"[FirebaseBootstrap] Dependency check failed: {task.Exception}");
            return;
        }

        DependencyStatus = task.Result;
        if (DependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError($"[FirebaseBootstrap] Firebase dependencies are not available: {DependencyStatus}");
            return;
        }

        IsInitialized = true;
        Debug.Log("[FirebaseBootstrap] Firebase initialized successfully.");

        authService ??= FindFirstObjectByType<PlayerAuthService>();
        if (authService == null)
        {
            Debug.LogError("[FirebaseBootstrap] PlayerAuthService not found in scene.");
            return;
        }

        authService.Initialize();
    }
}
