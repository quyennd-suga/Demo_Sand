using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class PopupPiggyBank : PopupUI
{
    [SerializeField] private TextMeshProUGUI txtCoinPiggy;
    [SerializeField] private SlicedFilledImage fill;
    private int totalCoinPiggy = 4000;
    public void OnvalueFill(int amount)
    {
        txtCoinPiggy.text = $"{amount}";
        fill.fillAmount = (float)amount / totalCoinPiggy;
    }    
    public void OnBuyPiggyBank()
    {
       
    }
}