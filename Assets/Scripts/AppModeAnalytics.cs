using System;
using Firebase.Analytics;
using UnityEngine;

public static class AppModeAnalytics
{
    private enum AppMode
    {
        None,
        Gameplay,
        OfferWebView
    }

    private static AppMode _currentMode = AppMode.None;
    private static DateTime _segmentStartedAtUtc;
    private static string _currentOfferHost;

    public static void Initialize()
    {
        if (_currentMode != AppMode.None)
            return;

        StartMode(AppMode.Gameplay, null, "session_ready");
    }

    public static void OnWebViewLocked(string url)
    {
        EndCurrentMode("webview_locked");
        StartMode(AppMode.OfferWebView, ExtractHost(url), "webview_locked");
        LogModeChanged("gameplay", "offer_webview", "webview_locked");
    }

    public static void OnWebViewUnlocked()
    {
        EndCurrentMode("webview_unlocked");
        StartMode(AppMode.Gameplay, null, "webview_unlocked");
        LogModeChanged("offer_webview", "gameplay", "webview_unlocked");
    }

    public static void OnAppPaused()
    {
        EndCurrentMode("app_paused");
    }

    public static void OnAppResumed()
    {
        if (_currentMode == AppMode.None)
        {
            StartMode(AppMode.Gameplay, null, "app_resumed");
        }
    }

    public static void OnAppQuit()
    {
        EndCurrentMode("app_quit");
    }

    private static void StartMode(AppMode mode, string offerHost, string reason)
    {
        _currentMode = mode;
        _currentOfferHost = offerHost;
        _segmentStartedAtUtc = DateTime.UtcNow;

        FirebaseAnalytics.SetUserProperty(
            "current_app_mode",
            mode == AppMode.Gameplay ? "gameplay" : "offer_webview"
        );
    }

    private static void EndCurrentMode(string reason)
    {
        if (_currentMode == AppMode.None)
            return;

        var durationSec = Math.Max(
            0L,
            (long)(DateTime.UtcNow - _segmentStartedAtUtc).TotalSeconds
        );

        string modeValue =
            _currentMode == AppMode.Gameplay ? "gameplay" : "offer_webview";

        var parameters = new System.Collections.Generic.List<Parameter>
        {
            new Parameter("mode", modeValue),
            new Parameter("duration_sec", durationSec),
            new Parameter("reason", reason),
            new Parameter(FirebaseAnalytics.ParameterValue, durationSec)
        };

        if (!string.IsNullOrEmpty(_currentOfferHost) && _currentMode == AppMode.OfferWebView)
        {
            parameters.Add(new Parameter("offer_host", _currentOfferHost));
        }

        AnalyticsLogger.Log("app_mode_time", parameters.ToArray());

        _currentMode = AppMode.None;
        _currentOfferHost = null;
    }

    private static void LogModeChanged(string fromMode, string toMode, string reason)
    {
        AnalyticsLogger.Log(
            "app_mode_changed",
            new Parameter("from_mode", fromMode),
            new Parameter("to_mode", toMode),
            new Parameter("reason", reason)
        );
    }

    private static string ExtractHost(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            return new Uri(url).Host;
        }
        catch
        {
            return null;
        }
    }
}