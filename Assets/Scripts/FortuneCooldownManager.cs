using System;
using UnityEngine;

public class FortuneCooldownManager : MonoBehaviour
{
    public static FortuneCooldownManager Instance { get; private set; }

    public int CooldownHours => cooldownHours;
    [SerializeField] private int cooldownHours = 24;

    private const string PrefKeyNextUtc = "Fortune_NextSpinUtc";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsOnCooldown()
    {
        return GetRemainingSeconds() > 0;
    }

    public long GetRemainingSeconds()
    {
        long next = PlayerPrefs.GetInt(PrefKeyNextUtc, 0);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0, next - now);
    }

    public void StartCooldown()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long next = now + cooldownHours * 3600L;
        PlayerPrefs.SetInt(PrefKeyNextUtc, (int)Math.Min(next, int.MaxValue));
        PlayerPrefs.Save();
    }

    public void ClearCooldown()
    {
        PlayerPrefs.DeleteKey(PrefKeyNextUtc);
        PlayerPrefs.Save();
    }

    public string GetRemainingText()
    {
        long sec = GetRemainingSeconds();
        if (sec <= 0) return "";

        var ts = TimeSpan.FromSeconds(sec);

        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";

        return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    }
}
