using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/SpawnItemData", order = 2)]
public class ItemDataModule : ScriptableObject
{
    public Sprite buttonOff;
    public ButtonOnModule[] buttonOn;
    public ItemUnit[] items;
    public ItemUnlockData[] unlockDatas;

    public ItemUnit GetItemUnit(ItemType itemType)
    {
        for(int i = 0; i < items.Length; i++)
        {
            if(items[i].type == itemType)
            {
                return items[i];
            }
        }
        return items[0];
    }
    public Sprite GetButtonOnSprite(LevelDifficult state, bool isLock)
    {
        if(isLock)
            return buttonOff;
        for (int i = 0; i < buttonOn.Length; i++)
        {
            if (buttonOn[i].levelState == state)
                return buttonOn[i].sprite;
        }
        return buttonOn[0].sprite;
    }
    public Sprite GetBaseLight(LevelDifficult state)
    {
        for(int i = 0; i < buttonOn.Length; i++)
        {
            if (buttonOn[i].levelState == state)
                return buttonOn[i].baseLight;
        }
        return buttonOn[0].baseLight;
    }
    public Sprite GetTimeSprite(LevelDifficult state)
    {
        for(int i = 0; i < buttonOn.Length; i++)
        {
            if (buttonOn[i].levelState == state)
                return buttonOn[i].timeSprite;
        }
        return buttonOn[0].timeSprite;
    }

    public int GetUnlockLevel(ItemType type)
    {
        for(int i = 0; i < unlockDatas.Length; i++)
        {
            if (unlockDatas[i].type == type)
                return unlockDatas[i].levelUnlock;
        }
        return 1;
    }
    //public TMP_FontAsset GetTimeFont(LevelState state)
    //{
    //    for(int i = 0; i < buttonOn.Length; i++)
    //    {
    //        if (buttonOn[i].levelState == state)
    //            return buttonOn[i].timeFont;
    //    }
    //    return buttonOn[0].timeFont;
    //}
}

[System.Serializable]
public struct ItemUnit
{
    public ItemType type;
    //public int levelUnlock;
    public int price;
    public Sprite titleImg;
    public Sprite smallIconSprite;
    public Sprite bigIconSprite;
    public Sprite notUnlockIcon;
    public string description;
    public int unlockAmount;
}

[System.Serializable]
public struct ButtonOnModule
{
    public LevelDifficult levelState;
    public Sprite sprite;
    public Sprite timeSprite;
    public Sprite baseLight;
    //public TMP_FontAsset timeFont;
}
[System.Serializable]
public struct ItemUnlockData
{
    public ItemType type;
    public int levelUnlock;

}

