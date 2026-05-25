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

public sealed class GameManager : Singleton<GameManager>
{
    public bool BootTutorialActive { get; private set; }
    private const string PREF_BOOT_TUTORIAL_DONE = "BOOT_TUTORIAL_DONE";

    [SerializeField, Min(0f)] private float firstLoadingDuration = 3f;
    [SerializeField] private ParticleSystem[] winParticles;
    [SerializeField, Min(0f)] private float winPanelDelay = 1f;

    private Coroutine loadingCoroutine;
    private Coroutine winCoroutine;
    private bool hasStateChanged;

    public GameState State { get; private set; }
    public int LastWinStarCount { get; private set; } = 3;

    private void Start()
    {
        if (!hasStateChanged)
        {
            PlayInitialFlow();
        }
    }

    public void PlayInitialFlow()
    {
        StopWinSequence();
        ChangeState(GameState.Loading);
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(ShowHomeAfterLoading());
    }

    public void PlayCurrentLevel()
    {
        StopWinSequence();
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
        StopWinSequence();
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

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
        State = GameState.Win;
        UIManager.Instance.CloseUIDirectly<PanelGameplay>();

        winCoroutine = StartCoroutine(ShowWinAfterParticles());
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
        StopWinSequence();
        ChangeState(GameState.GamePlay);
        LevelManager.Instance.LoadCurrentLevel(true);
        var panel = UIManager.Instance.OpenUI<PanelGameplay>();
        panel.SetTemporaryRule(LevelManager.Instance.CurrentCutCount, 3);
    }

    private IEnumerator ShowHomeAfterLoading()
    {
        float loadingDuration = firstLoadingDuration;
        if (UIManager.Instance.IsUILoaded<PanelLoading>())
        {
            loadingDuration = UIManager.Instance.GetUI<PanelLoading>().AnimationDuration;
        }

        yield return new WaitForSecondsRealtime(loadingDuration);
        loadingCoroutine = null;
        ChangeState(GameState.Home);
    }

    private IEnumerator ShowWinAfterParticles()
    {
        PlayWinParticles();

        if (winPanelDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(winPanelDelay);
        }

        LevelManager.Instance.CompleteCurrentLevel(LastWinStarCount);
        LevelManager.Instance.ClearCurrentLevel();
        winCoroutine = null;
        ChangeState(GameState.Win);
    }

    private void ChangeState(GameState nextState)
    {
        hasStateChanged = true;
        State = nextState;
        UIManager.Instance.CloseAllDirectly();

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

    private void PlayWinParticles()
    {
        ResolveWinParticles();
        for (int i = 0; i < winParticles.Length; i++)
        {
            if (winParticles[i] == null)
            {
                continue;
            }

            winParticles[i].gameObject.SetActive(true);
            winParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            winParticles[i].Play(true);
        }
    }

    private void ResolveWinParticles()
    {
        if (winParticles != null && winParticles.Length > 0)
        {
            return;
        }

        var sceneParticles = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var particles = new System.Collections.Generic.List<ParticleSystem>();
        for (int i = 0; i < sceneParticles.Length && particles.Count < 2; i++)
        {
            if (sceneParticles[i] != null && sceneParticles[i].gameObject.scene.IsValid())
            {
                particles.Add(sceneParticles[i]);
            }
        }

        winParticles = particles.ToArray();
    }

    private void StopWinSequence()
    {
        if (winCoroutine == null)
        {
            return;
        }

        StopCoroutine(winCoroutine);
        winCoroutine = null;
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
