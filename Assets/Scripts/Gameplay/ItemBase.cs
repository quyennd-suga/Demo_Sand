using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemBase : MonoBehaviour
{
    public ItemType itemType;

    public CanvasGroup canvasGroup;
    [SerializeField]
    private Animation anim;
    [SerializeField]
    private Image baseLight;
    [SerializeField]
    private Image iconImg;
    [SerializeField]
    private Image bgImg;
    [SerializeField]
    private GameObject activeObject;
    [SerializeField]
    private GameObject lockObject;
    [SerializeField]
    private GameObject numbObject;
    [SerializeField]
    private TextMeshProUGUI numbText;
    [SerializeField]
    private TextMeshProUGUI levelUnlockText;
    [SerializeField]
    private GameObject plusObject;
    

    public static System.Action<ItemType> onUseItem;
    [SerializeField]
    private bool locked;
    public bool isLocked
    {
        get
        {
            return locked;
        }
        set
        {
            locked = value;
            lockObject.SetActive(value);
            activeObject.SetActive(!value);
            //bgImg.sprite = value ? ItemManager.itemData.buttonOff : ItemManager.itemData.GetButtonOnSprite(LevelManager.levelData.levelDifficult);
            //iconImg.sprite = value ? ItemManager.itemData.GetItemUnit(itemType).notUnlockIcon : ItemManager.itemData.GetItemUnit(itemType).smallIconSprite;

            SaveLockState(value);
        }
    }
    [SerializeField]
    int numb;
    public int Numb
    {
        get
        {
            return numb;
        }
        set
        {
            numb = value;
            SaveItemRemain(value);
            numbText.text = value.ToString();
            numbObject.SetActive(value > 0);
            plusObject.SetActive(value <= 0);

        }
    }
    private void SaveLockState(bool value)
    {
        ItemModule itemModule = DataManager.data.GetItemModule(itemType);
        itemModule.isLock = value;
    }
    
    private void Start()
    {
        
        ConfigItem();
    }

    public void OnStartUseItem()
    {
        numbText.gameObject.SetActive(false);
        numbObject.SetActive(false);
        isOpen = false;
        baseLight.gameObject.SetActive(false);
        anim.Stop();
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

    }    
    private void ConfigItemByDifficulty()
    {
        bgImg.sprite = DataContainer.Instance.itemData.GetButtonOnSprite(LevelManager.levelData.levelDifficult, isLocked);
        baseLight.sprite = DataContainer.Instance.itemData.GetBaseLight(LevelManager.levelData.levelDifficult);
    }    
    public void OnFinishUseItem()
    {
        Numb--;
        numbObject.SetActive(true);
        numbText.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
    private void ConfigItem()
    {
        ItemModule itemModule = DataManager.data.GetItemModule(itemType);
        isLocked = itemModule.isLock;
        Numb = itemModule.Numb;
        levelUnlockText.text = "Level " + DataContainer.Instance.itemData.GetUnlockLevel(itemType).ToString();
    }
    public void OnClaimItem(ItemType type,int amount)
    {
        if (type != itemType)
            return;

        if(isLocked)
        {
            isLocked = false;
        }
        Numb += amount;
    }
    
    private void SaveItemRemain(int value)
    {
        ItemModule itemModule = DataManager.data.GetItemModule(itemType);
        itemModule.Numb = value;
    }

    public void OnClick()
    {
        if(isLocked)
        {
            return;
        }
        if(ItemManager.lockBoosters)
        {
            return;
        }
        VibrateHandler.ButtonVibrate();
        SoundManager.PlaySound(SoundType.ClickButton);
        if (Numb <= 0)
        {
            //PurchaseItemPopup.itemType = itemType;
            PopupManager.OpenPopup(PopupType.PurchaseItem);
        }
        else
        {
            //use item!!!
            UseItem();
        }
    }
    private bool isOpen;
    public virtual void UseItem()
    {
        isOpen = !isOpen;
        if(isOpen == true)
        {
            anim.Play();
            baseLight.gameObject.SetActive(true);
        }    
        else
        {
               anim.Stop();
            baseLight.gameObject.SetActive(false);
        }    
        onUseItem?.Invoke(itemType);
    }
}


