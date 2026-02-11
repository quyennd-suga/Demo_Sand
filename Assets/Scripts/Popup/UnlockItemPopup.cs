using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UnlockItemPopup : PopupStateChange
{
    public static Action<ItemType,bool> onUnlockItem;
    [SerializeField]
    private Image iconImg;
    [SerializeField]
    private TextMeshProUGUI itemNameText;
    [SerializeField]
    private TextMeshProUGUI description;

    public static ItemType itemType;
    public static bool isLevelUnlock;
    public static bool isTreasureUnlock;
    public override void OpenPopup()
    {
        base.OpenPopup();
        GameController.GameState = EnumGameState.Idle;
        Config();
    }
    private void Config()
    {
        itemNameText.text = itemType.ToString();
        ItemUnit item = DataContainer.Instance.itemData.GetItemUnit(itemType);
        iconImg.sprite = item.bigIconSprite;
        description.text = item.description;
    }

    public override void ClosePopup()
    {
        base.ClosePopup();
        if(!isTreasureUnlock)
            GameController.GameState = EnumGameState.Playing;
        isTreasureUnlock = false;
    }
    public void Claim()
    {
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();
        ClosePopup();
          
        if(!isTreasureUnlock)
        {
            ItemManager.onClaimItem?.Invoke(itemType, DataContainer.Instance.itemData.GetItemUnit(itemType).unlockAmount);
            
            onUnlockItem?.Invoke(itemType, isLevelUnlock);

            ResourceName name = ResourceName.Freeze_Booster;
            ItemModule itemModule = DataManager.data.GetItemModule(itemType);
            int total = itemModule.Numb;
            switch (itemType)
            {
                case ItemType.Freeze:
                    name = ResourceName.Freeze_Booster;
                    break;
                case ItemType.Pump:
                    name = ResourceName.Pump_Booster;
                    break;
                case ItemType.Expand:
                    name = ResourceName.Expand_Booster;
                    break;
                case ItemType.Hammer:
                    name = ResourceName.Hammer_Booster;
                    break;
                default:
                    break;
            }

            int itemCount = DataContainer.Instance.itemData.GetItemUnit(itemType).unlockAmount;
            SuGame.Get<SuAnalytics>().LogEventResourceEarn(name, itemCount, total, ActionEarn.UnlockBooster.ToString(), ActionCategory.FreeReward.ToString(), BoosterType.UnlockReward);
        }
        isLevelUnlock = false;
    }
}
