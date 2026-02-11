using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[System.Serializable]
public class AdsUnit
{
    [HideInInspector]
    public bool IsTest = false;
    [ShowIf("@this.IsTest == false")]
    public string Android, IOS;
    [ShowIf("@this.IsTest == true")]
    public string Android_Test, IOS_Test;
    public string ID
    {
        get
        {
            if (IsTest)
            {
                return Application.platform == RuntimePlatform.IPhonePlayer ? IOS_Test : Android_Test;
            }
            else
                return Application.platform == RuntimePlatform.IPhonePlayer ? IOS : Android;
        }
    }
    public void SetAdId(string id)
    {
        if (Application.platform == RuntimePlatform.Android)
            Android = id;
        if (Application.platform == RuntimePlatform.IPhonePlayer)
            IOS = id;
    }
}
