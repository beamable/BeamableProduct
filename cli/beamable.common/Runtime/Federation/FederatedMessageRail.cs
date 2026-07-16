using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	/// <summary>
	/// Federation for the Message Rail "last-mile" delivery. A microservice implements this to
	/// deliver a batch of messages (push / email / in-game) and report a per-player funnel result.
	/// </summary>
	public interface IMessageRailFederation<in T> : IFederation where T : IFederationId, new()
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
}
