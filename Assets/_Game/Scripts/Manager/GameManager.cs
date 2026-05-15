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
                UIManager.Instance.OpenUI<PanelHome>();
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
}
