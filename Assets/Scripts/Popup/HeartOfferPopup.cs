using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//using static UnityEditor.Progress;

public class HeartOfferPopup : BasePopup
{
    [SerializeField]
    private TextMeshProUGUI heartPurchasePrice;
    [SerializeField]
    private TextMeshProUGUI freeHeartTime;
    [SerializeField]
    private TextMeshProUGUI purchaseHeartTime;
    [SerializeField]
    private CanvasGroup freeHeartCanvasGroup;
    [SerializeField]
    private GameObject freeHeartLoading;
    [SerializeField]
    private GameObject freeHeartShadow;
    [SerializeField]
    private GameObject rewardPopup;
    public static System.Action onPurchaseHeart;


    public override void ClosePopup()
    {
        base.ClosePopup();
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();
        rewardPopup.SetActive(false);
    }
    //public void CloseReward()
    //{
    //    rewardPopup.SetActive(false);
    //}
    public override void OpenPopup()
    {
        base.OpenPopup();
        freeHeartTime.text = "+" + GameController.generalConfig.heart_offers.free_heart_time.ToString() + " " + "m";
        heartPurchasePrice.text = SuGame.Get<SuInAppPurchase>().GetLocalizePrice(IAPProductIDName.heart_offer);
        string hour = "hours";
        if(GameController.generalConfig.heart_offers.purchase_heart_time == 1)
            hour = "hour";
        purchaseHeartTime.text = "+" + GameController.generalConfig.heart_offers.purchase_heart_time.ToString() + " " + hour;
        bool haveVideo = SuGame.Get<SuAds>().HaveReadyRewardVideo;
        onVideoAvailable(haveVideo);
        if (!haveVideo)
            StartCoroutine(CheckVideoAvailable());
    }
    //private int min;
    public void FreeHeartButton()
    {
        if(showingAds)
            return;
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();
        if (TicketManager.totalTicket > 0)
        {
            TicketManager.totalTicket--;
            showingAds = false;
            OnErnRewardVideo();
            SuGame.Get<SuAnalytics>().LogEventResourceSpend(ResourceName.SkipAds_Ticket, 1, TicketManager.totalTicket, ActionEarn.FreeHeartOffer, ActionCategory.VideoReward, BoosterType.RewardVideo);
        }  
        else
        {
            showingAds = true;
            SuGame.Get<SuAds>().ShowRewardVideo(OnErnRewardVideo, OnCloseAdsWithoutReward);
        }
        
    }
    public void BuyInfiniteHeart()
    {
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();
        SuGame.Get<SuInAppPurchase>().BuyProduct(IAPProductIDName.heart_offer,ActionCategory.PurchaseIAP.ToString(), () =>
        {
            ClosePopup();
            PopupManager.OpenPopup(PopupType.PurchaseSuccess);
        });
    }    
    private bool showingAds;
    private void OnErnRewardVideo()
    {
        showingAds = false;
        
        onPurchaseHeart?.Invoke();
        bool haveVideo = SuGame.Get<SuAds>().HaveReadyRewardVideo;
        onVideoAvailable(haveVideo);
        if (!haveVideo)
            StartCoroutine(CheckVideoAvailable());

        rewardPopup.SetActive(true);
        LevelMode levelMode = (LevelMode)GameController.generalConfig.difficult_level;
        SuGame.Get<SuAnalytics>().LogEventResourceEarn(ResourceName.Immortal_Life, GameController.generalConfig.heart_offers.free_heart_time, LifeSystem.freezeTime / 60, ActionEarn.FreeHeartOffer.ToString(), ActionCategory.VideoReward.ToString() , BoosterType.RewardVideo);
    }
    private void OnCloseAdsWithoutReward()
    {
        showingAds = false;
        bool haveVideo = SuGame.Get<SuAds>().HaveReadyRewardVideo;
        onVideoAvailable(haveVideo);
        if (!haveVideo)
            StartCoroutine(CheckVideoAvailable());
    }
    private void onVideoAvailable(bool haveVideo)
    {
        freeHeartCanvasGroup.blocksRaycasts = haveVideo;
        freeHeartShadow.SetActive(!haveVideo);
        freeHeartLoading.SetActive(!haveVideo);
    }
    IEnumerator CheckVideoAvailable()
    {
        while (gameObject.activeSelf && SuGame.Get<SuAds>().HaveReadyRewardVideo == false)
        {
            yield return new WaitForSeconds(1f);
        }
        onVideoAvailable(true);
    }
    public void Claim()
    {
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();
        rewardPopup.SetActive(false);
    }    
}
