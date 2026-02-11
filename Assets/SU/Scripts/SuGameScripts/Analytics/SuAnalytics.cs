using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class SuAnalytics : BaseSUUnit
{
    static bool firebaseInitialized = false;
    public override void Init(bool isTest)
    {
        // call after check dependency success 
        firebaseInitialized = false;
        
    }

    void LogExam()
    {

    }
    private readonly int CONSENT_PURPOSE_1 = 1;
    private readonly int CONSENT_PURPOSE_3 = 3;
    private readonly int CONSENT_PURPOSE_4 = 4;
    private readonly int CONSENT_PURPOSE_7 = 7;
    public void CheckConsentStatus(string purposeConsents)
    {
        bool ad_storage = false;
        bool ad_user_data = false;
        bool ad_personalization = false;
        bool analytics_storage = false;

        if (string.IsNullOrEmpty(purposeConsents))
        {
            ad_storage = true;
            ad_user_data = true;
            ad_personalization = true;
            analytics_storage = true;
        }
        else
        {
            ad_storage = IsPurposeConsent(CONSENT_PURPOSE_1, purposeConsents);
            ad_user_data = IsPurposeConsent(CONSENT_PURPOSE_1, purposeConsents) && IsPurposeConsent(CONSENT_PURPOSE_7, purposeConsents);
            ad_personalization = IsPurposeConsent(CONSENT_PURPOSE_3, purposeConsents) && IsPurposeConsent(CONSENT_PURPOSE_4, purposeConsents);
            analytics_storage = IsPurposeConsent(CONSENT_PURPOSE_4, purposeConsents);
        }    
        
        Dictionary<ConsentType, ConsentStatus> consentStatus = new Dictionary<ConsentType, ConsentStatus>
        {
            { ConsentType.AdStorage, ad_storage ? ConsentStatus.Granted : ConsentStatus.Denied },
            { ConsentType.AdUserData, ad_user_data ? ConsentStatus.Granted : ConsentStatus.Denied },
            { ConsentType.AdPersonalization, ad_personalization ? ConsentStatus.Granted : ConsentStatus.Denied },
            { ConsentType.AnalyticsStorage, analytics_storage ? ConsentStatus.Granted : ConsentStatus.Denied }
        };

        FirebaseAnalytics.SetConsent(consentStatus);
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(analytics_storage);
        Crashlytics.IsCrashlyticsCollectionEnabled = true;

        if (SuGame.haveDependencies == true)
        {
            InitializeFirebaseAnalytics();
        }
    }

    private bool IsPurposeConsent(int purpose, string purposeConsents)
    {
        return purposeConsents[purpose - 1] == '1';
    }
    void InitializeFirebaseAnalytics()
    {
        LogManager.Log("Enabling data collection.");
        //FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

        LogManager.Log("Set user properties.");
        // Set the user ID.
        string userID = "";
#if UNITY_ANDROID
        userID = SystemInfo.deviceUniqueIdentifier;
#elif UNITY_IOS
            userID = UnityEngine.iOS.Device.vendorIdentifier;
#endif
        FirebaseAnalytics.SetUserId(userID);
        firebaseInitialized = true;
        LogManager.Log("Init firebase analytics success ");
    }

    public void LogEvent(string eventName, params Param[] _params)
    {
        if (!firebaseInitialized)
        {
            LogManager.Log("Can't log");
            return;
        }
        if (_params == null)
        {
            FirebaseAnalytics.LogEvent(eventName.ToString());
            return;
        }
        double revenue = 0;
        List<Parameter> pr = new List<Parameter>();
        foreach (Param _pr in _params)
        {
            /* bản cũ log theo đúng value 
            if (_pr.value is int)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (uint)_pr.value));
            }
            else if (_pr.value is uint)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (uint)_pr.value));
            }
            else if (_pr.value is string)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), _pr.value.ToString()));
            }
            else if (_pr.value is double)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (double)_pr.value));
            }
            else if (_pr.value is float)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (float)_pr.value));
            }
            else if (_pr.value is long)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (long)_pr.value));
            }
            else
            {
                LogManager.Log("special type : " + _pr.value.GetType());
                pr.Add(new Parameter(_pr.paramName.ToString(), _pr.value.ToString()));
            }
            */

            // bản mới log tất cả là string để có thể view được trên firebase analytics khi tạo Custom definitions / metrics
            pr.Add(new Parameter(_pr.paramName.ToString(), _pr.value.ToString()));
            if (_pr.paramName == ParaName.Revenue)
            {
                revenue = double.Parse(_pr.value.ToString());
            }
        }


        LogManager.Log("**********************************************  Log event " + eventName);
        FirebaseAnalytics.LogEvent(eventName.ToString(), pr.ToArray());
        SuGame.Get<SuAdjust>().LogEvent(eventName, revenue, "USD", _params);
    }

    public void LogEvent(EventName eventName, ParaName paramName, object value)
    {
        if (!firebaseInitialized)
        {
            LogManager.Log("Can't log");
            return;
        }
        Parameter[] parameters = { new Parameter(paramName.ToString(), value.ToString()) };
        FirebaseAnalytics.LogEvent(eventName.ToString(), parameters);
    }
    public void LogEvent(EventName eventName, params Param[] _param)
    {
        //Debug.Log("LogEvent " + eventName.ToString());
        if (!firebaseInitialized)
        {
            LogManager.Log("Can't log");
            return;
        }
        double revenue = 0;
        List<Parameter> pr = new List<Parameter>();
        foreach (Param _pr in _param)
        {
            /*
            if (_pr.value is int)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (uint)_pr.value));
            }
            else if (_pr.value is uint)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (uint)_pr.value));
            }
            else if (_pr.value is string)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), _pr.value.ToString()));
            }
            else if (_pr.value is double)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (double)_pr.value));
            }
            else if (_pr.value is float)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (float)_pr.value));
            }
            else if (_pr.value is long)
            {
                pr.Add(new Parameter(_pr.paramName.ToString(), (long)_pr.value));
            }
            else
            {
                LogManager.Log("Special type : " + _pr.value.GetType());
                pr.Add(new Parameter(_pr.paramName.ToString(), _pr.value.ToString()));
            }
            */
            pr.Add(new Parameter(_pr.paramName.ToString(), _pr.value.ToString()));
            if (_pr.paramName == ParaName.Revenue)
            {
                revenue = double.Parse(_pr.value.ToString());
            }
        }


        //LogManager.Log("Log firebase event : " + eventName.ToString());
        FirebaseAnalytics.LogEvent(eventName.ToString(), pr.ToArray());
        SuGame.Get<SuAdjust>().LogEvent(eventName, revenue, "USD", _param);
       
    }


    //--------------------- Nhóm log cho cc theo dõi tỉ lệ và thời điểm user bỏ ---------------------------

    public void LogEventLevelStart(int level)
    {
        if (firebaseInitialized)
        {
            string level_mode = LevelMode.Normal.ToString();
            Parameter[] LevelStartParameters = {
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter("level_mode",level_mode)
            };
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart, LevelStartParameters);
        }
    }

    public void LogEventLevelEnd(int level, bool success, FailedReason reason)
    {
        if (firebaseInitialized)
        {
            string level_mode = LevelMode.Normal.ToString();
            Parameter[] LevelEndParameters = {
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter("level_mode",level_mode),
            new Parameter(FirebaseAnalytics.ParameterSuccess, success.ToString()),
            new Parameter("reason", reason.ToString())
            };

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd, LevelEndParameters);
        }
    }


    public void LogEventResourceEarn(ResourceName resource_name, int amount, int balance, string actionEarn, string actionCategory, BoosterType booster)
    {
        if (firebaseInitialized)
        {
            int level = LevelManager.currentLevel;
            string level_mode = LevelMode.Normal.ToString();
            Parameter[] ResourceEarnParameters =
            {
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter("play_mode",level_mode.ToString()),
            new Parameter("name", resource_name.ToString()),
            new Parameter("amount", amount),
            new Parameter("balance", balance),
            new Parameter("item",actionEarn),
            new Parameter("item_type",actionCategory),
            new Parameter("booster",booster.ToString())
            };

            FirebaseAnalytics.LogEvent("resource_source", ResourceEarnParameters);
        }
    }
    public void LogEventResourceEarn(string resource_name, int amount, int balance, string actionEarn, string actionCategory, BoosterType booster)
    {
        if (firebaseInitialized)
        {
            int level = LevelManager.currentLevel;
            string level_mode = LevelMode.Normal.ToString();
            Parameter[] ResourceEarnParameters =
            {
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter("play_mode",level_mode.ToString()),
            new Parameter("name", resource_name),
            new Parameter("amount", amount),
            new Parameter("balance", balance),
            new Parameter("item",actionEarn),
            new Parameter("item_type",actionCategory),
            new Parameter("booster",booster.ToString())
            };

            FirebaseAnalytics.LogEvent("resource_source", ResourceEarnParameters);
        }
    }
    public void LogEventResourceSpend(ResourceName resource_name, int amount, int balance,ActionEarn actionSpend, ActionCategory actionCategory, BoosterType booster )
    {
        if (firebaseInitialized)
        {
            int level = LevelManager.currentLevel;
            string level_mode = LevelMode.Normal.ToString();
            Parameter[] ResourceSpendParameters =
            {
            new Parameter(FirebaseAnalytics.ParameterLevel, level),
            new Parameter("play_mode",level_mode.ToString()),
            new Parameter("name", resource_name.ToString()),
            new Parameter("amount", amount),
            new Parameter("balance", balance),
            new Parameter("item_type",actionCategory.ToString()),
            new Parameter("item",actionSpend.ToString()),
            new Parameter("booster",booster.ToString())
            };

            FirebaseAnalytics.LogEvent("resource_sink", ResourceSpendParameters);
        }
    }
    public void TrackPaidAdEvent(double value , string adFormat, int level, string placement, string ad_network)
    {
        Parameter[] AdRevenueParameters = {
            new Parameter(FirebaseAnalytics.ParameterValue, value),
            new Parameter(FirebaseAnalytics.ParameterCurrency, "USD"),
            //new Parameter("level_mode",level_mode),
            new Parameter("ad_format", adFormat),
            // not required (these are for level analytics)
            new Parameter(FirebaseAnalytics.ParameterLevel, level.ToString()),
            new Parameter("placement", placement),
            new Parameter("ad_network", ad_network)
        };

        FirebaseAnalytics.LogEvent("ad_revenue_sdk", AdRevenueParameters);
    }

    public void LogEventIAP(decimal value, string currency, int level, string product_id)
    {
        Parameter[] IAPRevenueParameters = {
            new Parameter(FirebaseAnalytics.ParameterLevel,level),
            new Parameter(FirebaseAnalytics.ParameterValue, value.ToString()),
            new Parameter( FirebaseAnalytics.ParameterCurrency, currency),
            new Parameter("product_id", product_id)
        };

        FirebaseAnalytics.LogEvent("iap_sdk", IAPRevenueParameters);
    }
    public void LogEventPurchase(decimal value, string currency)
    {
        Parameter[] IAPRevenueParameters = {
            new Parameter(FirebaseAnalytics.ParameterValue, value.ToString()),
            new Parameter( FirebaseAnalytics.ParameterCurrency, currency)
        };

        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase, IAPRevenueParameters);
    }    
    /*
    public static void TrackingScreen(ScreenName screen_view)
    {
        LogManager.Log("Track_Screen " + screen_view);
        LogEvent(EventName.tracking_screen,
            new Param(ParaName.level_id, GameManager.CurrentLevel),
            new Param(ParaName.mode_id, GameManager.CurrentGameMode.ToString()),
            new Param(ParaName.screen_view, screen_view.ToString()));
    }
    */




}

public class Param
{
    public ParaName paramName;
    public object value;
    public Param(ParaName _name, uint _value)
    {
        paramName = _name;
        value = _value;
    }
    public Param(ParaName _name, string _value)
    {
        paramName = _name;
        value = _value;
    }
    public Param(ParaName _name, float _value)
    {
        paramName = _name;
        value = _value;
    }
    public Param(ParaName _name, double _value)
    {
        paramName = _name;
        value = _value;
    }
    public Param(ParaName _name, long _value)
    {
        paramName = _name;
        value = _value;
    }
}
public enum ResourceName
{
    Coin,
    Heart,
    Immortal_Life,
    Time,
    Freeze_Booster,
    Pump_Booster,
    Expand_Booster,
    Hammer_Booster,
    SkipAds_Ticket,
    Remove_Ads,
    Remove_Ads_24h,
    Skin,
}
public enum LevelMode
{
    Normal,
    Moves, 
    Time
}

public enum FailedReason
{
    OutOfMove,
    OutOfTime,
    Win
}

public enum ActionEarn
{
    None,
    FreeTreasurePack,
    PurchaseTreasurePack,
    PurchaseShopBundle,
    PurchaseStarterPack,
    PurchaseHeartOffer,
    FreeHeartOffer,
    RewardOutOfLife,
    RefillHeart,
    RewardTimeOut,
    RewardX2Coin,
    UnlockBooster,
    PurchaseSkinPack,
    RopeOffer,
    PinOffer,
    BackgroundOffer,
    WinLevel,
    Use_Booster,
    Purchase_Booster,
    Continue_Play,
    Buy_Skin,
    Give_Up,
    Retry,
    Special_Offer,
    BattlePass_Reward,
    Finish_Race_Reward,
    Collection_Reward,
}
public enum ActionCategory
{
    PurchaseIAP,
    VideoReward,
    UseCoin,
    FreeReward,
    Booster,
    Purchase_With_Coin,
    Heart,
    BattlePassReward,
    RaceReward,
    CollectionReward,
}
public enum BoosterType
{
    PurchaseIAP,
    RewardVideo,
    UseCoin,
    UnlockReward,
    UseBooster,
    None
}


