//using GoogleMobileAds.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuForceWifi : BaseSUUnit
{
    public GameObject popupNoInternetConnection;
    public override void Init(bool test)
    {
        isTest = test;
        
    }

    public void ForceWifi()
    {
        if (EnableSU == false)
            return;
        InvokeRepeating(nameof(CheckInternetConnection), 0.5f, 1F);
    }
    /*
    private void Awake()
    {
        //SuRemoteConfig.OnFetchComplete += OnRemoteConfigFetched;

    }
    */

    //private void Start()
    //{

    //    // tạo forceWifiData theo mặc định 
    //    //forceWifiData = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<ForceWifi_Config>(RemoteConfigName.force_wifi);

    //}
    /*
    private void OnRemoteConfigFetched(bool success)
    {
        // update lại theo data từ remote
        //forceWifiData = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<ForceWifi_Config>(RemoteConfigName.force_wifi);
    }
    */
    public static bool noInternet;

    void CheckInternetConnection()
    {
        
        if (GameController.generalConfig.force_wifi.on)
        {
            NetworkReachability newNetworkState = Application.internetReachability;
            if(newNetworkState == NetworkReachability.NotReachable)
            {
                noInternet = true;
                popupNoInternetConnection.SetActive(true);
            }
            else
            {
                noInternet = false;
                popupNoInternetConnection.SetActive(false);
            }
            
        }
        else
        {
            noInternet = false;
            popupNoInternetConnection.SetActive(false);
        }
    }

    /*
    // không làm kiểu này nữa vì 
    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (forceWifiData.turn_on)
            {
                // vì có tải lại quảng cáo nên thực hiện check ở frame tiếp theo tránh anr
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    CheckInternetConnection();
                    // check lại 1 lần nữa sau 4s vì khi bật lại wifi sẽ bị delay 1 lúc trạng thái mới thay đổi
                    Invoke(nameof(CheckInternetConnection), 4F);
                });
            }

        }
    }
    */
}
