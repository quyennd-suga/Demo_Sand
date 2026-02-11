using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupShop : PopupUI
{
    [SerializeField] private GameObject btnClose;
    protected override void OnShow()
    {
        base.OnShow();
    }
    
    #region === PREMIUM & SPECIAL PACKAGES ===
    
    public void OnBuyPrimePass()
    {
    }
    
    public void OnBuyRemoveAdsPremium()
    {
     
    }
    public void OnBuyRemoveAds()
    {
       
    }
    
    #endregion
    
    #region === BUNDLE PACKAGES ===

    public void OnBuyWelcomeBundle()
    {

    }
    
    public void OnBuyBigBundle()
    {

    }

    public void OnBuyLuxuryBundle()
    {

    }
    
    #endregion
    
    #region === DAILY OFFER & AD REWARD ===

    public void OnClaimDailyOffer()
    {
    }
    
    #endregion
    
    #region === COIN PACKAGES (6 gói từ thấp đến cao) ===

    public void OnBuyCoinPackage_1()
    {

    }
    
    public void OnBuyCoinPackage_2()
    {

    }
    
    public void OnBuyCoinPackage_3()
    {
    }

    public void OnBuyCoinPackage_4()
    {

    }
    
    public void OnBuyCoinPackage_5()
    {
    }

    public void OnBuyCoinPackage_6()
    {

    }
    
    #endregion

}