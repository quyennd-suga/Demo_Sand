using Firebase.Messaging;
using Firebase.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;



#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif
using UnityEngine;

public class SuNotification : BaseSUUnit
{


    public static Action OnInitCompleted;
    private string AndroidChanelName;
    private string AndroidChanelID;
    LocalNotificationDataModule LocalNotificationData;
    public static string DeviceTokenID;
    public override void Init(bool isTest)
    {
        if (!EnableSU)
            return;

#if UNITY_ANDROID
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;

#endif

    }

    public void InitNotification()
    {
        if (!EnableSU)
            return;
#if UNITY_ANDROID
        InitAndroidNotification();
#elif UNITY_IOS
        InitAndSendIOSNotification();
#endif
    }

#if UNITY_IOS
    void InitAndSendIOSNotification()
    {
        StartCoroutine(RequestAuthorization());
    }
    public void ScheduleIOSNotification()
    {
        for (int i = 0; i < GameManager.generalConfig.notification_config.number_of_times; i++)
        {
            string title = "Tangle Rope";
            string message = GameManager.generalConfig.notification_config.messages[DataManager.data.messageNoti % GameManager.generalConfig.notification_config.messages.Length];
            DataManager.data.messageNoti++;
            long time = GameManager.generalConfig.notification_config.period * (i + 1);
            DateTime fireTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(time);
            string identifier = "tanglerope_" + i;
            SendIOSLocalNotification(identifier, title, message, fireTime);
        }
    }    
    void SendIOSLocalNotification(string identifier,string title, string mes, DateTime time)
    {
        var timeTrigger = new iOSNotificationCalendarTrigger()
        {
            //TimeInterval = System.DateTime.Now.AddDays(i + 1).AddMinutes(5).TimeOfDay,
            Repeats = false,
            Year = time.Year,
            Month = time.Month,
            Day = time.Day,
            Hour = time.Hour,
            Minute = time.Minute,
            Second = time.Second
        };

        var notification = new iOSNotification()
        {
            // You can specify a custom identifier which can be used to manage the notification later.
            // If you don't provide one, a unique string will be generated automatically.
            Identifier = identifier,
            Title = title,
            Body = mes,
            //Subtitle = "Cái này là subtitlt, bên android không có",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
    }

    IEnumerator RequestAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            };

            string res = "\n RequestAuthorization:";
            res += "\n finished: " + req.IsFinished;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log(res);

            
            yield return new WaitForEndOfFrame();
            if (req.Granted)
            {
                iOSNotificationCenter.RemoveAllDeliveredNotifications();
                iOSNotificationCenter.RemoveAllScheduledNotifications();
                yield return new WaitForSeconds(1f);
                Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
                Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
                acceptNotification = true;
                ScheduleIOSNotification();
            }
        }
    }
    private bool acceptNotification;

    public void CancelRefillNoti()
    {
        if(acceptNotification && DataManager.data.notiRefillId != string.Empty && DataManager.data.notiRefillId != "" && DataManager.data.notiRefillId != null)
        {
            LogManager.Log("Cancel refill noti");
            iOSNotificationCenter.RemoveScheduledNotification(DataManager.data.notiRefillId);
        }    
    }
    public void ScheduleRefillNotification(string title, string message,DateTime time)
    {
        if(acceptNotification == false)
        {
            return;
        }
        LogManager.Log("Schedule refill noti");
        var timeTrigger = new iOSNotificationCalendarTrigger()
        {
            Repeats = false,
            Year = time.Year,
            Month = time.Month,
            Day = time.Day,
            Hour = time.Hour,
            Minute = time.Minute,
            Second = time.Second
        };

        var notification = new iOSNotification()
        {
            // You can specify a custom identifier which can be used to manage the notification later.
            // If you don't provide one, a unique string will be generated automatically.
            Identifier = "tanglerope_refill",
            Title = title,
            Body = message,
            //Subtitle = "Cái này là subtitlt, bên android không có",
            ShowInForeground = false,
            //ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };
        DataManager.data.notiRefillId = notification.Identifier;
        iOSNotificationCenter.ScheduleNotification(notification);
    }
#endif


#if UNITY_ANDROID
    public void InitAndroidNotification()
    {
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        AndroidNotificationCenter.CancelAllNotifications();
        // lấy data của notification lần trước push
        var notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();
        if (notificationIntentData != null)
        {

            var id = notificationIntentData.Id;
            var channel = notificationIntentData.Channel;
            var notification = notificationIntentData.Notification;
            string extraData = notification.IntentData;
            //
            // Nếu muốn làm gì khi user mở app bằng local notification thì code ở đây
            //
        }
        StartCoroutine(RequestNotificationPermission());



    }
    private bool isInitializedNotify = false;
    IEnumerator RequestNotificationPermission()
    {
        var wait = new WaitForSeconds(0.5f);
        var request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
            yield return wait;
        // here use request.Status to determine users response
        if (request.Status == PermissionStatus.Allowed)
        {
            InitLocalNotification();
            // send local notification
            LogManager.Log("Allow notificationnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn");
            isInitializedNotify = true;
            ScheduleAndoidNotification();
            OnInitCompleted?.Invoke();
        }

    }

    public void CancelRefillNoti()
    {
        if(isInitializedNotify)
            AndroidNotificationCenter.CancelNotification(DataManager.data.notiRefillId);
    }
    public void ScheduleRefillNotification(string title, string message, string extraData, DateTime fireTime, bool foreground)
    {
        if (isInitializedNotify == false)
            return;
        //DateTime fireTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(GlobalValues.lifeTime);
        var notification = new AndroidNotification
        {
            Title = title,
            Text = message,
            FireTime = fireTime,
            //LargeIcon = "icon_0_large",
            //SmallIcon = "small icon name",
            Style = NotificationStyle.BigTextStyle,
            // set thêm color để trên android abcxyz nào đó không bị hiện ô vuông hay hình tròn background trùng màu với small icon
            Color = Color.red,
            IntentData = extraData,
            ShowInForeground = true

        };
        DataManager.data.notiRefillId = AndroidNotificationCenter.SendNotification(notification, AndroidChanelID);
    }
    private void ScheduleAndoidNotification()
    {
        for (int i = 0; i < GameController.generalConfig.notification_config.number_of_times; i++)
        {
            string title = "Tangle Rope";
            string message = GameController.generalConfig.notification_config.messages[DataManager.data.messageNoti % GameController.generalConfig.notification_config.messages.Length];
            DataManager.data.messageNoti++;
            string extraData = "";
            DateTime fireTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(GameController.generalConfig.notification_config.period);
            SendAndroidLocalNotification(title, message, extraData, fireTime, true);
        }
    }
    void InitLocalNotification()
    {
        AndroidChanelName = Application.productName;
        AndroidChanelID = Application.identifier;
        var channel = new AndroidNotificationChannel()
        {
            Id = AndroidChanelID,
            Name = AndroidChanelName,
            Importance = Importance.High,
            Description = Application.productName + " chanel",
            CanShowBadge = true,
            EnableLights = true,
            LockScreenVisibility = LockScreenVisibility.Public,
            EnableVibration = false,
            CanBypassDnd = true,


        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    public void SendAndroidLocalNotification(string title, string mes, string extraData, DateTime time, bool foreground)
    {
        var notification = new AndroidNotification
        {
            Title = title,
            Text = mes,
            FireTime = time,
            //LargeIcon = "icon_0_large",
            //SmallIcon = "small icon name",
            Style = NotificationStyle.BigTextStyle,
            // set thêm color để trên android abcxyz nào đó không bị hiện ô vuông hay hình tròn background trùng màu với small icon
            Color = Color.red,
            IntentData = extraData,
            ShowInForeground = foreground

        };
        AndroidNotificationCenter.SendNotification(notification, AndroidChanelID);
    }
#endif



    void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        UnityEngine.Debug.Log("Received Registration Token: " + token.Token);
        DeviceTokenID = token.Token;
        //
        // lưu token hay làm gì đó với token thì code ở đây 
        //
    }

    void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {
        foreach (string key in e.Message.Data.Keys)
        {
            Debug.Log("Data của notification nhận được là " + key + " : " + e.Message.Data[key]);
            string data = e.Message.Data[key];
            // cần làm gì đó với data gửi từ firebase notification thì code ở đây

        }
    }






    //New Method to Implement Notify (Testing):


}

[System.Serializable]
public class LocalNotificationDataModule
{
    public List<LocalNotificationDataItemModule> Notifications;
}

[System.Serializable]
public class LocalNotificationDataItemModule
{
    public string title;
    public string mess;
    public int HoursAdd, MinutesAdd, DaysAdd, SecsAdd;
}