using System;
using UnityEngine;

public sealed class HeartManager : Singleton<HeartManager>
{
    private const string HeartCountKey = "ChocoBento.HeartCount";
    private const string LastRechargeUtcTicksKey = "ChocoBento.HeartLastRechargeUtcTicks";

    [SerializeField, Min(1)] private int maxHeartCount = 5;
    [SerializeField, Min(1f)] private float rechargeSeconds = 300f;
    [SerializeField] private bool startFullIfNoSave = true;

    private float nextTickTime;

    public event Action<int> HeartChanged;
    public event Action<TimeSpan, bool> CountdownChanged;

    public int HeartCount => Mathf.Clamp(PlayerPrefs.GetInt(HeartCountKey, startFullIfNoSave ? maxHeartCount : 0), 0, maxHeartCount);
    public int MaxHeartCount => maxHeartCount;
    public bool IsFull => HeartCount >= maxHeartCount;

    private void Awake()
    {
        EnsureSaveExists();
        ApplyOfflineRecharge();
        PublishState();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTickTime)
        {
            return;
        }

        nextTickTime = Time.unscaledTime + 1f;
        ApplyOfflineRecharge();
        PublishState();
    }

    public bool TrySpendHeart(int amount = 1)
    {
        if (amount <= 0)
        {
            return true;
        }

        ApplyOfflineRecharge();
        if (HeartCount < amount)
        {
            PublishState();
            return false;
        }

        int previousHeartCount = HeartCount;
        SetHeartCountInternal(previousHeartCount - amount);

        if (previousHeartCount >= maxHeartCount)
        {
            SetLastRechargeUtc(DateTime.UtcNow);
        }

        PublishState();
        return true;
    }

    public void AddHeart(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        ApplyOfflineRecharge();
        SetHeartCountInternal(HeartCount + amount);
        if (IsFull)
        {
            SetLastRechargeUtc(DateTime.UtcNow);
        }

        PublishState();
    }

    public void SetHeart(int heartCount)
    {
        SetHeartCountInternal(heartCount);
        SetLastRechargeUtc(DateTime.UtcNow);
        PublishState();
    }

    public TimeSpan GetTimeUntilNextHeart()
    {
        ApplyOfflineRecharge();
        return CalculateTimeUntilNextHeart(DateTime.UtcNow);
    }

    public void RefreshHeartDisplay()
    {
        ApplyOfflineRecharge();
        PublishState();
    }

    private void EnsureSaveExists()
    {
        if (!PlayerPrefs.HasKey(HeartCountKey))
        {
            PlayerPrefs.SetInt(HeartCountKey, startFullIfNoSave ? maxHeartCount : 0);
        }

        if (!PlayerPrefs.HasKey(LastRechargeUtcTicksKey))
        {
            PlayerPrefs.SetString(LastRechargeUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
        }

        PlayerPrefs.Save();
    }

    private void ApplyOfflineRecharge()
    {
        int currentHeartCount = HeartCount;
        if (currentHeartCount >= maxHeartCount)
        {
            SetHeartCountInternal(maxHeartCount);
            SetLastRechargeUtc(DateTime.UtcNow);
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateTime lastRechargeUtc = GetLastRechargeUtc(now);
        double elapsedSeconds = Mathf.Max(0f, (float)(now - lastRechargeUtc).TotalSeconds);
        int gainedHearts = Mathf.FloorToInt((float)(elapsedSeconds / rechargeSeconds));
        if (gainedHearts <= 0)
        {
            return;
        }

        int nextHeartCount = Mathf.Min(maxHeartCount, currentHeartCount + gainedHearts);
        SetHeartCountInternal(nextHeartCount);

        if (nextHeartCount >= maxHeartCount)
        {
            SetLastRechargeUtc(now);
            return;
        }

        SetLastRechargeUtc(lastRechargeUtc.AddSeconds(gainedHearts * rechargeSeconds));
    }

    private TimeSpan CalculateTimeUntilNextHeart(DateTime now)
    {
        if (IsFull)
        {
            return TimeSpan.Zero;
        }

        DateTime lastRechargeUtc = GetLastRechargeUtc(now);
        DateTime nextRechargeUtc = lastRechargeUtc.AddSeconds(rechargeSeconds);
        TimeSpan remaining = nextRechargeUtc - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private DateTime GetLastRechargeUtc(DateTime fallback)
    {
        string rawTicks = PlayerPrefs.GetString(LastRechargeUtcTicksKey, fallback.Ticks.ToString());
        return long.TryParse(rawTicks, out long ticks) ? new DateTime(ticks, DateTimeKind.Utc) : fallback;
    }

    private void SetHeartCountInternal(int heartCount)
    {
        PlayerPrefs.SetInt(HeartCountKey, Mathf.Clamp(heartCount, 0, maxHeartCount));
        PlayerPrefs.Save();
    }

    private void SetLastRechargeUtc(DateTime time)
    {
        PlayerPrefs.SetString(LastRechargeUtcTicksKey, time.ToUniversalTime().Ticks.ToString());
        PlayerPrefs.Save();
    }

    private void PublishState()
    {
        HeartChanged?.Invoke(HeartCount);
        CountdownChanged?.Invoke(CalculateTimeUntilNextHeart(DateTime.UtcNow), IsFull);
    }
}
