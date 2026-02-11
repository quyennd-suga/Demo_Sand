//using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public abstract class BaseSuAds : MonoBehaviour
{
    public abstract void Initialize(bool isTest, Ads_Config ads_Config);

    [HideInInspector]
    public bool Inited;
    [HideInInspector]
    public ActionShowAds ActionShowAdsName;
    [HideInInspector]
    public Ads_Config ads_Config;

    public void InitAll()
    {
        StartCoroutine(InitAdsRoutine());
    }
    
    private IEnumerator InitAdsRoutine()
    {
        if (EnableBanner && !SuAds.IsRemoveAds && !SuAds.IsRemoveAds24h)
        {
            InitBanner();
        }
        yield return new WaitForSeconds(1f);
        
        if (EnableInterstitial && !SuAds.IsRemoveAds && !SuAds.IsRemoveAds24h)
        {
            InitInterstitial();
        }
        
        yield return new WaitForSeconds(1f);
        if (EnableRewardVideo)
        {
            InitRewardVideo();
        }
        if (EnableAppOpen && !SuAds.IsRemoveAds && !SuAds.IsRemoveAds24h)
        {
            InitAppOpen();
        }
        if (EnableRewardedInterstitial)
        {
            InitRewardedInterstitial();
        }
    }

    public AdsNetwork Network;
    [HideInInspector]
    public bool IsTest;
    public abstract void OnRemoteConfigFetched(Ads_Config ads_Config);
    //---------------- Banner ------------------
    [Header("* Banner ----------------------------------------------------------------------------------------")]
    //[HideInInspector]
    //public bool AtLeastBannerLoaded = false;
    public bool EnableBanner;
    [ShowIf(@"EnableBanner")]
    public AdsUnit BannerID;
    [HideInInspector]
    public bool IsBannerShowing;
    [HideInInspector]
    public bool isBannerLoaded;
    [HideInInspector]
    public bool isLoadingBanner;
    public abstract bool HaveReadyBanner { get; }
    public abstract void InitBanner();
    public abstract void LoadBanner();
    public abstract void RegisterBannerEvents();
    public abstract void ShowBanner();
    public abstract void HideBanner();
    public abstract void DestroyBanner();
    public abstract float GetBannerHeight();
    //[HideInInspector]
    //public bool NeedRequestBannerOnUserAction = false;
    //public DateTime LastTimeRequestBannerOnUserAction;
    public abstract void RequestBannerOnUserAction();

    //---------------- Banner ------------------
    [Header("* Collapsible Banner ----------------------------------------------------------------------------------------")]
    public bool EnableCollapsibleBanner;
    [ShowIf(@"EnableCollapsibleBanner")]
    public AdsUnit CollapsibleBannerID;
    [HideInInspector]
    public bool IsCollapsibleBannerShowing;
    [HideInInspector]
    public bool isCollapsibleBannerLoaded;
    [HideInInspector]
    public bool isLoadingCollapsibleBanner;
    public abstract bool HaveReadyCollapsibleBanner { get; }
    public abstract void InitCollapsibleBanner();
    public abstract void LoadCollapsibleBanner();
    public abstract void RegisterCollapsibleBannerEvents();
    public abstract void ShowCollapsibleBanner();
    public abstract void HideCollapsibleBanner();
    public abstract void DestroyCollapsibleBanner();
    //[HideInInspector]
    //public bool NeedRequestBannerOnUserAction = false;
    //public DateTime LastTimeRequestBannerOnUserAction;
    public abstract void RequestCollapsibleBannerOnUserAction();


    [Header("* Rectangle Banner ----------------------------------------------------------------------------------------")]
    //[HideInInspector]
    //public bool AtLeastBannerLoaded = false;
    public bool EnableRectangleBanner;
    [ShowIf(@"EnableRectangleBanner")]
    public AdsUnit rectangleBannerId;
    [HideInInspector]
    public bool IsRectangleBannerShowing;
    [HideInInspector]
    public bool isRectangleBannerLoaded;
    [HideInInspector]
    public bool isLoadingRectangleBanner;
    public abstract bool HaveReadyRectangleBanner { get; }
    public abstract void InitRectangleBanner();
    public abstract void LoadRectangleBanner();
    public abstract void RegisterRectangleBannerEvents();
    public abstract void ShowRectangleBanner();
    public abstract void HideRectangleBanner();
    public abstract void DestroyRectangleBanner();
    //[HideInInspector]
    //public bool NeedRequestBannerOnUserAction = false;
    //public DateTime LastTimeRequestBannerOnUserAction;
    public abstract void RequestRectangleBannerOnUserAction();

    // ------------ Interstitial --------------------
    [Space(20)]
    [Header("* Interstitial ----------------------------------------------------------------------------------------")]
    public bool EnableInterstitial;
    [HideInInspector]
    public bool isLoadingInterstitial;
    public Action OnInterstitialCloseAction, OnInterstitialFailedToShowAction, OnIntersitialOpen;
    [ShowIf(@"EnableInterstitial")]
    public AdsUnit InterstitialID;
    [HideInInspector]
    public abstract bool HaveReadyInterstitial { get; }
    //[HideInInspector]
    //public DateTime LastTimeShowInterstitial;
    [HideInInspector]
    public DateTime LastTimeInterstitialLoaded;
    public abstract void InitInterstitial();
    public abstract void LoadInterstitial();
    public abstract void RegisterInterstitialEvents();
    public abstract void ShowInterstitial(Action onClose,Action onOpenAd, ActionShowAds actionShowAdsName);
    //[HideInInspector]
    //public bool NeedRequestInterstitialOnUserAction = false;
    public abstract void RequestInterstitialOnUserAction();

    // ------------ RewardVide --------------------
    [Space(20)]
    [Header("* RewardVideo ----------------------------------------------------------------------------------------")]
    public bool EnableRewardVideo;
    //[HideInInspector]
    //public bool isLoadingRewardVideo;
    public Action OnRewardVideoCloseAction, OnRewardVideoFailedToShowAction,OnRewardVideoEarnAction;
    [ShowIf(@"EnableRewardVideo")]
    public AdsUnit RewardVideoID;
    [HideInInspector]
    public abstract bool HaveReadyRewardVideo { get; }
    //[HideInInspector]
    //public DateTime LastTimeShowRewardVideo;
    //public DateTime LastTimeRewardVideoLoaded;
    public abstract void InitRewardVideo();
    public abstract void LoadRewardVideo(int id);
    public abstract void RegisterRewardVideoEvents(int id);
    public abstract void ShowRewardVideo(Action onEarnReward, Action onClose, Action onNoAds, ActionShowAds actionShowAdsName);
    [HideInInspector]
    //public bool NeedRequestRewardVideoOnUserAction = false;
    public abstract void RequestRewardVideoOnUserAction();


    // ------------ AppOpen 
    [Space(20)]
    [Header("* AppOpen ----------------------------------------------------------------------------------------")]
    public bool EnableAppOpen;
    [HideInInspector]
    public bool isLoadingAppOpen;
    public Action OnAppOpenCloseAction, OnAppOpenFailedToShowAction;
    [ShowIf(@"EnableAppOpen")]
    public AdsUnit AppOpenID;
    [HideInInspector]
    public abstract bool HaveReadyAppOpen { get; }
    //[HideInInspector]
    //public DateTime LastTimeShowAppOpen;
    [HideInInspector]
    public DateTime LastTimeAppOpenLoaded;
    public abstract void InitAppOpen();
    public abstract void LoadAppOpen();
    public abstract void RegisterAppOpenEvents();
    public abstract void ShowAppOpen(Action onClose, Action onNoAds, ActionShowAds actionShowAdsName);
    //[HideInInspector]
    //public bool NeedRequestAppOpenOnUserAction = false;
    public abstract void RequestAppOpenOnUserAction();

    //------------ RewardedInterstitial 
    [Space(20)]
    [Header("* Rewarded Interstitial ----------------------------------------------------------------------------------------")]
    public bool EnableRewardedInterstitial;
    [HideInInspector]
    public bool isLoadingRewardedInter;
    public Action<bool> OnRewardedInterstitialCloseAction;
    public Action OnRewardedInterstitialFailedToShowAction;
    [ShowIf(@"EnableRewardedInterstitial")]
    public AdsUnit RewardedInterstitialID;
    [HideInInspector]
    public abstract bool HaveReadyRewardedInterstitial { get; }
    //[HideInInspector]
    //public DateTime LastTimeShowRewardedInterstitial;
    public DateTime LastTimeRewardedInterstitialLoaded;
    public abstract void InitRewardedInterstitial();
    public abstract void LoadRewardedInterstitial();
    public abstract void RegisterRewardedInterstitialEvents();
    public abstract void ShowRewardedInterstitial(Action<bool> onClose, Action onFailedToShow, ActionShowAds actionShowAdsName);
    //[HideInInspector]
    //public bool NeedRequestRewardedInterstitialOnUserAction = false;
    public abstract void RequestRewardedInterstitialOnUserAction();
}

public enum AdsNetwork
{
    admob,
    max,
    ironsource
}
