using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelLeaderBoard : UICanvas
{
    [Serializable]
    private class LeaderboardRowBinding
    {
        public Transform root;
        public Text displayNameText;
        public Text levelText;
        public Color defaultDisplayNameColor;
    }

    [Header("Leaderboard UI")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int maxRows = 10;
    [SerializeField] private bool showRankPrefix = true;
    [SerializeField] private string emptyDisplayName = "---";
    [SerializeField] private string emptyLevelText = "Lv.0";
    [SerializeField] private Color selfDisplayNameColor = new Color(0.95f, 0.2f, 0.2f, 1f);
    [SerializeField] private List<LeaderboardRowBinding> rowBindings = new List<LeaderboardRowBinding>();

    [Header("Lock By Level")]
    [SerializeField] private GameObject lockObjectAtLevel20;
    [SerializeField] private int lockAtLevel = 20;

    private Coroutine loadLeaderboardCoroutine;
    private bool rowDefaultColorsCached;

    public override void SetUp()
    {
        base.SetUp();
        CacheRowDefaultColors();
        FillEmptyRows();
        ResetScrollToTop();
    }

    public override void Open()
    {
        base.Open();
        RefreshLeaderboard();
    }

    private void OnEnable()
    {
        CacheRowDefaultColors();
        FillEmptyRows();
        ResetScrollToTop();
        RefreshLockByLevel();

        if (Application.isPlaying)
        {
            RefreshLeaderboard();
        }

        PanelEnterUserName.UserNameSaved -= HandleUserNameSaved;
        PanelEnterUserName.UserNameSaved += HandleUserNameSaved;
    }

    private void OnDisable()
    {
        PanelEnterUserName.UserNameSaved -= HandleUserNameSaved;

        if (loadLeaderboardCoroutine != null)
        {
            StopCoroutine(loadLeaderboardCoroutine);
            loadLeaderboardCoroutine = null;
        }
    }

    public void NotifySelectedFromTab()
    {
        RefreshLockByLevel();

        if (ShouldOpenUserNamePrompt())
        {
            OpenUserNamePrompt();
            return;
        }

        RefreshLeaderboard();
    }

    private bool ShouldOpenUserNamePrompt()
    {
        if (!Application.isPlaying || !IsLeaderboardUnlockedByLevel())
        {
            return false;
        }

        return LeaderBoardService.Instance == null || !LeaderBoardService.Instance.HasLocalDisplayName();
    }

    private void OpenUserNamePrompt()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsUIOpened<PanelEnterUserName>())
        {
            return;
        }

        UIManager.Instance.OpenUI<PanelEnterUserName>();
    }

    private void HandleUserNameSaved(string _)
    {
        ShowLocalPlayerRow();
        RefreshLeaderboard();
    }

    private void ShowLocalPlayerRow()
    {
        if (LeaderBoardService.Instance == null || rowBindings.Count == 0)
        {
            return;
        }

        string displayName = LeaderBoardService.Instance.GetLocalDisplayName();
        if (string.IsNullOrEmpty(displayName))
        {
            return;
        }

        int level = Mathf.Max(0, LeaderBoardService.Instance.GetLocalHighestCompletedLevel());
        if (LevelManager.Instance != null)
        {
            level = Mathf.Max(level, LevelManager.Instance.CurrentNormalLevelNumber);
        }

        ApplyRow(rowBindings[0], 1, displayName, $"Lv.{level}", true);
    }

    private void RefreshLeaderboard()
    {
        if (loadLeaderboardCoroutine != null)
        {
            StopCoroutine(loadLeaderboardCoroutine);
        }

        loadLeaderboardCoroutine = StartCoroutine(LoadLeaderboardWhenReady());
    }

    private IEnumerator LoadLeaderboardWhenReady()
    {
        const float timeoutSeconds = 10f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            bool firebaseReady = FirebaseBootstrap.Instance != null && FirebaseBootstrap.Instance.IsInitialized;
            bool authReady = PlayerAuthService.Instance != null && PlayerAuthService.Instance.IsSignedIn;
            bool leaderboardReady = LeaderBoardService.Instance != null;

            if (firebaseReady && authReady && leaderboardReady)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (LeaderBoardService.Instance == null
            || FirebaseBootstrap.Instance == null
            || !FirebaseBootstrap.Instance.IsInitialized
            || PlayerAuthService.Instance == null
            || !PlayerAuthService.Instance.IsSignedIn)
        {
            loadLeaderboardCoroutine = null;
            yield break;
        }

        int limit = Mathf.Max(1, rowBindings.Count > 0 ? rowBindings.Count : maxRows);
        LeaderBoardService.Instance.GetTopPlayers(limit, OnLeaderboardLoaded, OnLeaderboardLoadFailed);

        loadLeaderboardCoroutine = null;
    }

    private void OnLeaderboardLoaded(List<LeaderboardPlayerData> players)
    {
        for (int i = 0; i < rowBindings.Count; i++)
        {
            if (players != null && i < players.Count)
            {
                LeaderboardPlayerData player = players[i];
                string displayName = string.IsNullOrEmpty(player.DisplayName) ? emptyDisplayName : player.DisplayName;
                string levelText = $"Lv.{Mathf.Max(0, player.HighestCompletedLevel)}";
                bool isCurrentPlayer = PlayerAuthService.Instance != null
                    && !string.IsNullOrEmpty(player.Uid)
                    && player.Uid == PlayerAuthService.Instance.CurrentUserId;

                ApplyRow(rowBindings[i], i + 1, displayName, levelText, isCurrentPlayer);
            }
            else
            {
                ApplyRow(rowBindings[i], i + 1, emptyDisplayName, emptyLevelText, false);
            }
        }

        ResetScrollToTop();
    }

    private void OnLeaderboardLoadFailed(Exception exception)
    {
        Debug.LogError($"[PanelLeaderBoard] Failed to load leaderboard: {exception}");
        FillEmptyRows();
        ShowLocalPlayerRow();
    }

    private void CacheRowDefaultColors()
    {
        if (rowDefaultColorsCached)
        {
            return;
        }

        for (int i = 0; i < rowBindings.Count; i++)
        {
            LeaderboardRowBinding binding = rowBindings[i];
            if (binding != null && binding.displayNameText != null)
            {
                binding.defaultDisplayNameColor = binding.displayNameText.color;
            }
        }

        rowDefaultColorsCached = true;
    }

    private void ResetScrollToTop()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void FillEmptyRows()
    {
        for (int i = 0; i < rowBindings.Count; i++)
        {
            ApplyRow(rowBindings[i], i + 1, emptyDisplayName, emptyLevelText, false);
        }
    }

    private void ApplyRow(LeaderboardRowBinding binding, int rank, string displayName, string levelText, bool isCurrentPlayer)
    {
        if (binding == null)
        {
            return;
        }

        if (binding.root != null)
        {
            binding.root.gameObject.SetActive(true);
        }

        if (binding.displayNameText != null)
        {
            binding.displayNameText.text = showRankPrefix ? $"{rank}. {displayName}" : displayName;
            binding.displayNameText.color = isCurrentPlayer ? selfDisplayNameColor : binding.defaultDisplayNameColor;
        }

        if (binding.levelText != null)
        {
            binding.levelText.text = levelText;
        }
    }

    private void RefreshLockByLevel()
    {
        if (lockObjectAtLevel20 == null)
        {
            return;
        }

        lockObjectAtLevel20.SetActive(!IsLeaderboardUnlockedByLevel());
    }

    private bool IsLeaderboardUnlockedByLevel()
    {
        return LevelManager.Instance != null && LevelManager.Instance.CurrentNormalLevelNumber >= lockAtLevel;
    }
}
