using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SuCheckData.Asset", menuName = "SuTool/Create Tool Data")]
[System.Serializable]
public class SuToolData : ScriptableObject
{
    public bool isMissInstallReferer;
    public bool isMissSDKSignature;
}
