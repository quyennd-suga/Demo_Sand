using System.Collections;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class SuCheckTool : IPreprocessBuildWithReport
{
    //[MenuItem("SuTool/FindFile")]
    //public static void FindFile()
    //{
    //    bool isMissInstallReferrer = true;
    //    string[] dependencies = Directory.GetFiles("Assets", "*Dependencies.xml", SearchOption.AllDirectories);
    //    for (int i = 0; i < dependencies.Length; i++)
    //    {
    //        string[] readText = File.ReadAllLines(dependencies[i]);
    //        foreach (string s in readText)
    //        {
    //            if (s.Contains("com.android.installreferrer:installreferrer"))
    //            {
    //                Debug.Log("contained");
    //                isMissInstallReferrer = false;
    //            }
    //        }
    //    }

    //    bool isMissSDKSignature = true;
    //    string[] files = Directory.GetFiles("Assets/Adjust/Android", "*.aar", SearchOption.AllDirectories);
    //    if (files.Length > 0)
    //    {
    //        isMissSDKSignature = false;
    //    }
        
    //    string[] paths = Directory.GetFiles("Assets/Sutool/Resources", "*SuCheckData.Asset");
    //    string pathData = paths[0];
    //    SuToolData data = (SuToolData)AssetDatabase.LoadAssetAtPath(pathData, typeof(SuToolData));
    //    data.isMissInstallReferer = isMissInstallReferrer;
    //    data.isMissSDKSignature = isMissSDKSignature;
    //    //Undo.RecordObject(data, "Update SuData");
    //    //FindObjectOfType<SUGame>().isMissSDK = true;
    //}
    

    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        bool isMissInstallReferrer = true;
        string[] dependencies = Directory.GetFiles("Assets", "*Dependencies.xml", SearchOption.AllDirectories);
        for (int i = 0; i < dependencies.Length; i++)
        {
            string[] readText = File.ReadAllLines(dependencies[i]);
            foreach (string s in readText)
            {
                if (s.Contains("com.android.installreferrer:installreferrer"))
                {
                    isMissInstallReferrer = false;
                }
            }
        }

        bool isMissSDKSignature = true;


#if UNITY_ANDROID
        string[] files = Directory.GetFiles("Assets/Adjust/Android", "*.aar", SearchOption.AllDirectories);
#elif UNITY_IOS
        string[] files = Directory.GetFiles("Assets/Adjust/IOS", "*AdjustSigSdk.a", SearchOption.AllDirectories);
#endif
        
        if (files.Length > 0)
        {
            isMissSDKSignature = false;
        }

        //string[] paths = Directory.GetFiles("Assets/SU/Sutool/Resources", "*SuCheckData.Asset");
        //string pathData = paths[0];
        //SuToolData data = (SuToolData)AssetDatabase.LoadAssetAtPath(pathData, typeof(SuToolData));
        SuToolData data = Resources.Load<SuToolData>("SuCheckData");
        //SuToolData data = await AddressableLoader.LoadAssetAsync<SuToolData>($"{GlobalValues.gameAssetsPath}SU/Sutool/SuCheckData.asset");
        data.isMissInstallReferer = isMissInstallReferrer;
        data.isMissSDKSignature = isMissSDKSignature;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        string text = "";
        if (data.isMissInstallReferer && data.isMissSDKSignature)
        {
            text = "Missing SDK Signature and Install Referrer";
        }
        else if (data.isMissSDKSignature)
        {
            text = "Missing SDK Signature";
        }
        else if (data.isMissInstallReferer)
        {
            text = "Missing Install Referrer";
        }
        else
        {
            return;
        }
        bool output = EditorUtility.DisplayDialog("WARNING", text,"Continue","Cancel");
        if(output == false)
        {
            throw new BuildFailedException("Cancel build because of " + text);
        }
    }


}
#endif
