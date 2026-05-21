using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdMobProvider : MonoBehaviour
{
    [Header("Android Ad Unit IDs")]
    [SerializeField] private string androidRewardedId = "ca-app-pub-6988952582458184/3302497559";
    [SerializeField] private string androidInterstitialId = "ca-app-pub-6988952582458184/6653316139";
    [SerializeField] private string androidBannerId = "ca-app-pub-6988952582458184/7966397807";

    [Header("iOS Ad Unit IDs")]
    [SerializeField] private string iosRewardedId = "";
    [SerializeField] private string iosInterstitialId = "";
    [SerializeField] private string iosBannerId = "";

    [Header("Test devices")]
    [SerializeField] private string[] testDeviceIds = { "E2D426604A92FFADE86351336AAC473E" };

    [SerializeField] private float retryDelaySeconds = 8f;

    private BannerView _banner;
    private InterstitialAd _interstitial;
    private RewardedAd _rewarded;

    private string _bannerId;
    private string _interstitialId;
    private string _rewardedId;

    private bool _initialized;
    private bool _adsEnabled = true;
    private bool _bannerLoaded;
    private bool _bannerLoading;
    private bool _interstitialLoading;
    private bool _rewardedLoading;

    private Coroutine _bannerRetry;
    private Coroutine _interstitialRetry;
    private Coroutine _rewardedRetry;

    public event Action InterstitialImpression;
    public event Action RewardedImpression;

    public bool BannerIsReady => _initialized && _banner != null && _bannerLoaded;
    public bool IsInterstitialReady => _initialized && _interstitial != null && _interstitial.CanShowAd();
    public bool IsRewardedReady => _initialized && _rewarded != null && _rewarded.CanShowAd();

    private void Awake()
    {
#if UNITY_ANDROID
        _bannerId = androidBannerId;
        _interstitialId = androidInterstitialId;
        _rewardedId = androidRewardedId;
#elif UNITY_IOS
        _bannerId = iosBannerId;
        _interstitialId = iosInterstitialId;
        _rewardedId = iosRewardedId;
#else
        _bannerId = "unused";
        _interstitialId = "unused";
        _rewardedId = "unused";
#endif
    }

    public void Init(bool enabled)
    {
        _adsEnabled = enabled;
        ConfigureTestDevices();

        MobileAds.Initialize(_ =>
        {
            _initialized = true;
            Debug.Log("[AdMobProvider] Initialized");

            if (!_adsEnabled)
            {
                SetAdsDisabled(true);
                return;
            }

            PreloadBannerAd();
            LoadInterstitialAd();
            LoadRewardedAd();
        });
    }

    public void SetAdsDisabled(bool disabled)
    {
        _adsEnabled = !disabled;

        if (disabled)
        {
            DestroyBanner();
            DestroyInterstitial();
            DestroyRewarded();
            return;
        }

        PreloadBannerAd();
        LoadInterstitialAd();
        LoadRewardedAd();
    }

    public void PreloadBannerAd()
    {
        if (!_initialized || !_adsEnabled || _bannerLoading || string.IsNullOrEmpty(_bannerId))
            return;

        if (BannerIsReady)
            return;

        _bannerLoading = true;
        _bannerLoaded = false;

        DestroyBanner();
        _banner = new BannerView(_bannerId, AdSize.Banner, AdPosition.Bottom);
        _banner.OnBannerAdLoaded += () =>
        {
            _bannerLoading = false;
            _bannerLoaded = true;
            _banner.Hide();
            Debug.Log("[AdMobProvider] Banner loaded");
        };
        _banner.OnBannerAdLoadFailed += error =>
        {
            _bannerLoading = false;
            _bannerLoaded = false;
            Debug.LogWarning("[AdMobProvider] Banner load failed: " + error.GetMessage());
            ScheduleBannerRetry();
        };
        _banner.LoadAd(new AdRequest());
    }

    public void ShowBanner()
    {
        if (!_adsEnabled)
            return;

        if (BannerIsReady)
        {
            _banner.Show();
            return;
        }

        PreloadBannerAd();
    }

    public void HideBanner()
    {
        _banner?.Hide();
    }

    public void DestroyBanner()
    {
        _banner?.Destroy();
        _banner = null;
        _bannerLoaded = false;
        _bannerLoading = false;
    }

    public void LoadInterstitialAd()
    {
        if (!_initialized || !_adsEnabled || _interstitialLoading || string.IsNullOrEmpty(_interstitialId))
            return;

        if (IsInterstitialReady)
            return;

        _interstitialLoading = true;
        DestroyInterstitial();

        InterstitialAd.Load(_interstitialId, new AdRequest(), (ad, error) =>
        {
            _interstitialLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdMobProvider] Interstitial load failed: " + (error?.GetMessage() ?? "null ad"));
                ScheduleInterstitialRetry();
                return;
            }

            _interstitial = ad;
            _interstitial.OnAdImpressionRecorded += () => InterstitialImpression?.Invoke();
            _interstitial.OnAdFullScreenContentClosed += () =>
            {
                DestroyInterstitial();
                LoadInterstitialAd();
            };
            _interstitial.OnAdFullScreenContentFailed += err =>
            {
                Debug.LogWarning("[AdMobProvider] Interstitial show failed: " + err.GetMessage());
                DestroyInterstitial();
                LoadInterstitialAd();
            };
            Debug.Log("[AdMobProvider] Interstitial loaded");
        });
    }

    public void ShowInterstitialAd(Action onClose = null)
    {
        if (!IsInterstitialReady)
        {
            LoadInterstitialAd();
            onClose?.Invoke();
            return;
        }

        var ad = _interstitial;
        _interstitial = null;
        ad.OnAdFullScreenContentClosed += () => onClose?.Invoke();
        ad.OnAdFullScreenContentFailed += _ => onClose?.Invoke();
        ad.Show();
    }

    public void LoadRewardedAd()
    {
        if (!_initialized || !_adsEnabled || _rewardedLoading || string.IsNullOrEmpty(_rewardedId))
            return;

        if (IsRewardedReady)
            return;

        _rewardedLoading = true;
        DestroyRewarded();

        RewardedAd.Load(_rewardedId, new AdRequest(), (ad, error) =>
        {
            _rewardedLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdMobProvider] Rewarded load failed: " + (error?.GetMessage() ?? "null ad"));
                ScheduleRewardedRetry();
                return;
            }

            _rewarded = ad;
            _rewarded.OnAdImpressionRecorded += () => RewardedImpression?.Invoke();
            _rewarded.OnAdFullScreenContentClosed += () =>
            {
                DestroyRewarded();
                LoadRewardedAd();
            };
            _rewarded.OnAdFullScreenContentFailed += err =>
            {
                Debug.LogWarning("[AdMobProvider] Rewarded show failed: " + err.GetMessage());
                DestroyRewarded();
                LoadRewardedAd();
            };
            Debug.Log("[AdMobProvider] Rewarded loaded");
        });
    }

    public void ShowRewardedAd(Action onReward = null, Action onClose = null)
    {
        if (!IsRewardedReady)
        {
            LoadRewardedAd();
            onClose?.Invoke();
            return;
        }

        var ad = _rewarded;
        _rewarded = null;
        ad.OnAdFullScreenContentClosed += () => onClose?.Invoke();
        ad.OnAdFullScreenContentFailed += _ => onClose?.Invoke();
        ad.Show(_ => onReward?.Invoke());
    }

    private void ConfigureTestDevices()
    {
        if (testDeviceIds == null || testDeviceIds.Length == 0)
            return;

        var ids = new List<string>();
        foreach (var id in testDeviceIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
                ids.Add(id.Trim());
        }

        if (ids.Count == 0)
            return;

        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            TestDeviceIds = ids
        });
        Debug.Log("[AdMobProvider] Test device ids configured: " + string.Join(",", ids));
    }

    private void ScheduleBannerRetry()
    {
        if (_bannerRetry == null && _adsEnabled)
            _bannerRetry = StartCoroutine(RetryRoutine(() =>
            {
                _bannerRetry = null;
                PreloadBannerAd();
            }));
    }

    private void ScheduleInterstitialRetry()
    {
        if (_interstitialRetry == null && _adsEnabled)
            _interstitialRetry = StartCoroutine(RetryRoutine(() =>
            {
                _interstitialRetry = null;
                LoadInterstitialAd();
            }));
    }

    private void ScheduleRewardedRetry()
    {
        if (_rewardedRetry == null && _adsEnabled)
            _rewardedRetry = StartCoroutine(RetryRoutine(() =>
            {
                _rewardedRetry = null;
                LoadRewardedAd();
            }));
    }

    private IEnumerator RetryRoutine(Action action)
    {
        yield return new WaitForSecondsRealtime(retryDelaySeconds);
        action?.Invoke();
    }

    private void DestroyInterstitial()
    {
        _interstitial?.Destroy();
        _interstitial = null;
        _interstitialLoading = false;
    }

    private void DestroyRewarded()
    {
        _rewarded?.Destroy();
        _rewarded = null;
        _rewardedLoading = false;
    }

    private void OnDestroy()
    {
        DestroyBanner();
        DestroyInterstitial();
        DestroyRewarded();
    }
}
