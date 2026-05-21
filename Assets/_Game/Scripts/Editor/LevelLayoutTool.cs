#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class LevelLayoutTool : EditorWindow
{
    private const string DefaultChocolateCellPath = "Assets/_Game/Prefabs/ChocolateCell.prefab";
    private const string DefaultStrawberryCellPath = "Assets/_Game/Prefabs/ChocolateStrawberryCell.prefab";
    private const string DefaultTraySlotPath = "Assets/_Game/Prefabs/TraySlot.prefab";
    private const string DefaultTrayStrawberryPath = "Assets/_Game/Prefabs/StrawberryHolder.prefab";
    private const string DefaultRockPath = "Assets/_Game/Prefabs/Rock.prefab";

    [SerializeField] private Transform levelRoot;
    [SerializeField] private Transform chocolateParent;
    [SerializeField] private Transform trayParent;
    [SerializeField] private GameObject chocolateCellPrefab;
    [SerializeField] private GameObject chocolateStrawberryCellPrefab;
    [SerializeField] private GameObject traySlotPrefab;
    [SerializeField] private GameObject trayStrawberryPrefab;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject overflowSlotPrefab;
    [SerializeField] private Vector2Int originCell;
    [SerializeField] private string chocolatePattern = "CCCC\nCCCC\nCCCC";
    [SerializeField] private string trayPattern = "TTT\nTST\nTRT";

    private Grid grid;
    private Tilemap tilemap;

    [MenuItem("ChocoBento/Level Layout Tool")]
    private static void Open()
    {
        var window = GetWindow<LevelLayoutTool>("Level Layout");
        window.minSize = new Vector2(360f, 520f);
        window.LoadDefaultPrefabs();
    }

    private void OnEnable()
    {
        LoadDefaultPrefabs();
        RefreshGridReferences();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Parents", EditorStyles.boldLabel);
        levelRoot = (Transform)EditorGUILayout.ObjectField("Level Root", levelRoot, typeof(Transform), true);
        chocolateParent = (Transform)EditorGUILayout.ObjectField("Chocolate Parent", chocolateParent, typeof(Transform), true);
        trayParent = (Transform)EditorGUILayout.ObjectField("Tray Parent", trayParent, typeof(Transform), true);

        if (GUILayout.Button("Create Missing Parents"))
        {
            CreateMissingParents();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        chocolateCellPrefab = (GameObject)EditorGUILayout.ObjectField("Chocolate Cell", chocolateCellPrefab, typeof(GameObject), false);
        chocolateStrawberryCellPrefab = (GameObject)EditorGUILayout.ObjectField("Chocolate Strawberry", chocolateStrawberryCellPrefab, typeof(GameObject), false);
        traySlotPrefab = (GameObject)EditorGUILayout.ObjectField("Tray Slot", traySlotPrefab, typeof(GameObject), false);
        trayStrawberryPrefab = (GameObject)EditorGUILayout.ObjectField("Tray Strawberry", trayStrawberryPrefab, typeof(GameObject), false);
        rockPrefab = (GameObject)EditorGUILayout.ObjectField("Rock", rockPrefab, typeof(GameObject), false);
        overflowSlotPrefab = (GameObject)EditorGUILayout.ObjectField("Overflow Slot", overflowSlotPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Reload Default Prefabs"))
        {
            LoadDefaultPrefabs();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            originCell = EditorGUILayout.Vector2IntField("Origin Cell", originCell);
            if (GUILayout.Button("Use Selection", GUILayout.Width(110f)))
            {
                UseSelectedObjectCell();
            }
        }

        EditorGUILayout.HelpBox(
            "Pattern reads top-to-bottom. Chocolate: C = normal, S = strawberry, . or space = empty. Tray: T/F = fill slot, S = strawberry, R = rock, O = overflow, . or space = empty.",
            MessageType.Info);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Chocolate Piece Pattern", EditorStyles.boldLabel);
        chocolatePattern = EditorGUILayout.TextArea(chocolatePattern, GUILayout.MinHeight(86f));
        if (GUILayout.Button("Create Chocolate Piece"))
        {
            CreateChocolatePiece();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Target Tray Pattern", EditorStyles.boldLabel);
        trayPattern = EditorGUILayout.TextArea(trayPattern, GUILayout.MinHeight(86f));
        if (GUILayout.Button("Create Target Tray"))
        {
            CreateTargetTray();
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Refresh Grid References"))
        {
            RefreshGridReferences();
        }
    }

    private void LoadDefaultPrefabs()
    {
        if (chocolateCellPrefab == null)
        {
            chocolateCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultChocolateCellPath);
        }

        if (chocolateStrawberryCellPrefab == null)
        {
            chocolateStrawberryCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultStrawberryCellPath);
        }

        if (traySlotPrefab == null)
        {
            traySlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTraySlotPath);
        }

        if (trayStrawberryPrefab == null)
        {
            trayStrawberryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTrayStrawberryPath);
        }

        if (rockPrefab == null)
        {
            rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultRockPath);
        }
    }

    private void RefreshGridReferences()
    {
        tilemap = FindFirstObjectByType<Tilemap>();
        grid = tilemap != null ? tilemap.layoutGrid : FindFirstObjectByType<Grid>();
    }

    private void CreateMissingParents()
    {
        if (levelRoot == null)
        {
            var root = new GameObject("LevelRoot");
            Undo.RegisterCreatedObjectUndo(root, "Create Level Root");
            levelRoot = root.transform;
        }

        if (chocolateParent == null)
        {
            chocolateParent = CreateChildParent("ChocolatePieces", levelRoot);
        }

        if (trayParent == null)
        {
            trayParent = CreateChildParent("Trays", levelRoot);
        }
    }

    private static Transform CreateChildParent(string name, Transform parent)
    {
        var child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private void UseSelectedObjectCell()
    {
        RefreshGridReferences();
        if (Selection.activeTransform == null || grid == null)
        {
            return;
        }

        var cell = WorldToCell(Selection.activeTransform.position);
        originCell = new Vector2Int(cell.x, cell.y);
    }

    private void CreateChocolatePiece()
    {
        RefreshGridReferences();
        CreateMissingParents();

        var rows = ParsePattern(chocolatePattern);
        if (rows.Count == 0 || grid == null)
        {
            return;
        }

        var root = new GameObject("ChocolatePiece");
        Undo.RegisterCreatedObjectUndo(root, "Create Chocolate Piece");
        root.transform.SetParent(chocolateParent, true);
        root.transform.position = CellToWorldCenter(new Vector3Int(originCell.x, originCell.y, 0));

        var usedCells = 0;
        for (int row = 0; row < rows.Count; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                var token = rows[row][column];
                if (IsEmptyToken(token))
                {
                    continue;
                }

                var prefab = token == 'S' || token == 's'
                    ? chocolateStrawberryCellPrefab
                    : chocolateCellPrefab;

                var cellObject = InstantiateEditorObject(prefab, root.transform, token == 'S' || token == 's' ? "ChocolateStrawberryCell" : "ChocolateCell");
                var cell = PatternCell(column, row, rows.Count);
                cellObject.transform.position = CellToWorldCenter(cell);
                usedCells++;
            }
        }

        var piece = root.AddComponent<ChocolatePiece>();
        piece.Configure(GetPatternSize(rows), true);

        if (usedCells == 0)
        {
            Undo.DestroyObjectImmediate(root);
            return;
        }

        Selection.activeGameObject = root;
    }

    private void CreateTargetTray()
    {
        RefreshGridReferences();
        CreateMissingParents();

        var rows = ParsePattern(trayPattern);
        if (rows.Count == 0 || grid == null)
        {
            return;
        }

        var root = new GameObject("TargetTray");
        Undo.RegisterCreatedObjectUndo(root, "Create Target Tray");
        root.transform.SetParent(trayParent, true);
        root.transform.position = CellToWorldCenter(new Vector3Int(originCell.x, originCell.y, 0));
        root.AddComponent<TargetTray>();

        var usedCells = 0;
        for (int row = 0; row < rows.Count; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                var token = rows[row][column];
                if (IsEmptyToken(token))
                {
                    continue;
                }

                var prefab = GetTrayPrefab(token);
                var objectName = GetTrayObjectName(token);
                var slotObject = InstantiateEditorObject(prefab, root.transform, objectName);
                slotObject.transform.position = CellToWorldCenter(PatternCell(column, row, rows.Count));
                usedCells++;
            }
        }

        if (usedCells == 0)
        {
            Undo.DestroyObjectImmediate(root);
            return;
        }

        Selection.activeGameObject = root;
    }

    private GameObject InstantiateEditorObject(GameObject prefab, Transform parent, string fallbackName)
    {
        GameObject instance;
        if (prefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, $"Create {fallbackName}");
        }
        else
        {
            instance = new GameObject(fallbackName);
            Undo.RegisterCreatedObjectUndo(instance, $"Create {fallbackName}");
        }

        instance.name = fallbackName;
        instance.transform.SetParent(parent, true);
        return instance;
    }

    private GameObject GetTrayPrefab(char token)
    {
        return token switch
        {
            'S' or 's' => trayStrawberryPrefab,
            'R' or 'r' => rockPrefab,
            'O' or 'o' => overflowSlotPrefab,
            _ => traySlotPrefab
        };
    }

    private static string GetTrayObjectName(char token)
    {
        return token switch
        {
            'S' or 's' => "StrawberryHolder",
            'R' or 'r' => "Rock",
            'O' or 'o' => "OverflowSlot",
            _ => "TraySlot"
        };
    }

    private Vector3Int PatternCell(int column, int row, int rowCount)
    {
        return new Vector3Int(originCell.x + column, originCell.y + rowCount - 1 - row, 0);
    }

    private Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return tilemap != null ? tilemap.WorldToCell(worldPosition) : grid.WorldToCell(worldPosition);
    }

    private Vector3 CellToWorldCenter(Vector3Int cell)
    {
        return tilemap != null ? tilemap.GetCellCenterWorld(cell) : grid.GetCellCenterWorld(cell);
    }

    private static List<string> ParsePattern(string pattern)
    {
        var rows = new List<string>();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return rows;
        }

        var splitRows = pattern.Replace("\r", string.Empty).Split('\n');
        for (int i = 0; i < splitRows.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(splitRows[i]))
            {
                rows.Add(splitRows[i]);
            }
        }

        return rows;
    }

    private static Vector2Int GetPatternSize(List<string> rows)
    {
        var width = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            width = Mathf.Max(width, rows[i].Length);
        }

        return new Vector2Int(width, rows.Count);
    }

    private static bool IsEmptyToken(char token)
    {
        return token == '.' || token == '_' || token == ' ';
    }
}
#endif
