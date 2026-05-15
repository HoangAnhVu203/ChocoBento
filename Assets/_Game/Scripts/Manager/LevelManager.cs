using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class LevelManager : MonoBehaviour
{
    private const string CurrentLevelKey = "ChocoBento.CurrentLevelIndex";

    private static LevelManager instance;

    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LevelManager>();
                if (instance == null)
                {
                    instance = new GameObject("Level Manager").AddComponent<LevelManager>();
                }
            }

            return instance;
        }
        private set => instance = value;
    }

    [SerializeField] private Transform levelParent;
    [SerializeField] private LevelConfig[] levels;

    private GameObject currentLevelInstance;

    public int CurrentLevelIndex { get; private set; }
    public int CurrentCutCount => GetLevel(CurrentLevelIndex).CutCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if (LevelCount == 0)
        {
            return;
        }

        var nextIndex = (CurrentLevelIndex + 1) % LevelCount;
        SetCurrentLevelIndex(nextIndex);
    }

    public void ClearCurrentLevel()
    {
        if (currentLevelInstance == null)
        {
            return;
        }

        Destroy(currentLevelInstance);
        currentLevelInstance = null;
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

    private int LevelCount => levels != null ? levels.Length : 0;
}

[Serializable]
public struct LevelConfig
{
    [SerializeField] private GameObject prefab;
    [SerializeField, Range(0, 2)] private int cutCount;

    public GameObject Prefab => prefab;
    public int CutCount => cutCount;

    public LevelConfig(GameObject prefab, int cutCount)
    {
        this.prefab = prefab;
        this.cutCount = Mathf.Clamp(cutCount, 0, 2);
    }
}
