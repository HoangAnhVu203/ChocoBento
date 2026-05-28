// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.UI;

// public class PanelLock : UICanvas
// {
//     [Serializable]
//     private class LeaderboardRowBinding
//     {
//         public Transform root;
//         public Text displayNameText;
//         public Text levelText;
//         public Color defaultDisplayNameColor;
//     }

//     [Header("Leaderboard UI")]
//     [SerializeField] private Transform contentRoot;
//     [SerializeField] private ScrollRect scrollRect;
//     [SerializeField] private int maxRows = 10;
//     [SerializeField] private bool showRankPrefix = true;
//     [SerializeField] private string emptyDisplayName = "---";
//     [SerializeField] private string emptyLevelText = "Lv.0";
//     [SerializeField] private Color selfDisplayNameColor = new Color(0.95f, 0.2f, 0.2f, 1f);

//     [Header("Lock By Level")]
//     [SerializeField] private GameObject lockObjectAtLevel50;
//     [SerializeField] private int lockAtLevel = 50;

//     private readonly List<LeaderboardRowBinding> rowBindings = new List<LeaderboardRowBinding>();
//     private Coroutine loadLeaderboardCoroutine;
//     private Coroutine userNamePromptCoroutine;

//     public override void SetUp()
//     {
//         base.SetUp();
//         Debug.Log("[PanelLock] SetUp");
//         CacheRowsIfNeeded();
//         FillEmptyRows();
//         ResetScrollToTop();
//     }

//     public override void Open()
//     {
//         base.Open();
//         Debug.Log("[PanelLock] Open");
//         RefreshLeaderboard();
//     }

//     private void OnEnable()
//     {
//         Debug.Log("[PanelLock] OnEnable");
//         CacheRowsIfNeeded();
//         FillEmptyRows();
//         ResetScrollToTop();
//         RefreshLockByLevel();

//         if (Application.isPlaying)
//         {
//             RefreshLeaderboard();
//         }

//         PanelEnterUserName.UserNameSaved -= HandleUserNameSaved;
//         PanelEnterUserName.UserNameSaved += HandleUserNameSaved;
//     }

//     private void OnDisable()
//     {
//         Debug.Log("[PanelLock] OnDisable");
//         PanelEnterUserName.UserNameSaved -= HandleUserNameSaved;

//         if (loadLeaderboardCoroutine != null)
//         {
//             StopCoroutine(loadLeaderboardCoroutine);
//             loadLeaderboardCoroutine = null;
//         }

//         if (userNamePromptCoroutine != null)
//         {
//             StopCoroutine(userNamePromptCoroutine);
//             userNamePromptCoroutine = null;
//         }
//     }

//     public void NotifySelectedFromTab()
//     {
//         RefreshLockByLevel();

//         if (ShouldStartUserNamePrompt())
//         {
//             StartUserNamePromptWhenReady();
//             return;
//         }

//         RefreshLeaderboard();
//     }

//     private void RefreshLeaderboard()
//     {
//         if (loadLeaderboardCoroutine != null)
//         {
//             StopCoroutine(loadLeaderboardCoroutine);
//         }

//         loadLeaderboardCoroutine = StartCoroutine(LoadLeaderboardWhenReady());
//     }

//     private bool ShouldPromptForUserName()
//     {
//         if (!Application.isPlaying)
//         {
//             return false;
//         }

//         if (!IsLeaderboardUnlockedByLevel())
//         {
//             return false;
//         }

//         return LeaderBoardService.Instance != null
//             && LeaderBoardService.Instance.CanSyncUnlockedLeaderboard()
//             && !LeaderBoardService.Instance.HasLocalDisplayName();
//     }

//     private bool ShouldStartUserNamePrompt()
//     {
//         if (!Application.isPlaying || !IsLeaderboardUnlockedByLevel())
//         {
//             return false;
//         }

//         return LeaderBoardService.Instance == null
//             || (LeaderBoardService.Instance.CanSyncUnlockedLeaderboard()
//                 && !LeaderBoardService.Instance.HasLocalDisplayName());
//     }

//     private void StartUserNamePromptWhenReady()
//     {
//         if (userNamePromptCoroutine != null)
//         {
//             StopCoroutine(userNamePromptCoroutine);
//         }

//         userNamePromptCoroutine = StartCoroutine(OpenUserNamePromptWhenReady());
//     }

//     private IEnumerator OpenUserNamePromptWhenReady()
//     {
//         const float timeoutSeconds = 10f;
//         float elapsed = 0f;

//         while (elapsed < timeoutSeconds)
//         {
//             if (!IsLeaderboardUnlockedByLevel())
//             {
//                 userNamePromptCoroutine = null;
//                 yield break;
//             }

//             bool ready = LeaderBoardService.Instance != null
//                 && FirebaseBootstrap.Instance != null
//                 && FirebaseBootstrap.Instance.IsInitialized
//                 && PlayerAuthService.Instance != null
//                 && PlayerAuthService.Instance.IsSignedIn;

//             if (ready)
//             {
//                 break;
//             }

//             elapsed += Time.unscaledDeltaTime;
//             yield return null;
//         }

//         if (UIManager.Instance != null && UIManager.Instance.IsUIOpened<PanelEnterUserName>())
//         {
//             userNamePromptCoroutine = null;
//             yield break;
//         }

//         if (ShouldPromptForUserName() && UIManager.Instance != null)
//         {
//             UIManager.Instance.OpenUI<PanelEnterUserName>();
//         }

//         userNamePromptCoroutine = null;
//     }

//     private void HandleUserNameSaved(string _)
//     {
//         RefreshLeaderboard();
//     }

//     private IEnumerator LoadLeaderboardWhenReady()
//     {
//         const float timeoutSeconds = 10f;
//         float elapsed = 0f;

//         while (elapsed < timeoutSeconds)
//         {
//             bool firebaseReady = FirebaseBootstrap.Instance != null && FirebaseBootstrap.Instance.IsInitialized;
//             bool authReady = PlayerAuthService.Instance != null && PlayerAuthService.Instance.IsSignedIn;
//             bool leaderboardReady = LeaderBoardService.Instance != null;

//             if (firebaseReady && authReady && leaderboardReady)
//             {
//                 break;
//             }

//             elapsed += Time.unscaledDeltaTime;
//             yield return null;
//         }

//         if (LeaderBoardService.Instance == null)
//         {
//             Debug.LogWarning("[PanelLock] LeaderBoardService is not available.");
//             yield break;
//         }

//         if (FirebaseBootstrap.Instance == null || !FirebaseBootstrap.Instance.IsInitialized
//             || PlayerAuthService.Instance == null || !PlayerAuthService.Instance.IsSignedIn)
//         {
//             Debug.LogWarning("[PanelLock] Timed out waiting for Firebase/Auth. Leaderboard query was skipped.");
//             yield break;
//         }

//         int limit = Mathf.Max(1, rowBindings.Count > 0 ? rowBindings.Count : maxRows);
//         Debug.Log($"[PanelLock] Requesting top {limit} leaderboard player(s).");
//         LeaderBoardService.Instance.GetTopPlayers(limit, OnLeaderboardLoaded, OnLeaderboardLoadFailed);

//         loadLeaderboardCoroutine = null;
//     }

//     private void OnLeaderboardLoaded(List<LeaderboardPlayerData> players)
//     {
//         CacheRowsIfNeeded();
//         Debug.Log($"[PanelLock] Received {players?.Count ?? 0} leaderboard player(s).");

//         for (int i = 0; i < rowBindings.Count; i++)
//         {
//             if (players != null && i < players.Count)
//             {
//                 LeaderboardPlayerData player = players[i];
//                 string displayName = string.IsNullOrEmpty(player.DisplayName) ? emptyDisplayName : player.DisplayName;
//                 string levelText = $"Lv.{Mathf.Max(0, player.HighestCompletedLevel)}";
//                 bool isCurrentPlayer = PlayerAuthService.Instance != null
//                     && !string.IsNullOrEmpty(player.Uid)
//                     && player.Uid == PlayerAuthService.Instance.CurrentUserId;

//                 ApplyRow(rowBindings[i], i + 1, displayName, levelText, isCurrentPlayer);
//             }
//             else
//             {
//                 ApplyRow(rowBindings[i], i + 1, emptyDisplayName, emptyLevelText, false);
//             }
//         }

//         ResetScrollToTop();
//     }

//     private void OnLeaderboardLoadFailed(Exception exception)
//     {
//         Debug.LogError($"[PanelLock] Failed to load leaderboard: {exception}");
//         FillEmptyRows();
//     }

//     private void CacheRowsIfNeeded()
//     {
//         if (contentRoot == null)
//         {
//             contentRoot = FindContentRoot();
//         }

//         rowBindings.Clear();
//         if (contentRoot == null)
//         {
//             Debug.LogWarning("[PanelLock] Content root was not found.");
//             return;
//         }

//         int childCount = Mathf.Min(contentRoot.childCount, Mathf.Max(1, maxRows));
//         for (int i = 0; i < childCount; i++)
//         {
//             Transform rowRoot = contentRoot.GetChild(i);
//             LeaderboardRowBinding binding = BuildRowBinding(rowRoot);
//             if (binding != null)
//             {
//                 rowBindings.Add(binding);
//             }
//         }
//     }

//     private LeaderboardRowBinding BuildRowBinding(Transform rowRoot)
//     {
//         if (rowRoot == null)
//         {
//             return null;
//         }

//         Text[] texts = rowRoot.GetComponentsInChildren<Text>(true);
//         if (texts == null || texts.Length < 2)
//         {
//             Debug.LogWarning($"[PanelLock] Row '{rowRoot.name}' does not contain enough Text components.");
//             return null;
//         }

//         Text levelText = texts.FirstOrDefault(text => text != null && text.text.IndexOf("lv", StringComparison.OrdinalIgnoreCase) >= 0);
//         if (levelText == null)
//         {
//             levelText = texts.OrderByDescending(GetTextSortX).FirstOrDefault();
//         }

//         Text displayNameText = texts
//             .Where(text => text != null && text != levelText)
//             .OrderBy(GetTextSortX)
//             .FirstOrDefault();

//         if (displayNameText == null || levelText == null)
//         {
//             Debug.LogWarning($"[PanelLock] Row '{rowRoot.name}' could not auto-resolve displayName/level texts.");
//             return null;
//         }

//         return new LeaderboardRowBinding
//         {
//             root = rowRoot,
//             displayNameText = displayNameText,
//             levelText = levelText,
//             defaultDisplayNameColor = displayNameText.color
//         };
//     }

//     private float GetTextSortX(Text text)
//     {
//         RectTransform rect = text != null ? text.rectTransform : null;
//         return rect != null ? rect.anchoredPosition.x : 0f;
//     }

//     private Transform FindContentRoot()
//     {
//         Transform directPath = transform.Find("Scroll View/Viewport/Content");
//         if (directPath != null)
//         {
//             return directPath;
//         }

//         Transform[] children = GetComponentsInChildren<Transform>(true);
//         return children.FirstOrDefault(t => t != null && t.name == "Content");
//     }

//     private void ResetScrollToTop()
//     {
//         if (scrollRect == null)
//         {
//             scrollRect = GetComponentInChildren<ScrollRect>(true);
//         }

//         if (scrollRect == null)
//         {
//             return;
//         }

//         Canvas.ForceUpdateCanvases();
//         scrollRect.StopMovement();
//         scrollRect.verticalNormalizedPosition = 1f;
//     }

//     private void FillEmptyRows()
//     {
//         CacheRowsIfNeeded();
//         for (int i = 0; i < rowBindings.Count; i++)
//         {
//             ApplyRow(rowBindings[i], i + 1, emptyDisplayName, emptyLevelText, false);
//         }
//     }

//     private void ApplyRow(LeaderboardRowBinding binding, int rank, string displayName, string levelText, bool isCurrentPlayer)
//     {
//         if (binding == null)
//         {
//             return;
//         }

//         if (binding.root != null)
//         {
//             binding.root.gameObject.SetActive(true);
//         }

//         if (binding.displayNameText != null)
//         {
//             binding.displayNameText.text = showRankPrefix ? $"{rank}. {displayName}" : displayName;
//             binding.displayNameText.color = isCurrentPlayer ? selfDisplayNameColor : binding.defaultDisplayNameColor;
//         }

//         if (binding.levelText != null)
//         {
//             binding.levelText.text = levelText;
//         }
//     }

//     void RefreshLockByLevel()
//     {
//         if (lockObjectAtLevel50 == null) return;
//         lockObjectAtLevel50.SetActive(!IsLeaderboardUnlockedByLevel());
//     }

//     private bool IsLeaderboardUnlockedByLevel()
//     {
//         if (LevelManager.Instance == null) return false;
//         return LevelManager.Instance.CurrentNormalLevelNumber >= lockAtLevel;
//     }

// }
