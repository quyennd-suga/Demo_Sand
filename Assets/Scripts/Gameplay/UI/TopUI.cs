using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;
using Spine.Unity;

public class TopUI : MonoBehaviour
{
  
    //[SerializeField]
    //private Image banner;
    //[SerializeField]
    //private Image x_button_banner;
    //[SerializeField]
    //private SkeletonGraphic clockAnim;
    //[SerializeField]
    //private GameObject timeBoard;
    //[SerializeField]
    //private GameObject movesBoard;
    //[SerializeField]
    //GameObject warningIcon;
    //[SerializeField]
    //private Image warning;
    [SerializeField]
    private TextMeshProUGUI levelText;
    [SerializeField]
    private TextMeshProUGUI timeText;


    private static bool freezed;
    public static bool isFreezed
    {
        get
        {
            return freezed;
        }
        set
        {
            freezed = value;
        }
    }

    private int t;
    private int time
    {
        get
        {
            return t;
        }
        set
        {
            t = value;
            timeText.text = TimeSpan.FromSeconds(value).ToString(@"mm\:ss");
        }
    }

    //private int m;
    //public int moves
    //{
    //    get
    //    {
    //        return m;
    //    }
    //    set
    //    {
    //        m = value;
    //        if(value >= 0)
    //            moveText.text = "Moves " + value.ToString();
    //        if(value == 0)
    //        {
    //            StartCoroutine(CheckMoveLose());
    //        }
    //    }
    //}
    public void ConfigCollectionMode()
    {
        //itemCollector.gameObject.SetActive(true);
        timeText.rectTransform.anchoredPosition = new Vector2(24f, timeText.rectTransform.anchoredPosition.y);
    }    
    public void ConfigLevel()
    {
        isFreezed = false;
        isStarted = false;  
        int level = LevelManager.currentLevel;
        levelText.text = "Level " + level.ToString();

        time = LevelManager.levelData.time;
        
        //clockAnim.AnimationState.ClearTracks();
        //PopupDataUnit data = PopupManager.GetPopupData(LevelManager.levelState);
        //banner.sprite = data.banner;
        //x_button_banner.sprite = data.x_button_banner;
    }
    //public void RemoveAds()
    //{
    //    //if (isFreezed)
    //    //    return;
    //    GameController.GameState = GameState.Idle;
    //    SoundManager.PlaySound(SoundType.ClickButton);
    //    VibrateHandler.ButtonVibrate();
    //    PopupManager.GetPopup(PopupType.NoAds).OpenPopup();
    //}
    public void Setting()
    {
        if (GameController.GameState != EnumGameState.Playing)
            return;
        //if (isFreezed)
        //    return;
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();
        GameController.GameState = EnumGameState.Idle;
        PopupManager.OpenPopup(PopupType.Settings);
    }
    public void Replay()
    {
        if (GameController.GameState != EnumGameState.Playing)
            return;
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();

        if (GlobalValues.isTimeFree)
        {
            ResetTimeCount();
            PopupManager.OpenWitchAction(PopupType.ChangeScene, () =>
            {
                GameController.Instance.Replay();
            }, EnumGameState.Playing);
            
            return;
        }
        GameController.GameState = EnumGameState.Idle;
        PopupManager.OpenPopup(PopupType.Retry);
    }

    public void ResetTimeCount()
    {
        if (countTimeRoutine != null)
            StopCoroutine(countTimeRoutine);
        //warning.gameObject.SetActive(true);
        //Color col = warning.color;
        //warning.color = new Color(col.r, col.g, col.b, 0);
        //warningIcon.SetActive(false);
        //clockAnim.AnimationState.ClearTracks();
    }    
    public bool isLevelStart()
    {
        if(GlobalValues.isTimeFree)
            return true;
        return time < LevelManager.levelData.time;
    }
    public void AddExtraTime(int extraTime)
    {
        time = extraTime;
    }
    Coroutine countTimeRoutine;

    private bool isStarted = false;
    public void StartCountTime()
    {
        if (isStarted)
            return;
        isStarted = true;
        if (countTimeRoutine != null)
            StopCoroutine(countTimeRoutine);
        countTimeRoutine = StartCoroutine(CountTime());
    }
    
    private bool levelComplete;
    private string loop = "Animation1";
    //private string idle = "Idle";
    private string warningAnim = "Animation2";
    IEnumerator CountTime()
    {
        DataManager.data.lives--;

        //clockAnim.AnimationState.SetAnimation(0, loop, true);
        //warning.gameObject.SetActive(true);
        //Color col = warning.color;
        //warning.color = new Color(col.r,col.g,col.b, 0);
        //warningIcon.SetActive(false);
        Vector3 targetScale = Vector3.one * 1.03f;
        Vector3 normalScale = Vector3.one;
        while(time > 0 && (GameController.GameState == EnumGameState.Playing || GameController.GameState == EnumGameState.Idle || GameController.GameState == EnumGameState.UsingBooster))
        {
            timeText.transform.DOScale(targetScale, 0.15f).SetEase(Ease.OutQuad);
            if (time == 30 || time == 20 || time <= 10)
            {
                //clockAnim.AnimationState.SetAnimation(0, warningAnim, false);
                
                if (time == 30 || time == 20)
                {
                    VibrateHandler.ButtonVibrate();
                    //clockAnim.AnimationState.AddAnimation(1, loop, true, 1f);
                }
                if(time == 10)
                {
                    VibrateHandler.ButtonVibrate();
                    SoundManager.PlaySound(SoundType.TimeWarning);
                }
                //warning.DOFade(1f, 0.5f);
                //if (time < 12)
                //    warningIcon.SetActive(true);
                if(time < 11)
                    SoundManager.PlaySound(SoundType.TenSecLeft); 
            }
            yield return new WaitForSeconds(0.5f);
            
            

            
            //warning.DOFade(0f, 0.5f);
            timeText.transform.DOScale(normalScale, 0.15f).SetEase(Ease.InQuad);
            yield return new WaitForSeconds(0.5f);
            if (!isFreezed && GameController.GameState == EnumGameState.Playing && !loseFocus) //&& ItemManager.isFreezed == false)
                time--;

        }
        //warning.gameObject.SetActive(false);
        //warningIcon.gameObject.SetActive(false);
        //clockAnim.AnimationState.ClearTracks();
        
        //while(GameManager.collectingRope)
        //{
        //    yield return null;
        //}
        if(GameController.GameState == EnumGameState.Playing)
        {
            PopupManager.OpenPopup(PopupType.TimeOut);
        }

        //game over!!!
    }
    private bool loseFocus;
    private void OnApplicationFocus(bool focus)
    {
        loseFocus = !focus;
    }
}
