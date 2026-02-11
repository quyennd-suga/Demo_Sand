using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public class ItemManager : MonoBehaviour
{
    public static Action<ItemType,int> onClaimItem;

    [SerializeField]
    private UIFlyImage uiFlyImage;
    [SerializeField]
    private WarningLevel warningLevel;
    [SerializeField]
    TextMeshProUGUI timeText;
    [SerializeField]
    private Image timeBoard;
    [SerializeField]
    private Image[] buttonsTop;
    [SerializeField]
    private EmitFromUIBorder freezeItemEffect;
    [SerializeField]
    private GameObject movesItemObject;
    [SerializeField]
    private RectTransform[] itemRects;
    [SerializeField]
    private Transform highlightFreeze;
    [SerializeField]
    private Transform itemParent;
    [SerializeField]
    private Image timeBar;
    [SerializeField]
    private Transform tutorial;
    [SerializeField]
    private Transform handTut;

    public static bool usingItem;
    public static bool isOpen;
    public static bool lockBoosters;
    public static Action onLockBooster;



    private static readonly Dictionary<ItemType, ItemBase> items = new();

    private void Awake()
    {
        items.Clear();
        foreach (var it in GetComponentsInChildren<ItemBase>(true))
        {
            if (items.ContainsKey(it.itemType))
                Debug.LogError($"Duplicate ItemType {it.itemType} on {it.name}");
            else
                items.Add(it.itemType, it);
        }
    }

    private void OnEnable()
    {
        UnlockItemPopup.onUnlockItem += OnUnlockItemAction;
        ItemBase.onUseItem += UseItem;
        onLockBooster += OnLockBoosters;
        onClaimItem += ClaimItem;
    }
    private void OnDisable()
    {
        onClaimItem -= ClaimItem;
        onLockBooster -= OnLockBoosters;
        ItemBase.onUseItem -= UseItem;
        UnlockItemPopup.onUnlockItem -= OnUnlockItemAction;
    }

    public static bool TryGetItem(ItemType type, out ItemBase item) => items.TryGetValue(type, out item);

    
    
    private void ConfigWarningLevel()
    {
        for(int i = 0; i < buttonsTop.Length; i++)
        {
            buttonsTop[i].sprite = DataContainer.Instance.itemData.GetButtonOnSprite(LevelManager.levelData.levelDifficult,false);
        }    
        if(LevelManager.levelData.levelDifficult == LevelDifficult.Hard)
        {
            warningLevel.HardWarning();
        }    
        else if(LevelManager.levelData.levelDifficult == LevelDifficult.SuperHard)
        {
            warningLevel.SuperHardWarning();
        }
        else
        {
            LevelManager.CheckLevelUnlockItem();
        }    
    }    
    public static void ClaimItem(ItemType type, int numb)
    {
        if(TryGetItem(type, out var item))
        {
            item.OnClaimItem(type, numb);
        }
    }
    
    private void OnLockBoosters()
    {
        if(isOpen)
        {
            isOpen = !isOpen;
            StopAllCoroutines();
            UseFreeze(isOpen);
        }
    }
    private void OnUnlockItemAction(ItemType type,bool isLevelUnlock)
    {
        //itemUnlockEffect.StartEffect(type);
        levelLock = isLevelUnlock;
        if(isLevelUnlock)
        {
            //tutorial.gameObject.SetActive(true);
            Sprite sprite = DataContainer.Instance.itemData.GetItemUnit(type).smallIconSprite;
            uiFlyImage.Play(sprite, 0.7f, false, Mathf.Clamp((int)type, 0, 2));
            GameController.GameState = EnumGameState.UsingBooster;
            int id = Mathf.Clamp((int)type,0, 2);
            //itemUnlockEffect.targetPoints[id].parent.SetParent(tutorial);
            //itemUnlockEffect.targetPoints[id].parent.SetAsFirstSibling();
            //Vector3 pos = itemUnlockEffect.targetPoints[id].transform.position;
            //handTut.transform.position = new Vector3(pos.x, pos.y, pos.z + 0.5f);
        }
    }
    private bool levelLock;

    public void UseItem(ItemType type)
    {
        if(levelLock)
        {
            tutorial.gameObject.SetActive(false);
        }
        isOpen = !isOpen;
        switch(type)
        {
            case ItemType.Freeze:
                UseFreeze(isOpen);
                break;
        }
        
    }
    private void UseFreeze(bool isOpen)
    {
        highlightFreeze.gameObject.SetActive(isOpen);
        TopUI.isFreezed = isOpen;
        if (isOpen)
            GameController.GameState = EnumGameState.UsingBooster;
        else
            GameController.GameState = EnumGameState.Playing;
    }
    
       
    public void FreezeButton()
    {
        isOpen = false;
        
        StartCoroutine(FreezeTimeRoutine());
    }
    public static bool isFreezed;


    
    IEnumerator FreezeTimeRoutine()
    {
        SoundManager.PlaySound(SoundType.FreezeTime);
        isFreezed = true;
        usingItem = true;
        if(TryGetItem(ItemType.Freeze, out var item))
        {
            item.OnStartUseItem();
        }
        highlightFreeze.gameObject.SetActive(false);
        //numbText[1].SetParent(numbParent);
        //numbText[1].SetAsLastSibling();
        GameController.GameState = EnumGameState.Playing;
        
        float maxTime = GameController.generalConfig.freeze_config.time;
        float endTime = maxTime - 0.5f;
        float t = 0f;
        
        VibrateHandler.PlayPatternVibrate(Lofelt.NiceVibrations.HapticPatterns.PresetType.MediumImpact);
        freezeItemEffect.StartEmitting();
        timeBar.transform.parent.gameObject.SetActive(true);
        timeBar.fillAmount = 1f;
        t = 0f;
        while(t < maxTime)
        {
            if(isFreezed == false)
            {
                break;
            }
            if(GameController.GameState == EnumGameState.Playing)
            {
                timeBar.fillAmount = 1f - t / maxTime;
                t += Time.deltaTime;
            }    
            else
            {
                if (GameController.GameState == EnumGameState.Win)
                {
                    break;
                }    
            }    
            
            yield return null;
        }
        timeBar.transform.parent.gameObject.SetActive(false);
        TopUI.isFreezed = false;
        isFreezed = false;
        usingItem = false;
        
        item.OnFinishUseItem();

        ItemModule freezeItem = DataManager.data.GetItemModule(ItemType.Freeze);
        SuGame.Get<SuAnalytics>().LogEventResourceSpend(ResourceName.Freeze_Booster, 1, freezeItem.Numb, ActionEarn.Use_Booster,ActionCategory.Booster,BoosterType.UseBooster);
    }
}
