using System;
using UnityEngine;

public class ZeyWinProvider : MonoBehaviour
{
    public bool IsInitialized => true;
    public bool IsBannerReady => false;
    public bool IsInterstitialReady => false;
    public bool IsRewardedReady => false;

    public void Init(bool enabled) { }
    public void LoadBanner() { }
    public void LoadInterstitial() { }
    public void LoadRewarded() { }
    public void ShowBannerBottom() { }
    public void HideBanner() { }
    public void HideSdkBannerInternal() { }
    public void ShowInterstitial(Action onClose = null) => onClose?.Invoke();
    public void ShowRewarded(Action onReward = null, Action onClose = null)
    {
        onReward?.Invoke();
        onClose?.Invoke();
    }
}
