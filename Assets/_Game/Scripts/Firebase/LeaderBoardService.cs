// using System;
// using System.Collections.Generic;
// using Firebase.Extensions;
// using Firebase.Firestore;
// using UnityEngine;

// public class LeaderBoardService : MonoBehaviour
// {
//     public static LeaderBoardService Instance { get; private set; }

//     private const string CollectionName = "leaderboard_players";
//     private const string PrefHighestCompletedLevel = "LB_HIGHEST_COMPLETED_LEVEL";
//     private const string PrefDisplayName = "LB_DISPLAY_NAME";
//     private const int MinimumFirestoreSyncLevel = 50;
//     private const string FieldUid = "uid";
//     private const string FieldDisplayName = "displayName";
//     private const string FieldHighestCompletedLevel = "highestCompletedLevel";

//     public bool IsInitialized { get; private set; }

//     private FirebaseFirestore db;
//     private bool isInitializing;

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(this);
//             return;
//         }

//         Instance = this;
//     }

//     public void InitializeAfterSignIn()
//     {
//         if (IsInitialized || isInitializing)
//         {
//             return;
//         }

//         if (PlayerAuthService.Instance == null || !PlayerAuthService.Instance.IsSignedIn)
//         {
//             Debug.LogWarning("[LeaderBoardService] Cannot initialize before Firebase Auth sign-in.");
//             return;
//         }

//         db = FirebaseFirestore.DefaultInstance;
//         isInitializing = true;

//         if (!CanSyncPlayerDataToFirestore())
//         {
//             isInitializing = false;
//             IsInitialized = true;
//             Debug.Log($"[LeaderBoardService] Firestore profile sync is locked until level {MinimumFirestoreSyncLevel}.");
//             return;
//         }

//         EnsurePlayerProfile();
//     }

//     public int GetLocalHighestCompletedLevel()
//     {
//         return PlayerPrefs.GetInt(PrefHighestCompletedLevel, 0);
//     }

//     public bool HasLocalDisplayName()
//     {
//         return !string.IsNullOrWhiteSpace(GetLocalDisplayName());
//     }

//     public bool CanSyncUnlockedLeaderboard()
//     {
//         return GetLeaderboardEligibleLevel() >= MinimumFirestoreSyncLevel;
//     }

//     public string GetLocalDisplayName()
//     {
//         return PlayerPrefs.GetString(PrefDisplayName, string.Empty).Trim();
//     }

//     public bool ValidateDisplayName(string rawName, out string normalizedName, out string errorMessage)
//     {
//         normalizedName = NormalizeDisplayName(rawName);
//         errorMessage = string.Empty;

//         if (string.IsNullOrEmpty(normalizedName))
//         {
//             errorMessage = "Please enter a username.";
//             return false;
//         }

//         if (normalizedName.Length < 3)
//         {
//             errorMessage = "Username must be at least 3 characters.";
//             return false;
//         }

//         if (normalizedName.Length > 16)
//         {
//             errorMessage = "Username must be 16 characters or fewer.";
//             return false;
//         }

//         foreach (char c in normalizedName)
//         {
//             bool valid = char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-';
//             if (!valid)
//             {
//                 errorMessage = "Only letters, numbers, spaces, _ and - are allowed.";
//                 return false;
//             }
//         }

//         return true;
//     }

//     public void SaveDisplayName(string rawName, Action<string> onSuccess, Action<string> onError)
//     {
//         if (!ValidateDisplayName(rawName, out string normalizedName, out string validationError))
//         {
//             onError?.Invoke(validationError);
//             return;
//         }

//         SetLocalDisplayName(normalizedName);

//         if (!CanSyncPlayerDataToFirestore())
//         {
//             Debug.Log($"[LeaderBoardService] Saved displayName locally. Firestore sync is locked until level {MinimumFirestoreSyncLevel}.");
//             onSuccess?.Invoke(normalizedName);
//             return;
//         }

//         if (!CanUseFirestore())
//         {
//             onError?.Invoke("Firebase is not ready yet. Please try again.");
//             return;
//         }

//         DocumentReference document = GetPlayerDocument();
//         if (document == null)
//         {
//             onError?.Invoke("Player profile is unavailable.");
//             return;
//         }

//         Dictionary<string, object> updates = new Dictionary<string, object>
//         {
//             { FieldUid, PlayerAuthService.Instance.CurrentUserId },
//             { FieldDisplayName, normalizedName },
//             { FieldHighestCompletedLevel, GetLeaderboardEligibleLevel() },
//             { "updatedAt", FieldValue.ServerTimestamp }
//         };

//         document.SetAsync(updates, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
//         {
//             if (task.IsCanceled)
//             {
//                 onError?.Invoke("Save username was canceled.");
//                 return;
//             }

//             if (task.IsFaulted)
//             {
//                 Debug.LogError($"[LeaderBoardService] Failed to save display name: {task.Exception}");
//                 onError?.Invoke("Failed to save username. Please try again.");
//                 return;
//             }

//             Debug.Log($"[LeaderBoardService] Saved displayName = {normalizedName}");
//             onSuccess?.Invoke(normalizedName);
//         });
//     }

//     public void ReportLevelCompleted(int completedLevel)
//     {
//         if (completedLevel <= 0)
//         {
//             return;
//         }

//         int localBest = GetLocalHighestCompletedLevel();
//         if (completedLevel <= localBest)
//         {
//             return;
//         }

//         SetLocalHighestCompletedLevel(completedLevel);

//         if (!CanSyncPlayerDataToFirestore())
//         {
//             Debug.Log($"[LeaderBoardService] Saved level {completedLevel} locally. Firestore sync starts at level {MinimumFirestoreSyncLevel}.");
//             return;
//         }

//         if (!CanUseFirestore())
//         {
//             Debug.Log($"[LeaderBoardService] Saved level {completedLevel} locally. Firestore sync will retry later.");
//             return;
//         }

//         SyncCompletedLevelToRemote(completedLevel);
//     }

//     public void TrySyncLocalProgressToRemote()
//     {
//         if (!CanSyncPlayerDataToFirestore())
//         {
//             return;
//         }

//         int localBest = GetLocalHighestCompletedLevel();
//         if (localBest > 0)
//         {
//             SyncCompletedLevelToRemote(localBest);
//         }
//     }

//     public void GetTopPlayers(int limit, Action<List<LeaderboardPlayerData>> onSuccess, Action<Exception> onError = null)
//     {
//         if (!CanUseFirestore())
//         {
//             onError?.Invoke(new InvalidOperationException("Firebase Auth or Firestore is not ready."));
//             return;
//         }

//         int safeLimit = Mathf.Max(1, limit);

//         db.Collection(CollectionName)
//             .OrderByDescending(FieldHighestCompletedLevel)
//             .Limit(safeLimit)
//             .GetSnapshotAsync()
//             .ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsCanceled)
//                 {
//                     onError?.Invoke(new OperationCanceledException("Leaderboard query was canceled."));
//                     return;
//                 }

//                 if (task.IsFaulted)
//                 {
//                     onError?.Invoke(task.Exception);
//                     return;
//                 }

//                 List<LeaderboardPlayerData> players = new List<LeaderboardPlayerData>();
//                 foreach (DocumentSnapshot document in task.Result.Documents)
//                 {
//                     LeaderboardPlayerData player = CreatePlayerDataFromDocument(document);

//                     players.Add(player);
//                 }

//                 Debug.Log($"[LeaderBoardService] Loaded {players.Count} leaderboard player(s).");
//                 onSuccess?.Invoke(players);
//             });
//     }

//     private void EnsurePlayerProfile()
//     {
//         if (!CanSyncPlayerDataToFirestore())
//         {
//             isInitializing = false;
//             IsInitialized = true;
//             return;
//         }

//         DocumentReference document = GetPlayerDocument();
//         if (document == null)
//         {
//             isInitializing = false;
//             return;
//         }

//         document.GetSnapshotAsync().ContinueWithOnMainThread(task =>
//         {
//             if (task.IsCanceled)
//             {
//                 isInitializing = false;
//                 Debug.LogError("[LeaderBoardService] Load player profile was canceled.");
//                 return;
//             }

//             if (task.IsFaulted)
//             {
//                 isInitializing = false;
//                 Debug.LogError($"[LeaderBoardService] Failed to load player profile: {task.Exception}");
//                 return;
//             }

//             DocumentSnapshot snapshot = task.Result;
//             if (snapshot.Exists)
//             {
//                 isInitializing = false;
//                 IsInitialized = true;
//                 Debug.Log("[LeaderBoardService] Player profile already exists.");
//                 TrySyncLocalProgressToRemote();
//                 return;
//             }

//             Dictionary<string, object> profile = BuildDefaultProfile();
//             document.SetAsync(profile).ContinueWithOnMainThread(setTask =>
//             {
//                 isInitializing = false;

//                 if (setTask.IsCanceled)
//                 {
//                     Debug.LogError("[LeaderBoardService] Create player profile was canceled.");
//                     return;
//                 }

//                 if (setTask.IsFaulted)
//                 {
//                     Debug.LogError($"[LeaderBoardService] Failed to create player profile: {setTask.Exception}");
//                     return;
//                 }

//                 IsInitialized = true;
//                 Debug.Log("[LeaderBoardService] Player profile created successfully.");
//                 TrySyncLocalProgressToRemote();
//             });
//         });
//     }

//     private void SyncCompletedLevelToRemote(int completedLevel)
//     {
//         if (completedLevel < MinimumFirestoreSyncLevel)
//         {
//             return;
//         }

//         DocumentReference document = GetPlayerDocument();
//         if (document == null)
//         {
//             return;
//         }

//         document.GetSnapshotAsync().ContinueWithOnMainThread(task =>
//         {
//             if (task.IsCanceled)
//             {
//                 Debug.LogError("[LeaderBoardService] Sync leaderboard was canceled.");
//                 return;
//             }

//             if (task.IsFaulted)
//             {
//                 Debug.LogError($"[LeaderBoardService] Failed to read leaderboard profile before sync: {task.Exception}");
//                 return;
//             }

//             DocumentSnapshot snapshot = task.Result;
//             int remoteBest = 0;
//             if (snapshot.Exists)
//             {
//                 snapshot.TryGetValue(FieldHighestCompletedLevel, out remoteBest);
//             }

//             if (completedLevel <= remoteBest)
//             {
//                 IsInitialized = true;
//                 return;
//             }

//             string uid = PlayerAuthService.Instance.CurrentUserId;
//             Dictionary<string, object> updates = new Dictionary<string, object>
//             {
//                 { FieldHighestCompletedLevel, completedLevel },
//                 { "updatedAt", FieldValue.ServerTimestamp }
//             };

//             if (!snapshot.Exists)
//             {
//                 updates[FieldUid] = uid;
//                 updates[FieldDisplayName] = GetPreferredDisplayName(uid);
//                 updates["createdAt"] = FieldValue.ServerTimestamp;
//             }

//             document.SetAsync(updates, SetOptions.MergeAll).ContinueWithOnMainThread(updateTask =>
//             {
//                 if (updateTask.IsCanceled)
//                 {
//                     Debug.LogError("[LeaderBoardService] Update leaderboard was canceled.");
//                     return;
//                 }

//                 if (updateTask.IsFaulted)
//                 {
//                     Debug.LogError($"[LeaderBoardService] Failed to update leaderboard: {updateTask.Exception}");
//                     return;
//                 }

//                 IsInitialized = true;
//                 Debug.Log($"[LeaderBoardService] Synced highestCompletedLevel = {completedLevel}.");
//             });
//         });
//     }

//     private void SetLocalHighestCompletedLevel(int completedLevel)
//     {
//         int current = GetLocalHighestCompletedLevel();
//         if (completedLevel <= current)
//         {
//             return;
//         }

//         PlayerPrefs.SetInt(PrefHighestCompletedLevel, completedLevel);
//         PlayerPrefs.Save();
//     }

//     private DocumentReference GetPlayerDocument()
//     {
//         string uid = PlayerAuthService.Instance != null ? PlayerAuthService.Instance.CurrentUserId : string.Empty;
//         if (string.IsNullOrEmpty(uid))
//         {
//             Debug.LogWarning("[LeaderBoardService] Current Firebase user id is empty.");
//             return null;
//         }

//         if (db == null)
//         {
//             db = FirebaseFirestore.DefaultInstance;
//         }

//         return db.Collection(CollectionName).Document(uid);
//     }

//     private bool CanUseFirestore()
//     {
//         return FirebaseBootstrap.Instance != null
//             && FirebaseBootstrap.Instance.IsInitialized
//             && PlayerAuthService.Instance != null
//             && PlayerAuthService.Instance.IsSignedIn;
//     }

//     private bool CanSyncPlayerDataToFirestore()
//     {
//         return GetLeaderboardEligibleLevel() >= MinimumFirestoreSyncLevel;
//     }

//     private int GetLeaderboardEligibleLevel()
//     {
//         int localBest = GetLocalHighestCompletedLevel();

//         if (LevelManager.Instance != null)
//             localBest = Mathf.Max(localBest, LevelManager.Instance.CurrentNormalLevelNumber);

//         return localBest;
//     }

//     private Dictionary<string, object> BuildDefaultProfile()
//     {
//         string uid = PlayerAuthService.Instance.CurrentUserId;

//         return new Dictionary<string, object>
//         {
//             { FieldUid, uid },
//             { FieldDisplayName, GetPreferredDisplayName(uid) },
//             { FieldHighestCompletedLevel, GetLeaderboardEligibleLevel() },
//             { "createdAt", FieldValue.ServerTimestamp },
//             { "updatedAt", FieldValue.ServerTimestamp }
//         };
//     }

//     private string BuildDisplayName(string uid)
//     {
//         if (string.IsNullOrEmpty(uid))
//         {
//             return "Player";
//         }

//         string suffix = uid.Length <= 4 ? uid : uid.Substring(uid.Length - 4);
//         return $"Player_{suffix.ToUpper()}";
//     }

//     private string GetPreferredDisplayName(string uid)
//     {
//         string localDisplayName = GetLocalDisplayName();
//         return string.IsNullOrEmpty(localDisplayName) ? BuildDisplayName(uid) : localDisplayName;
//     }

//     private string NormalizeDisplayName(string rawName)
//     {
//         if (string.IsNullOrWhiteSpace(rawName))
//         {
//             return string.Empty;
//         }

//         string trimmed = rawName.Trim();
//         List<char> chars = new List<char>(trimmed.Length);
//         bool previousWasSpace = false;

//         for (int i = 0; i < trimmed.Length; i++)
//         {
//             char c = trimmed[i];
//             bool isSpace = char.IsWhiteSpace(c);
//             if (isSpace)
//             {
//                 if (previousWasSpace)
//                 {
//                     continue;
//                 }

//                 chars.Add(' ');
//                 previousWasSpace = true;
//                 continue;
//             }

//             chars.Add(c);
//             previousWasSpace = false;
//         }

//         return new string(chars.ToArray());
//     }

//     private void SetLocalDisplayName(string displayName)
//     {
//         PlayerPrefs.SetString(PrefDisplayName, displayName);
//         PlayerPrefs.Save();
//     }

//     private LeaderboardPlayerData CreatePlayerDataFromDocument(DocumentSnapshot document)
//     {
//         string uid = document != null ? document.Id : string.Empty;
//         string displayName = GetPreferredDisplayName(uid);
//         int highestCompletedLevel = 0;

//         if (document != null)
//         {
//             if (document.TryGetValue(FieldUid, out string storedUid) && !string.IsNullOrEmpty(storedUid))
//             {
//                 uid = storedUid;
//             }

//             if (document.TryGetValue(FieldDisplayName, out string storedDisplayName) && !string.IsNullOrEmpty(storedDisplayName))
//             {
//                 displayName = storedDisplayName;
//             }

//             document.TryGetValue(FieldHighestCompletedLevel, out highestCompletedLevel);
//         }

//         return new LeaderboardPlayerData
//         {
//             Uid = uid,
//             DisplayName = displayName,
//             HighestCompletedLevel = highestCompletedLevel
//         };
//     }
// }
