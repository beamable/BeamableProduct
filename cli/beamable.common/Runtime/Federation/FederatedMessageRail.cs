using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Federation for the Message Rail "last-mile" delivery. A microservice implements this to
	/// deliver a batch of messages (push / email / in-game) and report a per-player funnel result.
	/// </summary>
	public interface IFederatedMessageRail<in T> : IFederation where T : IFederationId, new()
	{
		Promise<MessageRailSendResponse> SendMessage(MessageRailRecipient recipient, MessageRailPayload payload);
		Promise<MessageRailSendResponse> SendMessageBatch(List<MessageRailRecipient> recipients, MessageRailPayload payload);
		Promise<MessageRailRegistrationResponse> RegisterUserWithMessageRail(string playerId, Dictionary<string, string> registrationData);
		Promise<MessageRailRegistrationResponse> UnregisterUserWithMessageRail(string playerId);
	}

	[Serializable]
	public class MessageRailRecipient
	{
		public long gamerTag;

		/// <summary>
		/// The canonical per-recipient join key. The backend sends it on every recipient; the
		/// federation embeds it into the rendered message/deep-links under
		/// <see cref="MessageRailContract.OutreachKey"/> so downstream Opened/Clicked events attribute
		/// back to this exact recipient. Optional on the wire (empty when the producer omits it).
		/// </summary>
		public string outreachId;

		/// <summary>
		/// Ready-made open-tracking pixel URL for this recipient. A rendering federation places it as
		/// <c>&lt;img src="..." width="1" height="1" alt=""&gt;</c> and needs to understand nothing
		/// about it — the URL is opaque, signed by the platform, and identifies this one recipient.
		/// <para>EMPTY means tracking is not configured, and empty must render NO pixel at all: a
		/// broken image in someone's inbox is worse than a missing metric.</para>
		/// <para>Only rails whose engagement signal arrives out of band need this. Push gets Opened
		/// from the handset echoing <see cref="MessageRailContract.OutreachKey"/>, so a push federation
		/// can ignore it; email has no handset, which is why the pixel is the only signal available.</para>
		/// <para>Do NOT build this URL yourself. It carries a signed token whose payload includes the
		/// recipient's gamer tag, which the campaign funnel needs to resolve an account — a
		/// federation-built copy cannot be signed and has historically dropped that field, which makes
		/// every open it records invisible to the campaign funnel.</para>
		/// </summary>
		public string openTrackingUrl;
	}

	[Serializable]
	public class MessageRailPayload
	{
		public string trackId;
		public string externalSystemTrackId;
		public string extraDataFed;
		public string analyticsTrackRef;
	}

	[Serializable]
	public class MessageRailSendResponse
	{
		public List<string> sentPlayers = new List<string>();
		public Dictionary<string, string> sentPayloadFed = new Dictionary<string, string>();
		public Dictionary<string, string> @params = new Dictionary<string, string>();
		public List<MessageRailErrorPlayerStatus> errorPlayersWithStatus = new List<MessageRailErrorPlayerStatus>();

		/// <summary>
		/// The federation's declared max batch size. When it partial-accepts an oversized page it
		/// returns the overflow as retriable <see cref="MessageRailContract.OverCapacityStatus"/> errors
		/// and sets this so the backend caches it and right-sizes later pages. 0 = unspecified.
		/// </summary>
		public int maxBatchSize;
	}

	[Serializable]
	public class MessageRailErrorPlayerStatus
	{
		public string playerId;
		public string status;
		public string message;
		public bool retriable;
	}

	[Serializable]
	public class MessageRailRegistrationResponse
	{
		public string playerId;
		public bool success;
		public string message;
	}

	/// <summary>
	/// Shared wire vocabulary for the message-rail federation contract, so the SDK-side federation and
	/// the Beamable backend agree on the exact strings. The backend mirrors these values
	/// (<c>RailReconcile.OverCapacityStatus</c> and the rendered-payload outreach key).
	/// </summary>
	public static class MessageRailContract
	{
		/// <summary>
		/// Per-player error status a federation returns for recipients it could not accept because a
		/// page exceeded its provider batch limit. Retriable — the backend right-sizes and re-sends.
		/// </summary>
		public const string OverCapacityStatus = "over-capacity";

		/// <summary>
		/// The well-known key a federation embeds into the rendered message/deep-links carrying the
		/// recipient's <see cref="MessageRailRecipient.outreachId"/>, and that the SDK echoes on
		/// Opened/Clicked so the funnel attributes back to the exact recipient.
		/// </summary>
		public const string OutreachKey = "beam_outreach";

		/// <summary>
		/// Ack param a federation sets on <c>MessageRailSendResponse.params</c> to declare how delivery
		/// works for it. Absent means the default: the message is in flight and a receipt may follow.
		/// </summary>
		public const string DeliveryModeKey = "delivery";

		/// <summary>
		/// The federation delivers DURABLY: an accepted message is already in the recipient's possession
		/// (in-game mail lands in a mailbox and waits there), so there is no in-flight state and no
		/// separate receipt will ever arrive. The platform emits <c>Delivered</c> alongside <c>Sent</c>
		/// for such a send rather than leaving the stage structurally zero forever.
		/// <para>Consequence worth surfacing in any UI: for a durable rail Sent and Delivered track ~1:1.
		/// That is honest, not a bug — the two genuinely coincide when there is no transport in between.</para>
		/// </summary>
		public const string DeliveryDurable = "durable";
	}
}
