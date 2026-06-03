 using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class LevelManager : Singleton<LevelManager>
{
    private const string CurrentLevelKey = "ChocoBento.CurrentLevelIndex";
    private const string LevelStarKeyPrefix = "ChocoBento.LevelStars.";

    [SerializeField] private Transform levelParent;
    [SerializeField] private LevelConfig[] levels;

    private GameObject currentLevelInstance;

    public int CurrentLevelIndex { get; private set; }
    public int CurrentNormalLevelNumber => CurrentLevelIndex + 1;
    public int CurrentCutCount => GetLevel(CurrentLevelIndex).CutCount;
    public bool CurrentLevelHasTimer => GetLevel(CurrentLevelIndex).HasTimer;
    public float CurrentLevelTimeLimitSeconds => GetLevel(CurrentLevelIndex).TimeLimitSeconds;
    public int LevelCount => levels != null ? levels.Length : 0;

    private void Awake()
    {
        TryAutoFillLevelsInEditor();
        CurrentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(CurrentLevelKey, 0), 0, Mathf.Max(0, LevelCount - 1));
    }

    public void LoadCurrentLevel(bool restart)
    {
        LoadLevel(CurrentLevelIndex, restart);
    }

    public void LoadNextLevel(bool restart)
    {
        var nextIndex = LevelCount > 0 ? (CurrentLevelIndex + 1) % LevelCount : 0;
        SetCurrentLevelIndex(nextIndex);
        LoadLevel(CurrentLevelIndex, restart);
    }

    public void CompleteCurrentLevel()
    {
        CompleteCurrentLevel(0);
    }

    public void CompleteCurrentLevel(int earnedStars)
    {
        if (LevelCount == 0)
        {
            return;
        }

        SaveBestStars(CurrentLevelIndex, earnedStars);
        LeaderBoardService.Instance?.ReportLevelCompleted(CurrentNormalLevelNumber);
        var nextIndex = (CurrentLevelIndex + 1) % LevelCount;
        SetCurrentLevelIndex(nextIndex);
    }

    public void SelectLevel(int levelIndex)
    {
        SetCurrentLevelIndex(levelIndex);
    }

    public void PlayLevel(int levelIndex)
    {
        SelectLevel(levelIndex);
        GameManager.Instance.PlayCurrentLevel();
    }

    public int GetBestStars(int levelIndex)
    {
        if (LevelCount == 0)
        {
            return 0;
        }

        levelIndex = Mathf.Clamp(levelIndex, 0, LevelCount - 1);
        return Mathf.Clamp(PlayerPrefs.GetInt(GetLevelStarKey(levelIndex), 0), 0, 3);
    }

    public void SaveBestStars(int levelIndex, int stars)
    {
        if (LevelCount == 0)
        {
            return;
        }

        levelIndex = Mathf.Clamp(levelIndex, 0, LevelCount - 1);
        stars = Mathf.Clamp(stars, 0, 3);
        if (stars <= GetBestStars(levelIndex))
        {
            return;
        }

        PlayerPrefs.SetInt(GetLevelStarKey(levelIndex), stars);
        PlayerPrefs.Save();
    }

    public void ClearCurrentLevel()
    {
        if (GameplayHintController.Instance != null)
        {
            GameplayHintController.Instance.ResetHint();
        }

        if (currentLevelInstance == null)
        {
            return;
        }

        Destroy(currentLevelInstance);
        currentLevelInstance = null;
    }

    public void GoHome()
    {
        GameManager.Instance.GoHome();
    }

    public void SetLevels(LevelConfig[] newLevels)
    {
        levels = newLevels ?? Array.Empty<LevelConfig>();
        CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, Mathf.Max(0, LevelCount - 1));
    }

    private void LoadLevel(int levelIndex, bool restart)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("[LevelManager] No levels assigned.");
            return;
        }

        if (restart || currentLevelInstance == null)
        {
            ClearCurrentLevel();
            var level = GetLevel(levelIndex);
            if (level.Prefab == null)
            {
                Debug.LogWarning($"[LevelManager] Level {levelIndex + 1} has no prefab.");
                return;
            }

            var parent = levelParent != null ? levelParent : transform;
            currentLevelInstance = Instantiate(level.Prefab, parent);
            currentLevelInstance.name = level.Prefab.name;
        }

        RefreshRuntimeGameplayComponents();
    }

    private LevelConfig GetLevel(int levelIndex)
    {
        if (levels == null || levels.Length == 0)
        {
            return default;
        }

        return levels[Mathf.Clamp(levelIndex, 0, levels.Length - 1)];
    }

    private void SetCurrentLevelIndex(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, LevelCount - 1));
        PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevelIndex);
        PlayerPrefs.Save();
    }

    private static void RefreshRuntimeGameplayComponents()
    {
        if (GameplayHintController.Instance != null)
        {
            GameplayHintController.Instance.ResetHint();
        }
        else if (FindFirstObjectByType<GameplayHintController>() == null)
        {
            new GameObject("Gameplay Hint Controller").AddComponent<GameplayHintController>();
        }

        var targetTray = FindFirstObjectByType<TargetTray>();
        if (targetTray == null)
        {
            var targetTrayTransform = GameObject.Find("TargetTray");
            if (targetTrayTransform != null)
            {
                targetTray = targetTrayTransform.AddComponent<TargetTray>();
            }
        }

        foreach (var piece in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (IsChocolatePieceRoot(piece.name) && piece.GetComponent<ChocolatePiece>() == null)
            {
                piece.gameObject.AddComponent<ChocolatePiece>();
            }
        }

        if (FindFirstObjectByType<GameplayLevelController>() == null)
        {
            new GameObject("Gameplay Level Controller").AddComponent<GameplayLevelController>();
        }
        else
        {
            FindFirstObjectByType<GameplayLevelController>().RefreshLevelObjects();
        }

        if (FindFirstObjectByType<GameplayInputRouter>() == null)
        {
            new GameObject("Gameplay Input Router").AddComponent<GameplayInputRouter>();
        }
    }

    private static bool IsChocolatePieceRoot(string objectName)
    {
        return objectName == "ChocolatePiece"
            || objectName.StartsWith("ChocolatePiece_")
            || objectName.StartsWith("ChocolatePiece (");
    }

    private void TryAutoFillLevelsInEditor()
    {
#if UNITY_EDITOR
        if (levels != null && levels.Length > 0)
        {
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Prefab Level_", new[] { "Assets/_Game/Prefabs/_Level" });
        Array.Sort(guids, (left, right) =>
        {
            var leftPath = AssetDatabase.GUIDToAssetPath(left);
            var rightPath = AssetDatabase.GUIDToAssetPath(right);
            return string.Compare(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
        });

        levels = new LevelConfig[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
            levels[i] = new LevelConfig(prefab, Mathf.Clamp(i % 3, 0, 2));
        }
#endif
    }

    private static string GetLevelStarKey(int levelIndex)
    {
        return $"{LevelStarKeyPrefix}{levelIndex}";
    }
}

[Serializable]
public struct LevelConfig
{
    [SerializeField] private GameObject prefab;
    [SerializeField, Range(0, 2)] private int cutCount;
    [SerializeField] private bool hasTimer;
    [SerializeField, Min(1f)] private float timeLimitSeconds;

    public GameObject Prefab => prefab;
    public int CutCount => cutCount;
    public bool HasTimer => hasTimer;
    public float TimeLimitSeconds => timeLimitSeconds > 0f ? timeLimitSeconds : 60f;

    public LevelConfig(GameObject prefab, int cutCount)
    {
        this.prefab = prefab;
        this.cutCount = Mathf.Clamp(cutCount, 0, 2);
        hasTimer = false;
        timeLimitSeconds = 60f;
    }
}
