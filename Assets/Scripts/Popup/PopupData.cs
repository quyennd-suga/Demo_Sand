using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PopupData", menuName = "ScriptableObjects/SpawnPopupData", order = 1)]
public class PopupData : ScriptableObject
{
    public PopupDataUnit[] popupDatas;

    public PopupDataUnit GetPopupData(LevelDifficult state)
    {
        for(int i = 0; i < popupDatas.Length; i++)
        {
            if (popupDatas[i].state == state)
                return popupDatas[i];
        }
        return popupDatas[0];
    }
}

[System.Serializable]
public class PopupDataUnit
{
    public LevelDifficult state;
    public Sprite backgroundSprite;
    public Sprite titleSprite;
    //public Sprite x_sprite;
    public Sprite banner;
    public Sprite x_button_banner;
}