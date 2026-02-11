
using GoogleMobileAds.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YoutubeLight;

public class SuRelatedApps : BaseSUUnit
{
    public static SuRelatedApps instance;
    public GameObject RelatedAppsItemPrefab;
    public GameObject RelatedAppsObject;
    public Transform scrollContent;
    public ScrollRect scroll;
    List<RelatedAppsItem> Items;
    RelatedAppsDataModule Data;
    //List<PackageInfo> installedApps;


    private void Awake()
    {
        Items = new List<RelatedAppsItem>();
        instance = this;
        RelatedAppsObject.SetActive(false);
    }

    private void Start()
    {
        SuRemoteConfig.OnFetchComplete += OnRemoteConfigFetchCompleted;
    }

    private void OnRemoteConfigFetchCompleted(bool success)
    {
        Debug.Log("Fetch completed , call awake async");
        AwakeAsync();
    }

    public void ShowRelatedApps()
    {
        Debug.Log("check Show related apps");
        if (instance != null && instance.Items != null && instance.Items.Count > 0)
        {
            Debug.Log("Show related apps");
            for (int i = 0; i < instance.Items.Count; i++)
            {
                if (instance.Items[i].AppInfo.Show == true && !string.IsNullOrEmpty(instance.Items[i].AppInfo.VideoUrl))
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        instance.RelatedAppsObject.SetActive(true);
                        break;
                    }
                    else if (Application.platform == RuntimePlatform.IPhonePlayer && !string.IsNullOrEmpty(instance.Items[i].AppInfo.StoreUrlIOS))
                    {
                        instance.RelatedAppsObject.SetActive(true);
                        break;
                    }
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(instance.RelatedAppsObject.GetComponent<RectTransform>());
            ExecuteInUpdate.ExecuteInNextFrame(() =>
            {

                int itemShowCount = 0;
                for (int i = 0; i < instance.Items.Count; i++)
                {
#if UNITY_ANDROID
                    if (instance.Items[i].AppInfo.Show == true && !string.IsNullOrEmpty(instance.Items[i].AppInfo.VideoUrl) && !isInstalledApp(instance.Items[i].AppInfo.PakageName))
                    {
                        instance.Items[i].gameObject.SetActive(true);
                        int index = i;
                        MobileAdsEventExecutor.ExecuteInUpdate(() =>
                        {
                            instance.Items[index].PlayVideo();
                        });

                        itemShowCount++;
                    }
                    else
                    {
                        instance.Items[i].gameObject.SetActive(false);
                    }
#endif
#if UNITY_IOS
                if (instance.Items[i].AppInfo.Show == true && !string.IsNullOrEmpty(instance.Items[i].AppInfo.VideoUrl) && !string.IsNullOrEmpty(instance.Items[i].AppInfo.StoreUrlIOS))
                {
                    // nếu show , có video và có ios store url thì mới cho hiện
                    instance.Items[i].gameObject.SetActive(true);
                    instance.Items[i].PlayVideo();
                    itemShowCount++;
                }
                else
                {
                    instance.Items[i].gameObject.SetActive(false);
                }
#endif
                }
                instance.scroll.horizontalNormalizedPosition = 0;
                if (itemShowCount > 0)
                {

                    instance.RelatedAppsObject.SetActive(true);
                }
                else
                {
                    // nếu không có item nào show thì k cho hiện nữa
                    instance.RelatedAppsObject.SetActive(false);
                }
            });

        }

    }

    public void HideRelatedApps()
    {
        for (int i = 0; i < instance.Items.Count; i++)
        {
            instance.Items[i].vPlayer.Stop();
        }
        instance.RelatedAppsObject.SetActive(false);
    }

    private void AwakeAsync()
    {
#if SUGAME_VALIDATED
        // get json data from remote config
        string relatedAPPdata = SuGame.Get<SuRemoteConfig>().GetStringValue(RemoteConfigName.Related_Apps);
        Data = JsonUtility.FromJson<RelatedAppsDataModule>(relatedAPPdata);
        if (Data != null)
        {
            for (int i = 0; i < Data.Apps.Count; i++)
            {
                RelatedAppInfoModule appInfo = Data.Apps[i];
                RelatedAppsItem item = null;
                if (i < Items.Count)
                {
                    item = Items[i];
                }
                else
                {
                    item = Instantiate(RelatedAppsItemPrefab, scrollContent, false).GetComponent<RelatedAppsItem>();
                    Items.Add(item);
                }
                item.AppInfo = appInfo;
                StartCoroutine(LoadVideoCrt(appInfo.VideoUrl, (vUrl) =>
                {
                    if (!string.IsNullOrEmpty(vUrl))
                    {
                        item.vPlayer.url = vUrl;
                        if (item.gameObject.activeInHierarchy)
                        {
                            item.PlayVideo();
                        }
                    }
                }));
            }
        }
#endif
    }


    public IEnumerator LoadVideoCrt(string url, Action<string> onSuccess)
    {
        string hashName = MD5Hash(url);
        var path = Application.persistentDataPath + "/" + hashName + ".mp4";
        if (File.Exists(path))
        {
#if UNITY_ANDROID
            onSuccess?.Invoke(path);
            yield break;
#else
            //return Path.Combine("file://" + path);
            onSuccess?.Invoke("file://" + path);
            yield break;
#endif
        }

        bool isYoutubeVideo = TryNormalizeYoutubeUrl(url, out string youtubeUrl);
        if (isYoutubeVideo)
        {
            StartCoroutine(DownloadYoutubeUrl(youtubeUrl, (videoInfos) =>
            {

                // lấy video dạng mp4 có chất lượng thấp nhất 
                VideoInfo[] allMp4Video = Array.FindAll(videoInfos, x => x.VideoType == VideoType.Mp4 && x.Resolution > 0);
                Array.Sort(allMp4Video, (a, b) => a.Resolution.CompareTo(b.Resolution));
                ///
                StartCoroutine(LoadVideoFromUrlCrt(allMp4Video[0].DownloadUrl, path, onSuccess));
            }));
        }
        else
        {
            StartCoroutine(LoadVideoFromUrlCrt(url, path, onSuccess));
        }


    }

    IEnumerator LoadVideoFromUrlCrt(string url, string path, Action<string> onSuccess)
    {
        var www = new WWW(url);
        yield return www;
        if (www.isDone && string.IsNullOrEmpty(www.error))
        {
            Debug.Log("Load xong video " + url);
            SaveVideo(path, www.bytes);
            //File.WriteAllBytes(path, www.bytes);
            yield return new WaitForSeconds(5F);
#if UNITY_ANDROID
            onSuccess?.Invoke(path);
            yield break;
#else
            //return Path.Combine("file://" + path);
            onSuccess?.Invoke("file://" + path);
            yield break;
#endif


        }
        else
        {
            onSuccess?.Invoke("");
        }
    }

    /*
    private async Task<string> LoadVideo(string url)
    {

        string hashName = MD5Hash(url);
        var path = Application.persistentDataPath + "/" + hashName + ".mp4";
        if (File.Exists(path))
        {
#if UNITY_ANDROID
            return path;
#else
            return Path.Combine("file://" + path);
#endif
        }
        var www = new WWW(url);
        while (!www.isDone)
        {
            print(" loading video ...  " + www.progress);
        }
        //File.WriteAllBytes(path, www.bytes);
        using (FileStream sourceStream = new FileStream(path,
             System.IO.FileMode.Append, FileAccess.Write, FileShare.None,
             bufferSize: 4096, useAsync: true))
        {
            await sourceStream.WriteAsync(www.bytes, 0, www.bytes.Length);
        };
#if UNITY_ANDROID
        string returnUrl = path;
#else
        string returnUrl = Path.Combine("file://" + path);
#endif
        return returnUrl;
    }
    */

    public static string MD5Hash(string input)
    {
        StringBuilder hash = new StringBuilder();
        MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
        byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

        for (int i = 0; i < bytes.Length; i++)
        {
            hash.Append(bytes[i].ToString("x2"));
        }
        return hash.ToString();
    }
    private static AndroidJavaObject currentActivity
    {

        get
        {
            return new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
        }

    }
    public static void OpenGooglePlay(string packageName)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            try
            {
                Application.OpenURL("market://details?id=" + packageName);
            }
            catch (Exception ee)
            {
                Debug.Log("Lỗi mở google play " + ee);
                Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
            }
        }
        else
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
        }
    }
    public static bool isInstalledApp(string packageName)
    {
        if (Application.platform != RuntimePlatform.Android)
            return false;
        try
        {
            currentActivity.Call<AndroidJavaObject>("getPackageManager").Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
            Debug.Log(" Bingo Pakage " + packageName + " đã cài đặt");
            return true;
        }
        catch
        {
            Debug.Log("Pakage " + packageName + " chưa cài đặt");
            return false;
        }
    }
    /*
    public static List<AndroidNativeFunctions.PackageInfo> GetInstalledApps()
    {
        if (Application.platform != RuntimePlatform.Android)
            return null;
        AndroidJavaObject packages = currentActivity.Call<AndroidJavaObject>("getPackageManager").Call<AndroidJavaObject>("getInstalledPackages", 0);
        int size = packages.Call<int>("size");
        List<PackageInfo> list = new List<PackageInfo>();
        for (int i = 0; i < size; i++)
        {
            AndroidJavaObject info = packages.Call<AndroidJavaObject>("get", i);
            PackageInfo packageInfo = new PackageInfo
            {
                firstInstallTime = info.Get<long>("firstInstallTime"),
                packageName = info.Get<string>("packageName"),
                lastUpdateTime = info.Get<long>("lastUpdateTime"),
                versionCode = info.Get<int>("versionCode"),
                versionName = info.Get<string>("versionName")
            };
            list.Add(packageInfo);
        }
        return list;
    }
    */

    void SaveVideo(string path, byte[] bytes)
    {

        Debug.Log("Save video with path " + path);
        SaveVideoJob svj = new SaveVideoJob
        {
            path = new NativeArray<char>(path.ToCharArray(), Allocator.Persistent),
            bytes = new NativeArray<byte>(bytes, Allocator.Persistent)
        };
        JobHandle handle = svj.Schedule();
        handle.Complete();

        //File.WriteAllBytes(path, bytes);
    }


    /// ----------------------------
    private class DownloadUrlResponse
    {
        public string data = null;
        public bool isValid = false;
        public long httpCode = 0;
        public DownloadUrlResponse() { data = null; isValid = false; httpCode = 0; }
    }
    string jsUrl;
    string jsonForHtmlVersion;
    private DownloadUrlResponse downloadYoutubeUrlResponse;
    IEnumerator DownloadYoutubeUrl(string url, Action<VideoInfo[]> onSuccess)
    {
        downloadYoutubeUrlResponse = new DownloadUrlResponse();
        var videoId = url.Replace("https://youtube.com/watch?v=", "");

        var newUrl = "https://www.youtube.com/watch?v=" + videoId + "&gl=US&hl=en&has_verified=1&bpctr=9999999999";
        UnityWebRequest request = UnityWebRequest.Get(newUrl);
        //request.SetRequestHeader("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:10.0) Gecko/20100101 Firefox/10.0 (Chrome)");
        request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Linux; Android 12) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.87 Mobile Safari/537.36");
        yield return request.SendWebRequest();
        downloadYoutubeUrlResponse.httpCode = request.responseCode;
        if (request.result == UnityWebRequest.Result.ConnectionError) { Debug.Log("Youtube UnityWebRequest isNetworkError!"); }
        else if (request.result == UnityWebRequest.Result.ProtocolError) { Debug.Log("Youtube UnityWebRequest isHttpError!"); }
        else if (request.responseCode == 200)
        {

            //Debug.Log("Youtube UnityWebRequest responseCode 200: OK!");
            if (request.downloadHandler != null && request.downloadHandler.text != null)
            {
                if (request.downloadHandler.isDone)
                {
                    downloadYoutubeUrlResponse.isValid = true;
                    jsonForHtmlVersion = request.downloadHandler.text;
                    Debug.Log("JsonForHtmlVersion = " + jsonForHtmlVersion);
                    downloadYoutubeUrlResponse.data = request.downloadHandler.text;
                    Debug.Log("Data = " + downloadYoutubeUrlResponse.data);

                }
            }
            else { Debug.Log("Youtube UnityWebRequest Null response"); }
        }
        else
        { Debug.Log("Youtube UnityWebRequest responseCode:" + request.responseCode); }
        if (!string.IsNullOrEmpty(jsonForHtmlVersion))
        {
            StartCoroutine(YoutubeURLDownloadFinished(url, onSuccess));
        }
    }

    IEnumerator YoutubeURLDownloadFinished(string _url, Action<VideoInfo[]> onSuccess)
    {
        var videoId = _url.Replace("https://youtube.com/watch?v=", "");
        //jsonforHtml
        var player_response = string.Empty;
        if (Regex.IsMatch(jsonForHtmlVersion, @"[""\']status[""\']\s*:\s*[""\']LOGIN_REQUIRED"))
        {
            Debug.Log("MM");
            var url = "https://www.youtube.com/get_video_info?video_id=" + videoId + "&eurl=https://youtube.googleapis.com/v/" + videoId;
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Linux; Android 12) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.87 Mobile Safari/537.36");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError) { Debug.Log("Youtube UnityWebRequest isNetworkError!"); }
            else if (request.result == UnityWebRequest.Result.DataProcessingError) { Debug.Log("Youtube UnityWebRequest isHttpError!"); }
            else if (request.responseCode == 200)
            {
                //ok;
            }
            else
            { Debug.Log("Youtube UnityWebRequest responseCode:" + request.responseCode); }

            player_response = UnityWebRequest.UnEscapeURL(ParseQueryString(request.downloadHandler.text)["player_response"]);
        }
        else
        {
            var dataRegexOption = new Regex(@"ytplayer\.config\s*=\s*(\{.+?\});", RegexOptions.Multiline);
            var dataMatch = dataRegexOption.Match(jsonForHtmlVersion);
            if (dataMatch.Success)
            {
                string extractedJson = dataMatch.Result("$1");
                if (!extractedJson.Contains("raw_player_response:ytInitialPlayerResponse"))
                {
                    //player_response = JObject.Parse(extractedJson)["args"]["player_response"].ToString();

                }
            }

            dataRegexOption = new Regex(@"ytInitialPlayerResponse\s*=\s*({.+?})\s*;\s*(?:var\s+meta|</script|\n)", RegexOptions.Multiline);
            dataMatch = dataRegexOption.Match(jsonForHtmlVersion);
            if (dataMatch.Success)
            {
                player_response = dataMatch.Result("$1");
            }

            dataRegexOption = new Regex(@"ytInitialPlayerResponse\s*=\s*({.+?})\s*;", RegexOptions.Multiline);
            dataMatch = dataRegexOption.Match(jsonForHtmlVersion);
            if (dataMatch.Success)
            {
                player_response = dataMatch.Result("$1");
            }
        }

        //File.WriteAllText("D:\\Test.txt", player_response.ToString());
        JObject json = JObject.Parse(player_response);


        IEnumerable<ExtractionInfo> downloadUrls = ExtractDownloadUrls(json);
        VideoInfo[] videoInfos = GetVideoInfos(downloadUrls, "quangcao").ToArray();
        onSuccess?.Invoke(videoInfos);
    }
    public static bool TryNormalizeYoutubeUrl(string url, out string normalizedUrl)
    {
        url = url.Trim();

        url = url.Replace("youtu.be/", "youtube.com/watch?v=");
        url = url.Replace("www.youtube", "youtube");
        url = url.Replace("youtube.com/embed/", "youtube.com/watch?v=");

        if (url.Contains("/v/"))
        {
            url = "https://youtube.com" + new Uri(url).AbsolutePath.Replace("/v/", "/watch?v=");
        }

        url = url.Replace("/watch#", "/watch?");

        IDictionary<string, string> query = ParseQueryString(url);

        string v;

        if (!query.TryGetValue("v", out v))
        {
            normalizedUrl = null;
            return false;
        }

        normalizedUrl = "https://youtube.com/watch?v=" + v;

        return true;
    }
    public static IDictionary<string, string> ParseQueryString(string s)
    {
        // remove anything other than query string from url
        if (s.StartsWith("http") && s.Contains("?"))
        {
            s = s.Substring(s.IndexOf('?') + 1);
        }
        //Debug.Log("ADDAAP "+ s);

        var dictionary = new Dictionary<string, string>();

        foreach (string vp in Regex.Split(s, "&"))
        {
            string[] strings = Regex.Split(vp, "=");
            //dictionary.Add(strings[0], strings.Length == 2 ? UrlDecode(strings[1]) : string.Empty); //old
            string key = strings[0];
            string value = string.Empty;

            if (strings.Length == 2)
                value = strings[1];
            else if (strings.Length > 2)
                value = string.Join("=", strings.Skip(1).Take(strings.Length).ToArray());

            dictionary.Add(key, value);
        }

        return dictionary;
    }
    private class ExtractionInfo
    {
        public bool RequiresDecryption { get; set; }

        public Uri Uri { get; set; }
    }
    public static string SignatureQuery = "sig";
    private const string RateBypassFlag = "ratebypass";
    private static IEnumerable<ExtractionInfo> ExtractDownloadUrls(JObject json)
    {
        List<string> urls = new List<string>();
        List<string> ciphers = new List<string>();
        JObject newJson = json;

        if (newJson["streamingData"]["formats"][0]["cipher"] != null)
        {
            foreach (var j in newJson["streamingData"]["formats"])
            {
                ciphers.Add(j["cipher"].ToString());
            }

            foreach (var j in newJson["streamingData"]["adaptiveFormats"])
            {
                ciphers.Add(j["cipher"].ToString());
            }
        }
        else if (newJson["streamingData"]["formats"][0]["signatureCipher"] != null)
        {
            foreach (var j in newJson["streamingData"]["formats"])
            {
                ciphers.Add(j["signatureCipher"].ToString());
            }

            foreach (var j in newJson["streamingData"]["adaptiveFormats"])
            {
                ciphers.Add(j["signatureCipher"].ToString());
            }
        }
        else
        {
            foreach (var j in newJson["streamingData"]["formats"])
            {
                urls.Add(j["url"].ToString());
            }

            foreach (var j in newJson["streamingData"]["adaptiveFormats"])
            {
                urls.Add(j["url"].ToString());
            }
        }

        foreach (string s in ciphers)
        {
            IDictionary<string, string> queries = ParseQueryString(s);

            string url;

            bool requiresDecryption = false;

            if (queries.ContainsKey("sp"))
                SignatureQuery = "sig";
            else
                SignatureQuery = "signatures";




            if (queries.ContainsKey("s") || queries.ContainsKey("signature"))
            {
                requiresDecryption = queries.ContainsKey("s");
                string signature = queries.ContainsKey("s") ? queries["s"] : queries["signature"];

                //if (sp != "none")
                //{
                url = string.Format("{0}&{1}={2}", queries["url"], SignatureQuery, signature);
                //}
                //else
                //{
                //url = string.Format("{0}&{1}={2}", queries["url"], SignatureQuery, signature);
                //}


                string fallbackHost = queries.ContainsKey("fallback_host") ? "&fallback_host=" + queries["fallback_host"] : string.Empty;

                url += fallbackHost;
            }

            else
            {
                url = queries["url"];
            }

            url = UrlDecode(url);
            url = UrlDecode(url);

            IDictionary<string, string> parameters = ParseQueryString(url);
            if (!parameters.ContainsKey(RateBypassFlag))
                url += string.Format("&{0}={1}", RateBypassFlag, "yes");
            yield return new ExtractionInfo { RequiresDecryption = requiresDecryption, Uri = new Uri(url) };
        }

        foreach (string s in urls)
        {
            string url = s;
            url = UrlDecode(url);
            url = UrlDecode(url);

            IDictionary<string, string> parameters = ParseQueryString(url);
            if (!parameters.ContainsKey(RateBypassFlag))
                url += string.Format("&{0}={1}", RateBypassFlag, "yes");
            yield return new ExtractionInfo { RequiresDecryption = false, Uri = new Uri(url) };
        }
    }
    private static IEnumerable<VideoInfo> GetVideoInfos(IEnumerable<ExtractionInfo> extractionInfos, string videoTitle)
    {
        var downLoadInfos = new List<VideoInfo>();

        foreach (ExtractionInfo extractionInfo in extractionInfos)
        {
            string itag = ParseQueryString(extractionInfo.Uri.Query)["itag"];

            int formatCode = int.Parse(itag);

            VideoInfo info = VideoInfo.Defaults.SingleOrDefault(videoInfo => videoInfo.FormatCode == formatCode);

            if (info != null)
            {
                info = new VideoInfo(info)
                {
                    DownloadUrl = extractionInfo.Uri.ToString(),
                    Title = videoTitle,
                    RequiresDecryption = extractionInfo.RequiresDecryption
                };
            }

            else
            {
                info = new VideoInfo(formatCode)
                {
                    DownloadUrl = extractionInfo.Uri.ToString()
                };
            }

            downLoadInfos.Add(info);
        }

        return downLoadInfos;
    }
    public static string UrlDecode(string url)
    {

        return UnityWebRequest.UnEscapeURL(url);
    }

    public override void Init(bool isTest)
    {
        //throw new NotImplementedException();
    }
}

public struct StringComponent
{
    public string Value;
}
public struct SaveVideoJob : IJob
{
    public NativeArray<char> path;
    public NativeArray<byte> bytes;
    public void Execute()
    {
        try
        {
            string _path = new string(path.ToArray());
            Debug.Log("Path là " + _path);
            File.WriteAllBytes(_path, bytes.ToArray());
        }
        catch (System.Exception ee)
        {
            Debug.Log("Lỗi " + ee);
        }
    }
}


[System.Serializable]
public class RelatedAppsDataModule
{
    public List<RelatedAppInfoModule> Apps;
}


[System.Serializable]
public class RelatedAppInfoModule
{
    public bool Show = true;
    public string AppName;
    public string StoreUrl;
    public string VideoUrl;
    public string IconUrl;
    public string StoreUrlIOS;
    public string PakageName
    {
        get
        {
            return StoreUrl.Replace("https://play.google.com/store/apps/details?id=", "");
        }
    }
}
