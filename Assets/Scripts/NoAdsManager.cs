using System;
using UnityEngine;

public static class NoAdsManager
{
    private const string Key = "no_ads_owned";

    public static bool _subscriptionActive;

    public static event Action<bool> OnChanged;

    public static bool IsOwned => PlayerPrefs.GetInt(Key, 0) == 1 || _subscriptionActive;


    public static void SetOwned(bool owned)
    {
        if (owned == (PlayerPrefs.GetInt(Key, 0) == 1)) return;

        PlayerPrefs.SetInt(Key, owned ? 1 : 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke(IsOwned);
    }

    public static void SetSubscriptionActive(bool active)
    {
        Debug.Log($"SetSubscriptionActive: {active}");

        if (active == _subscriptionActive) return;

        _subscriptionActive = active;
        OnChanged?.Invoke(IsOwned);
    }
}
