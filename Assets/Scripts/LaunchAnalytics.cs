using UnityEngine;
using Firebase.Analytics;
using ZeyWinAds;

public class LaunchAnalytics : MonoBehaviour
{
    public static LaunchAnalytics Instance { get; private set; }

    private const string PREF_FIRST_OPEN_SENT = "analytics_first_open_sent";

    private bool _wasInBackground = false;
    private bool _sessionActive = false;
    private float _sessionStartRealtime = 0f;
    private string _entryPoint = "unknown";

    private bool _webViewEventsSubscribed = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SubscribeWebViewEvents();

        if (PlayerPrefs.GetInt(PREF_FIRST_OPEN_SENT, 0) == 0)
        {
            AnalyticsLogger.Log("app_first_open");
            PlayerPrefs.SetInt(PREF_FIRST_OPEN_SENT, 1);
            PlayerPrefs.Save();
        }
    }

    private void Start()
    {
        AnalyticsLogger.Log("app_launch",
            new Parameter("launch_type", "cold")
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnsubscribeWebViewEvents();
            AppModeAnalytics.OnAppQuit();
            Instance = null;
        }
    }

    public void SetEntryPoint(string entryPoint)
    {
        if (!string.IsNullOrEmpty(entryPoint))
            _entryPoint = entryPoint;
    }

    public void NotifySessionReady()
    {
        if (_sessionActive) return;

        _sessionActive = true;
        _sessionStartRealtime = Time.realtimeSinceStartup;

        AnalyticsLogger.Log("session_start");

        // ВАЖНО:
        // gameplay считаем только когда игровая сессия реально готова,
        // а не в момент старта bootstrap/splash сцены.
        AppModeAnalytics.Initialize();
    }

    private void EndSessionIfActive()
    {
        if (!_sessionActive) return;

        int durationSec = Mathf.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - _sessionStartRealtime));

        AnalyticsLogger.Log("session_end",
            new Parameter("duration_sec", (long)durationSec)
        );

        _sessionActive = false;
        _sessionStartRealtime = 0f;
        _entryPoint = "unknown";
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (_wasInBackground)
            {
                AnalyticsLogger.Log("app_launch",
                    new Parameter("launch_type", "warm")
                );

                AppModeAnalytics.OnAppResumed();
            }

            _wasInBackground = false;
        }
        else
        {
            _wasInBackground = true;

            AppModeAnalytics.OnAppPaused();
            EndSessionIfActive();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            _wasInBackground = true;

            AppModeAnalytics.OnAppPaused();
            EndSessionIfActive();
        }
        else
        {
            AppModeAnalytics.OnAppResumed();
        }
    }

    private void OnApplicationQuit()
    {
        AppModeAnalytics.OnAppQuit();
        EndSessionIfActive();
    }

    private void SubscribeWebViewEvents()
    {
        if (_webViewEventsSubscribed)
            return;

        ZeyWinAds.ZeyWinAds.OnWebViewLocked += HandleWebViewLocked;
        ZeyWinAds.ZeyWinAds.OnWebViewUnlocked += HandleWebViewUnlocked;

        _webViewEventsSubscribed = true;
    }

    private void UnsubscribeWebViewEvents()
    {
        if (!_webViewEventsSubscribed)
            return;

        ZeyWinAds.ZeyWinAds.OnWebViewLocked -= HandleWebViewLocked;
        ZeyWinAds.ZeyWinAds.OnWebViewUnlocked -= HandleWebViewUnlocked;

        _webViewEventsSubscribed = false;
    }

    private void HandleWebViewLocked(string url)
    {
        AppModeAnalytics.OnWebViewLocked(url);
    }

    private void HandleWebViewUnlocked()
    {
        AppModeAnalytics.OnWebViewUnlocked();
    }
}