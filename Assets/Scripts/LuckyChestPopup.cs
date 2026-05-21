using UnityEngine;
using TMPro;
using Firebase.Analytics;
// using Firebase;                 // больше не нужно
// using Firebase.RemoteConfig;     // больше не нужно
using System;

public class LuckyChestPopup : Popup
{
    [Header("States")]
    [SerializeField] private GameObject _closedStateObjects;
    [SerializeField] private GameObject _openedStateObjects;

    [Header("Rewards (defaults, overwritten by Init)")]
    [SerializeField] private int _rewardForFirstAd = 1000;
    [SerializeField] private int _rewardMultiplierSecondAd = 2;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private GameObject _rewardX2Button;

    private LuckyChestState _luckyChestState = LuckyChestState.CLOSED;

    private const int MaxAdViews = 2;
    private int _adViews = 0;
    private int _currentReward = 0;

    // ===== ANALYTICS =====
    private string _closeReason = "unknown";
    private int _closeReward = 0;
    private bool _rewardGranted = false;

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    private static string S(LuckyChestState s) => s == LuckyChestState.CLOSED ? "closed" : "opened";
    // ===== /ANALYTICS =====

    /// <summary>
    /// Инициализация наград снаружи (из LuckyChestManager).
    /// Вызывай сразу после Instantiate.
    /// </summary>
    public void Init(int firstAdReward, int x2Multiplier)
    {
        _rewardForFirstAd = Mathf.Max(0, firstAdReward);
        _rewardMultiplierSecondAd = Mathf.Max(1, x2Multiplier);
        // UI можно обновить (хотя _currentReward всё равно 0 до рекламы)
        UpdateUI();
    }

    private void Awake()
    {
        ChangeState(LuckyChestState.CLOSED);
        UpdateUI();
    }

    private void OnEnable()
    {
        _closeReason = "unknown";
        _closeReward = 0;
        _rewardGranted = false;

        LogEvent("lucky_chest_screen_open",
            new Parameter("state", S(_luckyChestState)),
            new Parameter("ad_views", _adViews)
        );
    }

    private void OnDisable()
    {
        int rewardToReport = (_closeReward > 0) ? _closeReward : _currentReward;

        LogEvent("lucky_chest_screen_close",
            new Parameter("close_reason", _closeReason),
            new Parameter("reward", rewardToReport),
            new Parameter("ad_views", _adViews)
        );
    }

    public void OnClose()
    {
        _closeReason = "x";
        GrantReward("close_x");
        Destroy(gameObject);
    }

    public void ShowUnlockAd()
    {
        if (_adViews >= MaxAdViews) return;
        if (_luckyChestState != LuckyChestState.CLOSED) return;

        LogEvent("lucky_chest_unlock_ad_click",
            new Parameter("ad_purpose", "unlock"),
            new Parameter("ad_views_before", _adViews)
        );

        ShowRewarded("unlock", () =>
        {
            _adViews = 1;
            _currentReward = _rewardForFirstAd;

            ChangeState(LuckyChestState.OPENED);
            UpdateUI();
        });
    }

    public void ShowRewardX2Ad()
    {
        if (_adViews >= MaxAdViews) return;
        if (_luckyChestState != LuckyChestState.OPENED) return;
        if (_adViews != 1) return;

        LogEvent("lucky_chest_rewardx2_ad_click",
            new Parameter("ad_purpose", "reward_x2"),
            new Parameter("ad_views_before", _adViews)
        );

        ShowRewarded("reward_x2", () =>
        {
            _adViews = 2;
            _currentReward = Mathf.Max(0, _rewardForFirstAd) * Mathf.Max(1, _rewardMultiplierSecondAd);
            UpdateUI();
        });
    }

    public void Claim()
    {
        LogEvent("lucky_chest_claim_click",
            new Parameter("reward", _currentReward),
            new Parameter("ad_views", _adViews)
        );

        _closeReason = "claim";
        GrantReward("claim");
        Destroy(gameObject);
    }

    private void ShowRewarded(string adPurpose, Action onSuccess)
    {
        AdManager adManager = FindFirstObjectByType<AdManager>();
        if (adManager == null) return;

        adManager.ShowRewardedAd(() =>
        {
            LogEvent("lucky_chest_rewarded_ad_success",
                new Parameter("ad_purpose", adPurpose)
            );

            onSuccess?.Invoke();
        });
    }

    private void ChangeState(LuckyChestState luckyChestState)
    {
        var prev = _luckyChestState;
        _luckyChestState = luckyChestState;

        if (_closedStateObjects != null)
            _closedStateObjects.SetActive(luckyChestState == LuckyChestState.CLOSED);

        if (_openedStateObjects != null)
            _openedStateObjects.SetActive(luckyChestState == LuckyChestState.OPENED);

        if (prev != luckyChestState)
        {
            LogEvent("lucky_chest_state_changed",
                new Parameter("from_state", S(prev)),
                new Parameter("to_state", S(luckyChestState)),
                new Parameter("reward", _currentReward)
            );
        }
    }

    private void UpdateUI()
    {
        if (_rewardText != null)
            _rewardText.text = _currentReward.ToString();

        bool rewardX2Available = (_adViews == 1);
        if (_rewardX2Button != null)
            _rewardX2Button.SetActive(rewardX2Available);
    }

    private void GrantReward(string grantReason)
    {
        if (_rewardGranted) return;

        int amount = _currentReward;
        _closeReward = amount;

        if (amount <= 0)
        {
            _rewardGranted = true;
            return;
        }

        _rewardGranted = true;
        _currentReward = 0;

        FindFirstObjectByType<BalanceManager>().AddWinnings(amount, "LuckyChestReward");

        LogEvent("lucky_chest_reward_granted",
            new Parameter("amount", amount),
            new Parameter("grant_reason", grantReason)
        );
    }
}


public enum LuckyChestState
{
    CLOSED,
    OPENED
}
