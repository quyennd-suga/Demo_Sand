
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

    public class RelatedAppsItem : MonoBehaviour
    {
        public VideoPlayer vPlayer;
        public RawImage rImg;
        public Image iLoading;
        private RelatedAppInfoModule _appInfo;
        public RelatedAppInfoModule AppInfo
        {
            get
            {
                return _appInfo;
            }
            set
            {
                _appInfo = value;
            }
        }

        private void Awake()
        {

            vPlayer.waitForFirstFrame = true;
        }
        private void OnEnable()
        {
            iLoading.enabled = true;
        }
        public void OnClick()
        {
            if (AppInfo != null)
            {
#if UNITY_ANDROID
                string pakageName = AppInfo.StoreUrl.Replace("https://play.google.com/store/apps/details?id=", "");

                SuRelatedApps.OpenGooglePlay(pakageName);
                /*
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {

                };
                AppsFlyer.attributeAndOpenStore(pakageName, "crossPromotion", parameters, this);
                */
#elif UNITY_IOS
            if (!string.IsNullOrEmpty(AppInfo.StoreUrlIOS))
            {
                Application.OpenURL(AppInfo.StoreUrlIOS);
            }
#endif
            }
        }

        public void PlayVideo()
        {
            Debug.Log("Play video , vPlayer enable = " + vPlayer.enabled + " gameobject enable = " + gameObject.activeInHierarchy);
            if (vPlayer.isPrepared)
            {
                iLoading.enabled = false;
                rImg.texture = vPlayer.texture;
                vPlayer.Play();
                vPlayer.isLooping = true;
            }
            else
            {
                Debug.Log("Prepare");
                vPlayer.prepareCompleted += VPlayer_prepareCompleted;
                vPlayer.errorReceived += VPlayer_errorReceived;
                vPlayer.Prepare();
            }
        }

        private void VPlayer_errorReceived(VideoPlayer source, string message)
        {
            Debug.Log("Không thể play video " + message);
            // nếu lỗi không play được video thì load lại video
            SuRelatedApps.instance.StartCoroutine(SuRelatedApps.instance.LoadVideoCrt(AppInfo.VideoUrl, vUrl =>
            {
                if (!string.IsNullOrEmpty(vUrl))
                {
                    Debug.Log("Url là " + vUrl);
                    vPlayer.url = vUrl;
                    if (gameObject.activeInHierarchy)
                    {
                        PlayVideo();
                    }
                }
            }));
        }

        private void VPlayer_prepareCompleted(VideoPlayer source)
        {
            Debug.Log("Prepare video complete");
            iLoading.enabled = false;
            rImg.texture = source.texture;
            source.Play();
            source.isLooping = true;
        }

    }
