using System.Collections.Generic;
using UnityEngine;

public sealed class GameplayHintController : MonoBehaviour
{
    public static GameplayHintController Instance { get; private set; }

    [SerializeField] private ShapeHintPreview preview;

    private readonly List<Vector3Int> stepCells = new();
    private LevelHintData currentHintData;
    private int currentStepIndex = -1;

    private void Awake()
    {
        Instance = this;
        ResolvePreview();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowNextHint()
    {
        ResolveHintData();
        ResolvePreview();

        if (currentHintData == null || currentHintData.StepCount <= 0 || preview == null)
        {
            return;
        }

        for (int stepIndex = currentStepIndex + 1; stepIndex < currentHintData.StepCount; stepIndex++)
        {
            currentStepIndex = stepIndex;
            if (currentHintData.TryGetStepCells(currentStepIndex, stepCells))
            {
                preview.Show(stepCells);
                return;
            }
        }
    }

    public void ShowHintStep(int stepIndex)
    {
        ResolveHintData();
        ResolvePreview();

        if (currentHintData == null || currentHintData.StepCount <= 0 || preview == null)
        {
            return;
        }

        currentStepIndex = Mathf.Clamp(stepIndex, 0, currentHintData.StepCount - 1);
        if (currentHintData.TryGetStepCells(currentStepIndex, stepCells))
        {
            preview.Show(stepCells);
        }
    }

    public void HideHint()
    {
        if (preview != null)
        {
            preview.Hide();
        }
    }

    public void ResetHint()
    {
        currentHintData = null;
        currentStepIndex = -1;
        if (preview != null)
        {
            preview.Clear();
        }
    }

    private void ResolveHintData()
    {
        var hintData = FindFirstObjectByType<LevelHintData>();
        if (hintData != currentHintData)
        {
            currentHintData = hintData;
            currentStepIndex = -1;
        }
    }

    private void ResolvePreview()
    {
        if (preview != null)
        {
            return;
        }

        preview = GetComponent<ShapeHintPreview>();
        if (preview == null)
        {
            preview = gameObject.AddComponent<ShapeHintPreview>();
        }
    }

    public static GameplayHintController GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var controller = FindFirstObjectByType<GameplayHintController>();
        if (controller != null)
        {
            Instance = controller;
            return controller;
        }

        return new GameObject("Gameplay Hint Controller").AddComponent<GameplayHintController>();
    }
}
