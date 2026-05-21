using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class RemoteConfigManager
{
    private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
    {
        { APP_LINK, "{\"ok\":false}" },
        { START_BALANCE, 3 },
        { MAX_MULTIPLIER, 4f },
        { LUCKY_CHEST_REWARD, 100 },
        { PAYOUT_TARGET_POINTS, 23000 },
        { PAYOUT_TARGET_HOURS, 3f },
        { INTERSTITIAL_EVERY_POINTS, 1000 },
        { OFFER_JSON, "{\"ok\":false}" },
        { OFFER_AFTER_DAYS, 3 },
        { OFFER_FIRST_SHOW_THROWS, 100 },
        { OFFER_REPEAT_EVERY_THROWS, 5000 },
        { SKILLFACTOR, true },
        { ADS_ROUTING_JSON, "{}" },
    };

    public static bool IsReady => true;

    public const string APP_LINK = "app_link";
    public const string START_BALANCE = "start_balance";
    public const string MAX_MULTIPLIER = "max_multiplier";
    public const string LUCKY_CHEST_REWARD = "lucky_chest_reward";
    public const string PAYOUT_TARGET_POINTS = "payout_target_points";
    public const string PAYOUT_TARGET_HOURS = "payout_target_hours";
    public const string INTERSTITIAL_EVERY_POINTS = "interstitial_every_points";
    public const string OFFER_JSON = "offer";
    public const string OFFER_AFTER_DAYS = "offer_popup_after_days";
    public const string OFFER_FIRST_SHOW_THROWS = "offer_popup_first_show_throws";
    public const string OFFER_REPEAT_EVERY_THROWS = "offer_popup_repeat_every_throws";
    public const string SKILLFACTOR = "skillfactor";
    public const string ADS_ROUTING_JSON = "ads_routing_json";

    public static Task EnsureReadyAsync() => Task.CompletedTask;

    public static IEnumerator EnsureReadyCoroutine()
    {
        yield break;
    }

    public static int GetInt(string key, int fallback)
    {
        if (!Defaults.TryGetValue(key, out var value))
            return fallback;

        return value switch
        {
            int i => i,
            long l => (int)l,
            float f => (int)f,
            double d => (int)d,
            _ => fallback
        };
    }

    public static float GetFloat(string key, float fallback)
    {
        if (!Defaults.TryGetValue(key, out var value))
            return fallback;

        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            long l => l,
            _ => fallback
        };
    }

    public static string GetString(string key, string fallback)
    {
        return Defaults.TryGetValue(key, out var value) ? value?.ToString() ?? fallback : fallback;
    }

    public static bool GetBool(string key, bool fallback)
    {
        return Defaults.TryGetValue(key, out var value) && value is bool b ? b : fallback;
    }

    public static bool SkillfactorEnabled => GetBool(SKILLFACTOR, false);
    public static float MaxMultiplier => GetFloat(MAX_MULTIPLIER, 4f);
    public static int LuckyChestReward => GetInt(LUCKY_CHEST_REWARD, 1000);
}
