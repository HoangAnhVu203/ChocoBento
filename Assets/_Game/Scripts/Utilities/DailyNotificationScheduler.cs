using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class DailyNotificationScheduler : MonoBehaviour
{
    public static DailyNotificationScheduler Instance;

    private const string KEY_LAST_6H_SCHEDULE_DAY = "LAST_6H_SCHEDULE_DAY";

    [Header("Allowed Notify Time")]
    [SerializeField] private int earliestHour = 8;
    [SerializeField] private int latestHour = 21;

    [Header("Notification Text")]
    [SerializeField] private string title = "ChocoBento";

    [TextArea(2, 4)]
    [SerializeField] private string body6h = "Your choco reward is ready to unwrap! 🎁";

    [TextArea(2, 4)]
    [SerializeField] private string body24h = "A fresh bento tray is waiting for your chocolate cuts! 🍫";

    [TextArea(2, 4)]
    [SerializeField] private string body3d = "The chocolate pieces miss your perfect slicing. Come solve a tray! ⭐";

    [TextArea(2, 4)]
    [SerializeField] private string body7d = "Your ChocoBento box has a sweet reward inside! 🎁";

    [TextArea(2, 4)]
    [SerializeField] private string body14d = "A special chocolate tray is ready. Time to fill the bento! 🍓";

#if UNITY_ANDROID
    private const string CHANNEL_ID = "smart_reminder_channel";
    private PermissionRequest androidPermissionRequest;
#endif

#if UNITY_IOS
    private AuthorizationRequest iOSAuthorizationRequest;
#endif

    private Coroutine permissionAndScheduleCR;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeAndSchedule();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            InitializeAndSchedule();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            ScheduleNotificationsIfAllowed();
        else
            InitializeAndSchedule();
    }

    private void OnApplicationQuit()
    {
        ScheduleNotificationsIfAllowed();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

#if UNITY_IOS
        iOSAuthorizationRequest?.Dispose();
        iOSAuthorizationRequest = null;
#endif
    }

    public void InitializeAndSchedule()
    {
        RegisterChannelIfNeeded();

        if (permissionAndScheduleCR != null)
            return;

        permissionAndScheduleCR = StartCoroutine(RequestPermissionAndScheduleCR());
    }

    private IEnumerator RequestPermissionAndScheduleCR()
    {
    #if UNITY_ANDROID
        if (androidPermissionRequest != null &&
            androidPermissionRequest.Status == PermissionStatus.RequestPending)
        {
            while (androidPermissionRequest.Status == PermissionStatus.RequestPending)
                yield return null;
        }
        else if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.NotRequested)
        {
            androidPermissionRequest = new PermissionRequest();

            while (androidPermissionRequest.Status == PermissionStatus.RequestPending)
                yield return null;
        }
    #endif

    #if UNITY_IOS
        if (iOSAuthorizationRequest != null && !iOSAuthorizationRequest.IsFinished)
        {
            while (!iOSAuthorizationRequest.IsFinished)
                yield return null;
        }

        var settings = iOSNotificationCenter.GetNotificationSettings();

        if (settings.AuthorizationStatus == AuthorizationStatus.NotDetermined)
        {
            iOSAuthorizationRequest?.Dispose();

            iOSAuthorizationRequest = new AuthorizationRequest(
                AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound,
                false
            );

            while (!iOSAuthorizationRequest.IsFinished)
                yield return null;
        }
    #endif

        ScheduleNotificationsIfAllowed();

        permissionAndScheduleCR = null;

        yield break;
    }

    private void RegisterChannelIfNeeded()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.Initialize();

        var channel = new AndroidNotificationChannel
        {
            Id = CHANNEL_ID,
            Name = "ChocoBento Reminders",
            Importance = Importance.Default,
            Description = "Sweet ChocoBento reminder notifications"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    private void ScheduleNotificationsIfAllowed()
    {
        if (!CanScheduleNotifications())
            return;

        ScheduleSmartNotifications();
    }

    private bool CanScheduleNotifications()
    {
#if UNITY_ANDROID
        if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
            return false;
#endif

#if UNITY_IOS
        var settings = iOSNotificationCenter.GetNotificationSettings();

        if (settings.AuthorizationStatus != AuthorizationStatus.Authorized &&
            settings.AuthorizationStatus != AuthorizationStatus.Provisional &&
            settings.AuthorizationStatus != AuthorizationStatus.Ephemeral)
        {
            return false;
        }
#endif

        return true;
    }

    public void ScheduleSmartNotifications()
    {
        CancelAllScheduledNotifications();

        if (CanSchedule6hToday())
        {
            ScheduleOne(
                GetSafeFireTime(DateTime.Now.AddHours(6)),
                title,
                body6h,
                "notify_6h"
            );

            Mark6hScheduledToday();
        }

        ScheduleOne(
            GetSafeFireTime(DateTime.Now.AddDays(1)),
            title,
            body24h,
            "notify_24h"
        );

        ScheduleOne(
            GetSafeFireTime(DateTime.Now.AddDays(3)),
            title,
            body3d,
            "notify_3d"
        );

        ScheduleOne(
            GetSafeFireTime(DateTime.Now.AddDays(7)),
            title,
            body7d,
            "notify_7d"
        );

        ScheduleOne(
            GetSafeFireTime(DateTime.Now.AddDays(14)),
            title,
            body14d,
            "notify_14d"
        );
    }

    private bool CanSchedule6hToday()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        string lastDay = PlayerPrefs.GetString(KEY_LAST_6H_SCHEDULE_DAY, "");

        return lastDay != today;
    }

    private void Mark6hScheduledToday()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        PlayerPrefs.SetString(KEY_LAST_6H_SCHEDULE_DAY, today);
        PlayerPrefs.Save();
    }

    private DateTime GetSafeFireTime(DateTime target)
    {
        if (target.Hour < earliestHour)
        {
            target = target.Date.AddHours(earliestHour);
        }
        else if (target.Hour >= latestHour)
        {
            target = target.Date.AddDays(1).AddHours(earliestHour);
        }

        return target;
    }

    private void ScheduleOne(DateTime fireTime, string title, string body, string identifier)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = body,
            FireTime = fireTime,
            ShouldAutoCancel = true,
            SmallIcon = "default",
            LargeIcon = "default"
        };

        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
#endif

#if UNITY_IOS
        TimeSpan timeUntilFire = fireTime - DateTime.Now;

        if (timeUntilFire.TotalSeconds < 60)
            timeUntilFire = TimeSpan.FromMinutes(1);

        var trigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = timeUntilFire,
            Repeats = false
        };

        var notification = new iOSNotification
        {
            Identifier = identifier,
            Title = title,
            Body = body,
            ShowInForeground = false,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = trigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public void CancelAllScheduledNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif

#if UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
    }
}
