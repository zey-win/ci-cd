using System;
using UnityEngine;

public class PlayerSessionAnalytics : MonoBehaviour
{
    public static PlayerSessionAnalytics Instance { get; private set; }

    // PlayerPrefs keys
    private const string PREF_WINDOW_START = "psa_window_start_utc_ticks";
    private const string PREF_WINDOW_EXPIRE = "psa_window_expire_utc_ticks";
    private const string PREF_BALLS = "psa_balls";
    private const string PREF_ACTIVE_SEC = "psa_active_sec";
    private const string PREF_SESSIONS = "psa_sessions";

    private const string PREF_SEGMENT_OPEN = "psa_seg_open";
    private const string PREF_SEGMENT_START_UTC = "psa_seg_start_utc_ticks";

    public int BallsThrownInWindow { get; private set; }
    public double ActiveSecondsInWindow { get; private set; }
    public int SessionsInWindow { get; private set; }

    public long WindowStartUtcTicks { get; private set; }
    public long WindowExpireUtcTicks { get; private set; }

    public bool WindowActive =>
        WindowStartUtcTicks > 0 &&
        WindowExpireUtcTicks > 0 &&
        DateTime.UtcNow.Ticks < WindowExpireUtcTicks;

    private bool _segmentOpen;
    private DateTime _segmentStartUtc;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadState();
        FixIfExpiredOrInvalid();

        // если внезапно остался "открытый сегмент" из прошлой жизни — закрываем без начисления
        if (PlayerPrefs.GetInt(PREF_SEGMENT_OPEN, 0) == 1)
        {
            PlayerPrefs.SetInt(PREF_SEGMENT_OPEN, 0);
            PlayerPrefs.DeleteKey(PREF_SEGMENT_START_UTC);
            PlayerPrefs.Save();
        }

        // если окно активно и мы в фокусе — начнем сегмент
        BeginSegmentIfNeeded();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) BeginSegmentIfNeeded();
        else EndSegmentIfNeeded();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) BeginSegmentIfNeeded();
        else EndSegmentIfNeeded();
    }

    private void OnDestroy()
    {
        EndSegmentIfNeeded();
    }

    /// <summary>
    /// Запустить/перезапустить аналитическое окно (обычно при первом броске).
    /// </summary>
    public void StartWindow(long startUtcTicks, long expireUtcTicks)
    {
        // закрываем активный сегмент, если был
        EndSegmentIfNeeded();

        WindowStartUtcTicks = startUtcTicks;
        WindowExpireUtcTicks = expireUtcTicks;

        BallsThrownInWindow = 0;
        ActiveSecondsInWindow = 0;
        SessionsInWindow = 0;

        SaveAll();

        BeginSegmentIfNeeded();
    }

    public void ResetWindow()
    {
        EndSegmentIfNeeded();

        WindowStartUtcTicks = 0;
        WindowExpireUtcTicks = 0;
        BallsThrownInWindow = 0;
        ActiveSecondsInWindow = 0;
        SessionsInWindow = 0;

        PlayerPrefs.DeleteKey(PREF_WINDOW_START);
        PlayerPrefs.DeleteKey(PREF_WINDOW_EXPIRE);
        PlayerPrefs.DeleteKey(PREF_BALLS);
        PlayerPrefs.DeleteKey(PREF_ACTIVE_SEC);
        PlayerPrefs.DeleteKey(PREF_SESSIONS);

        PlayerPrefs.SetInt(PREF_SEGMENT_OPEN, 0);
        PlayerPrefs.DeleteKey(PREF_SEGMENT_START_UTC);

        PlayerPrefs.Save();
    }


    public void RegisterBallThrow(int count = 1)
    {
        if (count <= 0) return;

        FixIfExpiredOrInvalid();
        if (!WindowActive) return;

        BallsThrownInWindow += count;
        PlayerPrefs.SetInt(PREF_BALLS, BallsThrownInWindow);
        PlayerPrefs.Save();
    }


    public void FlushActiveTime()
    {
        FixIfExpiredOrInvalid();
        if (!WindowActive) return;
        if (!_segmentOpen) return;

        var now = DateTime.UtcNow;
        double delta = (now - _segmentStartUtc).TotalSeconds;
        if (delta < 0) delta = 0;

        ActiveSecondsInWindow += delta;
        ClampActiveSecondsToWindowMax();

        _segmentStartUtc = now;

        PlayerPrefs.SetFloat(PREF_ACTIVE_SEC, (float)ActiveSecondsInWindow);
        PlayerPrefs.Save();
    }


    private void BeginSegmentIfNeeded()
    {
        FixIfExpiredOrInvalid();
        if (!WindowActive) return;
        if (_segmentOpen) return;

        _segmentOpen = true;
        _segmentStartUtc = DateTime.UtcNow;

        SessionsInWindow++;
        PlayerPrefs.SetInt(PREF_SESSIONS, SessionsInWindow);

        PlayerPrefs.SetInt(PREF_SEGMENT_OPEN, 1);
        PlayerPrefs.SetString(PREF_SEGMENT_START_UTC, _segmentStartUtc.Ticks.ToString());

        PlayerPrefs.Save();
    }

    private void EndSegmentIfNeeded()
    {
        if (!_segmentOpen) return;

        var now = DateTime.UtcNow;
        double delta = (now - _segmentStartUtc).TotalSeconds;
        if (delta < 0) delta = 0;

        ActiveSecondsInWindow += delta;
        ClampActiveSecondsToWindowMax();

        _segmentOpen = false;

        PlayerPrefs.SetFloat(PREF_ACTIVE_SEC, (float)ActiveSecondsInWindow);
        PlayerPrefs.SetInt(PREF_SEGMENT_OPEN, 0);
        PlayerPrefs.DeleteKey(PREF_SEGMENT_START_UTC);
        PlayerPrefs.Save();
    }

    private void ClampActiveSecondsToWindowMax()
    {
        if (WindowStartUtcTicks <= 0 || WindowExpireUtcTicks <= 0) return;
        double max = TimeSpan.FromTicks(WindowExpireUtcTicks - WindowStartUtcTicks).TotalSeconds;
        if (max < 0) max = 0;
        if (ActiveSecondsInWindow > max) ActiveSecondsInWindow = max;
    }

    private void FixIfExpiredOrInvalid()
    {
        if (WindowExpireUtcTicks <= 0) return;

        if (DateTime.UtcNow.Ticks >= WindowExpireUtcTicks)
        {
            // окно закончилось — сбрасываем
            ResetWindow();
        }
    }

    private void LoadState()
    {
        WindowStartUtcTicks = TryGetLong(PlayerPrefs.GetString(PREF_WINDOW_START, "0"));
        WindowExpireUtcTicks = TryGetLong(PlayerPrefs.GetString(PREF_WINDOW_EXPIRE, "0"));

        BallsThrownInWindow = PlayerPrefs.GetInt(PREF_BALLS, 0);
        ActiveSecondsInWindow = PlayerPrefs.GetFloat(PREF_ACTIVE_SEC, 0f);
        SessionsInWindow = PlayerPrefs.GetInt(PREF_SESSIONS, 0);

        _segmentOpen = false;
    }

    private void SaveAll()
    {
        PlayerPrefs.SetString(PREF_WINDOW_START, WindowStartUtcTicks.ToString());
        PlayerPrefs.SetString(PREF_WINDOW_EXPIRE, WindowExpireUtcTicks.ToString());
        PlayerPrefs.SetInt(PREF_BALLS, BallsThrownInWindow);
        PlayerPrefs.SetFloat(PREF_ACTIVE_SEC, (float)ActiveSecondsInWindow);
        PlayerPrefs.SetInt(PREF_SESSIONS, SessionsInWindow);

        PlayerPrefs.SetInt(PREF_SEGMENT_OPEN, 0);
        PlayerPrefs.DeleteKey(PREF_SEGMENT_START_UTC);

        PlayerPrefs.Save();
    }

    private static long TryGetLong(string s)
    {
        return long.TryParse(s, out var t) ? t : 0;
    }
}
