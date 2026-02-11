using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using GoogleMobileAds.Common;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SuRemoteConfig : BaseSUUnit
{
    [DisableInEditorMode]
    private bool initCompleted = false;
    [DisableInEditorMode]
    public bool fetchComplete = false;
    public bool setPropertyForJsonID = false;
    Dictionary<RemoteConfigName, object> RemoteDictRealtime;
    Dictionary<string, object> RemoteDict;
    public List<RemoteConfigDataModule> RemoteData;
    //[SerializeField]
    //private TextAsset event_config;
    //[SerializeField]
    //private TextAsset winstreak_config;
    //[SerializeField]
    //private TextAsset special_offer_config;
    //[SerializeField]
    //private TextAsset collect_item_config;
    //[SerializeField]
    //private TextAsset battle_pass_config;
    //[SerializeField]
    //private TextAsset race_config;
    public override void Init(bool test)
    {
        RemoteDictRealtime = new Dictionary<RemoteConfigName, object>();
        RemoteDict = new System.Collections.Generic.Dictionary<string, object>
        {

        };

        foreach (RemoteConfigDataModule _item in RemoteData)
        {
               
            string defaultValue = _item.defaultValue;
            //switch(_item.Name)
            //{
            //    case RemoteConfigName.event_config:
            //        defaultValue = event_config.text;
            //        break;
            //    case RemoteConfigName.winstreak_config:
            //        defaultValue = winstreak_config.text;
            //        break;
            //    case RemoteConfigName.special_offer_config:
            //        defaultValue = special_offer_config.text;
            //        break;
            //    case RemoteConfigName.collect_item_config:
            //        defaultValue = collect_item_config.text;
            //        break;
            //    case RemoteConfigName.battle_pass_config:
            //        defaultValue = battle_pass_config.text;
            //        break;
            //    case RemoteConfigName.race_config:
            //        defaultValue = race_config.text;
            //        break;
            //}
            RemoteDict.Add(_item.Name.ToString(), PlayerPrefs.GetString(_item.Name.ToString(), defaultValue));
        }
        isTest = test;
        initCompleted = false;
        if (SuGame.haveDependencies == true)
        {
            InitRemoteConfig();
        }
        else
        {
            LogManager.Log("Không có dependencies");
        }


    }
    public static Action<bool> OnFetchComplete;

    

    private void InitRemoteConfig()
    {
        //Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.OnConfigUpdateListener += ConfigUpdateListenerEventHandler;
        Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(RemoteDict).ContinueWithOnMainThread(task =>
        {
            initCompleted = true;
            FetchDataAsync();
        });

    }

    //void ConfigUpdateListenerEventHandler(object sender, Firebase.RemoteConfig.ConfigUpdateEventArgs args)
    //{
    //    if (args.Error != Firebase.RemoteConfig.RemoteConfigError.None)
    //    {
    //        LogManager.Log(string.Format("Error occurred while listening: {0}", args.Error));
    //        return;
    //    }

    //    LogManager.Log("Updated keys: " + string.Join(", ", args.UpdatedKeys));
    //    // Activate all fetched values and then display a welcome message.
    //    Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(task =>
    //    {
    //        FetchComplete(task);
    //    });
    //}

    Task FetchDataAsync()
    {
        LogManager.Log("Fetching data...");
        Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }


    void DisplayAllKeys()
    {
        Debug.Log("REMOTE FETCH COMPLETE!!!!!!!!!!!!!!!!!");
        System.Collections.Generic.IEnumerable<string> keys = FirebaseRemoteConfig.DefaultInstance.Keys;
        foreach (string key in keys)
        {
            string vl = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if(isTest)
            {
                LogManager.Log("    " + key + ":" + vl);
            }
            
            if (RemoteDict.ContainsKey(key))
            {
                RemoteDict[key] = vl;

            }
            else
            {
                RemoteDict.Add(key, vl);
            }
            PlayerPrefs.SetString(key, RemoteDict[key].ToString());
            bool isJsonData = false;
            try
            {
                JObject js = JObject.Parse(vl);
                JToken idValue;
                bool haveID = js.TryGetValue("id", out idValue);
                if (haveID && setPropertyForJsonID)
                {
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty(key, idValue.ToString());
                    LogManager.Log("ID của key " + key + " là " + idValue);
                }
                isJsonData = true;
            }
            catch (System.Exception ee)
            {
                isJsonData = false;
                LogManager.Log("data của key " + key + " không phải json");
            }

            bool parse = Enum.TryParse<RemoteConfigName>(key, out RemoteConfigName _key);
            if (parse && RemoteDictRealtime.ContainsKey(_key))
            {
                if (isJsonData)
                {
                    JsonUtility.FromJsonOverwrite(vl, RemoteDictRealtime[_key]);
                }
                else
                {
                    RemoteDictRealtime[_key] = vl;
                }
            }
        }
        /*
        Debug.Log("GetKeysByPrefix(\"config_test_s\"):");
        keys = FirebaseRemoteConfig.DefaultInstance.GetKeysByPrefix("config_test_s");
        foreach (string key in keys)
        {
            Debug.Log("    " + key);
        }
        */
        fetchComplete = true;

    }



    void FetchComplete(Task fetchTask)
    {
        FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(task =>
        {
            if (fetchTask.IsCanceled)
            {
                LogManager.Log("Fetch canceled.");
            }
            else if (fetchTask.IsFaulted)
            {
                LogManager.Log("Fetch encountered an error.");
            }
            else if (fetchTask.IsCompleted)
            {
                LogManager.Log("Fetch completed successfully!");
                //FirebaseRemoteConfig.ActivateFetched();
            }
            var info = FirebaseRemoteConfig.DefaultInstance.Info;
            switch (info.LastFetchStatus)
            {
                case Firebase.RemoteConfig.LastFetchStatus.Success:

                    LogManager.Log(string.Format("Remote data loaded and ready (last fetch time {0}).",
                        info.FetchTime));
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        // chạy action vào frame tiếp theo ở main thread tránh trường hợp các hàm action có lỗi sẽ không chạy , gây treo hoặc crash game
                        DisplayAllKeys();
                        OnFetchComplete?.Invoke(true);
                        //LoadingScene.isFetched = true;
                    });


                    break;
                case Firebase.RemoteConfig.LastFetchStatus.Failure:
                    switch (info.LastFetchFailureReason)
                    {
                        case Firebase.RemoteConfig.FetchFailureReason.Error:
                            LogManager.Log("Fetch failed for unknown reason");
                            break;
                        case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                            LogManager.Log("Fetch throttled until " + info.ThrottledEndTime);
                            break;
                    }
                    fetchComplete = true;
                    OnFetchComplete?.Invoke(false);
                    //LoadingScene.isFetched = true;
                    break;
                case Firebase.RemoteConfig.LastFetchStatus.Pending:
                    LogManager.Log("Latest Fetch call still pending.");
                    fetchComplete = true;
                    OnFetchComplete?.Invoke(false);
                    //LoadingScene.isFetched = true;
                    break;
                default:
                    fetchComplete = true;
                    OnFetchComplete?.Invoke(false);
                    //LoadingScene.isFetched = true;
                    break;
            }

                
            });

    }

    public bool HaveKey(RemoteConfigName keyName)
    {
        return HaveKey(keyName.ToString());
    }

    public bool HaveKey(string keyName)
    {
        return RemoteDict.ContainsKey(keyName);
    }

    // lấy giá trị string 
    public string GetStringValue(string name)
    {
        if (RemoteDict.ContainsKey(name))
        {
            return RemoteDict[name].ToString();
        }
        else
        {
            return "";
        }
    }
    public string GetStringValue(RemoteConfigName name)
    {
        string sName = name.ToString();
        return GetStringValue(sName);
    }

    // lấy giá trị int , nếu data sai thì trả về 0;
    public int GetIntValue(string name)
    {
        if (RemoteDict.ContainsKey(name))
        {
            if (int.TryParse(RemoteDict[name].ToString(), out int vl))
            {
                return vl;
            }
            return 0;
        }
        else
        {
            return 0;
        }
    }
    public int GetIntValue(RemoteConfigName name)
    {
        string sName = name.ToString();
        return GetIntValue(sName);
    }

    // lấy giá trị float
    public float GetFloatValue(string name)
    {
        if (RemoteDict.ContainsKey(name))
        {
            System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Float;
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.CurrentCulture;
            if (float.TryParse(RemoteDict[name].ToString(), style, cul, out float vl))
            {
                return vl;
            }
            return 0;
        }
        else
        {
            return 0;
        }
    }
    public float GetFloatValue(RemoteConfigName name)
    {
        string sName = name.ToString();
        return GetFloatValue(sName);
    }

    // lấy giá trị bool , nếu không có thì trả về false
    public bool GetBoolValue(string name)
    {
        if (RemoteDict.ContainsKey(name))
        {
            string value = RemoteDict[name].ToString();
            if (value == "1" || value == "true")
            {
                return true;
            }
            return false;
        }
        return false;
    }
    public bool GetBoolValue(RemoteConfigName name)
    {
        string sName = name.ToString();
        return GetBoolValue(sName);
    }

    // lấy giá trị json theo type 
    public T GetJsonValueAsType<T>(string name)
    {
        if (RemoteDict.ContainsKey(name))
        {
            try
            {
                T value = JsonUtility.FromJson<T>(RemoteDict[name].ToString());
                return value;
            }
            catch
            {
                // data không phải json 
                return default;
            }
        }
        return default;
        // default == giá trị null;
    }
    public T GetJsonValueAsType<T>(RemoteConfigName name)
    {
        string sName = name.ToString();
        return GetJsonValueAsType<T>(sName);


    }

    /// <summary>
    /// Gán value cho 1 biến đã tạo trước đó để biến đó tự thay đổi khi value thay đổi 
    /// </summary>   
    public void GetValue<T>(RemoteConfigName name, out T returnValue)
    {
        if (RemoteDictRealtime.ContainsKey(name))
        {
            try
            {
                returnValue = (T)RemoteDictRealtime[name];
            }
            catch (System.Exception ee)
            {
                //Debug.LogError("Bị lỗi ở " + name + " lối là " + ee);
                returnValue = default;
            }
        }
        else
        {
            if (RemoteDict.ContainsKey(name.ToString()))
            {
                returnValue = JsonUtility.FromJson<T>(RemoteDict[name.ToString()].ToString());
                RemoteDictRealtime.Add(name, returnValue);
            }
            else
            {
                //Debug.Log("Không tồn tại key " + name.ToString());
                returnValue = default;
            }

        }
    }
}


[System.Serializable]
public struct RemoteConfigDataModule
{
    [EnumPaging]
    public RemoteConfigName Name;
    // nếu giá trị là float , thì dùng định dạng A,B chứ không dùng định dạng A.B
    public string defaultValue;
}
