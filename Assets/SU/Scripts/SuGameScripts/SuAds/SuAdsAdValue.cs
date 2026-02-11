using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuAdsAdValue
{
    public AdsNetwork Mediation_Platform;
    public string adSource;
    public string Network;
    public string Precision;
    // cho game dùng admob
    public long Valuemicros;
    // cho game dùng MAX ,Iron
    public double Value;
    public string CurrencyCode;
    public string Ad_Format;
    public string UnitID;
    public ActionShowAds actionShowAds;
    public string mediationGroupName, mediationABTestName, mediationABTestVariant;
}
