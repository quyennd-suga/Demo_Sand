using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class PiggyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtCoinPiggy;
    [SerializeField] private GameObject noti;
    [SerializeField] private SlicedFilledImage fillAmout;
    private int totalPiggyCoin = 4000;
    public void OnClick()
    {
        UIManager.Instance.ShowPopup<PopupPiggyBank>();
    }
    private void Start()
    {
        OnChangeCoinPiggy(500);
    }
    public void OnChangeCoinPiggy(int amount)
    {
        noti.SetActive(amount>=totalPiggyCoin);
        txtCoinPiggy.text =$"{ amount}";
        fillAmout.fillAmount =(float)amount / totalPiggyCoin;
        if(amount>=totalPiggyCoin)
        {
            txtCoinPiggy.text = "Full";
        }    
    }    
}
