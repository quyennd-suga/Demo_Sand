using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FileChecker : BaseSUUnit
{
    [SerializeField]
    private GameObject canvasWarning;
    [SerializeField]
    private TextMeshProUGUI warning;



    public override void Init(bool test)
    {
        if (!EnableSU)
            return;
        isTest = test;
        if (test == false)
            return;
        if (EnableSU == false)
            return;
        string path = "SuCheckData";
        SuToolData data = Resources.Load<SuToolData>(path);
        //SuToolData data = await AddressableLoader.LoadAssetAsync<SuToolData>($"{GlobalValues.gameAssetsPath}SuCheckData.asset");
        string text = "";
        if (data.isMissInstallReferer && data.isMissSDKSignature)
        {
            text = "Missing SDK Signature and Install Referrer";
            ActiveWarning(text);
        }
        else if (data.isMissSDKSignature)
        {
            text = "Missing SDK Signature";
            ActiveWarning(text);
        }
        else if (data.isMissInstallReferer)
        {
            text = "Missing Install Referrer";
            ActiveWarning(text);
        }
    }

    /*
    private void Start()
    {
        Debug.Log("Init file checker");
        string path = "SuCheckData";
        SuToolData data = Resources.Load<SuToolData>(path);
        string text = "";
        if(data.isMissInstallReferer && data.isMissSDKSignature)
        {
            text = "Missing SDK Signature and Install Referrer";
            ActiveWarning(text);
        }
        else if(data.isMissSDKSignature)
        {
            text = "Missing SDK Signature";
            ActiveWarning(text);
        }
        else if(data.isMissInstallReferer)
        {
            text = "Missing Install Referrer";
            ActiveWarning(text);
        }
    }
    */
    private void ActiveWarning(string text)
    {
        canvasWarning.SetActive(true);
        warning.text = text;
    }
}
