using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif
using UnityEngine;
namespace SU.AutoInstallPackage
{
    public class AutoInstallPackage : MonoBehaviour
    {
        // install ianpp purchase pakage
#if UNITY_EDITOR
        static AddRequest RequestInstall;
        static ListRequest RequestAllPakage;
        [InitializeOnLoadMethod]
        static void InitWhenLoad()
        {
            //RequestAllPakage = Client.Add("com.unity.editorcoroutines");
            RequestAllPakage = Client.List(true, true);
            EditorApplication.update += Progress;
        }

        static void Progress()
        {
            // khi cài đặt xong editorcoroutine
            if (RequestAllPakage.IsCompleted)
            {
                if (RequestAllPakage.Status == StatusCode.Success)
                {
                    bool isInstalledIAPPacke = false;
                    foreach (var package in RequestAllPakage.Result)
                    {
                        //Debug.Log("Pakage " + package.packageId);
                        string packageId = package.packageId;
                        int index = packageId.IndexOf("@");
                        packageId = packageId.Remove(index);
                        if (packageId == "com.unity.purchasing")
                        {
                            isInstalledIAPPacke = true;
                            //Debug.Log("Đã cài IAP");

                            EditorApplication.update -= Progress;
                            break;
                        }
                    }
                    if (!isInstalledIAPPacke)
                    {
                        // nếu chưa cài IAP 
                        RequestInstall = Client.Add("com.unity.purchasing");
                    }

                }
                else if (RequestAllPakage.Status >= StatusCode.Failure)
                {
                    Debug.Log(RequestAllPakage.Error.message);
                    EditorApplication.update -= Progress;
                }
                else
                {
                    EditorApplication.update -= Progress;
                }
            }

            if (RequestInstall != null && RequestInstall.IsCompleted)
            {
                if (RequestInstall.Status == StatusCode.Success)
                {
                    Debug.Log("Install IAP success");
                }
                else if (RequestInstall.Status >= StatusCode.Failure)
                    Debug.Log(RequestInstall.Error.message);
                EditorApplication.update -= Progress;
            }
        }
#endif

    }
}