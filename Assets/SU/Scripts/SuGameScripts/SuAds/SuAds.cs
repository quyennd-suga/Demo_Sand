using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Spine.Unity;
using DG.Tweening;
public class SuAds : BaseSUUnit
{
    public static SuAds instance;
    private bool IsTest;
    public GameObject groupLoading;
    [SerializeField]
    private SkeletonGraphic skeletonAnimation;
    public AnimationReferenceAsset loadingAdsAnim;
    private void OnValidate()
    {
        if (ListAdsControl == null || ListAdsControl.Count == 0)
        {
            ListAdsControl = GetComponentsInChildren<BaseSuAds>(true).ToList();
        }
        //for (int i = 0; i < ListAdsControl.Count; i++)
        //{
        //    //ListAdsControl[i].IsTest = IsTest;
        //    //ListAdsControl[i].InitOnValidate();
        //}
    }

    static SuAdsSaveData _adsSaveData;
    public static SuAdsSaveData AdsSaveData
    {
        get
        {
            return _adsSaveData;
        }
        set
        {
            _adsSaveData = value;

        }
    }
    private static bool remove_ads;
    public static bool IsRemoveAds
    {
        get
        {
            //return PlayerPrefs.GetInt("IsRemoveAds", 0) == 1;
            return remove_ads;
        }
        set
        {
            remove_ads = value;
            DataManager.data.isRemoveAds = value;
        }
    }
    private static bool remove_ads_24h;
    public static bool IsRemoveAds24h
    {
        get
        {
            return remove_ads_24h;
        }
        set
        {
            remove_ads_24h = value;
            DataManager.data.isRemoveAds24h = value;
        }
    }
    [DisableInPlayMode]
    [DisableInEditorMode]
    public bool LockAppOpenAds;
    List<AdsNetwork> Networks;
    //public Open_Ad_Config openAdData;
    //public Inter_Ad_Config interData;
    public Ads_Network_Config NetworkData;
    //public Rewarded_Inter_Ad_Config rewardedInterData;
    public static Ads_Config ads_Config;
    public static DateTime LastTimeShowInterstitial;

    public List<BaseSuAds> ListAdsControl;

    private void Awake()
    {
        string saveDataString = PlayerPrefs.GetString("AdsSaveData");
        if (!string.IsNullOrEmpty(saveDataString))
        {
            AdsSaveData = JsonUtility.FromJson<SuAdsSaveData>(saveDataString);
        }
        else
        {
            AdsSaveData = new SuAdsSaveData()
            {
                AppOpenCount = 0,
                AppOpenRevenue = 0,
                BannerCount = 0,
                BannerRevenue = 0,
                InterstitialCount = 0,
                InterstitialRevenue = 0,
                RewardedVideoCount = 0,
                RewardedVideoRevenue = 0,
                RewardedInterstitialCount = 0,
                RewardedInterstitialRevenue = 0,
                AdsShowed5 = 0,
                AdsShowed6 = 0,
                AdsShowed8 = 0,
                AdsShowed9 = 0,
                AdsShowed10 = 0,
                AdsShowed20 = 0,
                AdsShowedCount5 = 0,
                AdsShowedCount6 = 0,
                AdsShowedCount8 = 0,
                AdsShowedCount9 = 0,
                AdsShowedCount10 = 0,
                AdsShowedCount20 = 0
            };
        }
        ListAdsControl = GetComponentsInChildren<BaseSuAds>(true).ToList();
        SuRemoteConfig.OnFetchComplete += OnFetchComplete;
        
    }

    private bool fetchSuccess;
    private void OnFetchComplete(bool success)
    {
        Networks = new List<AdsNetwork>();
        NetworkData = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Ads_Network_Config>(RemoteConfigName.ads_network_config);
        if (NetworkData.ads_networks == null || NetworkData.ads_networks.Count == 0)
        {
            Networks.Add(AdsNetwork.admob);
        }
        else
        {
            for (int i = 0; i < NetworkData.ads_networks.Count; i++)
            {
                string nw = NetworkData.ads_networks[i].ToLower();
                switch (nw)
                {
                    case "admob":
                        Networks.Add(AdsNetwork.admob);
                        break;
                    case "max":
                        Networks.Add(AdsNetwork.max);
                        break;
                    case "ironsource":
                        Networks.Add(AdsNetwork.ironsource);
                        break;
                }
            }
        }
        // sắp xếp lại list ads control 
        if(success)
        {
            ads_Config = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Ads_Config>(RemoteConfigName.ads_config);
            
        }
        List<BaseSuAds> ListAdsControlTemp = new List<BaseSuAds>();
        for (int i = 0; i < Networks.Count; i++)
        {
            BaseSuAds adControl = ListAdsControl.Find(x => x.Network == Networks[i]);
            if (adControl != null)
            {
                ListAdsControlTemp.Add(adControl);
                if(success)
                {
                    adControl.OnRemoteConfigFetched(ads_Config);
                }
                adControl.gameObject.SetActive(true);
            }
        }
        ListAdsControl = ListAdsControlTemp;

        fetchSuccess = true;
        //openAdData = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Open_Ad_Config>(RemoteConfigName.open_ad_config);
        //interData = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Inter_Ad_Config>(RemoteConfigName.inter_ad_config);
    }

    public bool HaveReadyInterstitial
    {
        get
        {
            /*
            if ((DateTime.Now - lastTimeInterstitialLoaded).TotalHours >= 1)
            {
                return false;
            }
            */
            for (int i = 0; i < ListAdsControl.Count; i++)
            {
                if (ListAdsControl[i].HaveReadyInterstitial)
                    return true;
            }
            return false;
        }
    }

    public bool HaveReadyRewardVideo
    {
        get
        {
            /*
            if ((DateTime.Now - lastTimeVideoLoaded).TotalHours >= 1)
            {
                return false;
            }
            */
            int capping_time = ads_Config.rewarded.capping_time;
            if ((DateTime.Now - LastTimeShowRewardVideo).TotalSeconds < capping_time)
            {
                return false;
            }
            for (int i = 0; i < ListAdsControl.Count; i++)
            {
                if (ListAdsControl[i].HaveReadyRewardVideo)
                    return true;
            }
            return false;
        }
    }

    public bool HaveReadyRewardedInterstitial
    {
        get
        {
            for (int i = 0; i < ListAdsControl.Count; i++)
            {
                if (ListAdsControl[i].HaveReadyRewardedInterstitial)
                    return true;
            }
            return false;
        }
    }

    public bool HaveReadyAppOpen
    {
        get
        {
            for (int i = 0; i < ListAdsControl.Count; i++)
            {
                if (ListAdsControl[i].HaveReadyAppOpen)
                    return true;
            }
            return false;
        }
    }


    public void HideBanner()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].HideBanner();
        }
    }

    public void ShowBanner()
    {
        if(IsRemoveAds || IsRemoveAds24h)
        {
            return;
        }
        if (ListAdsControl.Count > 0)
        {
            if (ListAdsControl[0] != null)
            {
                ListAdsControl[0].ShowBanner();
            }
            else
            {
                LogManager.Log("không có banner nào để show");
            }
        }
    }
    public float GetBannerHeight()
    {
        return ListAdsControl[0].GetBannerHeight();
    }
    public void HideCollapsibleBanner()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].HideCollapsibleBanner();
        }
    }
    public void ShowCollapsibleBanner()
    {
        
        if (ListAdsControl.Count > 0)
        {
            if (ListAdsControl[0] != null && ListAdsControl[0].HaveReadyCollapsibleBanner)
            {
                ListAdsControl[0].ShowCollapsibleBanner();
            }
            else
            {
                LogManager.Log("không có Collapsible banner nào để show");
            }
        }
    }
    public void DestroyBanner()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].DestroyBanner();
            ListAdsControl[i].DestroyCollapsibleBanner();
            ListAdsControl[i].DestroyRectangleBanner();
        }
    }
    public void ShowBigBanner()
    {
        if (fetchSuccess == false)
            return;
        if (IsRemoveAds || IsRemoveAds24h)
            return;
        int level = ads_Config.banner.level_show;
        int currentLevel = SuLevelManager.CurrentLevel;
        if (currentLevel < level)
            return;
        if (ads_Config.banner.enable_collapsible)
        {
            ShowCollapsibleBanner();
        }
        else
        {
            ShowRectangleBanner();
        }
    }
    public void HideBigBanner()
    {
        if (fetchSuccess == false)
            return;
        if (ads_Config.banner.enable_collapsible)
        {
            HideCollapsibleBanner();
        }
        else
        {
            HideRectangleBanner();
        }
    }
    public void RequestBigBannerOnUserAction()
    {
        if (fetchSuccess == false)
            return;
        if (IsRemoveAds || IsRemoveAds24h)
            return;
        if (ads_Config.banner.enable_collapsible)
        {
            RequestCollapsibleBannerOnUserAction();
        }
        else
        {
            RequestRectanbleBannerOnUserAction();
        }
    }
    public void RequestCollapsibleBannerOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestCollapsibleBannerOnUserAction();
        }
    }
    public void HideRectangleBanner()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].HideRectangleBanner();
        }
    }
    public void ShowRectangleBanner()
    {
        if (ListAdsControl.Count > 0)
        {
            if (ListAdsControl[0] != null && ListAdsControl[0].HaveReadyRectangleBanner)
            {
                ListAdsControl[0].ShowRectangleBanner();
            }
            else
            {
                LogManager.Log("không có Rectangle banner nào để show");
            }
        }
    }

    public void RequestAdsOnUserAction()
    {
        RequestBannerOnUserAction();
        RequestInterstitialOnUserAction();
        RequestBigBannerOnUserAction();
        RequestRewardVideoOnUserAction();
        RequestRewardedInterstitialOnUserAction();
        if (ads_Config != null && ads_Config.open_ads.on == true)
        {
            RequestAppOpenOnUserAction();
        }
    }

    public void RequestRectanbleBannerOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestRectangleBannerOnUserAction();
        }
    }

    public void RequestAppOpenOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestAppOpenOnUserAction();
        }
    }

    public void RequestBannerOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestBannerOnUserAction();
        }
    }


    public void RequestInterstitialOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestInterstitialOnUserAction();
        }
    }

    public void RequestRewardedInterstitialOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestRewardedInterstitialOnUserAction();
        }
    }

    public void RequestRewardVideoOnUserAction()
    {
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].RequestRewardVideoOnUserAction();
        }
    }

    public void ShowAppOpen(Action onClose, Action onNoAds, ActionShowAds actionShowAdsName)
    {
        if(IsRemoveAds || IsRemoveAds24h)
        {
            onClose?.Invoke();
            return;
        }
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            BaseSuAds adControl = ListAdsControl[i];
            if (adControl.HaveReadyAppOpen)
            {
                adControl.ShowAppOpen(onClose, onNoAds, actionShowAdsName);
                return;
            }
        }
        RequestAppOpenOnUserAction();
        onNoAds?.Invoke();
    }

    
    public void ShowInterstitial(Action onClose, ActionShowAds actionShowAdsName = ActionShowAds.Level_Interstitial)
    {
        if(IsRemoveAds || IsRemoveAds24h)
        {
            //Debug.Log("remove ads already");
            onClose?.Invoke();
            return;
        }
        int currentLevel = SuLevelManager.CurrentLevel;
        int level_start = ads_Config.interstitial.level_start;
        
        if(currentLevel < level_start)
        {
            //Debug.Log($"current level < {level_start}, không show interstitial");
            onClose?.Invoke();
            return;
        }
        int level_end = ads_Config.interstitial.level_end;
        if(level_end > 5)
        {
            if (currentLevel > level_end)
            {
                //Debug.Log("reach level_end");
                onClose?.Invoke();
                return;
            }
        }
        else
        {
            //Debug.Log("level_end is not set");
        }
        int capping_time = ads_Config.interstitial.capping_time;
        if ((DateTime.Now - LastTimeShowInterstitial).TotalSeconds < capping_time)
        {
            //Debug.Log(" không show inter vì lần show gần nhất đến giờ chưa đủ capping time"); 
            onClose?.Invoke();
            return;
        }

        //LogManager.Log("Show interstitial , tìm kiếm adControl có inter");
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            BaseSuAds adControl = ListAdsControl[i];
            if (adControl.HaveReadyInterstitial)
            {
                //adControl.ShowInterstitial(onClose, actionShowAdsName);
                StartCoroutine(ShowInterstitialCrt(adControl, onClose, actionShowAdsName));
                return;
            }
        }
        // nếu không adcontrol nào có interstitial thì onclose       
        RequestInterstitialOnUserAction();
        onClose?.Invoke();
    }

    IEnumerator ShowInterstitialCrt(BaseSuAds adControl, Action onClose, ActionShowAds actionShowAdsName)
    {
        groupLoading.SetActive(true);
        skeletonAnimation.AnimationState.SetAnimation(0, loadingAdsAnim, false);
        yield return new WaitForSecondsRealtime(1.35F);
        //LastTimeShowInterstitial = DateTime.Now;
        adControl.ShowInterstitial(() =>
        {
            groupLoading.SetActive(false);
            onClose?.Invoke();
        }, () =>
        {
            groupLoading.SetActive(false);
        }, actionShowAdsName);
    }


    public static DateTime LastTimeShowRewardedInterstitial;
    public void ShowRewardedInterstitial(Action<bool> onClose, Action onFailedToShow, ActionShowAds actionShowAdsName)
    {
        //if(IsRemoveAds)
        //{
        //    onClose?.Invoke(false);
        //    return;
        //}
        int currentLevel = SuLevelManager.CurrentLevel;
        int level = ads_Config.rewarded_inter.level_start;
        if (currentLevel < level)
        {
            onClose?.Invoke(false);
            return;
        }
        int capping_time = ads_Config.rewarded.capping_time;
        if ((DateTime.Now - LastTimeShowRewardedInterstitial).TotalSeconds < capping_time)
        {
            // không show rewarded inter vì lần show gần nhất đến giờ chưa đủ capping time 
            onClose?.Invoke(false);
            return;
        }

        
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            BaseSuAds adControl = ListAdsControl[i];
            if (adControl.HaveReadyRewardedInterstitial)
            {
                //adControl.ShowInterstitial(onClose, actionShowAdsName);
                StartCoroutine(ShowRewardedInterstitialCrt(adControl, onClose , onFailedToShow, actionShowAdsName));
                return;
            }
        }
        // nếu không adcontrol nào có interstitial thì onclose
        //LogManager.Log("Không có adcontrol nào có rewarded interstitial");
        RequestRewardedInterstitialOnUserAction();
        onClose?.Invoke(false);
    }

    IEnumerator ShowRewardedInterstitialCrt(BaseSuAds adControl, Action<bool> onClose, Action onFailedToShow, ActionShowAds actionShowAdsName)
    {
        groupLoading.SetActive(true);
        skeletonAnimation.AnimationState.SetAnimation(0, loadingAdsAnim, false);
        yield return new WaitForSecondsRealtime(1.35F);
        LastTimeShowRewardedInterstitial = DateTime.Now;
        adControl.ShowRewardedInterstitial((canReward) =>
        {
            groupLoading.SetActive(false);
            onClose?.Invoke(canReward);
        }, () =>
        {
            groupLoading.SetActive(false);
            onFailedToShow?.Invoke();
        }, actionShowAdsName);
        groupLoading.SetActive(false);
    }



    public static DateTime LastTimeShowRewardVideo;
    public void ShowRewardVideo(Action OnEarnReward,Action onClose, Action onNoAds = null, ActionShowAds actionShowAdsName = ActionShowAds.Reward)
    {
        int currentLevel = SuLevelManager.CurrentLevel;
        int levelStart = ads_Config.rewarded.level_start;
        if (currentLevel < levelStart)
        {
            onNoAds?.Invoke();
            return;
        }
        int capping_time = ads_Config.rewarded.capping_time;
        if((DateTime.Now - LastTimeShowRewardVideo).TotalSeconds < capping_time)
        {
            onNoAds?.Invoke();
            return;
        }
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            BaseSuAds adControl = ListAdsControl[i];
            if (adControl.HaveReadyRewardVideo)
            {
                //adControl.ShowRewardVideo(onClose, onNoAds, actionShowAdsName);
                ShowRewardVideoCrt(adControl, OnEarnReward, onClose, onNoAds, actionShowAdsName);
                //StartCoroutine(ShowRewardVideoCrt(adControl, OnEarnReward, onClose, onNoAds, actionShowAdsName));
                return;
            }
        }
        //LogManager.Log("Không mạng nào có video");
        RequestRewardVideoOnUserAction();
        onNoAds?.Invoke();
    }

    void ShowRewardVideoCrt(BaseSuAds adControl,Action onEarnReward, Action onClose, Action onNoAds, ActionShowAds actionShowAdsName)
    {
        //groupLoading.SetActive(true);
        //yield return new WaitForSecondsRealtime(1.35F);
        adControl.ShowRewardVideo(() =>
        {
            //groupLoading.SetActive(false);
            onEarnReward?.Invoke();
        }, () =>
        {
            //groupLoading.SetActive(false);
            onClose?.Invoke();
        }, () =>
        {
            //groupLoading.SetActive(false);
            onNoAds?.Invoke();
        }, actionShowAdsName);
    }


    public override void Init(bool isTestMode)
    {
        IsTest = isTestMode;
        ads_Config = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Ads_Config>(RemoteConfigName.ads_config);
        for (int i = 0; i < ListAdsControl.Count; i++)
        {
            ListAdsControl[i].Initialize(IsTest,ads_Config);
        }
        if (IsTest)
        {
            Firebase.Analytics.FirebaseAnalytics.SetUserProperty("TestUsers", "true");
        }
        
    }


    // app open 
    DateTime TimeStartPause;
    public static DateTime LastTimeShowAppOpenAd;
    private void OnApplicationFocus(bool focus)
    {
        
        switch (focus)
        {
            case false:
                TimeStartPause = DateTime.Now;
                PlayerPrefs.SetString("AdsSaveData", JsonUtility.ToJson(AdsSaveData));
                PlayerPrefs.Save();
                //LogManager.Log("App vào background");
                break;
            case true:
                //LogManager.Log("APp vào forege ground");
                
                if (LockAppOpenAds == false && HaveReadyAppOpen)
                {
                    int currentLevel = SuLevelManager.CurrentLevel;
                    int level = ads_Config.open_ads.start_level;
                    if(currentLevel >= level)
                    {
                        if (ads_Config.open_ads.on && (DateTime.Now - TimeStartPause).TotalSeconds >= ads_Config.open_ads.time_in_background && (DateTime.Now - LastTimeShowAppOpenAd).TotalSeconds >= ads_Config.open_ads.capping_time)
                        {
                            StartCoroutine(ShowAppOpenAdsWhenFocus());
                        }
                    }
                }
                else if (LockAppOpenAds)
                {
                    LockAppOpenAds = false;
                }
                break;
        }
    }

    IEnumerator ShowAppOpenAdsWhenFocus()
    {
        Debug.Log("Show App Open Ads");
        yield return new WaitForSecondsRealtime(0.1F);

        groupLoading.SetActive(true);
        skeletonAnimation.AnimationState.SetAnimation(0, loadingAdsAnim, false);
        yield return new WaitForSecondsRealtime(1.35F);

        ShowAppOpen(() =>
        {
            groupLoading.SetActive(false);
        }, () =>
        {
            groupLoading.SetActive(false);
        }, ActionShowAds.BackToGame);

    }


}
