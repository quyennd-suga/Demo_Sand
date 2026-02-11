using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//[System.Serializable]
public abstract class BasePopup : MonoBehaviour
{
    public PopupType type;

    //public RectTransform boardRect;
    //public void ConfigBoardRect()
    //{
    //    if (type == PopupType.Win)
    //        return;
        
    //    float y = 94f;
    //    switch(type)
    //    {
    //        case PopupType.Settings:
    //            y = 487f;
    //            break;
    //        case PopupType.TimeOut:
    //            y = 461f;
    //            break;
    //        case PopupType.Retry:
    //            y = 461f;
    //            break;
    //        case PopupType.GiveUp:
    //            y = 461f;
    //            break;
    //        case PopupType.NoHeartLeft:
    //            y = 461f;
    //            break;
    //        case PopupType.Rate:
    //            y = 461f;
    //            break;
    //        case PopupType.PurchaseItem:
    //            y = 461f;
    //            break;
    //        case PopupType.UnlockItem:
    //            y = 487f;
    //            break;
    //    }
    //    boardRect.anchoredPosition = new Vector2(0, y);
    //}
    public virtual void OpenPopup()
    {
        PopupManager.SetPopupActive(type, true);
        gameObject.SetActive(true);
    }

    public virtual void ClosePopup()
    {
        PopupManager.SetPopupActive(type, false);
        gameObject.SetActive(false);
    }

    public virtual void OpenWithAction(Action nextAction, EnumGameState nextState)
    {
        PopupManager.SetPopupActive(type, true);
        gameObject.SetActive(true);
    }
}
