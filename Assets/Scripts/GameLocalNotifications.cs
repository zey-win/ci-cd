using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public enum GameNotificationType
{
    WheelReady,
    PayoutTimerEnded,
    ComeBackLongTimeNoSee
}

public class GameLocalNotifications : MonoBehaviour
{
    public static GameLocalNotifications Instance { get; private set; }

    [Header("Android Channel (default)")]
    [SerializeField] private string androidDefaultChannelId = "general";
    [SerializeField] private string androidDefaultChannelName = "General";
    [SerializeField] private string androidDefaultChannelDesc = "General notifications";

    [SerializeField] private string androidSmallIcon = "icon_small";
    [SerializeField] private string androidLargeIcon = "icon_large";

    [Header("Language")]
    [SerializeField] private LanguageSource languageSource = LanguageSource.UsePlayerPrefsKey;
    [SerializeField] private string languagePrefsKey = "ui_lang";
    [SerializeField] private string fallbackLanguageCode = "en";

    // Чтобы не настраивать в инспекторе — заполним кодом
    [Header("Notification Templates (auto-filled)")]
    [SerializeField] private List<NotificationTemplate> templates = new List<NotificationTemplate>();

    private readonly Dictionary<GameNotificationType, NotificationTemplate> _templatesMap = new();

    private const string StorePrefsKey = "local_notif_store_v2";
    private NotifStore _store;

    private const string LastNotifOpenTokenPrefsKey = "last_notif_open_token_v1";


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadStore();

        EnsureDefaultTemplates();
        BuildTemplatesMap();

        InitializePlatform();

        CancelComeBackSeries();
    }


    private void Start()
    {
        PollNotificationOpen();
    }


    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            ScheduleComeBackSeries();
        }
        else
        {
            CancelComeBackSeries();
        }
    }

    private void OnApplicationQuit()
    {
        ScheduleComeBackSeries();
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) PollNotificationOpen();
    }


    private void InitializePlatform()
    {
#if UNITY_ANDROID
        // Важно: явно инициализируем центр уведомлений
        AndroidNotificationCenter.Initialize(); // :contentReference[oaicite:2]{index=2}

        var channel = new AndroidNotificationChannel
        {
            Id = androidDefaultChannelId,
            Name = androidDefaultChannelName,
            Importance = Importance.High, // для теста лучше High
            Description = androidDefaultChannelDesc
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        Debug.Log($"[LocalNotif] UserPermissionToPost={AndroidNotificationCenter.UserPermissionToPost}, UsingExactScheduling={AndroidNotificationCenter.UsingExactScheduling}");
#endif
    }


    private void EnsureDefaultTemplates()
    {
        templates = new List<NotificationTemplate>
        {
            new NotificationTemplate
            {
                type = GameNotificationType.WheelReady,
                key = "wheel_ready",
                defaultTitle = "Wheel of Fortune",
                defaultBody  = "You can spin again! 🎁",
                localized = new List<LocalizedVariant>
                {
                    new LocalizedVariant { langCode = "en", title = "Wheel of Fortune", body = "You can spin again! 🎁" },
                    new LocalizedVariant { langCode = "ru", title = "Колесо фортуны", body = "Можно снова крутить! Заходи за наградой 🎁" },
                    new LocalizedVariant { langCode = "es", title = "Ruleta de la fortuna", body = "¡Puedes girar de nuevo! 🎁" },
                    new LocalizedVariant { langCode = "de", title = "Glücksrad", body = "Du kannst wieder drehen! 🎁" },
                    new LocalizedVariant { langCode = "fr", title = "Roue de la fortune", body = "Tu peux tourner à nouveau ! 🎁" },
                    new LocalizedVariant { langCode = "pt", title = "Roda da Fortuna", body = "Você pode girar novamente! 🎁" },
                }
            },

            new NotificationTemplate
            {
                type = GameNotificationType.PayoutTimerEnded,
                key = "payout_timer_ended",
                defaultTitle = "Time is up",
                defaultBody  = "The timer ended — start again!",
                localized = new List<LocalizedVariant>
                {
                    new LocalizedVariant { langCode = "en", title = "Time is up", body = "The timer ended — start again!" },
                    new LocalizedVariant { langCode = "ru", title = "Время вышло", body = "Таймер закончился — начни заново!" },
                    new LocalizedVariant { langCode = "es", title = "Se acabó el tiempo", body = "El temporizador terminó — ¡empieza de nuevo!" },
                    new LocalizedVariant { langCode = "de", title = "Zeit ist um", body = "Der Timer ist abgelaufen — starte neu!" },
                    new LocalizedVariant { langCode = "fr", title = "Temps écoulé", body = "Le minuteur est terminé — recommence !" },
                    new LocalizedVariant { langCode = "pt", title = "O tempo acabou", body = "O temporizador terminou — comece de novo!" },
                }
            },
            new NotificationTemplate
            {
                type = GameNotificationType.ComeBackLongTimeNoSee,
                key = "come_back_long_time",
                defaultTitle = "We miss you!",
                defaultBody  = "Come back — there are gifts waiting 🎁",
                localized = new List<LocalizedVariant>
                {
                    new LocalizedVariant { langCode = "en", title = "We miss you!", body = "Come back — there are gifts waiting 🎁" },
                    new LocalizedVariant { langCode = "ru", title = "Мы скучаем!", body = "Возвращайся — тебя ждут подарки 🎁" },
                    new LocalizedVariant { langCode = "es", title = "¡Te extrañamos!", body = "Vuelve — hay regalos esperándote 🎁" },
                    new LocalizedVariant { langCode = "de", title = "Wir vermissen dich!", body = "Komm zurück — Geschenke warten 🎁" },
                    new LocalizedVariant { langCode = "fr", title = "Tu nous manques !", body = "Reviens — des cadeaux t’attendent 🎁" },
                    new LocalizedVariant { langCode = "pt", title = "Sentimos sua falta!", body = "Volte — há presentes te esperando 🎁" },
                }
            },
        };
    }

    private void BuildTemplatesMap()
    {
        _templatesMap.Clear();
        for (int i = 0; i < templates.Count; i++)
        {
            var t = templates[i];
            if (t == null) continue;
            _templatesMap[t.type] = t;
        }
    }

    // ---------------------------
    // PUBLIC API (enum-based)
    // ---------------------------

    public bool ScheduleAfter(GameNotificationType type, TimeSpan delay, bool replace = true, Dictionary<string, string> args = null)
    {
        if (delay <= TimeSpan.Zero) return false;
        var fireTime = DateTime.Now.Add(delay);
        return ScheduleAt(type, fireTime, replace, args);
    }

    public bool ScheduleAt(GameNotificationType type, DateTime fireTimeLocal, bool replace = true, Dictionary<string, string> args = null)
    {
        if (!HasPermission())
        {
            Debug.Log($"[LocalNotif] Skip '{type}': no permission");
            return false;
        }

        if (fireTimeLocal <= DateTime.Now.AddSeconds(1))
        {
            Debug.Log($"[LocalNotif] Skip '{type}': time in the past");
            return false;
        }

        if (!_templatesMap.TryGetValue(type, out var template) || template == null)
        {
            Debug.LogWarning($"[LocalNotif] No template for type: {type}");
            return false;
        }

        string lang = GetUiLanguageCode();
        template.GetTexts(lang, fallbackLanguageCode, out var title, out var body);

        title = ApplyArgs(title, args);
        body = ApplyArgs(body, args);

        string key = string.IsNullOrWhiteSpace(template.key) ? type.ToString() : template.key;

        if (replace) Cancel(type);

        string channelId = string.IsNullOrWhiteSpace(template.androidChannelIdOverride)
            ? androidDefaultChannelId
            : template.androidChannelIdOverride;

        return ScheduleRaw(key, title, body, fireTimeLocal, channelId);
    }

    public void Cancel(GameNotificationType type)
    {
        if (_templatesMap.TryGetValue(type, out var template) && template != null)
        {
            string key = string.IsNullOrWhiteSpace(template.key) ? type.ToString() : template.key;
            CancelByKey(key);
        }
        else
        {
            CancelByKey(type.ToString());
        }
    }

    public void CancelAll()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
        _store.Clear();
        SaveStore();
#endif

#if UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
    }

    // ---------------------------
    // LOW LEVEL (key-based)
    // ---------------------------

    private bool ScheduleRaw(string key, string title, string body, DateTime fireTimeLocal, string androidChannelId)
    {
#if UNITY_ANDROID
        try
        {
            var notification = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = fireTimeLocal,
                SmallIcon = androidSmallIcon,
                LargeIcon = androidLargeIcon,
                IntentData = key
            };

            int id = AndroidNotificationCenter.SendNotification(notification, androidChannelId);

            var st = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);
            Debug.Log($"[LocalNotif] Scheduled key={key} id={id} fire={fireTimeLocal:HH:mm:ss} status={st}");

            _store.SetAndroidId(key, id);
            SaveStore();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalNotif] Schedule failed: {e}");
            return false;
        }
#endif

#if UNITY_IOS
        var trigger = new iOSNotificationCalendarTrigger
        {
            Year = fireTimeLocal.Year,
            Month = fireTimeLocal.Month,
            Day = fireTimeLocal.Day,
            Hour = fireTimeLocal.Hour,
            Minute = fireTimeLocal.Minute,
            Second = fireTimeLocal.Second,
            Repeats = false
        };

        var notif = new iOSNotification
        {
            Identifier = key,
            Title = title,
            Body = body,
            Data = key,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = trigger
        };

        iOSNotificationCenter.ScheduleNotification(notif);
        return true;
#endif

        return false;
    }

    private void CancelByKey(string key)
    {
#if UNITY_ANDROID
        int id = _store.GetAndroidId(key);
        if (id != -1)
        {
            AndroidNotificationCenter.CancelNotification(id);
            AndroidNotificationCenter.CancelDisplayedNotification(id);
            _store.Remove(key);
            SaveStore();
        }
#endif

#if UNITY_IOS
        iOSNotificationCenter.RemoveScheduledNotification(key);
        iOSNotificationCenter.RemoveDeliveredNotification(key);
#endif
    }

    // ---------------------------
    // PERMISSION + LANGUAGE
    // ---------------------------

    public static bool HasPermission()
    {
#if UNITY_IOS
    var settings = iOSNotificationCenter.GetNotificationSettings();
    return settings.AuthorizationStatus == AuthorizationStatus.Authorized
        || settings.AuthorizationStatus == AuthorizationStatus.Provisional
        || settings.AuthorizationStatus == AuthorizationStatus.Ephemeral;

#elif UNITY_ANDROID
        // Самая правильная проверка в Mobile Notifications:
        // учитывает и Android 13 permission, и блокировку уведомлений в Settings
        return AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed; // :contentReference[oaicite:4]{index=4}
#else
    return false;
#endif
    }


    private string GetUiLanguageCode()
    {
        switch (languageSource)
        {
            case LanguageSource.UsePlayerPrefsKey:
                {
                    var code = PlayerPrefs.GetString(languagePrefsKey, fallbackLanguageCode);
                    return NormalizeLang(code);
                }
            case LanguageSource.UseSystemLanguage:
                {
                    return NormalizeLang(SystemLanguageToCode(Application.systemLanguage));
                }
            default:
                return fallbackLanguageCode;
        }
    }

    private static string NormalizeLang(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "en";
        code = code.Trim().ToLowerInvariant();
        int dash = code.IndexOf('-');
        if (dash > 0) code = code.Substring(0, dash);
        return code;
    }

    private static string SystemLanguageToCode(SystemLanguage lang)
    {
        return lang switch
        {
            SystemLanguage.English => "en",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Spanish => "es",
            SystemLanguage.German => "de",
            SystemLanguage.French => "fr",
            SystemLanguage.Portuguese => "pt",
            _ => "en"
        };
    }


    private void ScheduleComeBackSeries()
    {
        if (!HasPermission()) return;

        CancelComeBackSeries();

        var now = DateTime.Now;

        ScheduleComeBackOnce(now.AddDays(1), "d1");
        ScheduleComeBackOnce(now.AddDays(3), "d3");
        ScheduleComeBackOnce(now.AddDays(7), "d7");
        ScheduleComeBackOnce(now.AddMonths(1), "m1");
    }

    private void CancelComeBackSeries()
    {
        var baseKey = GetBaseKey(GameNotificationType.ComeBackLongTimeNoSee);

        CancelByKey($"{baseKey}_d1");
        CancelByKey($"{baseKey}_d3");
        CancelByKey($"{baseKey}_d7");
        CancelByKey($"{baseKey}_m1");
    }

    private void ScheduleComeBackOnce(DateTime fireTimeLocal, string keySuffix)
    {
        if (!_templatesMap.TryGetValue(GameNotificationType.ComeBackLongTimeNoSee, out var template) || template == null)
            return;

        string lang = GetUiLanguageCode();
        template.GetTexts(lang, fallbackLanguageCode, out var title, out var body);

        string baseKey = GetBaseKey(GameNotificationType.ComeBackLongTimeNoSee);
        string key = $"{baseKey}_{keySuffix}";

        string channelId = string.IsNullOrWhiteSpace(template.androidChannelIdOverride)
            ? androidDefaultChannelId
            : template.androidChannelIdOverride;

        CancelByKey(key);
        ScheduleRaw(key, title, body, fireTimeLocal, channelId);
    }

    private string GetBaseKey(GameNotificationType type)
    {
        if (_templatesMap.TryGetValue(type, out var template) && template != null && !string.IsNullOrWhiteSpace(template.key))
            return template.key;

        return type.ToString();
    }



    private static string ApplyArgs(string s, Dictionary<string, string> args)
    {
        if (string.IsNullOrEmpty(s) || args == null || args.Count == 0) return s;
        foreach (var kv in args)
            s = s.Replace("{" + kv.Key + "}", kv.Value);
        return s;
    }

    // ---------------------------
    // STORE (Android ids)
    // ---------------------------

    private void LoadStore()
    {
        var json = PlayerPrefs.GetString(StorePrefsKey, "");
        _store = string.IsNullOrEmpty(json) ? new NotifStore() : JsonUtility.FromJson<NotifStore>(json);
        if (_store == null) _store = new NotifStore();
    }

    private void SaveStore()
    {
        PlayerPrefs.SetString(StorePrefsKey, JsonUtility.ToJson(_store));
        PlayerPrefs.Save();
    }

#if UNITY_ANDROID
    private static int GetAndroidSdkInt()
    {
        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        return version.GetStatic<int>("SDK_INT");
    }
#endif



    public void PollNotificationOpen()
    {
#if UNITY_EDITOR
        return;
#endif

#if UNITY_ANDROID
        var intentData = AndroidNotificationCenter.GetLastNotificationIntent();
        if (intentData != null)
        {
            // то, что ты положил в IntentData
            var payload = intentData.Notification.IntentData;

            // дедуп (чтобы при нескольких Poll не логировать одно и то же)
            var token = $"android:{intentData.Id}:{payload}";
            if (PlayerPrefs.GetString(LastNotifOpenTokenPrefsKey, "") != token)
            {
                PlayerPrefs.SetString(LastNotifOpenTokenPrefsKey, token);
                PlayerPrefs.Save();

                Debug.Log($"[LocalNotif] Opened from notif payload={payload}");
                LogPushOpen(payload);
            }
        }
#endif

#if UNITY_IOS
        StartCoroutine(PollIOSRespondedNextFrame());
#endif
    }

#if UNITY_IOS
    private IEnumerator PollIOSRespondedNextFrame()
    {
        yield return null;

        var n = iOSNotificationCenter.GetLastRespondedNotification();
        if (n != null)
        {
            var payload = string.IsNullOrEmpty(n.Data) ? n.Identifier : n.Data;

            var token = $"ios:{n.Identifier}:{payload}";
            if (PlayerPrefs.GetString(LastNotifOpenTokenPrefsKey, "") != token)
            {
                PlayerPrefs.SetString(LastNotifOpenTokenPrefsKey, token);
                PlayerPrefs.Save();

                Debug.Log($"[LocalNotif] Opened from notif payload={payload}");

                LogPushOpen(payload);
            }
        }
    }
#endif


    private void LogPushOpen(string payload)
    {
        if (string.IsNullOrEmpty(payload)) payload = "unknown";

        // Попробуем разложить серию comeback на type/step
        string type = payload;
        string step = "";

        int idx = payload.LastIndexOf('_');
        if (idx > 0 && idx < payload.Length - 1)
        {
            var tail = payload.Substring(idx + 1);
            if (tail == "d1" || tail == "d3" || tail == "d7" || tail == "m1")
            {
                type = payload.Substring(0, idx);
                step = tail;
            }
        }

        // Для совместимости/удобства анализа — логируем и payload, и распарсенные поля
        if (string.IsNullOrEmpty(step))
        {
            AnalyticsSafe.LogEvent("push_open",
                new Parameter("payload", payload),
                new Parameter("type", type),
                new Parameter("source", "local_notification")
            );
        }
        else
        {
            AnalyticsSafe.LogEvent("push_open",
                new Parameter("payload", payload),
                new Parameter("type", type),
                new Parameter("step", step),
                new Parameter("source", "local_notification")
            );
        }
    }






    // ---------------------------
    // DATA
    // ---------------------------

    public enum LanguageSource
    {
        UsePlayerPrefsKey,
        UseSystemLanguage
    }

    [Serializable]
    public class NotificationTemplate
    {
        public GameNotificationType type;
        public string key;

        [Header("Android")]
        public string androidChannelIdOverride;

        [Header("Fallback texts")]
        public string defaultTitle;
        [TextArea] public string defaultBody;

        [Header("Localized variants")]
        public List<LocalizedVariant> localized = new List<LocalizedVariant>();

        public void GetTexts(string lang, string fallbackLang, out string title, out string body)
        {
            if (TryGet(lang, out title, out body)) return;
            if (TryGet(fallbackLang, out title, out body)) return;
            title = defaultTitle;
            body = defaultBody;
        }

        private bool TryGet(string lang, out string title, out string body)
        {
            title = null; body = null;
            if (localized == null) return false;

            for (int i = 0; i < localized.Count; i++)
            {
                var v = localized[i];
                if (v == null) continue;
                if (string.Equals(v.langCode?.Trim(), lang, StringComparison.OrdinalIgnoreCase))
                {
                    title = v.title;
                    body = v.body;
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public class LocalizedVariant
    {
        public string langCode;
        public string title;
        [TextArea] public string body;
    }

    [Serializable]
    private class NotifStore
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public int GetAndroidId(string key)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].key == key)
                    return entries[i].androidId;
            return -1;
        }

        public void SetAndroidId(string key, int id)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                {
                    entries[i].androidId = id;
                    return;
                }
            }
            entries.Add(new Entry { key = key, androidId = id });
        }

        public void Remove(string key) => entries.RemoveAll(e => e.key == key);
        public void Clear() => entries.Clear();

        [Serializable]
        private class Entry
        {
            public string key;
            public int androidId;
        }
    }
}
