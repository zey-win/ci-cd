using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firebase
{
    public enum DependencyStatus
    {
        Available,
        UnavailableOther
    }

    public sealed class FirebaseApp
    {
        public static FirebaseApp DefaultInstance { get; } = new FirebaseApp();
        public static Task<DependencyStatus> CheckAndFixDependenciesAsync() => Task.FromResult(DependencyStatus.Available);
    }
}

namespace Firebase.Analytics
{
    public readonly struct Parameter
    {
        public readonly string Name;
        public readonly object Value;

        public Parameter(string name, string value) { Name = name; Value = value; }
        public Parameter(string name, long value) { Name = name; Value = value; }
        public Parameter(string name, int value) { Name = name; Value = value; }
        public Parameter(string name, double value) { Name = name; Value = value; }
        public Parameter(string name, float value) { Name = name; Value = value; }
    }

    public static class FirebaseAnalytics
    {
        public const string ParameterValue = "value";

        public static void SetAnalyticsCollectionEnabled(bool enabled) { }
        public static void SetUserProperty(string name, string property) { }
        public static void LogEvent(string name) { }
        public static void LogEvent(string name, params Parameter[] parameters) { }
        public static void LogEvent(string name, IEnumerable<Parameter> parameters) { }
    }
}

namespace Firebase.RemoteConfig
{
    public readonly struct ConfigValue
    {
        public long LongValue => 0;
        public double DoubleValue => 0;
        public string StringValue => "";
        public bool BooleanValue => false;
    }

    public sealed class FirebaseRemoteConfig
    {
        public static FirebaseRemoteConfig DefaultInstance { get; } = new FirebaseRemoteConfig();

        public Task SetDefaultsAsync(Dictionary<string, object> defaults) => Task.CompletedTask;
        public Task FetchAsync(TimeSpan cacheExpiration) => Task.CompletedTask;
        public Task ActivateAsync() => Task.CompletedTask;
        public ConfigValue GetValue(string key) => new ConfigValue();
    }
}

namespace Firebase.Messaging
{
    public sealed class MessageReceivedEventArgs : EventArgs { }
    public sealed class TokenReceivedEventArgs : EventArgs
    {
        public string Token => "";
    }

    public static class FirebaseMessaging
    {
        public static event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public static event EventHandler<TokenReceivedEventArgs> TokenReceived;

        public static Task RequestPermissionAsync() => Task.CompletedTask;

        public static void ClearHandlers()
        {
            MessageReceived = null;
            TokenReceived = null;
        }
    }
}
