using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;

namespace Beamable.Server.Api.Notifications
{

	public interface IMicroserviceNotificationsApi
	{
		Promise<EmptyResponse> NotifyPlayer(long dbid, string subscriptionId, string messagePayload);
		Promise<EmptyResponse> NotifyPlayer(List<long> dbids, string subscriptionId, string messagePayload);

		Promise<EmptyResponse> NotifyPlayer<T>(long dbid, string subscriptionId, T messagePayload);
		Promise<EmptyResponse> NotifyPlayer<T>(List<long> dbids, string subscriptionId, T messagePayload);
	}


	[Serializable]
	public class PlayerNotification
	{
		public long dbid;
		public PlayerNotificationPayload payload;
	}

	[Serializable]
	public class PlayerBatchNotification
	{
		public List<long> dbids;
		public PlayerNotificationPayload payload;
	}

	[Serializable]
	public class PlayerNotificationPayload
	{
		public string messageFull;
		public string context;
	}
}
