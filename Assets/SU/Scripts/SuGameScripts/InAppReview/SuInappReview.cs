//#if UNITY_ANDROID
//using Google.Play.Review;
//#endif
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class SuInappReview : BaseSUUnit
//{
//#if UNITY_ANDROID

//    private ReviewManager _reviewManager;
//    PlayReviewInfo _playReviewInfo;
//    private void Awake()
//    {
//        _reviewManager = new ReviewManager();
//    }

//    public override void Init(bool test)
//    {
//        isTest = test;
//        StartCoroutine(RequestReview());
//    }

//    public void LauchReview(Action onError)
//    {
//        if (_playReviewInfo != null)
//        {
//            StartCoroutine(LaunchReviewCrt(onError));
//        }
//        else
//        {
//            onError?.Invoke();
//        }
//    }

//    IEnumerator RequestReview()
//    {
//        var requestFlowOperation = _reviewManager.RequestReviewFlow();
//        yield return requestFlowOperation;
//        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
//        {
//            Debug.Log("Request review failed : " + requestFlowOperation.Error.ToString());
//            yield break;
//        }
//        Debug.Log("Request review success");
//        _playReviewInfo = requestFlowOperation.GetResult();
//    }


//    public IEnumerator LaunchReviewCrt(Action onError)
//    {
//        Debug.Log("Lauch review");
//        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
//        yield return launchFlowOperation;
//        _playReviewInfo = null;
//        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
//        {
//            Debug.Log("Lauch review failed : " + launchFlowOperation.Error.ToString());
//            onError?.Invoke();
//            yield break;
//        }
//    }


//#else
//    public override void Init()
//    {
        
//    }
//#endif
//}
