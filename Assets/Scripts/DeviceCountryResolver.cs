using System;
using UnityEngine;

[Serializable]
public class DeviceCountrySignals
{
    public string country_by_network;
    public string country_by_sim;
    public string country_by_locale;
    public string timezone_id;

    public string country_final;
}

public static class DeviceCountryResolver
{
    public static DeviceCountrySignals Resolve()
    {
        var result = new DeviceCountrySignals
        {
            country_by_network = string.Empty,
            country_by_sim = string.Empty,
            country_by_locale = string.Empty,
            timezone_id = string.Empty,
            country_final = string.Empty
        };

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var context = activity.Call<AndroidJavaObject>("getApplicationContext");

            using (var telephonyManager = context.Call<AndroidJavaObject>("getSystemService", "phone"))
            {
                if (telephonyManager != null)
                {
                    result.country_by_network = NormalizeCountryCode(
                        SafeCallString(telephonyManager, "getNetworkCountryIso")
                    );

                    result.country_by_sim = NormalizeCountryCode(
                        SafeCallString(telephonyManager, "getSimCountryIso")
                    );
                }
            }

            using (var localeClass = new AndroidJavaClass("java.util.Locale"))
            using (var locale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
            {
                if (locale != null)
                {
                    result.country_by_locale = NormalizeCountryCode(
                        SafeCallString(locale, "getCountry")
                    );
                }
            }

            using (var timeZoneClass = new AndroidJavaClass("java.util.TimeZone"))
            using (var timeZone = timeZoneClass.CallStatic<AndroidJavaObject>("getDefault"))
            {
                if (timeZone != null)
                {
                    result.timezone_id = SafeCallString(timeZone, "getID");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DeviceCountryResolver.Resolve failed: {e}");
        }
#endif

        result.country_final = FirstNonEmpty(
            result.country_by_network,
            result.country_by_sim,
            result.country_by_locale
        );

        return result;
    }

    private static string SafeCallString(AndroidJavaObject obj, string methodName)
    {
        try
        {
            return obj.Call<string>(methodName) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeCountryCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim().ToUpperInvariant();

        return value;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        if (values == null || values.Length == 0)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }
}