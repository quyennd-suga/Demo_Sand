using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class SuAdsEventListener : MonoBehaviour
{

    // banner 
    public static Action OnBannerShowAction;
    public static Action OnBannerHideAction;
    public static Action OnBannerLoadedAction;
    public static Action<SuAdsAdError> OnBannerFailedToLoadAction;
    public static Action<SuAdsAdValue> OnBannerPaidAction;
    public static Action OnBannerClickAction;
    public static Action OnBannerImpressionAction;
    public static Action OnBannerOpenAction;
    public static Action OnBannerCloseAction;
    // Rectangle banner 
    public static Action OnRectangleBannerShowAction;
    public static Action OnRectangleBannerHideAction;
    public static Action OnRectangleBannerLoadedAction;
    public static Action<SuAdsAdError> OnRectangleBannerFailedToLoadAction;
    public static Action<SuAdsAdValue> OnRectangleBannerPaidAction;
    public static Action OnRectangleBannerClickAction;
    public static Action OnRectangleBannerImpressionAction;
    public static Action OnRectangleBannerOpenAction;
    public static Action OnRectangleBannerCloseAction;
    // Collapsible Banner
    public static Action OnCollapsibleBannerShowAction;
    public static Action OnCollapsibleBannerHideAction;
    // inter
    public static Action OnInterstitialShowAction;
    public static Action OnInterstititalCloseAction;
    public static Action OnInterstitialLoadedAction;
    public static Action<SuAdsAdError> OnInterstitialFailedToLoadAction;
    public static Action<SuAdsAdValue> OnInterstitialPaidAction;
    public static Action OnInterstitialImpressionAction;
    public static Action OnInterstitialClickAction;
    public static Action<SuAdsAdError> OnInterstitialFailedToShowAction;
    // reward
    public static Action OnRewardVideoShowAction;
    public static Action OnRewardVideoCloseAction;
    public static Action OnRewardVideoLoadedAction;
    public static Action<SuAdsAdError> OnRewardVideoFailedToLoadAction;
    public static Action<SuAdsAdValue> OnRewardVideoPaidAction;
    public static Action OnRewardVideoImpressionAction;
    public static Action OnRewardVideoClickAction;
    public static Action OnRewardVideoRewardAction;
    public static Action<SuAdsAdError> OnRewardVideoFailedToShowAction;
    // appOpen
    public static Action OnAppOpenShowAction;
    public static Action OnAppOpenCloseAction;
    public static Action OnAppOpenLoadedAction;
    public static Action<SuAdsAdError> OnAppOpenFailedToLoadAction;
    public static Action<SuAdsAdValue> OnAppOpenPaidAction;
    public static Action OnAppOpenImpressionAction;
    public static Action OnAppOpenClickAction;
    public static Action<SuAdsAdError> OnAppOpenFailedToShowAction;

    // rewardedInterstitial
    public static Action OnRewardedInterstitialShowAction;
    public static Action OnRewardedInterstitialCloseAction;
    public static Action OnRewardedInterstitialLoadedAction;
    public static Action<SuAdsAdError> OnRewardedInterstitialFailedToLoadAction;
    public static Action<SuAdsAdValue> OnRewardedInterstitialPaidAction;
    public static Action OnRewardedInterstitialImpressionAction;
    public static Action OnRewardedInterstitialClickAction;
    public static Action OnRewardedInterstitialRewardAction;
    public static Action<SuAdsAdError> OnRewardedInterstitialFailedToShowAction;


    /*
    List<ISuAdsEventListener> Listenners;
    public static SuAdsEventListener instance;
    void Awake()
    {
        instance = this;
        Listenners = FindObjectsOfType<MonoBehaviour>().OfType<ISuAdsEventListener>().ToList();       
        Listenners.ForEach(item =>
        {
            OnBannerShowAction += item.OnBannerShow;
            OnBannerHideAction += item.OnBannerHide;
            OnBannerLoadedAction += item.OnBannerLoaded;
            OnBannerFailedToLoadAction += item.OnBannerFailedToLoad;
            OnBannerPaidAction += item.OnBannerPaid;
            OnBannerClickAction += item.OnBannerClick;
            OnBannerImpressionAction += item.OnBannerImpression;
            OnBannerOpenAction += item.OnBannerOpen;
            OnBannerCloseAction += item.OnBannerClose;
            // inter
            OnInterstitialShowAction += item.OnInterstitialShow;
            OnInterstititalCloseAction += item.OnInterstititalClose;
            OnInterstitialLoadedAction += item.OnInterstitialLoaded;
            OnInterstitialFailedToLoadAction += item.OnInterstitialFailedToLoad;
            OnInterstitialPaidAction += item.OnInterstitialPaid;
            OnInterstitialImpressionAction += item.OnInterstitialImpression;
            OnInterstitialClickAction += item.OnInterstitialClick;
            OnInterstitialFailedToShowAction += item.OnInterstitialFailedToShow;
            //
            OnRewardVideoShowAction += item.OnRewardVideoShow;
            OnRewardVideoCloseAction += item.OnRewardVideoClose;
            OnRewardVideoLoadedAction += item.OnRewardVideoLoaded;
            OnRewardVideoFailedToLoadAction += item.OnRewardVideoFailedToLoad;
            OnRewardVideoPaidAction += item.OnRewardVideoPaid;
            OnRewardVideoImpressionAction += item.OnRewardVideoImpression;
            OnRewardVideoClickAction += item.OnRewardVideoClick;
            OnRewardVideoRewardAction += item.OnRewardVideoReward;
            OnRewardVideoFailedToShowAction += item.OnRewardVideoFailedToShow;
            // appOpen
            OnAppOpenShowAction += item.OnAppOpenShow;
            OnAppOpenCloseAction += item.OnAppOpenClose;
            OnAppOpenLoadedAction += item.OnAppOpenLoaded;
            OnAppOpenFailedToLoadAction += item.OnAppOpenFailedToLoad;
            OnAppOpenPaidAction += item.OnAppOpenPaid;
            OnAppOpenImpressionAction += item.OnAppOpenImpression;
            OnAppOpenClickAction += item.OnAppOpenClick;
            OnAppOpenFailedToShowAction += item.OnAppOpenFailedToShow;
            // rewardedInterstitial
            OnRewardedInterstitialShowAction += item.OnRewardedInterstitialShow;
            OnRewardedInterstitialCloseAction += item.OnRewardedInterstitialClose;
            OnRewardedInterstitialLoadedAction += item.OnRewardedInterstitialLoaded;
            OnRewardedInterstitialFailedToLoadAction += item.OnRewardedInterstitialFailedToLoad;
            OnRewardedInterstitialPaidAction += item.OnRewardedInterstitialPaid;
            OnRewardedInterstitialImpressionAction += item.OnRewardedInterstitialImpression;
            OnRewardedInterstitialClickAction += item.OnRewardedInterstitialClick;
            OnRewardedInterstitialRewardAction += item.OnRewardedInterstitialReward;
            OnRewardedInterstitialFailedToShowAction += item.OnRewardedInterstitialFailedToShow;

        });


    }
    */
    
}
