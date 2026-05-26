using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class HeartStatusUI : MonoBehaviour
{
    [SerializeField] private Text heartText;
    [SerializeField] private Text countdownText;
    [SerializeField] private string fullText = "Full";

    private HeartManager heartManager;

    private void OnEnable()
    {
        ResolveTexts();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        heartManager = HeartManager.Instance;
        heartManager.HeartChanged -= RefreshHeartText;
        heartManager.CountdownChanged -= RefreshCountdownText;
        heartManager.HeartChanged += RefreshHeartText;
        heartManager.CountdownChanged += RefreshCountdownText;
        heartManager.RefreshHeartDisplay();
    }

    private void Unbind()
    {
        if (heartManager == null)
        {
            return;
        }

        heartManager.HeartChanged -= RefreshHeartText;
        heartManager.CountdownChanged -= RefreshCountdownText;
        heartManager = null;
    }

    private void RefreshHeartText(int heartCount)
    {
        ResolveTexts();
        if (heartText != null)
        {
            heartText.text = heartCount.ToString();
        }
    }

    private void RefreshCountdownText(TimeSpan remaining, bool isFull)
    {
        ResolveTexts();
        if (countdownText == null)
        {
            return;
        }

        countdownText.text = isFull ? fullText : FormatRemainingTime(remaining);
    }

    private static string FormatRemainingTime(TimeSpan remaining)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void ResolveTexts()
    {
        if (heartText != null && countdownText != null)
        {
            return;
        }

        var texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (heartText == null && texts[i].name.IndexOf("Heart", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                heartText = texts[i];
                continue;
            }

            if (countdownText == null
                && (texts[i].name.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0
                    || texts[i].name.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                countdownText = texts[i];
            }
        }
    }
}
