using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Notifications
{
	public interface INotificationService
	{
		// TODO: Document these methods
		void Subscribe(string name, Action<object> callback);

		void Unsubscribe(string name, Action<object> handler);

		void Publish(string name, object payload);
		void CreateNotificationChannel(string id, string name, string description);

		void ScheduleLocalNotification(string channel,
									   string key,
									   int trackingId,
									   string title,
									   string message,
									   TimeSpan timeFromNow,
									   bool restrictTime,
									   Dictionary<string, string> customData = null);
		void RegisterForNotifications();
	}

	public static class NotificationServiceExtensions
	{
		public static string GetRefreshEventNameForService(this INotificationService _, string service) =>
		   $"{service}.refresh";
	}
}
