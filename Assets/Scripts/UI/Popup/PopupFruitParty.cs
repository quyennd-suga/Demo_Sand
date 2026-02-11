using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupFruitParty : PopupUI
{
    public void OnBuyPackage1()
    {
        Debug.Log("💰 Buy Coin Package 1");
        
        // TODO: Thêm IAP logic
        // IAPManager.Instance.PurchaseProduct("coin_package_1", OnCoinPackage1Success);
    }
    
    public void OnBuyPackage2()
    {
        Debug.Log("💰 Buy Coin Package 2");
        
        // TODO: Thêm IAP logic
        // IAPManager.Instance.PurchaseProduct("coin_package_2", OnCoinPackage2Success);
    }
        public void OnBuyPackage3()
    {
        Debug.Log("💰 Buy Coin Package 3");
        
        // TODO: Thêm IAP logic
        // IAPManager.Instance.PurchaseProduct("coin_package_3", OnCoinPackage3Success);
    }
    
}
