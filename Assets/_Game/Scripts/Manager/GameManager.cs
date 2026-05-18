using System.Collections;
using UnityEngine;

public enum GameState
{
    Loading,
    Home,
    GamePlay,
    Win,
    Lose
}

public sealed class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public bool BootTutorialActive { get; private set; }
    private const string PREF_BOOT_TUTORIAL_DONE = "BOOT_TUTORIAL_DONE";

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    instance = new GameObject("Game Manager").AddComponent<GameManager>();
                }
            }

            return instance;
        }
        private set => instance = value;
    }

    [SerializeField, Min(0f)] private float firstLoadingDuration = 3f;

    private Coroutine loadingCoroutine;
    private bool hasStateChanged;

    public GameState State { get; private set; }
    public int LastWinStarCount { get; private set; } = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!hasStateChanged)
        {
            PlayInitialFlow();
        }
    }

    public void PlayInitialFlow()
    {
        ChangeState(GameState.Loading);
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(ShowHomeAfterLoading());
    }

    public void PlayCurrentLevel()
    {
        ChangeState(GameState.GamePlay);
        LevelManager.Instance.LoadCurrentLevel(true);
        var panel = UIManager.Instance.OpenUI<PanelGameplay>();
        panel.SetTemporaryRule(LevelManager.Instance.CurrentCutCount, 3);
    }

    public void ReplayLevel()
    {
        PlayCurrentLevel();
    }

    public void GoHome()
    {
        LevelManager.Instance.ClearCurrentLevel();
        ChangeState(GameState.Home);
    }

    public void WinLevel()
    {
        if (State != GameState.GamePlay)
        {
            return;
        }

        LastWinStarCount = PanelGameplay.Instance != null ? PanelGameplay.Instance.RemainingStarCount : 3;
        LevelManager.Instance.CompleteCurrentLevel();
        ChangeState(GameState.Win);
    }

    public void LoseLevel()
    {
        if (State != GameState.GamePlay)
        {
            return;
        }

        ChangeState(GameState.Lose);
    }

    public void PlayNextLevel()
    {
        ChangeState(GameState.GamePlay);
        LevelManager.Instance.LoadCurrentLevel(true);
        var panel = UIManager.Instance.OpenUI<PanelGameplay>();
        panel.SetTemporaryRule(LevelManager.Instance.CurrentCutCount, 3);
    }

    private IEnumerator ShowHomeAfterLoading()
    {
        yield return new WaitForSeconds(firstLoadingDuration);
        loadingCoroutine = null;
        ChangeState(GameState.Home);
    }

    private void ChangeState(GameState nextState)
    {
        hasStateChanged = true;
        State = nextState;
        UIManager.Instance.CloseAll();

        switch (State)
        {
            case GameState.Loading:
                UIManager.Instance.OpenUI<PanelLoading>();
                break;
            case GameState.Home:
                OpenHomePanels();
                break;
            case GameState.GamePlay:
                UIManager.Instance.OpenUI<PanelGameplay>();
                break;
            case GameState.Win:
                UIManager.Instance.OpenUI<PanelWin>();
                break;
            case GameState.Lose:
                UIManager.Instance.OpenUI<PanelLose>();
                break;
        }
    }

    private void OpenHomePanels()
    {
        var leaderboard = UIManager.Instance.GetUI<PanelLeaderBoard>();
        var home = UIManager.Instance.GetUI<PanelHome>();
        var shop = UIManager.Instance.GetUI<PanelShop>();
        var footer = UIManager.Instance.GetUI<FooterTabBar>();

        leaderboard.transform.SetSiblingIndex(0);
        home.transform.SetSiblingIndex(1);
        shop.transform.SetSiblingIndex(2);
        footer.transform.SetSiblingIndex(3);

        leaderboard.gameObject.SetActive(false);
        home.Open();
        shop.gameObject.SetActive(false);
        footer.Open();

        var tabTransition = footer.GetComponent<TabTransitionController>();
        if (tabTransition == null)
        {
            tabTransition = footer.gameObject.AddComponent<TabTransitionController>();
        }

        if (tabTransition != null)
        {
            footer.SetTransitionController(tabTransition);
            tabTransition.ConfigurePanels(leaderboard, home, shop);
            tabTransition.ShowHomeImmediate();
            footer.SelectHome(false);
        }
    }

    public bool IsBootTutorialDone()
    {
        return PlayerPrefs.GetInt(PREF_BOOT_TUTORIAL_DONE, 0) == 1;
    }

    public void SetBootTutorial(bool on)
    {
        BootTutorialActive = on;

        if (on)
        {
            // Trong tutorial: không cho PanelGamePlay bật lên
            UIManager.Instance.CloseUIDirectly<PanelHome>();
            UIManager.Instance.CloseUIDirectly<FooterTabBar>();
            UIManager.Instance.CloseUIDirectly<PanelShop>();
            UIManager.Instance.CloseUIDirectly<PanelLeaderBoard>();
            UIManager.Instance.CloseUIDirectly<PanelWin>();
            UIManager.Instance.CloseUIDirectly<PanelFail>();
            UIManager.Instance.CloseUIDirectly<PanelGameplay>();
        }
    }
}
