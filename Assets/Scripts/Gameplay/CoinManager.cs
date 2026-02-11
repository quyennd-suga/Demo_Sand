using System.Collections;
using System.Collections.Generic;
//using UnityEngine;
using System;


public class CoinManager 
{
    public static Action<int> onCoinChange;
    private static int _coin;
    public static int totalCoin
    {
        get
        {
            return _coin;
        }
        set
        {
            _coin = value;
            onCoinChange?.Invoke(value);
            DataManager.data.totalCoin = value;
        }
    }
}
