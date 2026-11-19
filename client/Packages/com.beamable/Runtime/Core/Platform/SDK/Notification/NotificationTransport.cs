using Beamable.Common;

namespace Beamable.Api.Notification
{
	/// <summary>
	/// The channel the realm sends the player's notifications on. The <see cref="BeamContext"/> sets it
	/// once it has the realm's config, before it connects to that channel.
	/// </summary>
	public class NotificationTransport
	{
		/// <summary>
		/// Completes with true when the realm sends notifications over the Beamable websocket,
		/// and with false when it sends them over PubNub.
		/// </summary>
		public Promise<bool> IsUsingBeamableNotifications { get; } = new Promise<bool>();
	}
}
