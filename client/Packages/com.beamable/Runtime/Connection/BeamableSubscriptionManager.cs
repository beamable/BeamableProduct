using Beamable.Common.Api.Notifications;
using Beamable.Serialization.SmallerJSON;

namespace Connection
{
	public class BeamableSubscriptionManager
	{
		private readonly INotificationService _notificationService;

		public BeamableSubscriptionManager(IBeamableConnection connection, INotificationService notificationService)
		{
			_notificationService = notificationService;

			connection.Message += HandleMessage;
		}

		private void HandleMessage(string message)
		{
			var deserialized = (ArrayDict)Json.Deserialize(message);

			_notificationService.Publish(
				deserialized["context"] as string,
				deserialized["payload"]
			);
		}
	}
}
