using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Notifications;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Beamable.Server.Api.Notifications
{
	public class MicroserviceNotificationApi : IMicroserviceNotificationsApi
	{
		public IBeamableRequester Requester { get; }
		public RequestContext Context { get; }

		public MicroserviceNotificationApi(IBeamableRequester requester, RequestContext context)
		{
			Requester = requester;
			Context = context;
		}

		public Promise<EmptyResponse> NotifyPlayer(long dbid, string subscriptionId, string messagePayload)
		{
			var payload = CreateStringPayload(messagePayload);
			var jsonSerializedPayload = JsonConvert.SerializeObject(payload, Formatting.None, UnitySerializationSettings.Instance);
			return SendPlayerNotificationJson(dbid, subscriptionId, jsonSerializedPayload);
		}

		public Promise<EmptyResponse> NotifyPlayer(List<long> dbids, string subscriptionId, string messagePayload)
		{
			var payload = CreateStringPayload(messagePayload);
			var jsonSerializedPayload = JsonConvert.SerializeObject(payload, Formatting.None, UnitySerializationSettings.Instance);
			return SendPlayerNotificationJson(dbids, subscriptionId, jsonSerializedPayload);
		}

		public Promise<EmptyResponse> NotifyPlayer<T>(long dbid, string subscriptionId, T messagePayload)
		{
			var jsonSerializedPayload = JsonConvert.SerializeObject(messagePayload, Formatting.None, UnitySerializationSettings.Instance);
			return SendPlayerNotificationJson(dbid, subscriptionId, jsonSerializedPayload);
		}

		public Promise<EmptyResponse> NotifyPlayer<T>(List<long> dbids, string subscriptionId, T messagePayload)
		{
			var jsonSerializedPayload = JsonConvert.SerializeObject(messagePayload, Formatting.None, UnitySerializationSettings.Instance);
			return SendPlayerNotificationJson(dbids, subscriptionId, jsonSerializedPayload);
		}

		private PrimitiveStringPayload CreateStringPayload(string message)
		{
			return new PrimitiveStringPayload
			{
				stringValue = message
			};
		}

		private Promise<EmptyResponse> SendPlayerNotificationJson(long dbid, string subscriptionId, string json)
		{
			return Requester.Request<EmptyResponse>(Method.POST, "/basic/notification/player",
				new PlayerNotificationRequest
				{
					dbid = dbid,
					payload = new PlayerNotificationPayload { context = subscriptionId, messageFull = json }
				});
		}
		private Promise<EmptyResponse> SendPlayerNotificationJson(List<long> dbids, string subscriptionId, string json)
		{
			return Requester.Request<EmptyResponse>(Method.POST, "/basic/notification/player",
				new PlayerBatchNotificationRequest
				{
					dbids = dbids,
					payload = new PlayerNotificationPayload { context = subscriptionId, messageFull = json }
				});
		}


	}


	/// <summary>
	/// Notification request format.
	/// </summary>
	[Serializable]
	public class PlayerNotificationRequest
	{
		public long dbid;
		public PlayerNotificationPayload payload;
	}

	/// <summary>
	/// Format of the Notification request when notifying multiple players.
	/// </summary>
	[Serializable]
	public class PlayerBatchNotificationRequest
	{
		public List<long> dbids;
		public PlayerNotificationPayload payload;
	}

	/// <summary>
	/// Structure representing the expected format for the notification we are sending.
	/// </summary>
	[Serializable]
	public class PlayerNotificationPayload
	{
		public string messageFull;
		public string context;
	}
}
