using UnityEngine;

public sealed class GameplayAutoInstaller : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var grid = FindFirstObjectByType<Grid>();
        if (grid != null && grid.GetComponent<GridManager>() == null)
        {
            grid.gameObject.AddComponent<GridManager>();
        }

        var targetTray = GameObject.Find("TargetTray");
        if (targetTray != null && targetTray.GetComponent<TargetTray>() == null)
        {
            targetTray.AddComponent<TargetTray>();
        }

        var knife = GameObject.Find("Knife");
        if (knife != null && knife.GetComponent<KnifeDrag>() == null)
        {
            knife.AddComponent<KnifeDrag>();
        }

        foreach (var piece in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (piece.name.StartsWith("ChocolatePiece") && piece.GetComponent<ChocolatePiece>() == null)
            {
                piece.gameObject.AddComponent<ChocolatePiece>();
            }
        }

        if (FindFirstObjectByType<GameplayInputRouter>() == null)
        {
            new GameObject("Gameplay Input Router").AddComponent<GameplayInputRouter>();
        }

        if (FindFirstObjectByType<GameplayLevelController>() == null)
        {
            new GameObject("Gameplay Level Controller").AddComponent<GameplayLevelController>();
        }
    }
}
