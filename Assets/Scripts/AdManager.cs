using System;
using System.Collections;
using UnityEngine;
using ZeyWinAds.Core;

public class AdManager : MonoBehaviour
{
    public enum AdNetwork { ZeyWin, AdMob }
    public enum AdFormat { Banner, Interstitial, Rewarded }

    [Header("Providers")]
    [SerializeField] private AdMobProvider adMob;
    [SerializeField] private ZeyWinProvider zeyWin;

    [Header("Banner routing")]
    [SerializeField] private AdNetwork bannerPrimary = AdNetwork.ZeyWin;
    [SerializeField] private bool fallbackToOtherIfNotReady = true;

    [Header("Banner")]
    [SerializeField] private bool bannerAutoShowOnStart = true;

    [Header("Banner rotation")]
    [SerializeField] private bool rotateBannersIfBothReady = true;
    [SerializeField] private float bannerRotateSeconds = 30f;
    [SerializeField] private float bannerPollSeconds = 2f;
    private float _bannerNextRotateAt = -1f;
    private float _bannerRotateRemainingOnPause = -1f;

    [Header("Scheduled interstitial")]
    [SerializeField] private bool interstitialScheduleEnabled = true;
    [SerializeField] private float firstInterstitialDelaySeconds = 120f;
    [SerializeField] private int firstInterstitialBallsRequired = 25;
    [SerializeField] private float repeatInterstitialDelaySeconds = 180f;
    [SerializeField] private int repeatInterstitialBallsRequired = 30;
    [SerializeField] private float interstitialScheduleRetrySeconds = 2f;
    [SerializeField] private bool interstitialUseUnscaledTime = true;

    private bool _bannerShownOnce;
    private Coroutine _bannerRotationCoroutine;
    private AdNetwork _currentBannerNet = AdNetwork.ZeyWin;

    private bool _isFullscreenAdShowing;
    private bool _applicationFocused = true;

    private bool AdsEnabled => !NoAdsManager.IsOwned;

    private AdsRouter _router;

    private float _interstitialElapsedSeconds;
    private int _interstitialBallsSinceLastShow;
    private bool _hasShownInterstitialAtLeastOnce;
    private float _lastScheduledInterstitialTryTime = -999f;

    private const string FullscreenShowCountKey = "ads.fullscreen.show_count";

    private bool _bannerRequestedVisible;
    private bool _isPopupShowing;
    private bool _bannersHiddenForPopup;


    private void Awake()
    {
        ZeyWinAds.ZeyWinAds.OnAdWillShow += HandleZeyWinAdWillShow;
        ZeyWinAds.ZeyWinAds.OnAdClosed += HandleZeyWinAdClosed;
    }



    private IEnumerator Start()
    {
        if (zeyWin == null)
        {
            yield return new WaitUntil(() => FindFirstObjectByType<ZeyWinProvider>() != null);
            zeyWin = FindFirstObjectByType<ZeyWinProvider>();
        }

        if (zeyWin.IsInitialized == false)
        {
            yield return new WaitUntil(() => zeyWin.IsInitialized);
        }

        zeyWin?.Init(AdsEnabled);
        adMob?.Init(AdsEnabled);

        if (AdsEnabled && bannerAutoShowOnStart)
            ShowBanner();
        else if (!AdsEnabled)
            SetAdsDisabled(true);

        StartCoroutine(InitRouting());
    }


    private void OnDestroy()
    {
        ZeyWinAds.ZeyWinAds.OnAdWillShow -= HandleZeyWinAdWillShow;
        ZeyWinAds.ZeyWinAds.OnAdClosed -= HandleZeyWinAdClosed;
    }

    private void Update()
    {
        AdvanceInterstitialScheduleClock();
        TryShowScheduledInterstitialIfDue();
    }

    private IEnumerator InitRouting()
    {
        yield return RemoteConfigManager.EnsureReadyCoroutine();

        _router = new AdsRouter();
        var json = RemoteConfigManager.GetString(RemoteConfigManager.ADS_ROUTING_JSON, "{}");
        _router.LoadFromJson(json);

        if (adMob != null)
        {
            adMob.InterstitialImpression += () =>
                _router?.Record(AdFormat.Interstitial, AdNetwork.AdMob, AdsRouter.AdsEvent.Impression);

            adMob.RewardedImpression += () =>
                _router?.Record(AdFormat.Rewarded, AdNetwork.AdMob, AdsRouter.AdsEvent.Impression);
        }
    }

    // -------------------- ПУБЛИЧНЫЙ API --------------------

    public bool IsRewardedReady
    {
        get
        {
            var primary = ChooseNetwork(AdFormat.Rewarded);
            return IsReady(primary, AdFormat.Rewarded) ||
                   (fallbackToOtherIfNotReady && IsReady(Other(primary), AdFormat.Rewarded));
        }
    }

    public bool IsInterstitialReady
    {
        get
        {
            var primary = ChooseNetwork(AdFormat.Interstitial);
            return IsReady(primary, AdFormat.Interstitial) ||
                   (fallbackToOtherIfNotReady && IsReady(Other(primary), AdFormat.Interstitial));
        }
    }


    /// <summary>
    /// Вызывать на каждый реально созданный и выброшенный шар.
    /// </summary>
    public void RegisterInterstitialBallThrow(int count = 1)
    {
        if (!AdsEnabled) return;
        if (!interstitialScheduleEnabled) return;
        if (count <= 0) return;

        _interstitialBallsSinceLastShow += count;
        TryShowScheduledInterstitialIfDue();
    }

    public void ShowRewardedAd(Action onReward = null, Action onClose = null)
    {
        if (!AdsEnabled)
        {
            onClose?.Invoke();
            return;
        }

        var primary = ChooseNetwork(AdFormat.Rewarded);

        if (TryShowRewarded(primary, onReward, onClose)) return;

        if (fallbackToOtherIfNotReady && TryShowRewarded(Other(primary), onReward, onClose)) return;

        Load(primary, AdFormat.Rewarded);
        if (fallbackToOtherIfNotReady) Load(Other(primary), AdFormat.Rewarded);
    }

    public void ShowInterstitialAd(Action onClose = null)
    {
        if (!AdsEnabled)
        {
            onClose?.Invoke();
            return;
        }

        var primary = ChooseNetwork(AdFormat.Interstitial);

        if (TryShowInterstitial(primary, onClose)) return;

        if (fallbackToOtherIfNotReady && TryShowInterstitial(Other(primary), onClose)) return;

        Load(primary, AdFormat.Interstitial);
        if (fallbackToOtherIfNotReady) Load(Other(primary), AdFormat.Interstitial);
    }

    public void ShowBanner()
    {
        _bannerRequestedVisible = true;

        if (!AdsEnabled) return;
        if (_isPopupShowing) return;

        if (rotateBannersIfBothReady)
        {
            StartBannerRotation();
            return;
        }

        var net = ChooseNetwork(AdFormat.Banner);
        HideBanner(Other(net));

        if (net == AdNetwork.ZeyWin)
        {
            zeyWin?.ShowBannerBottom();
        }
        else
        {
            zeyWin?.HideSdkBannerInternal();
            adMob?.ShowBanner();
        }
    }

    public void HideBanner()
    {
        _bannerRequestedVisible = false;
        _bannersHiddenForPopup = false;

        StopBannerRotation();
        HideBanner(AdNetwork.ZeyWin);
        HideBanner(AdNetwork.AdMob);
    }

    public void SetAdsDisabled(bool disabled)
    {
        if (disabled)
        {
            StopBannerRotation();
            HideBanner();
            adMob?.SetAdsDisabled(true);
            zeyWin?.SetAdsDisabled(true);
            Debug.Log("[AdManager] Ads disabled.");
        }
        else
        {
            zeyWin?.SetAdsDisabled(false);
            adMob?.SetAdsDisabled(false);
            ShowBanner();
        }
    }

    // -------------------- POPUP TIMER --------------------


    private void AdvanceInterstitialScheduleClock()
    {
        if (!CanAdvanceInterstitialScheduleClock())
            return;

        _interstitialElapsedSeconds += interstitialUseUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    private bool CanAdvanceInterstitialScheduleClock()
    {
        return AdsEnabled
               && interstitialScheduleEnabled
               && _applicationFocused
               && !_isFullscreenAdShowing
               && !_isPopupShowing;
    }

    private void TryShowScheduledInterstitialIfDue()
    {
        if (!AdsEnabled) return;
        if (!interstitialScheduleEnabled) return;
        if (_isFullscreenAdShowing) return;
        if (_isPopupShowing) return;
        if (!IsScheduledInterstitialDue()) return;

        if (Time.realtimeSinceStartup - _lastScheduledInterstitialTryTime < interstitialScheduleRetrySeconds)
            return;

        _lastScheduledInterstitialTryTime = Time.realtimeSinceStartup;
        ShowInterstitialAd();
    }

    private bool IsScheduledInterstitialDue()
    {
        float requiredDelay = _hasShownInterstitialAtLeastOnce
            ? repeatInterstitialDelaySeconds
            : firstInterstitialDelaySeconds;

        int requiredBalls = _hasShownInterstitialAtLeastOnce
            ? repeatInterstitialBallsRequired
            : firstInterstitialBallsRequired;

        return _interstitialElapsedSeconds >= requiredDelay &&
               _interstitialBallsSinceLastShow >= requiredBalls;
    }

    private void MarkInterstitialShown()
    {
        _hasShownInterstitialAtLeastOnce = true;
        _interstitialElapsedSeconds = 0f;
        _interstitialBallsSinceLastShow = 0;
        _lastScheduledInterstitialTryTime = -999f;
    }

    // -------------------- BANNER ROTATION --------------------

    private void StartBannerRotation()
    {
        if (_bannerRotationCoroutine != null)
        {
            Debug.Log("[BannerRotation] start skipped, coroutine already running");
            return;
        }

        if (!_bannerShownOnce)
            _currentBannerNet = ChooseNetwork(AdFormat.Banner);

        Debug.Log(
            $"[BannerRotation] START current={_currentBannerNet}, bannerShownOnce={_bannerShownOnce}"
        );

        _bannerRotationCoroutine = StartCoroutine(BannerRotationLoop());
    }

    private void StopBannerRotation()
    {
        if (_bannerRotationCoroutine != null)
        {
            Debug.Log($"[BannerRotation] STOP current={_currentBannerNet}");
            StopCoroutine(_bannerRotationCoroutine);
            _bannerRotationCoroutine = null;
        }

        _bannerNextRotateAt = -1f;
        _bannerRotateRemainingOnPause = -1f;
    }

    private IEnumerator BannerRotationLoop()
    {
        Load(AdNetwork.AdMob, AdFormat.Banner);
        Load(AdNetwork.ZeyWin, AdFormat.Banner);

        while (AdsEnabled && !_bannerShownOnce)
        {
            if (_isPopupShowing || !_bannerRequestedVisible)
            {
                yield return null;
                continue;
            }

            bool currentReady = IsReady(_currentBannerNet, AdFormat.Banner);
            bool otherReady = IsReady(Other(_currentBannerNet), AdFormat.Banner);

            Debug.Log(
                $"[BannerRotation] warmup current={_currentBannerNet}, " +
                $"currentReady={currentReady}, " +
                $"other={Other(_currentBannerNet)}, " +
                $"otherReady={otherReady}"
            );

            if (currentReady)
            {
                ShowBannerForced(_currentBannerNet);
                _bannerShownOnce = true;
                ArmBannerRotationTimer();
                break;
            }

            if (otherReady)
            {
                _currentBannerNet = Other(_currentBannerNet);
                ShowBannerForced(_currentBannerNet);
                _bannerShownOnce = true;
                ArmBannerRotationTimer();
                break;
            }

            Load(_currentBannerNet, AdFormat.Banner);
            Load(Other(_currentBannerNet), AdFormat.Banner);

            yield return new WaitForSecondsRealtime(bannerPollSeconds);
        }

        while (AdsEnabled)
        {
            if (_isPopupShowing || !_bannerRequestedVisible)
            {
                yield return null;
                continue;
            }

            if (_bannerNextRotateAt < 0f)
            {
                ResumeBannerRotationTimer();
                yield return null;
                continue;
            }

            if (Time.realtimeSinceStartup < _bannerNextRotateAt)
            {
                yield return null;
                continue;
            }

            var other = Other(_currentBannerNet);
            bool otherReady = IsReady(other, AdFormat.Banner);

            Debug.Log(
                $"[BannerRotation] tick current={_currentBannerNet}, " +
                $"other={other}, otherReady={otherReady}"
            );

            if (otherReady)
            {
                Debug.Log($"[BannerRotation] switch to {other}");
                _currentBannerNet = other;
                ShowBannerForced(_currentBannerNet);
                ArmBannerRotationTimer();
            }
            else
            {
                Debug.Log($"[BannerRotation] other not ready, preload {other}");
                Load(other, AdFormat.Banner);

                _bannerNextRotateAt = Time.realtimeSinceStartup + bannerPollSeconds;
            }
        }
    }

    private void RestoreBannerAfterPopup()
    {
        if (!AdsEnabled) return;
        if (!_bannerRequestedVisible) return;
        if (_isPopupShowing) return;

        if (IsReady(_currentBannerNet, AdFormat.Banner))
        {
            Debug.Log($"[BannerRotation] restore current={_currentBannerNet}");
            ShowBannerForced(_currentBannerNet);
            return;
        }

        var other = Other(_currentBannerNet);

        if (IsReady(other, AdFormat.Banner))
        {
            _currentBannerNet = other;
            Debug.Log($"[BannerRotation] restore fallback switch to {_currentBannerNet}");
            ShowBannerForced(_currentBannerNet);
            return;
        }

        Debug.Log("[BannerRotation] restore failed, preload both");
        Load(AdNetwork.ZeyWin, AdFormat.Banner);
        Load(AdNetwork.AdMob, AdFormat.Banner);
    }

    private void ShowBannerForced(AdNetwork net)
    {
        if (!AdsEnabled) return;

        var other = Other(net);
        HideBanner(other);

        if (net == AdNetwork.ZeyWin)
        {
            zeyWin?.ShowBannerBottom();
        }
        else
        {
            zeyWin?.HideSdkBannerInternal();
            adMob?.ShowBanner();
        }
    }

    // -------------------- ВЫБОР СЕТИ --------------------

    private AdNetwork ChooseNetwork(AdFormat format)
    {
        if (format == AdFormat.Interstitial || format == AdFormat.Rewarded)
            return ChooseFullscreenNetworkBySequence();

        if (_router != null && _router.IsLoaded)
            return _router.Choose(format, net => IsReady(net, format));

        return bannerPrimary;
    }

    private AdNetwork ChooseFullscreenNetworkBySequence()
    {
        int nextShowNumber = GetFullscreenShowCount() + 1;

        // Первые 5 fullscreen ads — ZeyWin.
        if (nextShowNumber <= 5)
            return AdNetwork.ZeyWin;

        // Дальше каждая 3-я fullscreen ad — AdMob.
        // Паттерн после первых 5: ZeyWin, ZeyWin, AdMob, ...
        int postWarmupIndex = nextShowNumber - 5;
        return postWarmupIndex % 3 == 0
            ? AdNetwork.AdMob
            : AdNetwork.ZeyWin;
    }

    private int GetFullscreenShowCount()
    {
        return PlayerPrefs.GetInt(FullscreenShowCountKey, 0);
    }

    private void IncrementFullscreenShowCount()
    {
        int nextValue = PlayerPrefs.GetInt(FullscreenShowCountKey, 0) + 1;
        PlayerPrefs.SetInt(FullscreenShowCountKey, nextValue);
        PlayerPrefs.Save();
    }

    private static AdNetwork Other(AdNetwork n) =>
        n == AdNetwork.ZeyWin ? AdNetwork.AdMob : AdNetwork.ZeyWin;

    // -------------------- ВНУТРЕННИЕ ХЕЛПЕРЫ --------------------

    private bool IsReady(AdNetwork net, AdFormat format)
    {
        if (net == AdNetwork.ZeyWin)
        {
            if (zeyWin == null) return false;
            return format switch
            {
                AdFormat.Banner => zeyWin.IsBannerReady,
                AdFormat.Interstitial => zeyWin.IsInterstitialReady,
                AdFormat.Rewarded => zeyWin.IsRewardedReady,
                _ => false
            };
        }

        if (adMob == null) return false;
        return format switch
        {
            AdFormat.Banner => adMob.BannerIsReady,
            AdFormat.Interstitial => adMob.IsInterstitialReady,
            AdFormat.Rewarded => adMob.IsRewardedReady,
            _ => false
        };
    }

    private void Load(AdNetwork net, AdFormat format)
    {
        if (net == AdNetwork.ZeyWin)
        {
            if (zeyWin == null) return;

            switch (format)
            {
                case AdFormat.Banner:
                    zeyWin.LoadBanner();
                    break;
                case AdFormat.Interstitial:
                    zeyWin.LoadInterstitial();
                    break;
                case AdFormat.Rewarded:
                    zeyWin.LoadRewarded();
                    break;
            }

            return;
        }

        if (adMob == null) return;

        switch (format)
        {
            case AdFormat.Banner:
                adMob.PreloadBannerAd();
                break;
            case AdFormat.Interstitial:
                adMob.LoadInterstitialAd();
                break;
            case AdFormat.Rewarded:
                adMob.LoadRewardedAd();
                break;
        }
    }

    private bool TryShowInterstitial(AdNetwork net, Action onClose)
    {
        if (!IsReady(net, AdFormat.Interstitial)) return false;
        if (_isFullscreenAdShowing) return false;

        _isFullscreenAdShowing = true;

        Action wrappedClose = () =>
        {
            _isFullscreenAdShowing = false;
            _router?.Record(AdFormat.Interstitial, net, AdsRouter.AdsEvent.Closed);
            onClose?.Invoke();
        };

        _router?.Record(AdFormat.Interstitial, net, AdsRouter.AdsEvent.Shown);
        IncrementFullscreenShowCount();
        MarkInterstitialShown();

        if (net == AdNetwork.ZeyWin)
            zeyWin.ShowInterstitial(wrappedClose);
        else
            adMob.ShowInterstitialAd(wrappedClose);

        return true;
    }

    private bool TryShowRewarded(AdNetwork net, Action onReward, Action onClose)
    {
        if (!IsReady(net, AdFormat.Rewarded)) return false;
        if (_isFullscreenAdShowing) return false;

        _isFullscreenAdShowing = true;

        Action wrappedReward = () =>
        {
            _router?.Record(AdFormat.Rewarded, net, AdsRouter.AdsEvent.Reward);
            onReward?.Invoke();
        };

        Action wrappedClose = () =>
        {
            _isFullscreenAdShowing = false;
            _router?.Record(AdFormat.Rewarded, net, AdsRouter.AdsEvent.Closed);
            onClose?.Invoke();
        };

        _router?.Record(AdFormat.Rewarded, net, AdsRouter.AdsEvent.Shown);
        IncrementFullscreenShowCount();

        if (net == AdNetwork.ZeyWin)
            zeyWin.ShowRewarded(wrappedReward, wrappedClose);
        else
            adMob.ShowRewardedAd(wrappedReward, wrappedClose);

        return true;
    }

    private void HideBanner(AdNetwork net)
    {
        if (net == AdNetwork.ZeyWin) zeyWin?.HideBanner();
        else adMob?.HideBanner();
    }

    private void OnApplicationFocus(bool focus)
    {
        _applicationFocused = focus;
    }

    private void OnApplicationPause(bool pause)
    {
        _applicationFocused = !pause;
    }


    private void HandleZeyWinAdWillShow(AdType adType)
    {
        if (adType != AdType.Popup)
            return;

        Debug.Log("[BannerRotation] ZeyWin popup will show -> pause banner display only");

        _isPopupShowing = true;

        if (!AdsEnabled)
            return;

        if (!_bannerRequestedVisible)
            return;

        if (_bannersHiddenForPopup)
            return;

        _bannersHiddenForPopup = true;

        PauseBannerRotationTimer();

        HideBanner(AdNetwork.ZeyWin);
        HideBanner(AdNetwork.AdMob);
    }

    private void HandleZeyWinAdClosed(AdType adType)
    {
        if (adType != AdType.Popup)
            return;

        Debug.Log("[BannerRotation] ZeyWin popup closed -> resume banner display");

        _isPopupShowing = false;

        if (!_bannersHiddenForPopup)
            return;

        _bannersHiddenForPopup = false;

        if (!AdsEnabled)
            return;

        if (!_bannerRequestedVisible)
            return;

        ResumeBannerRotationTimer();
        RestoreBannerAfterPopup();
    }


    private void ArmBannerRotationTimer()
    {
        _bannerRotateRemainingOnPause = -1f;
        _bannerNextRotateAt = Time.realtimeSinceStartup + bannerRotateSeconds;
    }

    private void PauseBannerRotationTimer()
    {
        if (_bannerNextRotateAt < 0f)
            return;

        _bannerRotateRemainingOnPause = Mathf.Max(0f, _bannerNextRotateAt - Time.realtimeSinceStartup);
        _bannerNextRotateAt = -1f;

        Debug.Log($"[BannerRotation] timer paused, remaining={_bannerRotateRemainingOnPause:0.00}s");
    }

    private void ResumeBannerRotationTimer()
    {
        if (_bannerRotateRemainingOnPause >= 0f)
        {
            _bannerNextRotateAt = Time.realtimeSinceStartup + _bannerRotateRemainingOnPause;
            Debug.Log($"[BannerRotation] timer resumed, remaining={_bannerRotateRemainingOnPause:0.00}s");
            _bannerRotateRemainingOnPause = -1f;
            return;
        }

        if (_bannerNextRotateAt < 0f)
        {
            _bannerNextRotateAt = Time.realtimeSinceStartup + bannerRotateSeconds;
            Debug.Log($"[BannerRotation] timer resumed with full period={bannerRotateSeconds:0.00}s");
        }
    }
}