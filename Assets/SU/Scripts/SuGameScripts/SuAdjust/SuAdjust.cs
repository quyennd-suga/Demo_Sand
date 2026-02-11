using AdjustSdk;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Adjust))]
public class SuAdjust : BaseSUUnit
{
    private AdjustEnvironment environment = AdjustEnvironment.Production;
    [Header("----- ANDROID ---------------------------------------------------------------------------------------------------")]
    public string AppToken_Android;
    public AdjustEventTokenDB EventTokenDB_Android;
    [Space(10)]
    [Header("----- IOS ---------------------------------------------------------------------------------------------------")]
    public string AppToken_IOS;
    public AdjustEventTokenDB EventTokenDB_IOS;
    AdjustEventsConfig EventRemote;
    private Adjust _adjust;
    bool inited = false;

    
    private void Start()
    {
        RequestATT();
    }
    public void RequestATT()
    {
        Adjust.RequestAppTrackingAuthorization(ATTCallBack);
    }
    private void ATTCallBack(int status)
    {
        //Debug.Log("ATT status " + status);
        switch (status)
        {
            case 0:
                Debug.Log("The user has not responded to the access prompt yet.");
                break;
            case 1:
                Debug.Log("Access to app-related data is blocked at the device level.");
                break;
            case 2:
                Debug.Log("The user has denied access to app-related data for device tracking.");
                break;
            case 3:
                Debug.Log("The user has approved access to app-related data for device tracking.");
                break;
        }
    }
    private void OnRemoteConfigFetchCompleted(bool success)
    {

        EventRemote = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<AdjustEventsConfig>(RemoteConfigName.adjust_events_config);
        //Debug.Log("EventRemote là " + JsonUtility.ToJson(EventRemote));
        if (EventRemote != null)
        {
            // android 
            if (EventRemote.Android != null && EventRemote.Android.EventTokens != null)
            {
                for (int i = 0; i < EventRemote.Android.EventTokens.Count; i++)
                {
                    AdjustEventTokenModule evtk = EventRemote.Android.GetEventTokenAt(i);
                    if (evtk != null && !EventTokenDB_Android.EventTokenDict.ContainsKey(evtk.eventName))
                    {
                        EventTokenDB_Android.EventTokenDict.Add(evtk.eventName, evtk.token);
                        EventTokenDB_Android.EventTokens.Add(evtk);
                    }
                }
            }

            // ios 
            if (EventRemote.IOS != null && EventRemote.IOS.EventTokens != null)
            {
                for (int i = 0; i < EventRemote.IOS.EventTokens.Count; i++)
                {
                    AdjustEventTokenModule evtk = EventRemote.IOS.GetEventTokenAt(i);
                    if (evtk != null && !EventTokenDB_IOS.EventTokenDict.ContainsKey(evtk.eventName))
                    {
                        EventTokenDB_IOS.EventTokenDict.Add(evtk.eventName, evtk.token);
                        EventTokenDB_IOS.EventTokens.Add(evtk);
                    }
                }
            }

            // gắn app token và init lại nếu apptoken cũ chưa có
            if (string.IsNullOrEmpty(AppToken_Android))
            {
                AppToken_Android = EventRemote.Android.appToken;
            }
            if (string.IsNullOrEmpty(AppToken_IOS))
            {
                AppToken_IOS = EventRemote.IOS.appToken;
            }
            _adjust.appToken = Application.platform == RuntimePlatform.IPhonePlayer ? AppToken_IOS : AppToken_Android;
            if (!string.IsNullOrEmpty(_adjust.appToken) && !inited)
            {
                InitAdjust();
            }
        }

    }

    [SerializeField]
    private string FbAppId = "1171245586854364"; // Facebook App ID, replace with your actual ID
    void InitAdjust()
    {
#if UNITY_ANDROID && UNITY_2019_2_OR_NEWER
        Application.deepLinkActivated += (deeplink) =>
        {
            Adjust.ProcessDeeplink(new AdjustDeeplink(deeplink));
        };
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            // cold start and Application.absoluteURL not null so process deep link
            Adjust.ProcessDeeplink(new AdjustDeeplink(Application.absoluteURL));
        }
#endif
        

        AdjustConfig adjustConfig = new AdjustConfig(_adjust.appToken, _adjust.environment, (_adjust.logLevel == AdjustLogLevel.Suppress));
        adjustConfig.LogLevel = _adjust.logLevel;
        adjustConfig.IsSendingInBackgroundEnabled = _adjust.sendInBackground;
        adjustConfig.IsDeferredDeeplinkOpeningEnabled = _adjust.launchDeferredDeeplink;
        adjustConfig.DefaultTracker = _adjust.defaultTracker;
        adjustConfig.IsPreinstallTrackingEnabled = _adjust.preinstallTracking;
        adjustConfig.PreinstallFilePath = _adjust.preinstallFilePath;
        adjustConfig.IsCoppaComplianceEnabled = _adjust.coppaCompliance;
        adjustConfig.IsCostDataInAttributionEnabled = _adjust.costDataInAttribution;
        adjustConfig.IsPreinstallTrackingEnabled = _adjust.preinstallTracking;
        adjustConfig.IsAdServicesEnabled = _adjust.adServices;
        adjustConfig.IsIdfaReadingEnabled = _adjust.idfaReading;
        adjustConfig.IsLinkMeEnabled = _adjust.linkMe;
        adjustConfig.IsSkanAttributionEnabled = _adjust.skanAttribution;
        adjustConfig.FbAppId = FbAppId;
        //adjustConfig.DeferredDeeplinkDelegate += DeferredDeeplinkDelegate;


        Adjust.InitSdk(adjustConfig);
        
        inited = true;

    }  
    
    private void attributionChangedDelegate(AdjustAttribution obj)
    {
        Debug.Log("Adjust atribution changed " + JsonUtility.ToJson(obj));
    }

    private void EventFailureCallback(AdjustEventFailure obj)
    {
        Debug.LogError("Event Failed " + obj.GetJsonResponseAsString());
    }

    private void EventSuccessCallback(AdjustEventSuccess obj)
    {
        Debug.Log("Event Success " + obj.GetJsonResponseAsString());
    }

    private void SessionFailureCallback(AdjustSessionFailure obj)
    {
        Debug.Log("Adjust Session Failed " + obj.GetJsonResponseAsString());
    }

    private void SessionSuccessCallback(AdjustSessionSuccess obj)
    {
        Debug.Log("Adjust Session Success " + obj.GetJsonResponseAsString());
    }

    string GetTransactionID()
    {
        return System.DateTime.Now.Ticks.ToString();
    }
    public void LogRevenue(string adSource, string network, double revenue, string currencyCode, int impressionCount, string placement, string adUnit, Dictionary<string, string> addData)
    {
        AdjustAdRevenue adRevenue = new AdjustAdRevenue(adSource);
        adRevenue.SetRevenue(revenue, currencyCode);
        adRevenue.AdRevenueNetwork = network;
        adRevenue.AdImpressionsCount = impressionCount;
        adRevenue.AdRevenuePlacement = placement;
        adRevenue.AdRevenueNetwork = adUnit;

        foreach (KeyValuePair<string, string> pr in addData)
        {
            adRevenue.AddPartnerParameter(pr.Key, pr.Value);
        }
        Adjust.TrackAdRevenue(adRevenue);
    }

    public void LogEvent(EventName eventName, double revenue = 0, string currencyCode = "USD", params Param[] _param)
    {
        AdjustEventTokenDB tokenDB = Application.platform == RuntimePlatform.IPhonePlayer ? EventTokenDB_IOS : EventTokenDB_Android;
        string token = tokenDB.GetToken(eventName);
        if (token == "")
        {
            Debug.Log("Chưa set event token cho event name " + eventName + " này");
            return;
        }
        AdjustEvent ev = new AdjustEvent(token.ToString());
        foreach (Param _pr in _param)
        {
            ev.AddPartnerParameter(_pr.paramName.ToString(), _pr.value.ToString());
            ev.AddCallbackParameter(_pr.paramName.ToString(), _pr.value.ToString());
        }
        Debug.Log("Log Adjust event " + eventName.ToString() + "token : " + token + " rev là : " + revenue);
        ev.TransactionId = GetTransactionID();
        if (revenue > 0)
        {
            Debug.Log("Giá trị rev set cho event " + eventName + " là : " + revenue);
            ev.SetRevenue(revenue, currencyCode);
        }
        Adjust.TrackEvent(ev);

    }
    public void LogEvent(string eventName, double revenue = 0, string currencyCode = "USD", params Param[] _param)
    {
        AdjustEventTokenDB tokenDB = Application.platform == RuntimePlatform.IPhonePlayer ? EventTokenDB_IOS : EventTokenDB_Android;
        string token = tokenDB.GetToken(eventName);
        if (token == "")
        {
            Debug.Log("Chưa set event token cho event name " + eventName + " này");
            return;
        }
        AdjustEvent ev = new AdjustEvent(token.ToString());
        foreach (Param _pr in _param)
        {
            ev.AddPartnerParameter(_pr.paramName.ToString(), _pr.value.ToString());
            ev.AddCallbackParameter(_pr.paramName.ToString(), _pr.value.ToString());
        }
        Debug.Log("Log Adjust event " + eventName.ToString() + "token : " + token + " rev là : " + revenue);
        ev.TransactionId = GetTransactionID();
        if (revenue > 0)
        {
            Debug.Log("Giá trị rev set cho event " + eventName + " là : " + revenue);
            ev.SetRevenue(revenue, currencyCode);
        }
        Adjust.TrackEvent(ev);

    }



    public override void Init(bool test)
    {
        isTest = test;
        EventRemote = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<AdjustEventsConfig>(RemoteConfigName.adjust_events_config);
        inited = false;
        SuRemoteConfig.OnFetchComplete += OnRemoteConfigFetchCompleted;
        _adjust = GetComponent<Adjust>();
        if(test)
        {
            environment = AdjustEnvironment.Sandbox;
        }
        else
        {
            environment = AdjustEnvironment.Production;
        }
        _adjust.environment = environment;
        _adjust.appToken = Application.platform == RuntimePlatform.IPhonePlayer ? AppToken_IOS : AppToken_Android;
        if (!string.IsNullOrEmpty(_adjust.appToken) && !inited)
        {
            InitAdjust();
        }
    }
}

[System.Serializable]
public class AdjustEventsConfig
{
    public AF_PurchaseConfig af_purchase_config;
    public AdjustEventConfigModule Android, IOS;
}

[System.Serializable]
public class AF_PurchaseConfig
{
    public int bannerCount, interCount, rewardCount, nativeCount, appOpenCount;
    public float rev;
}

[System.Serializable]
public class AdjustEventConfigModule
{
    public string appToken;
    public List<string> EventTokens;
    public AdjustEventTokenModule GetEventTokenAt(int index)
    {
        string[] strSplit = EventTokens[index].Split('|');
        if (strSplit.Length >= 2)
        {
            bool parse = Enum.TryParse(strSplit[0], out EventName ev);
            if (parse)
            {
                AdjustEventTokenModule evmd = new AdjustEventTokenModule()
                {
                    eventName = ev,
                    token = strSplit[1]
                };
                return evmd;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }
}