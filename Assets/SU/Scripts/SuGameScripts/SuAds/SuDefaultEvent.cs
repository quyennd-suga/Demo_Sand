using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using AdjustSdk;

public class SuDefaultEvent : MonoBehaviour
{
    EventName[] events;
    int OpenGameCount
    {
        get
        {
            return PlayerPrefs.GetInt("OpenGameCount", 0);
        }
        set
        {
            PlayerPrefs.SetInt("OpenGameCount", value);
        }
    }

    private void Awake()
    {
        events = (EventName[])System.Enum.GetValues(typeof(EventName));
        OpenGameCount++;
        Init();
    }
    IEnumerator LogEventDelay(EventName name, float delay)
    {
        //Debug.Log("Log event " + name + " sau " + delay + " giây");
        yield return new WaitForSecondsRealtime(delay);
        SuGame.Get<SuAnalytics>().LogEvent(name);
    }

    void Init()
    {
        if (OpenGameCount == 1)
        {
            LogEventD0OnlineTime();
        }
        SuAdsEventListener.OnAppOpenPaidAction += OnAppOpenPaid;
        SuAdsEventListener.OnBannerPaidAction += OnBannerPaid;
        SuAdsEventListener.OnInterstitialPaidAction += OnInterstitialPaid;
        SuAdsEventListener.OnRewardedInterstitialPaidAction += OnRewardedInterstitialPaid;
        SuAdsEventListener.OnRewardVideoPaidAction += OnRewardVideoPaid;
    }

    void LogEventD0OnlineTime()
    {
        string pattern = @"^D0_\d+_Minutes$";
        for (int i = 0; i < events.Length; i++)
        {
            string name = events[i].ToString();
            if (Regex.IsMatch(name, pattern))
            {
                int minute = int.Parse(name.Remove(0, 3).Replace("_Minutes", ""));
                StartCoroutine(LogEventDelay(events[i], 60 * minute));
            }
        }
    }

    public static void LogEventBannerCount(uint count)
    {
        bool parse = System.Enum.TryParse("Banner_" + count,false,out EventName _eventName);
        if(parse)
        {
            SuGame.Get<SuAnalytics>().LogEvent(_eventName);
        }      
    }
    public static void LogEventInterCount(uint count)
    {
        bool parse = System.Enum.TryParse("Interstitial_" + count, false, out EventName _eventName);
        if (parse)
        {
            SuGame.Get<SuAnalytics>().LogEvent(_eventName);
        }
    }

    public static void LogEventAdsShowed(uint count, double value)
    {
        bool parse = System.Enum.TryParse("AdsShowed" + count, false, out EventName _eventName);
        if (parse)
        {
            //Param param = new Param(ParaName.value, value);
            SuGame.Get<SuAnalytics>().LogEvent(_eventName, new Param(ParaName.value, value), new Param(ParaName.currency,"USD"));
        }
    }    

    public static void LogEvenRewardedCount(uint count)
    {
        bool parse = System.Enum.TryParse("Rewarded_" + count, false, out EventName _eventName);
        if (parse)
        {
            SuGame.Get<SuAnalytics>().LogEvent(_eventName);
        }
    }

    public void OnAppOpenPaid(SuAdsAdValue adValue)
    {
        LogFirebaseInpression(EventName.paid_ad_impression_app_open, adValue);
    }


    public void OnBannerPaid(SuAdsAdValue adValue)
    {
        LogFirebaseInpression(EventName.paid_ad_impression_banner, adValue);
    }


    public void OnInterstitialPaid(SuAdsAdValue adValue)
    {
        LogFirebaseInpression(EventName.paid_ad_impression_interstitial, adValue);
    }

    public void OnRewardVideoPaid(SuAdsAdValue adValue)
    {
        LogFirebaseInpression(EventName.paid_ad_impression_video, adValue);
    }

    public void OnRewardedInterstitialPaid(SuAdsAdValue adValue)
    {
        LogFirebaseInpression(EventName.paid_ad_impression_rewarded_inter, adValue);
    }

    private static string value = "value";
    private static string ad_platform = "ad_platform";
    private static string ad_format = "ad_format";
    private static string currency = "currency";
    private static string precision = "precision";
    private static string ad_unit_name = "ad_unit_name";
    private static string ad_source = "ad_source";
    private static string ad_network = "ad_network";
    private static string action_show_ads = "action_show_ads";
    private static string ab_test_name = "ab_test_name";
    private static string ab_test_variant = "ab_test_variant";
    private static string mediation_group_name = "mediation_group_name";
    private static string Precision = "Precision";
    private static string USD = "USD";
    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    void LogFirebaseInpression(EventName eventName, SuAdsAdValue adValue)
    {
        //LogManager.Log("Log Paid Event: " + eventName);
        Firebase.Analytics.Parameter[] LTVParameters = {// Log ad value in micros.
            new Firebase.Analytics.Parameter(value, adValue.Value ),
            new Firebase.Analytics.Parameter(ad_platform, adValue.Mediation_Platform.ToString()),
            new Firebase.Analytics.Parameter(ad_format, adValue.Ad_Format ),
            new Firebase.Analytics.Parameter(currency, adValue.CurrencyCode ),
            new Firebase.Analytics.Parameter(precision, adValue.Precision),
            new Firebase.Analytics.Parameter(ad_unit_name, adValue.UnitID ),
            new Firebase.Analytics.Parameter(ad_source, adValue.Network),
            //new Firebase.Analytics.Parameter(ad_network,adValue.adSource),
            new Firebase.Analytics.Parameter(action_show_ads, adValue.actionShowAds.ToString()),
            new Firebase.Analytics.Parameter(ab_test_name,adValue.mediationABTestName),
            new Firebase.Analytics.Parameter(ab_test_variant,adValue.mediationABTestVariant),
            new Firebase.Analytics.Parameter(mediation_group_name,adValue.mediationGroupName)
        };
        Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName.ToString(), LTVParameters);


        SuGame.Get<SuAnalytics>().TrackPaidAdEvent(adValue.Value, adValue.Ad_Format.ToLower(),SuLevelManager.CurrentLevel, eventName.ToString(),adValue.adSource);
        // riêng cho MAX 
        // log firebase ad_impression
        if (adValue.Mediation_Platform == AdsNetwork.max)
        {

            var impressionParameters = new[] {
            new Firebase.Analytics.Parameter(ad_platform, "AppLovin"),
            new Firebase.Analytics.Parameter(ad_source, adValue.Network),
            new Firebase.Analytics.Parameter(ad_unit_name, adValue.UnitID),
            new Firebase.Analytics.Parameter(ad_format, adValue.Ad_Format),
            //new Firebase.Analytics.Parameter(ad_network,adValue.adSource),
            new Firebase.Analytics.Parameter(value, adValue.Value),
            new Firebase.Analytics.Parameter(currency, USD), // All AppLovin revenue is sent in USD
            };
            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
        }
        LogAdjustImpression(adValue);
        // log event 
        switch (eventName)
        {
            case EventName.paid_ad_impression_banner:
                SuAds.AdsSaveData.BannerCount++;
                SuAds.AdsSaveData.BannerRevenue += adValue.Value;
                SuDefaultEvent.LogEventBannerCount(SuAds.AdsSaveData.BannerCount);
                break;
            case EventName.paid_ad_impression_interstitial:
                SuAds.AdsSaveData.InterstitialCount++;
                SuAds.AdsSaveData.InterstitialRevenue += adValue.Value;
                ProcessAdsShow(adValue.Value);
                uint total = SuAds.AdsSaveData.InterstitialCount + SuAds.AdsSaveData.RewardedInterstitialCount;
                if(total == 3)
                {
                    //LogManager.Log("log inter 3");
                    //SuGame.Get<SuAdjust>().LogEvent(EventName.Interstitial_3);
                    SuGame.Get<SuAdjust>().LogEvent(EventName.ads_purchase, SuAds.AdsSaveData.InterstitialRevenue, "USD");
                }  
                SuDefaultEvent.LogEventInterCount(total);
                break;
            case EventName.paid_ad_impression_video:
                SuAds.AdsSaveData.RewardedVideoCount++;
                SuAds.AdsSaveData.RewardedVideoRevenue += adValue.Value;
                ProcessAdsShow(adValue.Value);
                SuDefaultEvent.LogEvenRewardedCount(SuAds.AdsSaveData.RewardedVideoCount);
                break;
            case EventName.paid_ad_impression_app_open:
                SuAds.AdsSaveData.AppOpenCount++;
                SuAds.AdsSaveData.AppOpenRevenue += adValue.Value;
                break;
            case EventName.paid_ad_impression_rewarded_inter:
                SuAds.AdsSaveData.RewardedInterstitialCount++;
                SuAds.AdsSaveData.RewardedInterstitialRevenue += adValue.Value;
                SuDefaultEvent.LogEventInterCount(SuAds.AdsSaveData.InterstitialCount + SuAds.AdsSaveData.RewardedInterstitialCount);
                break;
        }
    }

    private void ProcessAdsShow(double adValue)
    {
        SuAds.AdsSaveData.AdsShowed5 += adValue;
        SuAds.AdsSaveData.AdsShowedCount5++;
        if (SuAds.AdsSaveData.AdsShowedCount5 >= 5)
        {
            LogEventAdsShowed(5, SuAds.AdsSaveData.AdsShowed5);
            SuAds.AdsSaveData.AdsShowed5 = 0; // reset after log
            SuAds.AdsSaveData.AdsShowedCount5 = 0; // reset after log
        }

        SuAds.AdsSaveData.AdsShowed6 += adValue;
        SuAds.AdsSaveData.AdsShowedCount6++;
        if (SuAds.AdsSaveData.AdsShowedCount6 >= 6)
        {
            LogEventAdsShowed(6, SuAds.AdsSaveData.AdsShowed6);
            SuAds.AdsSaveData.AdsShowed6 = 0; // reset after log
            SuAds.AdsSaveData.AdsShowedCount6 = 0; // reset after log
        }

        SuAds.AdsSaveData.AdsShowed8 += adValue;
        SuAds.AdsSaveData.AdsShowedCount8++;
        if (SuAds.AdsSaveData.AdsShowedCount8 >= 8)
        {
            LogEventAdsShowed(8, SuAds.AdsSaveData.AdsShowed8);
            SuAds.AdsSaveData.AdsShowed8 = 0; // reset after log
            SuAds.AdsSaveData.AdsShowedCount8 = 0; // reset after log
        }

        SuAds.AdsSaveData.AdsShowed9 += adValue;
        SuAds.AdsSaveData.AdsShowedCount9++;
        if (SuAds.AdsSaveData.AdsShowedCount9 >= 9)
        {
            LogEventAdsShowed(9, SuAds.AdsSaveData.AdsShowed9);
            SuAds.AdsSaveData.AdsShowed9 = 0; // reset after log
            SuAds.AdsSaveData.AdsShowedCount9 = 0; // reset after log
        }

        SuAds.AdsSaveData.AdsShowed10 += adValue;
        SuAds.AdsSaveData.AdsShowedCount10++;
        if (SuAds.AdsSaveData.AdsShowedCount10 >= 10)
        {
            LogEventAdsShowed(10, SuAds.AdsSaveData.AdsShowed10);
            SuAds.AdsSaveData.AdsShowed10 = 0; // reset after log
            SuAds.AdsSaveData.AdsShowedCount10 = 0; // reset after log
        }

        SuAds.AdsSaveData.AdsShowed20 += adValue;
        SuAds.AdsSaveData.AdsShowedCount20++;
        if (SuAds.AdsSaveData.AdsShowedCount20 >= 20)
        {
            LogEventAdsShowed(20, SuAds.AdsSaveData.AdsShowed20);
            SuAds.AdsSaveData.AdsShowed20 = 0; // reset after log
            SuAds.AdsSaveData.AdsShowedCount20 = 0; // reset after log
        }
    }    
    private const string admob = "admob_sdk";
    private const string max = "applovin_max_sdk";
    private const string ironSource = "ironsource_sdk";
    void LogAdjustImpression(SuAdsAdValue adInfo)
    {
        string network = adInfo.Network;
        double revenue = adInfo.Value;
        Dictionary<string, string> dataDict = new Dictionary<string, string>
                    {
                        { Precision, adInfo.Precision },
                    };
        // log impression level revenue
        string adSource = admob;
        switch (adInfo.Mediation_Platform)
        {
            case AdsNetwork.admob:
                adSource = admob;
                break;
            case AdsNetwork.max:
                adSource = max;
                break;
            case AdsNetwork.ironsource:
                adSource = ironSource;
                break;
        }
        SuGame.Get<SuAdjust>().LogRevenue(adSource, network, revenue, USD, 1, adInfo.Ad_Format, adInfo.UnitID, dataDict);

    }
}
