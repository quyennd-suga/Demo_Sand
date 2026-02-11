//#if UNITY_ANDROID
//using Google.Play.AppUpdate;
//using Google.Play.Common;
//#endif
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class SuInAppUpdate : BaseSUUnit
//{
//    // Start is called before the first frame update
//    public GameObject UpdateProgressUI;
//    public Image iUpdateProgress;
//#if UNITY_ANDROID
//    AppUpdateManager appUpdateManager;
//    AppUpdateInfo appUpdateInfo;
//#endif
//    void Awake()
//    {


//    }

//    private void Start()
//    {
//        SuRemoteConfig.OnFetchComplete += OnRemoteConfigFetchCompleted;
//    }
//    bool forceUpdate = false;
//    private void OnRemoteConfigFetchCompleted(bool success)
//    {
//#if SUGAME_VALIDATED
//        forceUpdate = SuGame.Get<SuRemoteConfig>().GetBoolValue(RemoteConfigName.Force_update_to_new_version);
//#else
//        forceUpdate = false;
//#endif
//#if UNITY_EDITOR
//        // không làm gì
//#elif UNITY_ANDROID
        
//        StartCoroutine(CheckForUpdate(forceUpdate == true ? AppUpdateType.immediate : AppUpdateType.flexible));
//#endif
//    }

//#if UNITY_ANDROID
//    IEnumerator CheckForUpdate(AppUpdateType _type)
//    {

//        appUpdateManager = new AppUpdateManager();
//        PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation =
//          appUpdateManager.GetAppUpdateInfo();
//        // Wait until the asynchronous operation completes.
//        yield return appUpdateInfoOperation;

//        if (appUpdateInfoOperation.IsSuccessful)
//        {
//            appUpdateInfo = appUpdateInfoOperation.GetResult();
//            Debug.Log("AppUpdate availability là " + appUpdateInfo.UpdateAvailability);
//            if (appUpdateInfo != null)
//            {
//                //int stalenessDays = appUpdateInfo.ClientVersionStalenessDays.Value;
//                //var priority = appUpdateInfo.UpdatePriority;
//                // Check AppUpdateInfo's UpdateAvailability, UpdatePriority,
//                // IsUpdateTypeAllowed(), etc. and decide whether to ask the user
//                // to start an in-app update.
//                if (appUpdateInfo.UpdateAvailability == UpdateAvailability.UpdateAvailable)
//                {
//                    Debug.Log("Update app");
//                    UpdateApp(_type, () =>
//                   {
//                       // lỗi không thể update
//                       Debug.Log("Update bị lỗi");
//                   });
//                }
//                else
//                {
//                    Debug.Log("Không update vì availability là " + appUpdateInfo.UpdateAvailability);
//                }
//            }
//            else
//            {
//                Debug.Log("App update info bị null ");
//            }
//        }
//        else
//        {
//            Debug.Log("Lỗi check update app : " + appUpdateInfoOperation.Error);
//            // Log appUpdateInfoOperation.Error.
//        }

//    }
//#endif

//#if UNITY_ANDROID
//    void UpdateApp(AppUpdateType _type, System.Action onError)
//    {
//        if (appUpdateInfo == null)
//        {
//            // nếu update info chưa có thì ko gọi các hàm update
//            onError?.Invoke();
//            return;
//        }
//        switch (_type)
//        {
//            case AppUpdateType.flexible:

//                StartCoroutine(StartFlexibleUpdate(onError));
//                break;
//            case AppUpdateType.immediate:
//                StartCoroutine(StartImmediateUpdate(onError));
//                break;
//        }

//    }

//    IEnumerator StartFlexibleUpdate(System.Action onError)

//    {
//        var appUpdateOptions = AppUpdateOptions.FlexibleAppUpdateOptions(allowAssetPackDeletion: true);
//        if (appUpdateInfo.IsUpdateTypeAllowed(appUpdateOptions))
//        {
//            // Creates an AppUpdateRequest that can be used to monitor the
//            // requested in-app update flow.
//            var startUpdateRequest = appUpdateManager.StartUpdate(
//              // The result returned by PlayAsyncOperation.GetResult().
//              appUpdateInfo,
//              // The AppUpdateOptions created defining the requested in-app update
//              // and its parameters.
//              appUpdateOptions);
//            UpdateProgressUI.SetActive(true);
//            while (!startUpdateRequest.IsDone)
//            {
//                // For flexible flow,the user can continue to use the app while
//                // the update downloads in the background. You can implement a
//                // progress bar showing the download status during this time.
//                iUpdateProgress.fillAmount = startUpdateRequest.DownloadProgress;
//                yield return null;
//            }
//            UpdateProgressUI.SetActive(false);
//            StartCoroutine(CompleteFlexibleUpdate(onError));
//        }
//    }
//    IEnumerator CompleteFlexibleUpdate(System.Action onError)
//    {
//        var result = appUpdateManager.CompleteUpdate();
//        yield return result;

//        if (!string.IsNullOrEmpty(result.Error.ToString()))
//        {
//            Debug.Log("Lỗi update app flexible : " + result.Error.ToString());
//            onError?.Invoke();
//        }
//        // If the update completes successfully, then the app restarts and this line
//        // is never reached. If this line is reached, then handle the failure (e.g. by
//        // logging result.Error or by displaying a message to the user).
//    }

//    IEnumerator StartImmediateUpdate(System.Action onError)
//    {
//        var appUpdateOptions = AppUpdateOptions.ImmediateAppUpdateOptions(allowAssetPackDeletion: true);
//        if (appUpdateInfo.IsUpdateTypeAllowed(appUpdateOptions))
//        {
//            // Creates an AppUpdateRequest that can be used to monitor the
//            // requested in-app update flow.
//            var startUpdateRequest = appUpdateManager.StartUpdate(
//          // The result returned by PlayAsyncOperation.GetResult().
//          appUpdateInfo,
//          // The AppUpdateOptions created defining the requested in-app update
//          // and its parameters.
//          appUpdateOptions);
//            yield return startUpdateRequest;
//            if (!string.IsNullOrEmpty(startUpdateRequest.Error.ToString()))
//            {
//                onError?.Invoke();
//            }
//            // If the update completes successfully, then the app restarts and this line
//            // is never reached. If this line is reached, then handle the failure (for
//            // example, by logging result.Error or by displaying a message to the user).
//        }
//    }

//    public override void Init(bool test)
//    {
//        //throw new NotImplementedException();
//        isTest = test;
//    }
//#else
//    public override void Init()
//    {
//        //throw new NotImplementedException();
//    }
//#endif

//}

//[System.Serializable]
//public enum AppUpdateType
//{
//    immediate,
//    flexible
//}
