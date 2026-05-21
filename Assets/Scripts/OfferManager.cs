using System;
using System.Collections;
using System.Text.RegularExpressions;
using Firebase;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OfferManager : MonoBehaviour
{
    // PlayerPrefs keys
    private const string FirstOpenUtcKey = "offer_first_open_utc";

    // Threshold show storage (throws)
    private const string CountingStartedKey = "offer_counting_started"; // 0/1
    private const string ThrownCountKey = "offer_thrown_count";         // int
    private const string NextPopupAtKey = "offer_next_popup_at";        // int

    // Lucky chest trigger
    private const string ChestOpenedCountKey = "offer_chest_opened_count"; // int
    private const string ChestOfferShownKey = "offer_chest_offer_shown";   // 0/1

    // Legacy migration key
    private const string LegacyProcessedKey = "offer_day3_processed";

    public static OfferManager Instance => _instance;
    private static OfferManager _instance;

    [Header("Defaults (used if Remote Config unavailable)")]
    [SerializeField] private int defaultAfterDays = 3;
    [SerializeField] private int defaultFirstShowAtThrows = 100;
    [SerializeField] private int defaultRepeatEveryThrows = 5000;

    [Header("Lucky chest trigger")]
    [SerializeField] private bool enableLuckyChestTrigger = true;
    [SerializeField] private int showOnChestNumber = 3;

    [Header("Scene gating (optional)")]
    [Tooltip("Show popup only in this scene. Empty = any scene.")]
    [SerializeField] private string onlyInSceneName = "Game";

    [Header("Debug")]
    [SerializeField] private bool debugShowOnSpace = true;

    private OfferRemoteConfigData _cachedOffer;
    private bool _showFlowRunning;

    // runtime values (from RC or defaults)
    private int _afterDays;
    private int _firstShowAtThrows;
    private int _repeatEveryThrows;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        DontDestroyOnLoad(gameObject);
        EnsureFirstOpenSaved();

        if (PlayerPrefs.HasKey(LegacyProcessedKey))
        {
            PlayerPrefs.DeleteKey(LegacyProcessedKey);
            PlayerPrefs.Save();
        }

        _afterDays = Mathf.Max(1, defaultAfterDays);
        _firstShowAtThrows = Mathf.Max(1, defaultFirstShowAtThrows);
        _repeatEveryThrows = Mathf.Max(1, defaultRepeatEveryThrows);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(Bootstrap());
    }

    private IEnumerator Bootstrap()
    {
        yield return RemoteConfigManager.EnsureReadyCoroutine();
        ApplyTuningFromRemoteConfig();

        TryInitCountingIfNeeded();
        TryShowIfReady();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        TryInitCountingIfNeeded();
        TryShowIfReady();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) return;
        TryInitCountingIfNeeded();
        TryShowIfReady();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInitCountingIfNeeded();
        TryShowIfReady();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (!debugShowOnSpace) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("OfferManager DEBUG: Space pressed -> show popup from Remote Config");
            StartCoroutine(DebugShowPopupFromRemoteConfig());
        }
    }

    private IEnumerator DebugShowPopupFromRemoteConfig()
    {
        if (_showFlowRunning) yield break;

        if (!string.IsNullOrEmpty(onlyInSceneName) &&
            SceneManager.GetActiveScene().name != onlyInSceneName)
            yield break;

        _showFlowRunning = true;

        yield return RemoteConfigManager.EnsureReadyCoroutine();
        ApplyTuningFromRemoteConfig();

        var data = ReadOfferFromRemoteConfig();
        if (data == null || string.IsNullOrEmpty(data.picture_url))
        {
            _showFlowRunning = false;
            yield break;
        }

        var popup = FindFirstObjectByType<OfferPopup>();
        if (popup == null)
        {
            _showFlowRunning = false;
            yield break;
        }

        _cachedOffer = data;

        popup.Show(_cachedOffer, () => { OpenWebView(); });

        _showFlowRunning = false;
    }
#endif

    // ====== Lucky Chest trigger ======

    /// <summary>
    /// Вызывай при успешном открытии сундука (когда реально открылся).
    /// На 3-й сундук покажем offer (если включено).
    /// </summary>
    public void NotifyLuckyChestOpened()
    {
        if (!enableLuckyChestTrigger) return;

        int shown = PlayerPrefs.GetInt(ChestOfferShownKey, 0);
        if (shown == 1) return;

        int count = PlayerPrefs.GetInt(ChestOpenedCountKey, 0);
        count++;
        PlayerPrefs.SetInt(ChestOpenedCountKey, count);
        PlayerPrefs.Save();

        if (count == Mathf.Max(1, showOnChestNumber))
        {
            TryShowOfferForChest();
        }
    }

    private void TryShowOfferForChest()
    {
        if (_showFlowRunning) return;

        if (!string.IsNullOrEmpty(onlyInSceneName) &&
            SceneManager.GetActiveScene().name != onlyInSceneName)
            return;

        StartCoroutine(ShowOfferCoroutine(advanceThreshold: false, onShown: () =>
        {
            PlayerPrefs.SetInt(ChestOfferShownKey, 1);
            PlayerPrefs.Save();
        }));
    }

    // ====== Throws trigger (твоя текущая логика) ======

    public void AddThrownBall() => AddThrownBalls(1);

    public void AddThrownBalls(int count)
    {
        if (count <= 0) return;

        if (!IsEligibleDay())
            return;

        TryInitCountingIfNeeded();

        if (PlayerPrefs.GetInt(CountingStartedKey, 0) != 1)
            return;

        int current = PlayerPrefs.GetInt(ThrownCountKey, 0);
        current += count;
        PlayerPrefs.SetInt(ThrownCountKey, current);
        PlayerPrefs.Save();

        int nextAt = PlayerPrefs.GetInt(NextPopupAtKey, _firstShowAtThrows);
        if (current >= nextAt)
        {
            TryShowIfReady();
        }
    }

    private bool IsEligibleDay()
    {
        int dayNumber = GetDayNumberSinceFirstOpenUtc();
        return dayNumber >= _afterDays;
    }

    private void TryInitCountingIfNeeded()
    {
        if (!IsEligibleDay())
            return;

        if (PlayerPrefs.GetInt(CountingStartedKey, 0) == 1)
            return;

        PlayerPrefs.SetInt(CountingStartedKey, 1);
        PlayerPrefs.SetInt(ThrownCountKey, 0);
        PlayerPrefs.SetInt(NextPopupAtKey, Mathf.Max(1, _firstShowAtThrows));
        PlayerPrefs.Save();
    }

    private void TryShowIfReady()
    {
        if (_showFlowRunning) return;

        if (!string.IsNullOrEmpty(onlyInSceneName) &&
            SceneManager.GetActiveScene().name != onlyInSceneName)
            return;

        if (PlayerPrefs.GetInt(CountingStartedKey, 0) != 1)
            return;

        int current = PlayerPrefs.GetInt(ThrownCountKey, 0);
        int nextAt = PlayerPrefs.GetInt(NextPopupAtKey, _firstShowAtThrows);

        if (current < nextAt)
            return;

        StartCoroutine(ShowOfferCoroutine(advanceThreshold: true, onShown: null));
    }

    // ====== Common show coroutine ======

    private IEnumerator ShowOfferCoroutine(bool advanceThreshold, Action onShown)
    {
        _showFlowRunning = true;

        yield return RemoteConfigManager.EnsureReadyCoroutine();
        ApplyTuningFromRemoteConfig();

        _cachedOffer = ReadOfferFromRemoteConfig();

        // ✅ Если ok:false или данных нет — просто выходим (ничего не показываем)
        if (_cachedOffer == null || string.IsNullOrEmpty(_cachedOffer.picture_url))
        {
            _showFlowRunning = false;
            yield break;
        }

        var popup = FindFirstObjectByType<OfferPopup>();
        if (popup == null)
        {
            _showFlowRunning = false;
            yield break;
        }

        popup.Show(_cachedOffer, () =>
        {
            popup.Hide();
            OpenWebView();
        });

        onShown?.Invoke();

        if (advanceThreshold)
        {
            int nextAt = PlayerPrefs.GetInt(NextPopupAtKey, _firstShowAtThrows);
            int step = Mathf.Max(1, _repeatEveryThrows);
            nextAt += step;

            PlayerPrefs.SetInt(NextPopupAtKey, nextAt);
            PlayerPrefs.Save();
        }

        _showFlowRunning = false;
    }

    private void OpenWebView()
    {
        if (_cachedOffer == null || string.IsNullOrEmpty(_cachedOffer.open_url))
            return;

        var webView = new GameObject("WebView").AddComponent<View>();
        webView.Init(_cachedOffer.open_url);
    }

    private void EnsureFirstOpenSaved()
    {
        if (PlayerPrefs.HasKey(FirstOpenUtcKey))
            return;

        long firstOpen = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(FirstOpenUtcKey, firstOpen.ToString());
        PlayerPrefs.Save();
    }

    private int GetDayNumberSinceFirstOpenUtc()
    {
        if (!PlayerPrefs.HasKey(FirstOpenUtcKey))
            return 1;

        if (!long.TryParse(PlayerPrefs.GetString(FirstOpenUtcKey), out long firstUnix))
            return 1;

        DateTime firstDate = DateTimeOffset.FromUnixTimeSeconds(firstUnix).UtcDateTime.Date;
        DateTime nowDate = DateTime.UtcNow.Date;

        int diffDays = (nowDate - firstDate).Days;
        return diffDays + 1;
    }


    private void ApplyTuningFromRemoteConfig()
    {
        _afterDays = Mathf.Max(1, RemoteConfigManager.GetInt(RemoteConfigManager.OFFER_AFTER_DAYS, defaultAfterDays));
        _firstShowAtThrows = Mathf.Max(1, RemoteConfigManager.GetInt(RemoteConfigManager.OFFER_FIRST_SHOW_THROWS, defaultFirstShowAtThrows));
        _repeatEveryThrows = Mathf.Max(1, RemoteConfigManager.GetInt(RemoteConfigManager.OFFER_REPEAT_EVERY_THROWS, defaultRepeatEveryThrows));

        if (PlayerPrefs.GetInt(CountingStartedKey, 0) == 1)
        {
            int nextAt = PlayerPrefs.GetInt(NextPopupAtKey, _firstShowAtThrows);
            if (nextAt <= 0) nextAt = _firstShowAtThrows;
            PlayerPrefs.SetInt(NextPopupAtKey, nextAt);
            PlayerPrefs.Save();
        }
    }


    private OfferRemoteConfigData ReadOfferFromRemoteConfig()
    {
        try
        {
            string json = RemoteConfigManager.GetString(RemoteConfigManager.OFFER_JSON, "");
            if (string.IsNullOrEmpty(json)) return null;

            if (IsOfferExplicitlyDisabled(json))
            {
                Debug.Log("OfferManager: offer ok:false -> popup disabled.");
                return null;
            }

            return JsonUtility.FromJson<OfferRemoteConfigData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("OfferManager: ReadOfferFromRemoteConfig parse error: " + e);
            return null;
        }
    }


    /// <summary>
    /// True только если в JSON явно есть ok:false.
    /// Если поля ok нет — считаем, что включено.
    /// </summary>
    private static bool IsOfferExplicitlyDisabled(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;

        // Если ok вообще нет — не отключаем
        if (!json.Contains("\"ok\"")) return false;

        // Ловим: "ok":false (с пробелами)
        return Regex.IsMatch(json, "\"ok\"\\s*:\\s*false", RegexOptions.IgnoreCase);
    }
}

[Serializable]
public class OfferRemoteConfigData
{
    // ok можно не хранить — мы его парсим строкой через IsOfferExplicitlyDisabled
    public string title;
    public string description;
    public string open_url;
    public string picture_url;
    public string button_text;
}
