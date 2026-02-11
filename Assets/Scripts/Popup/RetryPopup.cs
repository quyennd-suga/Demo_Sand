using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class RetryPopup : PopupStateChange
{
    [SerializeField]
    private TextMeshProUGUI titleText;
    [SerializeField]
    private TextMeshProUGUI topDes;
    [SerializeField]
    private Image retryButtonImg;
    [SerializeField]
    private Sprite retrySprite;
    [SerializeField]
    private Sprite giveupSprite;
    [SerializeField]
    private GameObject retryText;
    [SerializeField]
    private GameObject giveupText;
    [SerializeField]
    private GameObject botDes;
    [SerializeField]
    private GameObject heartText;


    public void Close()
    {
        base.ClosePopup();
        SoundManager.PlaySound(SoundType.ClickButton);
        VibrateHandler.ButtonVibrate();

        if (GameController.GameState == EnumGameState.Idle)
        {
            GameController.GameState = EnumGameState.Playing;
        }
        else
        {
            GameController.Instance.OpenHome();
        }
        
    }
    public override void OpenPopup()
    {
        if (GameController.GameState == EnumGameState.Idle)
        {
            
        }
        else
        {
            LifeSystem.totalLives--;
        }
        base.OpenPopup();
        int heart = LifeSystem.totalLives;
        //Debug.Log(heart);
        if (heart >= 1)
        {
            if (GameController.GameState == EnumGameState.Idle)
                topDes.text = "Do you want to retry?";
            else
                topDes.text = "Failed";

            titleText.text = "Retry";
            retryButtonImg.sprite = retrySprite;
            heartText.SetActive(true);
            botDes.SetActive(true);
            retryText.SetActive(true);
            giveupText.SetActive(false);
        }
        else
        {
            titleText.text = "No Heart Left";
            topDes.text = "You run out of hearts";
            retryButtonImg.sprite = giveupSprite;
            heartText.SetActive(false);
            botDes.SetActive(false);
            retryText.SetActive(false);
            giveupText.SetActive(true);
        }
    }
    public void OkButton()
    {
        VibrateHandler.ButtonVibrate();
        base.ClosePopup();

        //if(GameEventManager.isWinstreakEvent)
        //{
        //    if(GameManager.gameState == GameState.Idle)
        //    {
        //        int level = GameEventManager.winStreak._winstreakManager.Level;
        //        if (level >= 1)
        //        {
        //            PopupManager.GetPopup(PopupType.LoseStreak).OpenWithAction(() =>
        //            {
        //                ProcessRetry();
        //            }, GameState.Idle);
        //        }
        //        else
        //        {
        //            ProcessRetry();
        //        }    
        //    }    
        //    else
        //    {
        //         ProcessRetry();
        //    }    
        //} 
        //else
        {
            ProcessRetry();
        }    
        
    }

    private void ProcessRetry()
    {
        int heart = LifeSystem.totalLives;
        GameController.Instance.topUI.ResetTimeCount();
        if(heart >= 1)
        {
            Retry();
        }
        else
        {
            LifeSystem.totalLives--;
            
            GameController.Instance.OpenHome();
        }
    }
    private void Retry()
    {
        if (GameController.GameState == EnumGameState.Idle)
        {
            LifeSystem.totalLives--;
            SuGame.Get<SuAnalytics>().LogEventResourceSpend(ResourceName.Heart, 1, LifeSystem.totalLives, ActionEarn.Retry, ActionCategory.Heart, BoosterType.None);
        }
        else
        {
            SoundManager.FadeInMusic(1f);
        }
        GameController.Instance.Replay();
    }
}
