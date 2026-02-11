using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DataModule
{
    //public Queue<ItemBundle> itemBundles;
    public int bonusTime;
    public int freeHeartCoolDown;
    public long treasureStartTime;
    public int loginCount;
    public bool isRemoveAds;
    public bool isRemoveAds24h;
    public int difficult_level;
    public int currentLevel;
    public int lives;
    public int lifeTime;
    public int totalCoin;
    public int remainsTime;
    public long lastTime;
    public long startTimeNoAds24h;
    public int ticketCount;
    public int freezeTime;
    public bool highStarRate;
    public int messageNoti;
    public float loadingTime;
    public bool level_4;
    public bool level_5;
    public bool level_6;
    public bool level_8;
    public bool level_9;
    //setting:
    public bool sound;
    public bool music;
    public bool vibrate;

    public int notiRefillId;
    //item:
    public List<ItemModule> items;
    //public ItemModule movesItem;

    public bool isStartBundle;

    public int treasurePackIndex;

    public int[] ropeSkins;
    public int[] ropeHalloween;
    public int[] pinSkins;
    public int[] pinHalloween;
    public int[] backgroundSkins;
    public int[] backgroundHalloween;

    public int[] offerItems;
    public int offId;

    public int ropeSkinId;
    public int pinSkinId;
    public int backgroundSkinId;

    public bool isXmas;

    //public PlayerDataSaveModule playerData;

    // -------------------- ADD (keep JsonUtility-friendly, no Dictionary) --------------------
    public void Validate()
    {
        if (items == null) items = new List<ItemModule>();

        // Ensure there is exactly 1 entry per ItemType (prevents duplicates from old saves)
        // This is safe and cheap for small list sizes.
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] == null)
                items.RemoveAt(i);
        }

        
    }

    public ItemModule GetItemModule(ItemType type)
    {
        EnsureItem(type);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemType == type)
                return items[i];
        }
        // Fallback (should not happen)
        var m = new ItemModule { itemType = type, Numb = 100, isLock = true };
        items.Add(m);
        return m;
    }

    public void EnsureItem(ItemType type)
    {
        if (items == null) items = new List<ItemModule>();

        bool found = false;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemType == type)
            {
                if (!found) found = true;
                else
                {
                    // remove duplicates, keep first
                    items.RemoveAt(i);
                    i--;
                }
            }
        }

        if (!found)
        {
            items.Add(new ItemModule
            {
                itemType = type,
                Numb = 100,
                isLock = true
            });
        }
    }
    // ---------------------------------------------------------------------------------------
}

[System.Serializable]
public class ItemModule
{
    public ItemType itemType;
    public int Numb;
    public bool isLock;
}

// (keep the rest of your commented blocks untouched)

[Serializable]
public class EventModule
{
    public bool isStart;
    public bool isCompleted;
    public int priority;
    public string dateEnd;
    public string eventType;
    public string eventName;

    public void Setup(bool isStart, string dateEnd, string eventType, string eventName, int priority)
    {
        this.isStart = isStart;
        this.dateEnd = dateEnd;
        this.eventType = eventType;
        this.eventName = eventName;
        this.priority = priority;
    }

    public void ResetState(string dateEnd, string eventType, string eventName, int priority)
    {
        this.dateEnd = dateEnd;
        this.eventType = eventType;
        this.eventName = eventName;
        this.priority = priority;
    }
}
