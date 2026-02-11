[System.Serializable]
public enum EventName
{
    // các giá trị không được thay đổi
	// các event paid_ad phải viết thường để bên marketing đọc được số liệu
    paid_ad_impression_app_open,
    paid_ad_impression_banner,
    paid_ad_impression_interstitial,
    paid_ad_impression_video,
    paid_ad_impression_native,
    paid_ad_impression_rewarded_inter,

    Rating,
    Purchase_Success,
    Purchase_Fail,
    af_purchase,
    Interstitial_Expired,
    App_Open_Expired,
    VideoReward_Expired,
    tracking_screen,
    //-------------------------------
    Banner_5,
    Inter_10,
    Tuto_Completed,
    Level_Up_4,
    Level_Up_5,
    Level_Up_6,
    Level_Up_7,
    Level_Up_8,
    Level_Up_9,
    Interstitial_1,
    Interstitial_2,
    Interstitial_3,
    Interstitial_4,
    Interstitial_5,
    Rewarded_1,
    Rewarded_2,
    Rewarded_3,
    inapp_purchase,
    ads_purchase,
    Finish_Loading,
    AdsShowed5,
    AdsShowed6,
    AdsShowed8,
    AdsShowed9,
    AdsShowed10,
    AdsShowed20,
    Start_Race,
    Finish_Race,
    Start_Collection,
    Start_BattlePass,
    Claim_BattlePass,
    Claim_Collection,

}
[System.Serializable]
public enum ParaName
{
    //-- các giá trị không được thay đổi
	// các paraname viết thường là để cho bên marketing lấy được số liệu
    valuemicros,
    currency,
    precision,
    adunitid,
    network,
    Action_Show_Ads,
    Revenue,
    level_id,
    mode_id,
    screen_view,
    value,
    level,
    // -- Các para thường dùng -------------------------------
    Type,
    Level,      
    Star_Number,
    Value,   
    Count,
    ID,
    Reason,

}