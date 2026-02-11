using System;
using System.Collections.Generic;
using UnityEngine;

public static class DataManager
{
    private const string DataKey = "waterout_data";
    public static DataModule data;

    // ADD: avoid frequent PlayerPrefs.Save calls when nothing changed

    public static void LoadData()
    {
        if (PlayerPrefs.HasKey(DataKey))
        {
            try
            {
                data = JsonUtility.FromJson<DataModule>(PlayerPrefs.GetString(DataKey));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LoadData failed, re-init default. Exception: {e}");
                InitializeDefaultData();
            }

            //if(data.playerData == null)
            //{
            //    Debug.Log("PlayerData is null, initializing default player data.");
            //    data.playerData = new PlayerDataSaveModule();
            //    data.playerData.Setup();
            //}
            //else
            //{
            //    data.playerData.Validate(); // Ensure player data is valid
            //}    
        }
        else
        {
            Debug.Log("No data found, initializing default data.");
            InitializeDefaultData();
        }

        if (data == null)
        {
            InitializeDefaultData();
        }

        // ADD: ensure lists/arrays exist & items are consistent
        data.Validate();

        SyncGameState();
        //SetupEventData();
        data.loginCount++;
        //SaveData(); // optional: keep behavior similar (loginCount persisted)
    }

    private static void InitializeDefaultData()
    {
        data = new DataModule
        {
            currentLevel = 1,
            lives = 5,
            lifeTime = 1800,
            sound = true,
            music = true,
            vibrate = true,
            totalCoin = 1000,
            loadingTime = 2f,
            items = new List<ItemModule>(),
            //playerData = new PlayerDataSaveModule(),
        };
        //data.playerData.Setup();

        data.Validate();
    }

    private static void SyncGameState()
    {
        CoinManager.totalCoin = data.totalCoin;
        LifeSystem.totalLives = data.lives;
        SoundManager.sound = data.sound;
        SoundManager.music = data.music;

        SuAds.IsRemoveAds = data.isRemoveAds;

        if (data.isRemoveAds24h)
        {
            CalculateNoAdsExpiry();
        }

        SuAds.IsRemoveAds24h = data.isRemoveAds24h;
    }

    private static void CalculateNoAdsExpiry()
    {
        if (data.startTimeNoAds24h == 0) return;

        double secondsPassed = DateTime.Now.Subtract(DateTime.FromBinary(data.startTimeNoAds24h)).TotalSeconds;
        if (secondsPassed > 86400)
        {
            data.isRemoveAds24h = false;
        }
    }

    public static void SaveData()
    {
        if (data == null) return;

        data.lastTime = DateTime.Now.ToBinary();
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(DataKey, json);
        PlayerPrefs.Save();
    }

    
}
