using System;
using UnityEngine;
using ZeyWinAds;
using ZeyWinAds.Core;

public class ZeyWinProvider : MonoBehaviour
{
    [SerializeField] private string apiKey;
    [SerializeField] private LogLevel logLevel = LogLevel.Info;
    [SerializeField] private float bannerHeightPx = 150f;

    private bool _adsEnabled = true;

    public bool IsInitialized => ZeyWinAds.ZeyWinAds.IsInitialized;
    public bool IsBannerReady => ZeyWinAds.ZeyWinAds.IsNativeReady() || ZeyWinAds.ZeyWinAds.IsBannerReady();
    public bool IsInterstitialReady => ZeyWinAds.ZeyWinAds.IsInterstitialReady();
    public bool IsRewardedReady => ZeyWinAds.ZeyWinAds.IsRewardedReady();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ZeyWinAds.ZeyWinAds.SetLogLevel(logLevel);

        if (!ZeyWinAds.ZeyWinAds.IsInitialized && !string.IsNullOrEmpty(apiKey))
            ZeyWinAds.ZeyWinAds.Initialize(apiKey);
    }

    public void Init(bool enabled)
    {
        _adsEnabled = enabled;

        if (!_adsEnabled)
        {
            SetAdsDisabled(true);
            return;
        }

        LoadBanner();
        LoadInterstitial();
        LoadRewarded();
    }

    public void SetAdsDisabled(bool disabled)
    {
        _adsEnabled = !disabled;

        if (disabled)
        {
            HideBanner();
            return;
        }

        LoadBanner();
        LoadInterstitial();
        LoadRewarded();
    }

    public void LoadBanner()
    {
        if (!_adsEnabled)
            return;

        ZeyWinAds.ZeyWinAds.LoadNative();
        ZeyWinAds.ZeyWinAds.LoadBanner();
    }

    public void LoadInterstitial()
    {
        if (_adsEnabled)
            ZeyWinAds.ZeyWinAds.LoadInterstitial();
    }

    public void LoadRewarded()
    {
        if (_adsEnabled)
            ZeyWinAds.ZeyWinAds.LoadRewarded();
    }

    public void ShowBannerBottom()
    {
        ShowBanner(BannerPosition.Bottom);
    }

    public void ShowBanner(BannerPosition position)
    {
        if (!_adsEnabled)
            return;

        ZeyWinAds.ZeyWinAds.SetBannerHeights(bannerHeightPx, bannerHeightPx);
        ZeyWinAds.ZeyWinAds.EnableBanner();

        if (ZeyWinAds.ZeyWinAds.IsNativeReady())
        {
            ZeyWinAds.ZeyWinAds.ShowNative(position);
            return;
        }

        if (ZeyWinAds.ZeyWinAds.IsBannerReady())
        {
            ZeyWinAds.ZeyWinAds.ShowBanner(position);
            return;
        }

        LoadBanner();
    }

    public void HideBanner()
    {
        ZeyWinAds.ZeyWinAds.HideNative();
        ZeyWinAds.ZeyWinAds.HideBanner();
    }

    public void HideSdkBannerInternal()
    {
        ZeyWinAds.ZeyWinAds.HideBanner();
    }

    public void ShowInterstitial(Action onClose = null)
    {
        if (!_adsEnabled)
        {
            onClose?.Invoke();
            return;
        }

        ZeyWinAds.ZeyWinAds.ShowInterstitial(onClose);
    }

    public void ShowRewarded(Action onReward = null, Action onClose = null)
    {
        if (!_adsEnabled)
        {
            onClose?.Invoke();
            return;
        }

        ZeyWinAds.ZeyWinAds.ShowRewarded(_ => onReward?.Invoke(), onClose);
    }
}
