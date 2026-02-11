using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuDeepLink : MonoBehaviour
{
    public static DeepLinkDataModule DeepLinkData;
    private void Awake()
    {
        Application.deepLinkActivated += (string deeplink) =>
        {
            DeferredDeeplinkCallback(deeplink);
        };

        //Debug.Log("Deeplink activate is " + Application.absoluteURL);
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            DeferredDeeplinkCallback(Application.absoluteURL);
        }
        Application.runInBackground = false;


    }

    private void Start()
    {
        //test deep link
        //string link = "https://tanglerope3d.go.link/event?event_name=special_offer_laborday&event_action=special_offer";
        //DeferredDeeplinkCallback(link);
    }


    private void DeferredDeeplinkCallback(string deeplink)
    {
        /*
        Debug.Log("Received Deeplink: " + deeplink);

        // Ví dụ: https://goodsblast.go.link/abc123?ref=affiliateA&level=3&user_id=998
        Uri uri = new Uri(deeplink);
        // test lấy các giá trị từ deeplink
        string country = GetQueryParam(uri, "country");
        string level = GetQueryParam(uri, "level");
        string userId = GetQueryParam(uri, "user_id");
        Debug.Log("Parsed Deeplink - Country: " + country + ", Level: " + level + ", UserID: " + userId );
        Debug.Log("Segment 0: " + GetSegmentIndex(uri, 0));
        Debug.Log("Segment 1: " + GetSegmentIndex(uri, 1));
        Debug.Log("Segment 2: " + GetSegmentIndex(uri, 2));
        */
        //Debug.Log("Received Deeplink: " + deeplink);
        DeepLinkData = new DeepLinkDataModule();
        Uri uri = new Uri(deeplink);
        // Lấy giá trị từ deeplink
        DeepLinkData.iap_product = GetQueryParam(uri, "iap_product");
        DeepLinkData.event_action = GetQueryParam(uri, "event_action");
        DeepLinkData.event_name = GetQueryParam(uri, "event_name");
        //Debug.Log("Pased Deeplink Data" + JsonUtility.ToJson(DeepLinkData));
    }

    string GetSegmentIndex(Uri uri, int index)
    {
        try
        {
            string[] segments = uri.Segments;
            if (index >= 0 && index < segments.Length)
            {
                return segments[index].TrimEnd('/'); // Remove trailing slash if exists
            }
            return "no_data";
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing segment: " + ex.Message);
            return "no_data";
        }
    }
    string GetQueryParam(Uri uri, string key)
    {
        try
        {
            string query = uri.Query;
            var parsed = System.Web.HttpUtility.ParseQueryString(query);
            return parsed.Get(key);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing query parameter: " + ex.Message);
            return "";
        }
    }
}

[System.Serializable]
public class DeepLinkDataModule
{
    public string iap_product;
    public string event_action;
    public string event_name;
}