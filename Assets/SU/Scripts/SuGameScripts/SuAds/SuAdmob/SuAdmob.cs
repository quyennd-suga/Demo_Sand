using GoogleMobileAds;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using GoogleMobileAds.Api.Mediation.IronSource;
//using GoogleMobileAds.Api.Mediation.UnityAds;

public class SuAdmob : BaseSuAds
{

    public override void Initialize(bool test, Ads_Config config)
    {
        IsTest = test;
        ads_Config = config;
        ConfigAdsId();
        if (IsTest)
        {
            BannerID.IsTest = IsTest;
            InterstitialID.IsTest = IsTest;
            RewardVideoID.IsTest = IsTest;
            AppOpenID.IsTest = IsTest;
            CollapsibleBannerID.IsTest = IsTest;
            rectangleBannerId.IsTest = IsTest;
            RewardedInterstitialID.IsTest = IsTest;
            BannerID.Android_Test = "ca-app-pub-3940256099942544/6300978111";
            BannerID.IOS_Test = "ca-app-pub-3940256099942544/2934735716";
            rectangleBannerId.Android_Test = "ca-app-pub-3940256099942544/6300978111";
            rectangleBannerId.IOS_Test = "ca-app-pub-3940256099942544/2934735716";
            CollapsibleBannerID.Android_Test = "ca-app-pub-3940256099942544/2014213617";
            CollapsibleBannerID.IOS_Test = "ca-app-pub-3940256099942544/8388050270";
            RewardVideoID.Android_Test = "ca-app-pub-3940256099942544/5224354917";
            RewardVideoID.IOS_Test = "ca-app-pub-3940256099942544/1712485313";
            InterstitialID.Android_Test = "ca-app-pub-3940256099942544/1033173712";
            InterstitialID.IOS_Test = "ca-app-pub-3940256099942544/4411468910";
            AppOpenID.Android_Test = "ca-app-pub-3940256099942544/9257395921";
            AppOpenID.IOS_Test = "ca-app-pub-3940256099942544/5662855259";
            RewardedInterstitialID.Android_Test = "ca-app-pub-3940256099942544/5354046379";
            RewardedInterstitialID.IOS_Test = "ca-app-pub-3940256099942544/6978759866";
        }
        _rewardVideos = new RewardedUnit[1];
        for (int i = 0; i < _rewardVideos.Length; i++)
        {
            _rewardVideos[i] = new RewardedUnit();
        }
        //RequestConsent();
        canRequestAds = true;
        StartCoroutine(WaitForConsentUpdate());
        
    }
    private void ConfigAdsId()
    {
        LogManager.Log("Config ads Ids");

        string banner_id = ads_Config.banner.banner_id;
        
        if (!string.IsNullOrEmpty(banner_id))
        {
            BannerID.SetAdId(banner_id);
        }

        string rectangle_banner_ad_unit = ads_Config.banner.bigbanner_id;
        if (!string.IsNullOrEmpty(rectangle_banner_ad_unit))
        {
            rectangleBannerId.SetAdId(rectangle_banner_ad_unit);
        }
        string collapsible_banner_ad_unit = ads_Config.banner.collapsiblebanner_id;
        if (!string.IsNullOrEmpty(collapsible_banner_ad_unit))
        {
            CollapsibleBannerID.SetAdId(collapsible_banner_ad_unit);
        }

        string inter_ad_unit = ads_Config.interstitial.main_ads_id;

        if (!string.IsNullOrEmpty(inter_ad_unit))
        {
            InterstitialID.SetAdId(inter_ad_unit);
        }

        string reward_ad_unit = ads_Config.rewarded.rewarded_id;

        if (!string.IsNullOrEmpty(reward_ad_unit))
        {
            RewardVideoID.SetAdId(reward_ad_unit);
        }

        string appOpen_ad_unit = ads_Config.open_ads.openads_id;
        if (!string.IsNullOrEmpty(appOpen_ad_unit))
        {
            AppOpenID.SetAdId(appOpen_ad_unit);
        }

        string rewardInter_id = ads_Config.rewarded_inter.rewarded_inter_id;
        if (!string.IsNullOrEmpty(rewardInter_id))
        {
            RewardedInterstitialID.SetAdId(rewardInter_id);
        }

        #region THAY ID QUẢNG CÁO KHI ĐẠT LEVEL NHẤT ĐỊNH
        if (ads_Config.interstitial.on)
        {
            int currentLevel = SuLevelManager.CurrentLevel;
            int level_change_ids = ads_Config.interstitial.level_change_ads_id;
            if (currentLevel < level_change_ids)
            {
                string pre_inter_id = ads_Config.interstitial.sub_ads_id;
                if (!string.IsNullOrEmpty(pre_inter_id))
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        InterstitialID.Android = pre_inter_id;
                    }
                    else if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        InterstitialID.IOS = pre_inter_id;
                    }
                }
            }
        }
        #endregion
    }
    private void RequestConsent()
    {
        //SuRemoteConfig.OnFetchComplete += OnRemoteConfigFetched;
        Inited = false;

        var debugSettings = new ConsentDebugSettings
        {
            // Geography appears as in EEA for debug devices.
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = new List<string>()
                        {
                     // máy test bên vid
                     "BDCB669F91ADC20E4458B407B04BBC70",
                    "F19951C4862CADADA35417145B00AE98"
                        }
        };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,

#if UNITY_EDITOR
            // chỉ trên editor mới chạy debug
            ConsentDebugSettings = debugSettings,

#endif
        };
        // Check the current consent information status.
//#if !UNITY_EDITOR
        ConsentInformation.Update(request, OnConsentInfoUpdated);
        canRequestAds = ConsentInformation.CanRequestAds();
//#else
//        canRequestAds = true;
//#endif
    }



    private bool canRequestAds;

    IEnumerator WaitForConsentUpdate()
    {
        while (canRequestAds == false)
        {
            yield return null;
        }
        GoogleMobileAds.Mediation.UnityAds.Api.UnityAds.SetConsentMetaData("gdpr.consent", true);
        //GoogleMobileAds.Mediation.IronSource.Api.IronSource.SetConsent(true);
        //GoogleMobileAds.Mediation.Pangle.Api.Pangle.SetGDPRConsent(0);
        InitAds();

    }

    void InitAds()
    {
        MobileAds.Initialize((initStatus) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (IsTest)
                {
                    string response = JsonUtility.ToJson(MobileAds.GetRequestConfiguration());
                    LogManager.Log("Response là " + response);
                }
                Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();

                foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
                {
                    string className = keyValuePair.Key;
                    AdapterStatus status = keyValuePair.Value;

                    switch (status.InitializationState)
                    {
                        case AdapterState.NotReady:
                            // The adapter initialization did not complete.
                            LogManager.Log("Adapter: " + className + " not ready.");
                            break;
                        case AdapterState.Ready:
                            // The adapter was successfully initialized.
                            LogManager.Log("Adapter: " + className + " is initialized.");
                            break;
                    }
                }
                Inited = true;
                InitAll();

            });
        });

    }

    public override void OnRemoteConfigFetched(Ads_Config config)
    {
        LogManager.Log("Fetch Complete and config ads ids");
        ads_Config = config;
        string banner_id = ads_Config.banner.banner_id;
        if (!string.IsNullOrEmpty(banner_id))
        {
            BannerID.SetAdId(banner_id);
        }

        string rectangle_banner_ad_unit = ads_Config.banner.bigbanner_id;

        if (!string.IsNullOrEmpty(rectangle_banner_ad_unit))
        {
            rectangleBannerId.SetAdId(rectangle_banner_ad_unit);
        }
        string collapsible_banner_ad_unit = ads_Config.banner.collapsiblebanner_id;
        if (!string.IsNullOrEmpty(collapsible_banner_ad_unit))
        {
            CollapsibleBannerID.SetAdId(collapsible_banner_ad_unit);
        }
        if ((EnableRectangleBanner || EnableCollapsibleBanner) && !SuAds.IsRemoveAds && !SuAds.IsRemoveAds24h)
        {
            if (ads_Config.banner.enable_collapsible)
            {
                EnableCollapsibleBanner = true;
                EnableRectangleBanner = false;
                InitCollapsibleBanner();
            }
            else
            {
                EnableRectangleBanner = true;
                EnableCollapsibleBanner = false;
                InitRectangleBanner();
            }
        }


        string inter_ad_unit = ads_Config.interstitial.main_ads_id;

        if (!string.IsNullOrEmpty(inter_ad_unit))
        {
            InterstitialID.SetAdId(inter_ad_unit);
        }

        string reward_ad_unit = ads_Config.rewarded.rewarded_id;

        if (!string.IsNullOrEmpty(reward_ad_unit))
        {
            RewardVideoID.SetAdId(reward_ad_unit);
        }

        string appOpen_ad_unit = ads_Config.open_ads.openads_id;
        if (!string.IsNullOrEmpty(appOpen_ad_unit))
        {
            AppOpenID.SetAdId(appOpen_ad_unit);
        }
        string rewardInter_id = ads_Config.rewarded_inter.rewarded_inter_id;
        if (!string.IsNullOrEmpty(rewardInter_id))
        {
            RewardedInterstitialID.SetAdId(rewardInter_id);
        }

        #region THAY ID QUẢNG CÁO KHI ĐẠT LEVEL NHẤT ĐỊNH
        if (ads_Config.interstitial.on)
        {
            int currentLevel = SuLevelManager.CurrentLevel;
            int level_change_ids = ads_Config.interstitial.level_change_ads_id;
            if (currentLevel < level_change_ids)
            {
                string pre_inter_id = ads_Config.interstitial.sub_ads_id;
                if (!string.IsNullOrEmpty(pre_inter_id))
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        InterstitialID.Android = pre_inter_id;
                    }
                    else if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        InterstitialID.IOS = pre_inter_id;
                    }
                }
            }
        }
        #endregion
    }

    #region User_Consent
    //---------- Consent
    void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            // Handle the error.
            canRequestAds = ConsentInformation.CanRequestAds();
            LogManager.Log(error.Message);
            return;
        }

        //If the error is null, the consent information state was updated.
        // You are now ready to check if a form is available.
        //Debug.Log(ConsentInformation.ConsentStatus);
        LogManager.Log("Can request ads: " + ConsentInformation.CanRequestAds());

        canRequestAds = ConsentInformation.CanRequestAds();
        //if(ConsentInformation.ConsentStatus == ConsentStatus.Required || ConsentInformation.ConsentStatus == ConsentStatus.Unknown)
        ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
        {
            //UpdatePrivacyButton();
            
            if (showError != null)
            {
                LogManager.Log(showError.Message);
                return;
            }
            canRequestAds = ConsentInformation.CanRequestAds(); 
            CheckConsentStatus();
        });

    }

    private readonly string CONSENT_STATUS_STRING = "IABTCF_PurposeConsents";
    
    private void CheckConsentStatus()
    {
        string purposeConsents = ApplicationPreferences.GetString(CONSENT_STATUS_STRING);
        LogManager.Log("Purpose Consents: " + purposeConsents);

        
        SuGame.Get<SuAnalytics>().CheckConsentStatus(purposeConsents);
    }    
    
    private ConsentForm _consentForm;

    void LoadConsentForm()
    {
        // Loads a consent form.
        ConsentForm.Load(OnLoadConsentForm);
    }

    void OnLoadConsentForm(ConsentForm consentForm, FormError error)
    {
        if (error != null)
        {
            // Handle the error.
            UnityEngine.Debug.LogError(error);
            return;
        }

        // The consent form was loaded.
        // Save the consent form for future requests.
        _consentForm = consentForm;
        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            _consentForm.Show(OnShowForm);
        }
        // You are now ready to show the form.
    }

    void OnShowForm(FormError error)
    {
        if (error != null)
        {
            // Handle the error.
            UnityEngine.Debug.LogError(error);
            return;
        }

        // Handle dismissal by reloading form.
        LoadConsentForm();
    }
    #endregion

    #region Collapsible Banner:
    BannerView collapsibleBanner;

    public override void InitCollapsibleBanner()
    {
        CreateCollapsibleBannerView();
        LoadCollapsibleBanner();
        //HideCollapsibleBanner();
    }
    public override void DestroyCollapsibleBanner()
    {
        if (collapsibleBanner != null)
        {
            collapsibleBanner.Destroy();
        }
    }
    public override bool HaveReadyCollapsibleBanner
    {
        get
        {
            return collapsibleBanner != null;
        }
    }
    void CreateCollapsibleBannerView()
    {
        string id = CollapsibleBannerID.ID;
        if (collapsibleBanner != null)
        {
            collapsibleBanner.Destroy();
        }

        AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        collapsibleBanner = new BannerView(id, adaptiveSize, AdPosition.Bottom);
        RegisterCollapsibleBannerEvents();
    }
    public override void LoadCollapsibleBanner()
    {
        if (!Inited)
        {
            // không tải quảng cáo khi user chưa đồng ý consent 
            return;
        }
        if (isCollapsibleBannerLoaded)
        {
            // không load banner nếu đã từng load được
            // vì banner bật auto refresh
            return;
        }
        if (!EnableCollapsibleBanner)
        {
            return;
        }
        if (SuAds.IsRemoveAds || SuAds.IsRemoveAds24h)
        {
            return;
        }
        if (isLoadingCollapsibleBanner)
        {
            return;
        }
        if (collapsibleBanner == null)
        {
            return;
        }
        isLoadingCollapsibleBanner = true;
        var adRequest = new AdRequest();
        adRequest.Extras.Add("collapsible", "bottom");
        collapsibleBanner.LoadAd(adRequest);
        collapsibleBanner.Hide();
    }

    public override void RegisterCollapsibleBannerEvents()
    {
        collapsibleBanner.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (isCollapsibleBannerLoaded == false)
                {
                    isCollapsibleBannerLoaded = true;
                }
                isLoadingCollapsibleBanner = false;
                SuAdsEventListener.OnBannerLoadedAction?.Invoke();
            });
        };

        collapsibleBanner.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoadingCollapsibleBanner = false;
                if (IsTest)
                {
                    SuAdsEventListener.OnBannerFailedToLoadAction?.Invoke(new SuAdsAdError()
                    {
                        errorInfo = JsonUtility.ToJson(error)
                    });
                }
            });
        };


        collapsibleBanner.OnAdPaid += (AdValue adValue) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                string mediationClassName = "Unknow";
                string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
                if (collapsibleBanner != null)
                {
                    ResponseInfo resInfo = collapsibleBanner.GetResponseInfo();
                    mediationClassName = resInfo == null ? "Unknow" : resInfo.GetMediationAdapterClassName();
                    // update                     
                    if (resInfo != null)
                    {
                        Dictionary<string, string> extras = resInfo.GetResponseExtras();
                        if (extras != null)
                        {
                            GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                        }
                        //AdapterResponseInfo adapter = resInfo.GetLoadedAdapterResponseInfo();
                        adSourceName = resInfo.GetLoadedAdapterResponseInfo()?.AdSourceName ?? "Unknown";
                    }
                }

                // gọi action onPaid
                SuAdsAdValue _adValue = new SuAdsAdValue()
                {
                    Network = mediationClassName,
                    adSource = adSourceName,
                    Valuemicros = adValue.Value,
                    Value = adValue.Value / 1000000F,
                    Precision = adValue.Precision.ToString(),
                    CurrencyCode = adValue.CurrencyCode,
                    actionShowAds = ActionShowAds.Banner_Impression,
                    Ad_Format = "banner",
                    Mediation_Platform = AdsNetwork.admob,
                    UnitID = CollapsibleBannerID.ID,
                    mediationABTestName = mediationABTestName,
                    mediationABTestVariant = mediationABTestVariant,
                    mediationGroupName = mediationGroupName
                };
                SuAdsEventListener.OnBannerPaidAction?.Invoke(_adValue);

            });
        };

        collapsibleBanner.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action Impression
                SuAdsEventListener.OnBannerImpressionAction?.Invoke();
            });
        };

        collapsibleBanner.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action on Ad Click
                SuAdsEventListener.OnBannerClickAction?.Invoke();
            });
        };

        collapsibleBanner.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action on aD open
                SuAdsEventListener.OnBannerOpenAction?.Invoke();
            });
        };

        collapsibleBanner.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action onAD Close
                SuAdsEventListener.OnBannerCloseAction?.Invoke();
            });
        };
    }
    public override void HideCollapsibleBanner()
    {
        if (collapsibleBanner != null)
        {
            collapsibleBanner.Hide();
            IsCollapsibleBannerShowing = false;
            SuAdsEventListener.OnCollapsibleBannerHideAction?.Invoke();
        }
    }
    public override void ShowCollapsibleBanner()
    {
        if (collapsibleBanner != null)
        {
            collapsibleBanner.Show();
            IsCollapsibleBannerShowing = true;
            SuAdsEventListener.OnCollapsibleBannerShowAction?.Invoke();
        }
    }

    public override void RequestCollapsibleBannerOnUserAction()
    {

        if (!SuAds.IsRemoveAds && isCollapsibleBannerLoaded == false && !SuAds.IsRemoveAds24h)
        {
            LogManager.Log("Request Collapsible banner On User Action");
            //LastTimeRequestBannerOnUserAction = DateTime.Now;
            LoadCollapsibleBanner();
        }
    }
    #endregion

    #region Rectangle_Banner
    // --------- Rectangle Banner --------------------------------
    BannerView _rectangleBannerView;
    public override void InitRectangleBanner()
    {
        CreateRectangleBannerView();
        LoadRectangleBanner();
    }
    public override void DestroyRectangleBanner()
    {
        if (_rectangleBannerView != null)
        {
            _rectangleBannerView.Destroy();
        }
    }
    public override bool HaveReadyRectangleBanner
    {
        get
        {
            return _rectangleBannerView != null;
        }
    }
    void CreateRectangleBannerView()
    {
        string id = rectangleBannerId.ID;
        if (_rectangleBannerView != null)
        {
            _rectangleBannerView.Destroy();
        }


        _rectangleBannerView = new BannerView(id, AdSize.MediumRectangle, AdPosition.Bottom);
        RegisterRectangleBannerEvents();
    }
    public override void LoadRectangleBanner()
    {
        if (!Inited)
        {
            // không tải quảng cáo khi user chưa đồng ý consent 
            return;
        }
        if (isRectangleBannerLoaded)
        {
            // không load banner nếu đã từng load được
            // vì banner bật auto refresh
            return;
        }
        if (!EnableRectangleBanner)
        {
            return;
        }
        if (SuAds.IsRemoveAds || SuAds.IsRemoveAds24h)
        {
            return;
        }
        if (isLoadingRectangleBanner)
        {
            return;
        }
        if (_rectangleBannerView == null)
        {
            return;
        }
        isLoadingRectangleBanner = true;
        var adRequest = new AdRequest();
        _rectangleBannerView.LoadAd(adRequest);
        _rectangleBannerView.Hide();
    }
    public override void RegisterRectangleBannerEvents()
    {
        _rectangleBannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (isRectangleBannerLoaded == false)
                {
                    isRectangleBannerLoaded = true;
                }
                isLoadingRectangleBanner = false;
                SuAdsEventListener.OnBannerLoadedAction?.Invoke();
            });
        };

        _rectangleBannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoadingRectangleBanner = false;
                if (IsTest)
                {
                    SuAdsEventListener.OnRectangleBannerFailedToLoadAction?.Invoke(new SuAdsAdError()
                    {
                        errorInfo = JsonUtility.ToJson(error)
                    });
                }
            });
        };


        _rectangleBannerView.OnAdPaid += (AdValue adValue) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                string mediationClassName = "Unknow";
                string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
                if (_rectangleBannerView != null)
                {
                    ResponseInfo resInfo = _rectangleBannerView.GetResponseInfo();
                    mediationClassName = resInfo == null ? "Unknow" : resInfo.GetMediationAdapterClassName();
                    // update                     
                    if (resInfo != null)
                    {
                        Dictionary<string, string> extras = resInfo.GetResponseExtras();
                        if (extras != null)
                        {
                            GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                        }
                        adSourceName = resInfo.GetLoadedAdapterResponseInfo()?.AdSourceName ?? "Unknown";
                    }
                }

                // gọi action onPaid
                SuAdsAdValue _adValue = new SuAdsAdValue()
                {
                    Network = mediationClassName,
                    adSource = adSourceName,
                    Valuemicros = adValue.Value,
                    Value = adValue.Value / 1000000F,
                    Precision = adValue.Precision.ToString(),
                    CurrencyCode = adValue.CurrencyCode,
                    actionShowAds = ActionShowAds.Banner_Impression,
                    Ad_Format = "banner",
                    Mediation_Platform = AdsNetwork.admob,
                    UnitID = rectangleBannerId.ID,
                    mediationABTestName = mediationABTestName,
                    mediationABTestVariant = mediationABTestVariant,
                    mediationGroupName = mediationGroupName
                };
                SuAdsEventListener.OnBannerPaidAction?.Invoke(_adValue);

            });
        };

        _rectangleBannerView.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action Impression
                SuAdsEventListener.OnBannerImpressionAction?.Invoke();
            });
        };

        _rectangleBannerView.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action on Ad Click
                SuAdsEventListener.OnBannerClickAction?.Invoke();
            });
        };

        _rectangleBannerView.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action on aD open
                SuAdsEventListener.OnBannerOpenAction?.Invoke();
            });
        };

        _rectangleBannerView.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action onAD Close
                SuAdsEventListener.OnBannerCloseAction?.Invoke();
            });
        };
    }
    public override void HideRectangleBanner()
    {
        if (_rectangleBannerView != null)
        {
            _rectangleBannerView.Hide();
            IsRectangleBannerShowing = false;
            SuAdsEventListener.OnRectangleBannerHideAction?.Invoke();
        }
    }
    public override void ShowRectangleBanner()
    {
        if (_rectangleBannerView != null)
        {
            _rectangleBannerView.Show();
            IsRectangleBannerShowing = true;
            SuAdsEventListener.OnRectangleBannerShowAction?.Invoke();
        }
    }
    public override void RequestRectangleBannerOnUserAction()
    {

        if (!SuAds.IsRemoveAds && isRectangleBannerLoaded == false && !SuAds.IsRemoveAds24h)
        {
            //LogManager.Log("Request Rectangle banner On User Action");
            //LastTimeRequestBannerOnUserAction = DateTime.Now;
            LoadRectangleBanner();
        }
    }
    #endregion


    #region Banner
    // --------- Banner --------------------------------
    BannerView _bannerView;
    public override void InitBanner()
    {
        CreateBannerView();
        LoadBanner();
        HideBanner();
    }

    public override bool HaveReadyBanner
    {
        get
        {
            return _bannerView != null;
        }
    }
    public override void DestroyBanner()
    {
        if (_bannerView != null)
        {
            LogManager.Log("destroy banner");
            _bannerView.Destroy();
        }
    }
    public static Action<float> onConfigBannerHeight;
    public static bool isConfigBannerHeight;
    void CreateBannerView()
    {
        string id = BannerID.ID;
        //LogManager.Log("banner id: " + id);
        if (_bannerView != null)
        {
            LogManager.Log("destroy bannerrrr");
            _bannerView.Destroy();
        }
        AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        _bannerView = new BannerView(id, adaptiveSize, AdPosition.Bottom);
        isConfigBannerHeight = true;
        onConfigBannerHeight?.Invoke(GetBannerHeight());
        RegisterBannerEvents();
    }
    public override float GetBannerHeight()
    {
        if (_bannerView != null)
        {
            return _bannerView.GetHeightInPixels();
        }
        else
        {
            return 150f;
        }
    }
    public override void LoadBanner()
    {
        LogManager.Log("load banner view");
        if (!Inited)
        {
            // không tải quảng cáo khi user chưa đồng ý consent 
            return;
        }
        if (isBannerLoaded)
        {
            // không load banner nếu đã từng load được
            // vì banner bật auto refresh
            return;
        }
        if (!EnableBanner)
        {
            return;
        }
        if (SuAds.IsRemoveAds || SuAds.IsRemoveAds24h)
        {
            return;
        }
        if (isLoadingBanner)
        {
            return;
        }
        if (_bannerView == null)
        {
            CreateBannerView();
        }
        isLoadingBanner = true;
        var adRequest = new AdRequest();
        _bannerView.LoadAd(adRequest);
    }
    private void ConfigBanner()
    {
        LogManager.Log(new Vector2(_bannerView.GetWidthInPixels(), _bannerView.GetHeightInPixels()));
        float y =  _bannerView.GetHeightInPixels();
        onConfigBannerHeight?.Invoke(y);
        isConfigBannerHeight = true;
    }
    //private ResponseInfo resInfo;
    public override void RegisterBannerEvents()
    {
        _bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (isBannerLoaded == false)
                {
                    isBannerLoaded = true;
                    if (hasShowBanner == false && isShowedBanner == true)
                    {
                        hasShowBanner = true;
                        ShowBanner();
                    }
                    ConfigBanner();
                }
                isLoadingBanner = false;
                SuAdsEventListener.OnBannerLoadedAction?.Invoke();
                //resInfo = _bannerView.GetResponseInfo();
            });
        };

        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoadingBanner = false;
                if (IsTest)
                {
                    SuAdsEventListener.OnBannerFailedToLoadAction?.Invoke(new SuAdsAdError()
                    {
                        errorInfo = JsonUtility.ToJson(error)
                    });
                }
            });
        };


        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            //ResponseInfo resInfo = _bannerView.GetResponseInfo();

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                string mediationClassName = "Unknow";
                string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
                ResponseInfo resInfo = _bannerView.GetResponseInfo();
                if (resInfo != null)
                {
                    string className = resInfo.GetMediationAdapterClassName();
                    mediationClassName = className == null ? "Unknow" : className;
                    Dictionary<string, string> extras = resInfo.GetResponseExtras();
                    if (extras != null)
                    {
                        GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                    }
                    try
                    {
                        AdapterResponseInfo adapter = resInfo.GetLoadedAdapterResponseInfo();
                        if (adapter != null)
                        {
                            if (adapter.AdSourceName != null)
                            {
                                adSourceName = adapter.AdSourceName;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        adSourceName = "Unknown";
                    }
                    //adSourceName = adapter == null ? "Unknown" : adapter.AdSourceName;
                }

                // gọi action onPaid
                SuAdsAdValue _adValue = new SuAdsAdValue()
                {
                    Network = mediationClassName,
                    adSource = adSourceName,
                    Valuemicros = adValue.Value,
                    Value = adValue.Value / 1000000F,
                    Precision = adValue.Precision.ToString(),
                    CurrencyCode = adValue.CurrencyCode,
                    actionShowAds = ActionShowAds.Banner_Impression,
                    Ad_Format = "banner",
                    Mediation_Platform = AdsNetwork.admob,
                    UnitID = BannerID.ID,
                    mediationABTestName = mediationABTestName,
                    mediationABTestVariant = mediationABTestVariant,
                    mediationGroupName = mediationGroupName
                };
                SuAdsEventListener.OnBannerPaidAction?.Invoke(_adValue);

            });
        };

        _bannerView.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action Impression
                SuAdsEventListener.OnBannerImpressionAction?.Invoke();
            });
        };

        _bannerView.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action on Ad Click
                SuAdsEventListener.OnBannerClickAction?.Invoke();
            });
        };

        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action on aD open
                SuAdsEventListener.OnBannerOpenAction?.Invoke();
            });
        };

        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

                // gọi action onAD Close
                SuAdsEventListener.OnBannerCloseAction?.Invoke();
            });
        };
    }
    public override void HideBanner()
    {
        if (_bannerView != null)
        {
            LogManager.Log("hide banner");
            _bannerView.Hide();
            IsBannerShowing = false;
            SuAdsEventListener.OnBannerHideAction?.Invoke();
        }
    }
    private bool isShowedBanner;
    public override void ShowBanner()
    {
        
        isShowedBanner = true;
        if (hasShowBanner == false)
        {
            ShowFirstBanner();
            return;
        }
        if (_bannerView != null)
        {
            LogManager.Log("show banner");
            _bannerView.Show();
            IsBannerShowing = true;
            SuAdsEventListener.OnBannerShowAction?.Invoke();
        }
    }
    private bool hasShowBanner;
    public void ShowFirstBanner()
    {
        if (isBannerLoaded == false)
            return;

        hasShowBanner = true;
        if (_bannerView != null)
        {
            LogManager.Log("show banner");
            _bannerView.Show();
            IsBannerShowing = true;
            SuAdsEventListener.OnBannerShowAction?.Invoke();
        }
    }
    public override void RequestBannerOnUserAction()
    {

        if (!SuAds.IsRemoveAds && isBannerLoaded == false && !SuAds.IsRemoveAds24h)
        {
            LogManager.Log("Request banner On User Action");
            //LastTimeRequestBannerOnUserAction = DateTime.Now;
            LoadBanner();
        }
    }
    #endregion

    #region Interstitial
    //-------------------------------------------------------------- Interstitial
    InterstitialAd _interstitial;

    public override bool HaveReadyInterstitial
    {
        get
        {
            if (_interstitial != null && _interstitial.CanShowAd() && (DateTime.Now - LastTimeInterstitialLoaded).TotalHours > 1)
            {
                // log event hết hạn inter 
                SuGame.Get<SuAnalytics>().LogEvent(EventName.Interstitial_Expired);
                return false;
            }
            return _interstitial != null && _interstitial.CanShowAd();
        }
    }



    public override void InitInterstitial()
    {
        LoadInterstitial();
    }
    public override void LoadInterstitial()
    {
        if (!Inited)
        {
            //không tải quảng cáo khi user chưa đồng ý consent
            return;
        }
        if (!EnableInterstitial)
        {
            // không tải qc khi đang tắt
            return;
        }
        if (SuAds.IsRemoveAds || SuAds.IsRemoveAds24h)
        {
            return;
        }
        if (isLoadingInterstitial)
        {
            return;
        }
        if (_interstitial != null)
        {
            _interstitial.Destroy();
            _interstitial = null;
        }
        var adRequest = new AdRequest();
        string id = InterstitialID.ID;

        #region THAY ID QUẢNG CÁO KHI ĐẠT LEVEL NHẤT ĐỊNH
        if (ads_Config.interstitial.on)
        {
            int currentLevel = SuLevelManager.CurrentLevel;
            int level_change_ids = ads_Config.interstitial.level_change_ads_id;
            if (currentLevel < level_change_ids)
            {
                string pre_inter_id = ads_Config.interstitial.sub_ads_id;
                if (!string.IsNullOrEmpty(pre_inter_id))
                {
                    id = pre_inter_id;
                }
            }
        }
        #endregion

        //Debug.Log("Load Interstitial with id: " + id);
        
        isLoadingInterstitial = true;
        InterstitialAd.Load(id, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.

                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    isLoadingInterstitial = false;

                    if (error != null || ad == null)
                    {
                        if (IsTest)
                        {
                            //Debug.Log("interstitial ad failed to load an ad " +
                            //       "with error : " + error.GetMessage());
                            SuAdsEventListener.OnInterstitialFailedToLoadAction?.Invoke(new SuAdsAdError()
                            {
                                errorInfo = JsonUtility.ToJson(error)
                            });
                        }
                        return;
                    }

                    // frame sau mới reg event nên check null lại 1 lần nữa
                    if (ad != null)
                    {
                        LastTimeInterstitialLoaded = DateTime.Now;
                        SuAdsEventListener.OnInterstitialLoadedAction?.Invoke();
                        _interstitial = ad;
                        RegisterInterstitialEvents();
                        if (IsTest)
                        {
                            LogManager.Log("Interstitial ad loaded with response : "
                              + JsonUtility.ToJson(ad.GetResponseInfo()));
                        }

                    }

                });


            });
    }

    private void Inter_OnAdPaid(AdValue adValue)
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            string mediationClassName = "Unknow";
            string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
            if (_interstitial != null)
            {
                try
                {
                    ResponseInfo resInfo = _interstitial.GetResponseInfo();

                    if (resInfo != null)
                    {
                        string className = resInfo.GetMediationAdapterClassName();
                        mediationClassName = className == null ? "Unknow" : className;
                        Dictionary<string, string> extras = resInfo.GetResponseExtras();
                        if (extras != null)
                        {
                            GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                        }
                        AdapterResponseInfo adapter = resInfo.GetLoadedAdapterResponseInfo();
                        if (adapter != null)
                        {
                            if (adapter.AdSourceName != null)
                            {
                                adSourceName = adapter.AdSourceName;
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    LogManager.Log(e.Message);
                }
                
            }
            SuAdsAdValue _adValue = new SuAdsAdValue()
            {
                Network = mediationClassName,
                adSource = adSourceName,
                Valuemicros = adValue.Value,
                Value = adValue.Value / 1000000F,
                Precision = adValue.Precision.ToString(),
                CurrencyCode = adValue.CurrencyCode,
                actionShowAds = ActionShowAdsName,
                Ad_Format = "inter",
                Mediation_Platform = AdsNetwork.admob,
                UnitID = InterstitialID.ID,
                mediationABTestName = mediationABTestName,
                mediationABTestVariant = mediationABTestVariant,
                mediationGroupName = mediationGroupName
            };
            if (IsTest)
            {
                LogManager.Log("OnPaidInterStitial" + JsonUtility.ToJson(_adValue));
            }
            SuAdsEventListener.OnInterstitialPaidAction?.Invoke(_adValue);
        });
    } 
    private void Inter_OnAdImpressionRecorded()
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {

            // gọi action on Ad Click
            SuAdsEventListener.OnInterstitialImpressionAction?.Invoke();

        });
    }  
    private void Inter_OnAdClicked()
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {

            // gọi action on Ad Click
            SuAdsEventListener.OnInterstitialClickAction?.Invoke();
        });
    }    
    private void Inter_OnAdFullScreenContentOpened()
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {

            // gọi action on open
            OnIntersitialOpen?.Invoke();
            OnIntersitialOpen = null;
            SuGame.Get<SuAds>().LockAppOpenAds = true;
            SuAdsEventListener.OnInterstitialShowAction?.Invoke();
        });
    } 
    private void Inter_OnAdFullScreenContentClosed()
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            OnInterstitialCloseAction?.Invoke();
            OnInterstitialCloseAction = null;
            SuAdsEventListener.OnInterstititalCloseAction?.Invoke();
            //LastTimeRequestInterstitialOnUserAction = DateTime.Now;
            Invoke(nameof(LoadInterstitial), 1f);
            //LoadInterstitial();
        });
    }    

    private void Inter_OnAdFullScreenContentFailed(AdError error)
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            OnInterstitialCloseAction?.Invoke();
            OnInterstitialCloseAction = null;
            //LastTimeRequestInterstitialOnUserAction = DateTime.Now;
            LoadInterstitial();
            if (IsTest)
            {
                SuAdsEventListener.OnInterstitialFailedToShowAction?.Invoke(new SuAdsAdError()
                {
                    errorInfo = JsonUtility.ToJson(error)
                });
            }

        });
    }
    public override void RegisterInterstitialEvents()
    {
        _interstitial.OnAdPaid += Inter_OnAdPaid;
        // Raised when an impression is recorded for an ad.
        _interstitial.OnAdImpressionRecorded += Inter_OnAdImpressionRecorded;
        // Raised when a click is recorded for an ad.
        _interstitial.OnAdClicked += Inter_OnAdClicked;
        // Raised when an ad opened full screen content.
        _interstitial.OnAdFullScreenContentOpened += Inter_OnAdFullScreenContentOpened;
        // Raised when the ad closed full screen content.
        _interstitial.OnAdFullScreenContentClosed += Inter_OnAdFullScreenContentClosed;
        // Raised when the ad failed to open full screen content.
        _interstitial.OnAdFullScreenContentFailed += Inter_OnAdFullScreenContentFailed;
    }

    public override void RequestInterstitialOnUserAction()
    {
        if (!SuAds.IsRemoveAds && !HaveReadyInterstitial && !SuAds.IsRemoveAds24h)
        {
            //LogManager.Log("Request inter On User Action");
            //LastTimeRequestInterstitialOnUserAction = DateTime.Now;
            LoadInterstitial();

        }
    }

    public override void ShowInterstitial(Action onClose, Action onOpenAd, ActionShowAds actionShowAdsName)
    {

        if (!HaveReadyInterstitial)
        {
            onClose?.Invoke();
            onOpenAd?.Invoke();
            return;
        }

        OnInterstitialCloseAction = onClose;
        OnIntersitialOpen = onOpenAd;
        ActionShowAdsName = actionShowAdsName;
        //Invoke(nameof(ShowInterDelay), 0.5F);
        ShowInterDelay();
    }

    void ShowInterDelay()
    {
        SuGame.Get<SuAds>().LockAppOpenAds = true;
        SuAds.LastTimeShowInterstitial = DateTime.Now;
        _interstitial.Show();
    }

    #endregion


    #region Rewarded_Video
    // ------ Reward Video ----------------------------------------------------------------------------------------------------------------
    RewardedUnit[] _rewardVideos;

    public override void InitRewardVideo()
    {
        
        StartCoroutine(LoadRewardRoutine());
    }
    IEnumerator LoadRewardRoutine()
    {
        for (int i = 0; i < _rewardVideos.Length; i++)
        {
            LoadRewardVideo(i);
            yield return new WaitForSeconds(1f);
        }
    }    
    public override void LoadRewardVideo(int numb)
    {
        if (!Inited)
        {
            //không tải quảng cáo khi user chưa đồng ý consent
            return;
        }
        if (!EnableRewardVideo)
        {
            // không tải qc khi đang tắt reward video
            return;
        }
        RewardedUnit rewardedUnit = _rewardVideos[numb];
        if (rewardedUnit.isLoadingRewardVideo)
        {
            return;
        }
        if (rewardedUnit._rewardVideo != null)
        {
            rewardedUnit._rewardVideo.Destroy();
            rewardedUnit._rewardVideo = null;
        }
        //LastTimeRequestRewardVideoOnUserAction = DateTime.Now;
        rewardedUnit.isLoadingRewardVideo = true;
        var adRequest = new AdRequest();
        string id = RewardVideoID.ID;

        //Debug.Log("Load Reward Video ID: " + id);

        #region THAY ID QUẢNG CÁO KHI ĐẠT LEVEL NHẤT ĐỊNH
        int currentLevel = SuLevelManager.CurrentLevel;
        int level_change_ids = ads_Config.rewarded.level_change_id;
        if (currentLevel < level_change_ids)
        {
            string sub_reward_id = ads_Config.rewarded.rewarded_sub_id;
            if (!string.IsNullOrEmpty(sub_reward_id))
            {
                id = sub_reward_id;
            }
        } 
        #endregion


        
        // send the request to load the ad.
        RewardedAd.Load(id, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    rewardedUnit.isLoadingRewardVideo = false;
                    if (error != null || ad == null)
                    {
                        //Debug.Log("Reward ad failed to load an ad " +
                        //               "with error : " + error);

                        //NeedRequestRewardVideoOnUserAction = true;
                        if (IsTest)
                        {
                            SuAdsEventListener.OnRewardVideoFailedToLoadAction?.Invoke(new SuAdsAdError()
                            {
                                errorInfo = JsonUtility.ToJson(error)
                            });
                        }

                        return;
                    }

                    if (ad != null)
                    {
                        rewardedUnit.LastTimeRewardVideoLoaded = DateTime.Now;
                        //NeedRequestRewardVideoOnUserAction = false;
                        rewardedUnit._rewardVideo = ad;
                        RegisterRewardVideoEvents(numb);
                        SuAdsEventListener.OnRewardVideoLoadedAction?.Invoke();
                    }
                });


            });
    }





    public override void RegisterRewardVideoEvents(int id)
    {
        RewardedAd _rewardVideo = _rewardVideos[id]._rewardVideo;
        _rewardVideo.OnAdPaid += (AdValue adValue) =>
        {
            

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                string mediationClassName = "Unknow";
                string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
                if (_rewardVideo != null)
                {

                    // update
                    ResponseInfo resInfo = _rewardVideo.GetResponseInfo();
                    if (resInfo != null)
                    {
                        string className = resInfo.GetMediationAdapterClassName();
                        mediationClassName = className == null ? "Unknow" : className;
                        Dictionary<string, string> extras = resInfo.GetResponseExtras();
                        if (extras != null)
                        {
                            GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                        }
                        try
                        {
                            AdapterResponseInfo adapter = resInfo.GetLoadedAdapterResponseInfo();
                            if (adapter != null)
                            {
                                if (adapter.AdSourceName != null)
                                {
                                    adSourceName = adapter.AdSourceName;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            adSourceName = "Unknown";
                        }
                    }
                }
                SuAdsAdValue _adValue = new SuAdsAdValue()
                {
                    Network = mediationClassName,
                    adSource = adSourceName,
                    Valuemicros = adValue.Value,
                    Value = adValue.Value / 1000000F,
                    Precision = adValue.Precision.ToString(),
                    CurrencyCode = adValue.CurrencyCode,
                    actionShowAds = ActionShowAdsName,
                    Ad_Format = "rewarded",
                    Mediation_Platform = AdsNetwork.admob,
                    UnitID = RewardVideoID.ID,
                    mediationABTestName = mediationABTestName,
                    mediationABTestVariant = mediationABTestVariant,
                    mediationGroupName = mediationGroupName
                };
                SuAdsEventListener.OnRewardVideoPaidAction?.Invoke(_adValue);
            });
        };
        // Raised when an impression is recorded for an ad.
        _rewardVideo.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {

            });
        };
        // Raised when a click is recorded for an ad.
        _rewardVideo.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuAdsEventListener.OnRewardVideoClickAction?.Invoke();
            });
        };
        // Raised when an ad opened full screen content.
        _rewardVideo.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuGame.Get<SuAds>().LockAppOpenAds = true;
                SuAdsEventListener.OnRewardVideoShowAction?.Invoke();
            });
        };
        // Raised when the ad closed full screen content.
        _rewardVideo.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (OnRewardVideoCloseAction != null)
                {
                    if(canRewardByVideo)
                    {
                        OnRewardVideoEarnAction?.Invoke();
                    }
                    else
                    {
                        OnRewardVideoCloseAction?.Invoke();
                    }
                    
                }

                OnRewardVideoCloseAction = null;
                OnRewardVideoEarnAction = null;
                canRewardByVideo = false;
                //LastTimeRequestRewardVideoOnUserAction = DateTime.Now;
                //LoadRewardVideo();
                //SuAdsEventListener.instance.OnRewardVideoClose();
                LoadRewardVideo(id);
            });
        };
        // Raised when the ad failed to open full screen content.
        _rewardVideo.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                OnRewardVideoFailedToShowAction?.Invoke();
                OnRewardVideoFailedToShowAction = null;
                LoadRewardVideo(id);
                if (IsTest)
                {
                    SuAdsEventListener.OnRewardVideoFailedToShowAction?.Invoke(new SuAdsAdError()
                    {
                        errorInfo = JsonUtility.ToJson(error)
                    });
                }

            });
        };
    }



    public override bool HaveReadyRewardVideo
    {
        get
        { 
            foreach (var item in _rewardVideos)
            {
                if(item.HaveReadyRewardVideo)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public override void RequestRewardVideoOnUserAction()
    {
        //LogManager.Log("Request Reward On User Action");
        //LastTimeRequestRewardVideoOnUserAction = DateTime.Now;
        for (int i = 0; i < _rewardVideos.Length; i++)
        {
            if (_rewardVideos[i].HaveReadyRewardVideo == false)
                LoadRewardVideo(i);
        }
    }




    bool canRewardByVideo = false;
    public override void ShowRewardVideo(Action onEarnReward,Action onClose, Action onNoAds, ActionShowAds actionShowAdsName)
    {
        for (int i = 0; i < _rewardVideos.Length; i++)
        {
            if (_rewardVideos[i].HaveReadyRewardVideo)
            {
                OnRewardVideoCloseAction = onClose;
                OnRewardVideoEarnAction = onEarnReward;
                OnRewardVideoFailedToShowAction = onNoAds;
                ShowRewardVideoDelay(i);
                return;
            }
        }

        onNoAds?.Invoke();

    }

    void ShowRewardVideoDelay(int id)
    {
        SuAds.LastTimeShowRewardVideo = DateTime.Now;
        SuGame.Get<SuAds>().LockAppOpenAds = true;
        canRewardByVideo = false;
        _rewardVideos[id]._rewardVideo.Show((rw) =>
        {
            canRewardByVideo = true;
            SuAdsEventListener.OnRewardVideoRewardAction?.Invoke();
        });
    }
    #endregion

    #region AppOpen_Ads
    // --------------------- APP OPEN -------------------------------------------------------
    AppOpenAd _appOpen;
    public override void InitAppOpen()
    {
        LoadAppOpen();
    }



    public override void LoadAppOpen()
    {
        if (!Inited)
        {
            //không tải quảng cáo khi user chưa đồng ý consent
            return;
        }
        if (SuAds.IsRemoveAds || SuAds.IsRemoveAds24h)
        {
            return;
        }
        if (!EnableAppOpen)
        {
            // không load app open nếu tắt
            return;
        }
        if (isLoadingAppOpen)
        {
            return;
        }
        if (_appOpen != null)
        {
            _appOpen.Destroy();
            _appOpen = null;
        }
        isLoadingAppOpen = true;
        //LastTimeRequestAppOpenOnUserAction = DateTime.Now;
        string id = AppOpenID.ID;
        var adRequest = new AdRequest();
        AppOpenAd.Load(id, adRequest,
            (AppOpenAd ad, LoadAdError error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    isLoadingAppOpen = false;
                    if (error != null || ad == null)
                    {
                        LogManager.Log("app open ad failed to load an ad " +
                                       "with error : " + error);
                        if (IsTest)
                        {
                            SuAdsEventListener.OnAppOpenFailedToLoadAction?.Invoke(new SuAdsAdError()
                            {
                                errorInfo = JsonUtility.ToJson(error)
                            });
                        }
                        return;
                    }
                    if (ad != null)
                    {
                        LogManager.Log("App open ad loaded with response : "
                              + ad.GetResponseInfo());

                        LastTimeAppOpenLoaded = DateTime.Now;
                        _appOpen = ad;
                        RegisterAppOpenEvents();
                        SuAdsEventListener.OnAppOpenLoadedAction?.Invoke();
                    }
                    else
                    {
                        //NeedRequestAppOpenOnUserAction = true;
                    }
                });
                // if error is not null, the load request failed.


            });
    }

    public override void RegisterAppOpenEvents()
    {
        _appOpen.OnAdPaid += (AdValue adValue) =>
        {
            

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                string mediationClassName = "Unknow";
                string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
                if (_appOpen != null)
                {
                    ResponseInfo resInfo = _appOpen.GetResponseInfo();

                    // update                    
                    if (resInfo != null)
                    {
                        string className = resInfo.GetMediationAdapterClassName();
                        mediationClassName = className == null ? "Unknow" : className;
                        Dictionary<string, string> extras = resInfo.GetResponseExtras();
                        if (extras != null)
                        {
                            GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                        }
                        try
                        {
                            AdapterResponseInfo adapter = resInfo.GetLoadedAdapterResponseInfo();
                            if (adapter != null)
                            {
                                if (adapter.AdSourceName != null)
                                {
                                    adSourceName = adapter.AdSourceName;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            adSourceName = "Unknown";
                        }
                    }
                }
                SuAdsAdValue _adValue = new SuAdsAdValue()
                {
                    Network = mediationClassName,
                    adSource = adSourceName,
                    Valuemicros = adValue.Value,
                    Value = adValue.Value / 1000000F,
                    Precision = adValue.Precision.ToString(),
                    CurrencyCode = adValue.CurrencyCode,
                    actionShowAds = ActionShowAds.BackToGame,
                    Ad_Format = "app_open",
                    Mediation_Platform = AdsNetwork.admob,
                    UnitID = AppOpenID.ID,
                    mediationABTestName = mediationABTestName,
                    mediationABTestVariant = mediationABTestVariant,
                    mediationGroupName = mediationGroupName
                };
                SuAdsEventListener.OnAppOpenPaidAction?.Invoke(_adValue);
            });
        };
        // Raised when an impression is recorded for an ad.
        _appOpen.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuAdsEventListener.OnAppOpenImpressionAction?.Invoke();
            });
        };
        // Raised when a click is recorded for an ad.
        _appOpen.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuAdsEventListener.OnAppOpenClickAction?.Invoke();
            });
        };
        // Raised when an ad opened full screen content.
        _appOpen.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuGame.Get<SuAds>().LockAppOpenAds = true;
                SuAdsEventListener.OnAppOpenShowAction?.Invoke();

            });
        };
        // Raised when the ad closed full screen content.
        _appOpen.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuAdsEventListener.OnAppOpenCloseAction?.Invoke();
                if (OnAppOpenCloseAction != null)
                {
                    OnAppOpenCloseAction?.Invoke();
                    OnAppOpenCloseAction = null;
                }
                //LastTimeRequestAppOpenOnUserAction = DateTime.Now;
                //LoadAppOpen();
                Invoke(nameof(LoadAppOpen), 1f);
            });
        };
        // Raised when the ad failed to open full screen content.
        _appOpen.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                OnAppOpenFailedToShowAction?.Invoke();
                OnAppOpenFailedToShowAction = null;
                if (IsTest)
                {
                    SuAdsEventListener.OnAppOpenFailedToShowAction?.Invoke(new SuAdsAdError()
                    {
                        errorInfo = JsonUtility.ToJson(error)
                    });
                }
                LoadAppOpen();
            });
        };
    }

    public override bool HaveReadyAppOpen
    {
        get
        {
            return _appOpen != null && _appOpen.CanShowAd();
        }
    }



    public override void ShowAppOpen(Action onClose, Action onNoAds, ActionShowAds actionShowAdsName)
    {
        //if (SuAds.IsRemoveAds)
        //{
        //    onClose?.Invoke();
        //    return;
        //}
        if (HaveReadyAppOpen)
        {
            OnAppOpenCloseAction = onClose;
            OnAppOpenFailedToShowAction = onNoAds;
            ActionShowAdsName = actionShowAdsName;
            ShowAppOpenDelay();
        }
        else
        {
            onNoAds?.Invoke();
        }
    }

    void ShowAppOpenDelay()
    {
        SuAds.LastTimeShowAppOpenAd = DateTime.Now;
        _appOpen.Show();
    }

    public override void RequestAppOpenOnUserAction()
    {
        if (!SuAds.IsRemoveAds && !HaveReadyAppOpen && !SuAds.IsRemoveAds24h)
        {
            LogManager.Log("Request App Open On User Action");
            //LastTimeRequestAppOpenOnUserAction = DateTime.Now;
            LoadAppOpen();
        }
    }
    #endregion


    #region Rewarded_Interstitial
    //--------------- Rewarded Interstitial -------------------------------------

    RewardedInterstitialAd _rewardedInterstitial;
    bool canRewardByRewardedInterstitial = false;
    public override bool HaveReadyRewardedInterstitial
    {
        get
        {
            if (_rewardedInterstitial != null && _rewardedInterstitial.CanShowAd() && (DateTime.Now - LastTimeRewardedInterstitialLoaded).TotalHours > 1)
            {
                //NeedRequestRewardedInterstitialOnUserAction = true;
                // log event hết hạn inter 
                //SuGame.Get<SuAnalytics>().LogEvent(EventName.rew);
                return false;
            }
            return _rewardedInterstitial != null && _rewardedInterstitial.CanShowAd();
        }
    }
    public override void InitRewardedInterstitial()
    {
        LoadRewardedInterstitial();
    }



    public override void LoadRewardedInterstitial()
    {
        if (!Inited)
        {
            //không tải quảng cáo khi user chưa đồng ý consent
            return;
        }
        if (!EnableRewardedInterstitial)
        {
            return;
        }
        if (isLoadingRewardedInter)
        {
            return;
        }
        if (_rewardedInterstitial != null)
        {
            _rewardedInterstitial.Destroy();
            _rewardedInterstitial = null;
        }
        isLoadingRewardedInter = true;
        var adRequest = new AdRequest();
        string id = RewardedInterstitialID.ID;

        RewardedInterstitialAd.Load(id, adRequest,
            (RewardedInterstitialAd ad, LoadAdError error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    isLoadingRewardedInter = false;
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        LogManager.Log("rewardedInterstitial ad failed to load an ad " +
                                       "with error : " + error);
                        if (IsTest)
                        {
                            SuAdsEventListener.OnRewardedInterstitialFailedToLoadAction?.Invoke(new SuAdsAdError()
                            {
                                errorInfo = JsonUtility.ToJson(error)
                            });
                        }

                        return;
                    }
                    // frame sau mới reg event nên check null lại 1 lần nữa
                    if (ad != null)
                    {
                        LastTimeRewardedInterstitialLoaded = DateTime.Now;
                        SuAdsEventListener.OnRewardedInterstitialLoadedAction?.Invoke();
                        _rewardedInterstitial = ad;
                        RegisterRewardedInterstitialEvents();
                        if (IsTest)
                        {
                            LogManager.Log("RewardedInterstitial ad loaded with response : "
                              + ad.GetResponseInfo());
                        }

                    }
                });


            });
    }

    public override void RegisterRewardedInterstitialEvents()
    {
        _rewardedInterstitial.OnAdPaid += (AdValue adValue) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                string mediationClassName = "Unknow";
                string mediationGroupName = "", mediationABTestName = "", mediationABTestVariant = "", adSourceName = "";
                if (_rewardedInterstitial != null)
                {
                    ResponseInfo resInfo = _rewardedInterstitial.GetResponseInfo();
                    mediationClassName = resInfo == null ? "Unknown" : resInfo.GetMediationAdapterClassName();
                    // update , log thêm các giá trị dưới                    
                    if (resInfo != null)
                    {
                        GetMediationData(resInfo, ref mediationGroupName, ref mediationABTestName, ref mediationABTestVariant);
                    }
                    adSourceName = resInfo.GetLoadedAdapterResponseInfo()?.AdSourceName ?? "Unknown";
                }
                SuAdsAdValue _adValue = new SuAdsAdValue()
                {
                    Network = mediationClassName,
                    adSource = adSourceName,
                    Valuemicros = adValue.Value,
                    Value = adValue.Value / 1000000F,
                    Precision = adValue.Precision.ToString(),
                    CurrencyCode = adValue.CurrencyCode,
                    actionShowAds = ActionShowAdsName,
                    Ad_Format = "rewarded_inter",
                    Mediation_Platform = AdsNetwork.admob,
                    UnitID = RewardedInterstitialID.ID,
                    mediationABTestName = mediationABTestName,
                    mediationABTestVariant = mediationABTestVariant,
                    mediationGroupName = mediationGroupName
                };
                SuAdsEventListener.OnRewardedInterstitialPaidAction?.Invoke(_adValue);
            });
        };
        // Raised when an impression is recorded for an ad.
        _rewardedInterstitial.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuAdsEventListener.OnRewardedInterstitialImpressionAction?.Invoke();
            });
        };
        // Raised when a click is recorded for an ad.
        _rewardedInterstitial.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuAdsEventListener.OnRewardedInterstitialClickAction?.Invoke();
            });
        };
        // Raised when an ad opened full screen content.
        _rewardedInterstitial.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                SuGame.Get<SuAds>().LockAppOpenAds = true;
                SuAdsEventListener.OnRewardedInterstitialShowAction?.Invoke();
            });
        };
        // Raised when the ad closed full screen content.
        _rewardedInterstitial.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (OnRewardedInterstitialCloseAction != null && canRewardByRewardedInterstitial == true)
                {
                    OnRewardedInterstitialCloseAction?.Invoke(true);
                    OnRewardedInterstitialCloseAction = null;
                }
                else
                {
                    OnRewardedInterstitialCloseAction = null;

                }
                canRewardByRewardedInterstitial = false;
                //LastTimeRequestRewardedInterstitialOnUserAction = DateTime.Now;
                //LoadRewardedInterstitial();
                Invoke(nameof(LoadRewardedInterstitial), 1f);
                SuAdsEventListener.OnRewardedInterstitialCloseAction?.Invoke();
            });
        };
        // Raised when the ad failed to open full screen content.
        _rewardedInterstitial.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadRewardedInterstitial();
                OnRewardedInterstitialFailedToShowAction?.Invoke();
                OnRewardedInterstitialFailedToShowAction = null;
                if (IsTest)
                {
                    SuAdsEventListener.OnRewardedInterstitialFailedToShowAction?.Invoke(new SuAdsAdError()
                    {
                        errorInfo = JsonUtility.ToJson(error)
                    });
                }

            });
        };
    }

    public override void ShowRewardedInterstitial(Action<bool> onClose, Action onFailedToShow, ActionShowAds actionShowAdsName)
    {
        canRewardByRewardedInterstitial = false;
        if (!HaveReadyRewardedInterstitial)
        {
            onClose?.Invoke(false);
            return;
        }
        OnRewardedInterstitialCloseAction = onClose;
        OnRewardedInterstitialFailedToShowAction = onFailedToShow;
        ShowRewardedInterstitialDelay();
        //Invoke(nameof(ShowRewardedInterstitialDelay), 0.5F);
    }

    void ShowRewardedInterstitialDelay()
    {
        SuAds.LastTimeShowRewardedInterstitial = DateTime.Now;
        SuGame.Get<SuAds>().LockAppOpenAds = true;
        _rewardedInterstitial.Show((rw) =>
        {
            canRewardByRewardedInterstitial = true;
            SuAdsEventListener.OnRewardedInterstitialRewardAction?.Invoke();
        });
    }

    public override void RequestRewardedInterstitialOnUserAction()
    {
        if (!HaveReadyRewardedInterstitial)
        {
            LogManager.Log("Request Rewarded Ads On User Action");
            LoadRewardedInterstitial();
        }
    }
    #endregion

    #region Others
    //---------------- Other 
    void GetMediationData(ResponseInfo resInfo, ref string mediationGroupName, ref string mediationABTestName, ref string mediationABTestVariant)
    {
        Dictionary<string, string> extras = resInfo.GetResponseExtras();
        if (extras != null)
        {
            if (extras.ContainsKey("mediation_group_name"))
            {
                mediationGroupName = extras["mediation_group_name"];
            }
            if (extras.ContainsKey("mediation_ab_test_name"))
            {
                mediationABTestName = extras["mediation_ab_test_name"];
            }
            if (extras.ContainsKey("mediation_ab_test_variant"))
            {
                mediationABTestVariant = extras["mediation_ab_test_variant"];
            }
        }
    }
    #endregion

}

public class RewardedUnit
{
    public RewardedAd _rewardVideo;

    public DateTime LastTimeRewardVideoLoaded;

    public bool isLoadingRewardVideo;
    public bool HaveReadyRewardVideo
    {
        get
        {
            if (_rewardVideo != null && _rewardVideo.CanShowAd())
            {
                if((DateTime.Now - LastTimeRewardVideoLoaded).TotalHours > 1)
                {
                    return false;
                }    
                return true;
            }
            return false;
        }
    }
}
