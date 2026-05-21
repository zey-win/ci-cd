using System;
using UnityEngine;

public class AdMobProvider : MonoBehaviour
{
    public event Action InterstitialImpression;
    public event Action RewardedImpression;

    public bool BannerIsReady => false;
    public bool IsInterstitialReady => false;
    public bool IsRewardedReady => false;

    public void Init(bool enabled) { }
    public void PreloadBannerAd() { }
    public void LoadInterstitialAd() { }
    public void LoadRewardedAd() { }
    public void ShowBanner() { }
    public void HideBanner() { }
    public void DestroyBanner() { }
    public void ShowInterstitialAd(Action onClose = null) => onClose?.Invoke();
    public void ShowRewardedAd(Action onReward = null, Action onClose = null)
    {
        onReward?.Invoke();
        onClose?.Invoke();
    }

    private void KeepEventsForSerializedCompatibility()
    {
        InterstitialImpression?.Invoke();
        RewardedImpression?.Invoke();
    }
}
