using Firebase.Analytics;

public static class AnalyticsLogger
{
    public static void Log(string eventName, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(eventName, parameters);
    }
}
