using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupStateChange : BasePopup 
{
    [SerializeField]
    private Image backgroundImg;
    [SerializeField]
    private Image titleImg;
    //[SerializeField]
    //private Image x_buttonImg;

    public override void OpenPopup()
    {
        base.OpenPopup();
        ConfigPopup();
    }
    public void ConfigPopup()
    {
        //PopupDataUnit data = PopupManager.GetPopupData(LevelManager.levelData.levelDifficult);
        //backgroundImg.sprite = data.backgroundSprite;
        //titleImg.sprite = data.titleSprite;
        //x_buttonImg.sprite = data.x_sprite;
    }

}
