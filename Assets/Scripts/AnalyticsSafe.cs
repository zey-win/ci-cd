using Firebase.Analytics;
using System.Collections.Generic;
using UnityEngine;

public static class AnalyticsSafe
{
    public static bool Ready { get; private set; }
    public static bool Disabled { get; private set; }

    private static readonly Queue<System.Action> _queue = new Queue<System.Action>(128);
    private const int MaxQueue = 200;

    public static void MarkReady()
    {
        if (Disabled) return;
        Ready = true;

        while (_queue.Count > 0)
        {
            try { _queue.Dequeue()?.Invoke(); }
            catch (System.Exception e) { Debug.LogWarning("Queued analytics failed: " + e); }
        }
    }

    public static void MarkDisabled()
    {
        Disabled = true;
        Ready = false;
        _queue.Clear();
    }

    public static void LogEvent(string name, params Parameter[] parameters)
    {
        if (Disabled) return;

        try
        {
            if (!Ready)
            {
                if (_queue.Count < MaxQueue)
                    _queue.Enqueue(() =>
                    {
                        try { FirebaseAnalytics.LogEvent(name, parameters); }
                        catch (System.Exception e) { Debug.LogWarning("Analytics failed: " + e); }
                    });
                return;
            }

            FirebaseAnalytics.LogEvent(name, parameters);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Analytics failed: " + e);
        }
    }
}
