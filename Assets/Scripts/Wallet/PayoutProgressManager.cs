using System;
using System.Collections;
using Firebase.Analytics;
using UnityEngine;

public class PayoutProgressManager : MonoBehaviour
{
    public static PayoutProgressManager Instance { get; private set; }

    [Header("Progress tuning")]
    [SerializeField] private int _targetPoints = 20000;
    [SerializeField] private float _windowHours = 6f;

    [Header("Withdraw tuning")]
    [SerializeField] private float _withdrawAmountUsd = 3f;
    public float WithdrawAmountUsd => _withdrawAmountUsd;
    public float EarnedBalanceUsd
    {
        get
        {
            if (_targetPoints <= 0) return 0f;
            if (!HasActiveWindow) return 0f;

            float p = Mathf.Clamp01((float)Points / _targetPoints);
            float m = Mathf.Max(1f, _firstHalfSpeedMultiplier);

            float totalWeight = 0.5f * (m + 1f);

            float earned01;
            if (p <= 0.5f)
            {
                float w = p * m;
                earned01 = w / totalWeight;
            }
            else
            {
                float w = 0.5f * m + (p - 0.5f) * 1f;
                earned01 = w / totalWeight;
            }

            return _withdrawAmountUsd * Mathf.Clamp01(earned01);
        }
    }


    public float WithdrawBalanceUsd => EarnedBalanceUsd;

    [Header("Earned balance curve")]
    [SerializeField, Min(1f)] private float _firstHalfSpeedMultiplier = 2f;


    private const string PREF_POINTS = "payout_points";
    private const string PREF_START_TICKS = "payout_start_utc_ticks";
    private const string PREF_EXPIRE_TICKS = "payout_expire_utc_ticks";


    public event Action OnChanged;
    public event Action OnExpired;
    public event Action OnTick;

    public int Points { get; private set; }
    public int TargetPoints => _targetPoints;

    public bool HasActiveWindow => Points > 0 && !IsExpiredUtc(DateTime.UtcNow.Ticks);
    public bool CanWithdraw => Points >= _targetPoints && HasActiveWindow;
    public float Progress01 => _targetPoints <= 0 ? 0f : Mathf.Clamp01((float)Points / _targetPoints);

    public TimeSpan Remaining
    {
        get
        {
            long exp = LoadExpireTicks();
            if (exp <= 0) return TimeSpan.Zero;
            long now = DateTime.UtcNow.Ticks;
            long diff = exp - now;
            return diff <= 0 ? TimeSpan.Zero : TimeSpan.FromTicks(diff);
        }
    }

    public string TimerText { get; private set; } = "";
    public string PointsText { get; private set; } = "";

    private Coroutine _tickerRoutine;



    // ===== ANALYTICS =====
    private bool _wasWithdrawReady = false;
    private const int POINTS_LOG_STEP = 100; // логируем points_added каждые 100 (можешь поменять)

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }
    // ===== /ANALYTICS =====


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadState();

        CheckExpireAndFix();
        RebuildUiStrings();

        _tickerRoutine = StartCoroutine(Ticker());

        CancelPayoutEndPush();
        OnChanged?.Invoke();
    }


    private IEnumerator Start()
    {
        yield return RemoteConfigManager.EnsureReadyCoroutine();

        _targetPoints = RemoteConfigManager.GetInt(RemoteConfigManager.PAYOUT_TARGET_POINTS, _targetPoints);
        _windowHours = RemoteConfigManager.GetFloat(RemoteConfigManager.PAYOUT_TARGET_HOURS, _windowHours);

        // дальше твой текущий пересчет
        CheckExpireAndFix();
        RebuildUiStrings();
        OnChanged?.Invoke();
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            CancelPayoutEndPush();

            CheckExpireAndFix();
            RebuildUiStrings();
            OnChanged?.Invoke();
        }
        else
        {
            SchedulePayoutEndPushIfNeeded();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SchedulePayoutEndPushIfNeeded();
        }
        else
        {
            CancelPayoutEndPush();

            CheckExpireAndFix();
            RebuildUiStrings();
            OnChanged?.Invoke();
        }
    }


    public void AddPoint() => AddPoints(1);



    public void AddPoints(int amount)
    {
        if (amount <= 0) return;

        CheckExpireAndFix();

        if (Points <= 0)
            StartWindow();

        Points += amount;
        PlayerSessionAnalytics.Instance?.RegisterBallThrow(amount);
        SavePoints();

        if (Points > 0 && (Points % POINTS_LOG_STEP) == 0)
        {
            LogEvent("payout_points_added",
                new Parameter("amount", POINTS_LOG_STEP),
                new Parameter("points_total", Points)
            );
        }

        RebuildUiStrings();
        OnChanged?.Invoke();
    }

    public void ResetProgress()
    {
        Points = 0;

        PlayerPrefs.SetInt(PREF_POINTS, 0);
        PlayerPrefs.DeleteKey(PREF_START_TICKS);
        PlayerPrefs.DeleteKey(PREF_EXPIRE_TICKS);
        PlayerPrefs.Save();

        RebuildUiStrings();
        OnChanged?.Invoke();
        PlayerSessionAnalytics.Instance?.ResetWindow();
    }

    public bool ConsumeForWithdraw()
    {
        if (!CanWithdraw) return false;
        ResetProgress();
        return true;
    }

    private IEnumerator Ticker()
    {
        while (true)
        {
            CheckExpireAndFix();

            if (Points > 0)
            {
                RebuildUiStrings();
                OnTick?.Invoke();
            }

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void RebuildUiStrings()
    {
        PointsText = $"{Points:N0}";

        bool ready = CanWithdraw;
        if (ready && !_wasWithdrawReady)
        {
            _wasWithdrawReady = true;

            LogEvent("payout_withdraw_ready",
                new Parameter("points", Points),
                new Parameter("target_points", _targetPoints),
                new Parameter("amount_usd", _withdrawAmountUsd)
            );
        }
        if (!ready && _wasWithdrawReady && Points == 0)
        {
            _wasWithdrawReady = false;
        }


        if (CanWithdraw)
        {
            TimerText = "READY";
            return;
        }

        if (!HasActiveWindow)
        {
            TimerText = "Play to start";
            return;
        }

        var r = Remaining;
        int hours = (int)r.TotalHours;
        TimerText = $"{hours:00}:{r.Minutes:00}:{r.Seconds:00}";
    }

    private void LoadState()
    {
        Points = PlayerPrefs.GetInt(PREF_POINTS, 0);
    }

    private void SavePoints()
    {
        PlayerPrefs.SetInt(PREF_POINTS, Points);
        PlayerPrefs.Save();
    }

    private void StartWindow()
    {
        long now = DateTime.UtcNow.Ticks;
        long expire = DateTime.UtcNow.AddHours(_windowHours).Ticks;

        PlayerPrefs.SetString(PREF_START_TICKS, now.ToString());
        PlayerPrefs.SetString(PREF_EXPIRE_TICKS, expire.ToString());
        PlayerPrefs.Save();

        var psa = PlayerSessionAnalytics.Instance;
        if (psa != null)
            psa.StartWindow(now, expire);

        LogEvent("payout_window_started",
            new Parameter("target_points", _targetPoints),
            new Parameter("window_hours", (float)_windowHours)
        );
    }

    private void CheckExpireAndFix()
    {
        if (Points <= 0) return;

        long now = DateTime.UtcNow.Ticks;
        if (IsExpiredUtc(now))
        {
            int before = Points;

            LogEvent("payout_window_expired",
                new Parameter("points_before_reset", before),
                new Parameter("target_points", _targetPoints)
            );

            Points = 0;

            PlayerPrefs.SetInt(PREF_POINTS, 0);
            PlayerPrefs.DeleteKey(PREF_START_TICKS);
            PlayerPrefs.DeleteKey(PREF_EXPIRE_TICKS);
            PlayerPrefs.Save();
            RebuildUiStrings();
            OnExpired?.Invoke();
            OnChanged?.Invoke();
        }
    }

    private bool IsExpiredUtc(long nowTicks)
    {
        long exp = LoadExpireTicks();
        return exp > 0 && nowTicks >= exp;
    }

    private long LoadExpireTicks()
    {
        string s = PlayerPrefs.GetString(PREF_EXPIRE_TICKS, "0");
        return long.TryParse(s, out var t) ? t : 0;
    }


    private void SchedulePayoutEndPushIfNeeded()
    {
        if (GameLocalNotifications.Instance == null) return;

        if (!HasActiveWindow) return;

        var delay = Remaining;
        if (delay <= TimeSpan.FromSeconds(1)) return;

        GameLocalNotifications.Instance.ScheduleAfter(
            GameNotificationType.PayoutTimerEnded,
            delay,
            replace: true
        );

        Debug.Log($"[Payout] Scheduled PayoutTimerEnded in {delay} (at {DateTime.Now.Add(delay):HH:mm:ss})");
    }

    private void CancelPayoutEndPush()
    {
        if (GameLocalNotifications.Instance == null) return;

        GameLocalNotifications.Instance.Cancel(GameNotificationType.PayoutTimerEnded);
        Debug.Log("[Payout] Cancelled PayoutTimerEnded");
    }

}
