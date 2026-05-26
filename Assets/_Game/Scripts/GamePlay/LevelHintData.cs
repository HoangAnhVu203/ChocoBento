using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelHintData : MonoBehaviour
{
    [SerializeField] private HintStep[] steps = Array.Empty<HintStep>();

    public int StepCount => steps != null ? steps.Length : 0;

    public bool TryGetStepCells(int stepIndex, List<Vector3Int> results)
    {
        results.Clear();
        if (GridManager.Instance == null || steps == null || stepIndex < 0 || stepIndex >= steps.Length)
        {
            return false;
        }

        steps[stepIndex].CollectCells(results);
        return results.Count > 0;
    }

    [Serializable]
    private sealed class HintStep
    {
        [SerializeField] private Transform[] targetSlots = Array.Empty<Transform>();
        [SerializeField] private Vector2Int[] gridCells = Array.Empty<Vector2Int>();

        public void CollectCells(List<Vector3Int> results)
        {
            var uniqueCells = new HashSet<Vector3Int>();

            for (int i = 0; targetSlots != null && i < targetSlots.Length; i++)
            {
                if (targetSlots[i] == null)
                {
                    continue;
                }

                uniqueCells.Add(GridManager.Instance.WorldToCell(targetSlots[i].position));
            }

            for (int i = 0; gridCells != null && i < gridCells.Length; i++)
            {
                uniqueCells.Add(new Vector3Int(gridCells[i].x, gridCells[i].y, 0));
            }

            foreach (var cell in uniqueCells)
            {
                results.Add(cell);
            }
        }
    }
}
