using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class LifeProcess : MonoBehaviour
{
    [SerializeField]
    private GameObject buyHeartButton;
    [SerializeField]
    private TextMeshProUGUI infiniteHeartTime;
    [SerializeField]
    private GameObject infiniteIcon;
    [SerializeField]
    private GameObject addHeartButton;
    [SerializeField]
    private TextMeshProUGUI timeText;
    [SerializeField]
    private TextMeshProUGUI livesText;
    [SerializeField]
    private TextMeshProUGUI time_NoHeartLeft_text;

    //private int coolDownHeart;
    private int coolDownFreeHeart
    {
        get
        {
            return DataManager.data.freeHeartCoolDown; 
        }
        set
        {
            DataManager.data.freeHeartCoolDown = value;
            if (value > 0)
            {
                infiniteHeartTime.text = TimeSpan.FromSeconds(value).ToString(@"hh\:mm\:ss");
            }
            else
            {
                infiniteHeartTime.gameObject.SetActive(false);
                buyHeartButton.SetActive(true);
            }
        }
    }
    //private int t;
    private int time
    {
        get
        {
            return DataManager.data.remainsTime;
        }
        set
        {
            //t = value;
            DataManager.data.remainsTime = value;
            if (LifeSystem.totalLives < 5)
                timeText.text = TimeSpan.FromSeconds(value).ToString(@"mm\:ss");
            else
                timeText.text = "Max";
            //if(DataManager.data.lives <= 0)
                time_NoHeartLeft_text.text = TimeSpan.FromSeconds(value).ToString(@"mm\:ss");
        }
    }



    private void Start()
    {
        //time = DataManager.data.remainsTime;
        if (DataManager.data.lives < 5)
            CalculateLife();
        else
        {
            time = DataManager.data.lifeTime;
        }
        if(LifeSystem.freezeTime > 0)
        {
            CalculateTimeFreeze();
        }
        LifeSystem.onLifeChange += OnLifeChange;
        LifeSystem.onFreezeTime += StartFreezeTime;
        OnLifeChange(LifeSystem.totalLives);
        StartCoroutine(CountdownTime());
        HeartOfferPopup.onPurchaseHeart += OnPurchaseHeart;
        CalculateHeartFreeCoolDown();
    }
    private void CalculateHeartFreeCoolDown()
    {
        long lastTime = DataManager.data.lastTime;
        //Debug.Log("last login time: " + lastTime);
        if (lastTime == 0)
            return;

        //Debug.Log(DateTime.FromBinary(lastTime));
        double timePassInSec = DateTime.Now.Subtract(DateTime.FromBinary(lastTime)).TotalSeconds;
        if(coolDownFreeHeart > timePassInSec)
        {
            coolDownFreeHeart -= (int)timePassInSec;
        }    
        else
        {
            coolDownFreeHeart = 0;
        }   
        if(coolDownFreeHeart > 0)
        {
            infiniteHeartTime.gameObject.SetActive(true);
            buyHeartButton.SetActive(false);
            StartCoroutine(FreeHeartCoolDown());
        }    
    }    
    private void OnPurchaseHeart()
    {
        infiniteHeartTime.gameObject.SetActive(true);
        coolDownFreeHeart = GameController.generalConfig.heart_offers.free_heart_cooldown;
        //infiniteHeartTime.text = TimeSpan.FromSeconds(coolDown).ToString(@"hh\:mm\:ss");
        LifeSystem.freezeTime += GameController.generalConfig.heart_offers.free_heart_time * 60;
        LifeSystem.onFreezeTime?.Invoke();
        buyHeartButton.SetActive(false);
        StartCoroutine(FreeHeartCoolDown());
    }  
    IEnumerator FreeHeartCoolDown()
    {
        while (coolDownFreeHeart > 0)
        {
            coolDownFreeHeart--;
            yield return new WaitForSeconds(1f);
        }
    }
    bool loseFocus;
    private void OnApplicationFocus(bool focus)
    {
        loseFocus = !focus;
        if (focus)
        {
            //LogManager.Log("focus");
            if (DataManager.data.lives < 5)
                CalculateLife();
            else
            {
                time = DataManager.data.lifeTime;
            }
            if (LifeSystem.freezeTime > 0)
            {
                CalculateTimeFreeze();
            }
        }
        else
        {
            //LogManager.Log("save last time");
            DataManager.data.lastTime = DateTime.Now.ToBinary();
        }
    }
    private void OnLifeChange(int lives)
    {
        livesText.text = lives.ToString();
    }


    private IEnumerator CountdownTime()
    {
        while(true)
        {
            if(LifeSystem.isFreezeTime == false)
            {
                if (LifeSystem.totalLives < 5)
                {
                    if (time > 0)
                    {
                        if(loseFocus == false)
                            time--;
                    }
                    else
                    {
                        LifeSystem.totalLives++;
                        time = DataManager.data.lifeTime;
                    }
                }
                else
                {
                    time = DataManager.data.lifeTime;
                }
            }
            
            yield return new WaitForSeconds(1f);
        }
        
    }
    [SerializeField]
    private Animation heartAnim;
    private void StartFreezeTime()
    {
        heartAnim.Stop();
        heartAnim.Play();
        if (LifeSystem.isFreezeTime)
            return;
        StartCoroutine(FreezeTime());
    }
    private IEnumerator FreezeTime()
    {
        LifeSystem.isFreezeTime = true;
        //timeText.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);
        addHeartButton.SetActive(false);
        infiniteIcon.SetActive(true);
        livesText.gameObject.SetActive(false);
        int timeLeft = time;
        while (LifeSystem.freezeTime > 0)
        {
            if (loseFocus == false)
                LifeSystem.freezeTime--;
            if (LifeSystem.freezeTime < 86400)
                timeText.text = TimeSpan.FromSeconds(LifeSystem.freezeTime).ToString(@"hh\:mm\:ss");
            else
                timeText.text = (LifeSystem.freezeTime / 3600).ToString() + ":" + TimeSpan.FromSeconds(LifeSystem.freezeTime % 3600).ToString(@"mm\:ss");
            yield return new WaitForSeconds(1f);
        }
        infiniteIcon.SetActive(false);
        LifeSystem.freezeTime = 0;
        LifeSystem.isFreezeTime = false;
        LifeSystem.totalLives = DataManager.data.lives;
        time = timeLeft;
        if (LifeSystem.totalLives < 5)
        {
            addHeartButton.SetActive(true);
        }
        //timeText.GetComponent<RectTransform>().anchoredPosition = new Vector3(17.1f, 0);
        livesText.gameObject.SetActive(true);
        
    }


    private void CalculateLife()
    {
        
        long lastTime = DataManager.data.lastTime;
        //Debug.Log("last login time: " + lastTime);
        if (lastTime == 0)
            return;

        //Debug.Log(DateTime.FromBinary(lastTime));
        double timePassInSec = DateTime.Now.Subtract(DateTime.FromBinary(lastTime)).TotalSeconds;
        //Debug.Log(timePassInSec);
        //Debug.Log(LifeSystem.totalLives);

        int livePass = (int)(timePassInSec / DataManager.data.lifeTime);
        int remains = (int)(timePassInSec % DataManager.data.lifeTime);
        if(remains >= DataManager.data.remainsTime)
        {
            livePass++;
            time = DataManager.data.lifeTime -  (remains - DataManager.data.remainsTime);
        }
        else
        {
            time = DataManager.data.remainsTime - remains;
        }
        LifeSystem.totalLives += livePass;

        
    }

    private void CalculateTimeFreeze()
    {
        long lastTime = DataManager.data.lastTime;
        //Debug.Log("last login time: " + lastTime);
        if (lastTime == 0)
            return;

        //Debug.Log(DateTime.FromBinary(lastTime));
        double timePassInSec = DateTime.Now.Subtract(DateTime.FromBinary(lastTime)).TotalSeconds;
        if (LifeSystem.freezeTime > 0)
        {
            if (LifeSystem.freezeTime > timePassInSec)
            {
                LifeSystem.freezeTime -= (int)timePassInSec;
                StartFreezeTime();
            }
            else
            {
                LifeSystem.freezeTime = 0;
            }
        }
    }
}
