using System;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class GameController : Singleton<GameController>
{
    public TopUI topUI;
    public BoardView boardView;
    

    public static Board board;

    public int currentLevel = 1;

    
    private static EnumGameState gameState;

    public static EnumGameState GameState
    {
        get {
            return gameState;
        }
        set {
            gameState = value;
        }
    }

    public static Action<int> onSetupCamera;

    private bool isprocessWifiFailed = false;
    private void Start()
    {
        DataManager.LoadData();
        LevelManager.LoadLevel();
        onSetupCamera?.Invoke(LevelManager.levelData.width);
        GameState = EnumGameState.Playing;
        LoadLevel(currentLevel);



        SuRemoteConfig.OnFetchComplete += OnRemoteFetchComplete;
        generalConfig = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<General_Config>(RemoteConfigName.general_config);
        if (showBanner == false)
            StartCoroutine(ShowBanner());

        if (isprocessWifiFailed)
        {
            if (generalConfig.force_wifi.on)
                SuGame.Get<SuForceWifi>().ForceWifi();
        }
        if (generalConfig.notification_config.on)
        {
            SuGame.Get<SuNotification>().InitNotification();
        }
    }
    private bool showBanner = false;
    IEnumerator ShowBanner()
    {
        float delay = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Ads_Config>(RemoteConfigName.ads_config).banner.delay_banner;
        int timeCount = 0;
        while (SuGame.Get<SuRemoteConfig>().fetchComplete == false)
        {
            timeCount++;
            if (timeCount >= delay)
            {
                break;
            }
            yield return new WaitForSeconds(1f);
        }

        delay = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<Ads_Config>(RemoteConfigName.ads_config).banner.delay_banner;
        delay -= timeCount;
        while (delay > 0)
        {
            delay--;
            yield return new WaitForSeconds(1f);
        }
        showBanner = true;
        SuGame.Get<SuAds>().ShowBanner();
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            if (!SuGame.Get<SuAds>().LockAppOpenAds)
            {
                ScheduleRefillNotification();
            }
        }
    }
    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (!SuGame.Get<SuAds>().LockAppOpenAds)
            {
                CancelRefillNoti();
            }
        }
    }
    private void ScheduleRefillNotification()
    {
        if (generalConfig.notification_config.heart_notification.on == false)
            return;
        if (LifeSystem.totalLives > 0)
            return;
        string title = generalConfig.notification_config.heart_notification.title;
        string message = generalConfig.notification_config.heart_notification.desc;
        int waitTime = generalConfig.notification_config.heart_notification.time;
        int heart = generalConfig.notification_config.heart_notification.heart;
        if (heart <= 0)
            heart = 1;
        if (heart > 5)
            heart = 5;
        int time = (heart - 1) * DataManager.data.lifeTime + DataManager.data.remainsTime + waitTime;
        DateTime fireTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(time);
        SuGame.Get<SuNotification>().ScheduleRefillNotification(title, message, "", fireTime, true);
    }
    private void CancelRefillNoti()
    {
        if (generalConfig.notification_config.heart_notification.on == false)
            return;
        SuGame.Get<SuNotification>().CancelRefillNoti();
    }
    //public static bool isFetchedRemote;
    public void OnRemoteFetchComplete(bool isFetched)
    {
        Debug.Log("fetched: " + isFetched);
        if (isFetched)
        {
            //isFetchedRemote = true;
            generalConfig = SuGame.Get<SuRemoteConfig>().GetJsonValueAsType<General_Config>(RemoteConfigName.general_config);
            Debug.Log("Difficult level: " + generalConfig.difficult_level);
            DataManager.data.difficult_level = generalConfig.difficult_level;
            DataManager.data.loadingTime = generalConfig.loading_config.loading_time;
            DataManager.data.lifeTime = generalConfig.life_config * 60;
            if (generalConfig.force_wifi.on)
                SuGame.Get<SuForceWifi>().ForceWifi();
        }
        else
        {
            isprocessWifiFailed = true;
            if (generalConfig.force_wifi.on)
                SuGame.Get<SuForceWifi>().ForceWifi();
        }
    }
    public static General_Config generalConfig;
    public void LoadLevel(int level)
    {
        topUI.ConfigLevel();
        GenerateBoard();
    }    
    void GenerateBoard()
    {
        board = new Board(LevelManager.levelData);


        boardView.SpawnAll(LevelManager.levelData, board);
    }

    public void OnLevelComplete()
    {
        Debug.Log("Level Complete!");
        // Handle level completion logic here (e.g., show UI, load next level)
    }


    public void Replay()
    {
        GameState = EnumGameState.Playing;
        topUI.ConfigLevel();
        board = new Board(LevelManager.levelData);
        boardView.SpawnAll(LevelManager.levelData, board);
        //ItemManager.isFreezed = false;


        Debug.Log("Level Restarted!");

    }

    public void OpenHome()
    {

    }    

}


public enum EnumGameState
{
    Idle,
    Home,
    Playing,
    UsingBooster,
    Win,
    Lose
}
