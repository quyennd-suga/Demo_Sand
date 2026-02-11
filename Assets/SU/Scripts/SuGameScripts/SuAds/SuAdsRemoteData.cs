using Sirenix.OdinInspector;
using System.Collections.Generic;
[System.Serializable]
public struct Inter_Ad_Config
{
    public int level_start, level_end, capping_time;
    public string main_ads_id;
    public string sub_ads_id;
    public bool on;
    public int level_change_ads_id;
}
[System.Serializable]
public struct Rewared_Ad_Config
{
    public string rewarded_id;
    public int capping_time;
    public string rewarded_sub_id;
    public int level_change_id;
    public int level_start;
}
[System.Serializable]
public struct Rewarded_Inter_Ad_Config
{
    public int level_start, capping_time;
    public string rewarded_inter_id;
}
[System.Serializable]
public struct Loading_Config
{
    public float loading_time;
}
[System.Serializable]
public struct Banner_Ad_Config
{
    public int level_show;
    public bool enable_collapsible;
    public string banner_id;
    public int delay_banner;
    public string collapsiblebanner_id;
    public string bigbanner_id;
}
[System.Serializable]
public struct Open_Ad_Config
{
    public bool on;
    public int start_level;
    public int capping_time, time_in_background;
    public string openads_id;
}
[System.Serializable]
public struct Rating_Config
{
    public int level_show;
    public int show_each;
    public bool on;
}
[System.Serializable]
public struct ForceWifi_Config
{
    public bool on;
}

[System.Serializable]
public struct Ads_Network_Config
{
    public int id;
    public List<string> ads_networks;
}

[System.Serializable]
public struct General_Config
{
    public int id;
    public int difficult_level;
    public int reward_button_config;
    public float delay_display;
    public Rating_Config rating_config;
    public Back_Home back_home;
    public ForceWifi_Config force_wifi;
    public Loading_Config loading_config;
    public Swap_Config[] swap_config;
    public Freeze_Config freeze_config;
    public Move_Config move_config;
    public Time_Config[] time_config;
    public int life_config;
    public Coin_Config coin_config;
    public Notification_Config notification_config;
    public Heart_Offers heart_offers;
    public TimeOut_Config timeout_config;
    public Special_Offers special_offer;
}
[System.Serializable]
public struct Back_Home
{
    public bool on;
}
[System.Serializable]
public struct Swap_Config
{
    public int level;
    public int position;
    public int difficulty_level;
}
[System.Serializable]
public struct Time_Config
{
    public int level;
    public int time;
}
[System.Serializable]
public struct Move_Config
{
    public int move;
}
[System.Serializable]
public struct Notification_Config
{
    public bool on;
    public long period;
    public int number_of_times;
    public string[] messages;
    public Heart_Notification heart_notification;
}
[System.Serializable]
public struct Heart_Notification
{
    public bool on;
    public int heart;
    public string title;
    public string desc;
    public int time;
}
[System.Serializable]
public struct Freeze_Config
{
    public int time;
}
[System.Serializable]
public struct Coin_Config
{
    public int coin_reward;
    public int coin_reward_hard;
    public int coin_reward_superhard;
    public int scissors_price;
    public int freeze_price;
    public int shuffle_price;
    public int move_price;
    public Time_Out_Price time_out_price;
}
[System.Serializable]
public struct Time_Out_Price
{
    public int first;
    public int addition;
}
[System.Serializable]
public struct Heart_Offers
{
    public int free_heart_cooldown;
    public int free_heart_time;
    public int purchase_heart_time;
}
[System.Serializable]
public struct TimeOut_Config
{
    public bool ads_continue;
    public int time;
    public int ads_time;
    public int bonus_time;
    public int moves;
    public int bonus_moves;
}
[System.Serializable]
public struct Special_Offers
{
    public int level;
    public int show_each;
}


//[System.Serializable]
//public class Event_Config
//{
//    public EventDataModule[] event_config;
//}


//[System.Serializable]
//public class WinstreakConfig
//{
//    public List<WinstreakConfigModule> dataList;
//}

//[System.Serializable]
//public struct WinstreakConfigModule
//{
//    public int level;
//    public List<WinstreakConfigGift> items;
//}
//[System.Serializable]
//public struct WinstreakConfigGift
//{
//    public int value;
//    public string type;
//}


