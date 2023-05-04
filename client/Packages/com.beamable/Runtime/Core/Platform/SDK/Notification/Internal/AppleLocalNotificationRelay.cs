#if UNITY_IOS && (NOTIFICATIONS_PACKAGE || !UNITY_2022_1_OR_NEWER)
#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using Beamable.Common.Spew;
using Beamable.Serialization.SmallerJSON;
using System.Text;
#if NOTIFICATIONS_PACKAGE
using Unity.Notifications.iOS;
#else
using UnityEngine.iOS;
#endif

namespace Beamable.Api.Notification.Internal
{
	/// <summary>
	/// Apple local notification relay, for scheduling or cancelling
	/// background notifications that are local to the device.
	/// </summary>
	public class AppleLocalNotificationRelay : ILocalNotificationRelay
	{
#if NOTIFICATIONS_PACKAGE
		int ScheduledLocalNotificationsAmount => iOSNotificationCenter.GetScheduledNotifications().Length;
#else
	   int ScheduledLocalNotificationsAmount => NotificationServices.scheduledLocalNotifications.Length;
#endif
		public const string CancellationKey = "NOTIFICATION_KEY";

		public void CreateNotificationChannel(string id, string name, string description)
		{
			NotificationLogger.LogFormat("Create Notification Channel not implemented on this platform.");
		}

		public void ScheduleNotification(string channel,
		                                 string key,
		                                 string title,
		                                 string message,
		                                 DateTime when,
		                                 Dictionary<string, string> data)
		{
			// Make certain we haven't reached our maximum.

			if (ScheduledLocalNotificationsAmount >= NotificationService.MaxLocalNotifications)
			{
				NotificationLogger.LogFormat("Local Notification Limit of {0} has been reached. Ignoring {1}",
				                             NotificationService.MaxLocalNotifications, key);
				return;
			}

			// Unless we cancel previous ones, the device will show multiple notifications.
			CancelNotification(key);
			Dictionary<string, string> userInfo = null;
			if (data != null)
			{
				userInfo = new Dictionary<string, string>(data) {{CancellationKey, key}};
			}
			else
			{
				userInfo = new Dictionary<string, string>() {{CancellationKey, key}};
			}
#if NOTIFICATIONS_PACKAGE
			var notification = new iOSNotification()
			{
				Title = title,
				Body = message,
				Trigger = new iOSNotificationTimeIntervalTrigger()
				{
					TimeInterval = when - DateTime.Now, Repeats = false
				},
				Data = Json.Serialize(userInfo, new StringBuilder()),
				Identifier = key,
				ShowInForeground = true,
				ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound
			};
			iOSNotificationCenter.ScheduleNotification(notification);
#else
         var note = new LocalNotification
         {
            alertBody = message,
            alertAction = title,
            fireDate = when,
            userInfo = userInfo
         };
         NotificationServices.ScheduleLocalNotification(note);
#endif
		}

		public void CancelNotification(string key)
		{
#if NOTIFICATIONS_PACKAGE
			iOSNotificationCenter.RemoveScheduledNotification(key);
#else
			var notifications = NotificationServices.scheduledLocalNotifications;
			for (var i = 0; i < notifications.Length; i++)
			{
				var note = notifications[i];
				if (key == (string)note.userInfo[CancellationKey])
				{
					NotificationServices.CancelLocalNotification(note);
				}
			}
#endif
		}

		[Obsolete("ClearDeliveredNotifications is deprecated with no replacement."+
				"Instead, cancel notifications individually with CancelNotification.")]
		public void ClearDeliveredNotifications()
		{
			//clearAllNotification();
		}

		//[DllImport("__Internal")]
		//private static extern void clearAllNotification();
	}
}
#pragma warning restore CS0618
#endif
