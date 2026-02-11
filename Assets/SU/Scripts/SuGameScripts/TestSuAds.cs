using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSuAds : MonoBehaviour
{
#if SUGAME_VALIDATED
    public GameObject popupRate;

    private void Awake()
    {

    }
    public void ShowBanner()
    {

        SuGame.Get<SuAds>().ShowBanner();

    }

    public void HideBanner()
    {

        SuGame.Get<SuAds>().HideBanner();

    }

    public void ShowInterstitial()
    {

        SuGame.Get<SuAds>().ShowInterstitial(() =>
        {
            Debug.Log("Đã đóng interstitial");
        }, ActionShowAds.Test);

    }

    public void ShowVideoReward()
    {

        SuGame.Get<SuAds>().ShowRewardVideo(() =>
        {
            Debug.Log("Nhận thưởng");
        }, () =>
         {
             Debug.Log("Không có video");
         }, ActionShowAds.Test);

    }

    public void ShowRelatedApps()
    {
        SuGame.Get<SuRelatedApps>().ShowRelatedApps();
    }

    public void HideRelatedApps()
    {
        SuGame.Get<SuRelatedApps>().HideRelatedApps();
    }

    public void BuyRemoveAds()
    {
        SuGame.Get<SuInAppPurchase>().BuyProduct(IAPProductIDName.noads);
    }

    public void InCreaseBannerCount()
    {
        SuAds.AdsSaveData.InterstitialCount++;

    }
    public void ShowRate()
    {
        popupRate.SetActive(true);
       
    }

    public void ShowRewardedInterstitial()
    {
        SuGame.Get<SuAds>().ShowRewardedInterstitial((reward) =>
        {
            if (reward)
            {
                Debug.Log("Nhận thưởng");
            }
            else
            {
                Debug.Log("Không nhận thưởng");
            }
        },()=>{

        },
        ActionShowAds.Test);
    }

#endif


}
