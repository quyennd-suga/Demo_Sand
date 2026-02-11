using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupRate : MonoBehaviour
{
    public GameObject StarContainer;
    RateStar[] RateStars;

    public Button bOk;
    private void Awake()
    {
        RateStars = StarContainer.GetComponentsInChildren<RateStar>(true);
        for (int i = 0; i < RateStars.Length; i++)
        {
            int id = i;
            RateStars[i].GetComponent<Button>().onClick.AddListener(() =>
            {
                Star = id + 1;
            });
        }
    }

    private void OnEnable()
    {
        Star = 0;

    }
    int _star;
    int Star
    {
        get
        {
            return _star;
        }
        set
        {
            _star = value;
            for (int i = 0; i < RateStars.Length; i++)
            {
                Image img = RateStars[i].iStar;
                img.enabled = i < value;
            }
            bOk.gameObject.SetActive(value > 0);
        }
    }
    public void Close()
    {
#if SUGAME_VALIDATED
        SuGame.Get<SuAnalytics>().LogEvent(EventName.Rating, new Param(ParaName.Star_Number, Star));
        if (Star == 5)
        {
#if UNITY_ANDROID
            //Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
            SuGame.Get<SuInappReview>().LauchReview(() =>
            {
                Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
            });
#elif UNITY_IOS
            //Application.OpenURL("itms-apps://itunes.apple.com/app/id1614576015?action=write-review");
#endif
        }
#endif

        //------------------------- Code tắt popup
        //PopupManager.instance.HidePopup(PopupName.POPUP_RATE);
        gameObject.SetActive(false);


    }
}
