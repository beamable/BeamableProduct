using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Chat
{
	public class ChatSubscription : PlatformSubscribable<GetMyRoomsResponse, ChatView>
	{
		private readonly IDependencyProvider _provider;
		private const string SERVICE = "chatV2";
		private readonly ChatView view = new ChatView();

		private const string GroupMembershipEvent = "GROUP.MEMBERSHIP";
		private bool _group_subscribed = false;

		public ChatSubscription(IDependencyProvider provider) : base(provider, SERVICE)
		{
			_provider = provider;
		}

		protected override void OnRefresh(GetMyRoomsResponse data)
		{
			// Subscribe for re-syncs
			if (!_group_subscribed)
			{
				_group_subscribed = true;
				notificationService.Subscribe(GroupMembershipEvent, _ => { Refresh(); });
			}

			view.Update(data.rooms, _provider);
			Notify(view);
		}
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %Chat feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/chat-feature">Chat</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class ChatService : IHasPlatformSubscriber<ChatSubscription, GetMyRoomsResponse, ChatView>
	{
		private readonly IPlatformService _platform;
		private IBeamableRequester _requester;
		private const string BaseUri = "/object/chatV2";

		public ChatSubscription Subscribable { get; }

		public ChatService(IPlatformService platform, IBeamableRequester requester, IDependencyProvider provider)
		{
			Subscribable = new ChatSubscription(provider);
			_platform = platform;
			_requester = requester;
		}

		/// <summary>
		/// Send a message to chat room.
		/// </summary>
		/// <param name="roomId">The room id</param>
		/// <param name="message">The message to send to the room</param>
		/// <returns>A <see cref="Promise"/> containing the sent <see cref="Message"/></returns>
		public Promise<Message> SendMessage(string roomId, string message)
		{
			return _requester.Request<SendChatResponse>(
			   Method.POST,
			   string.Format("{0}/{1}/messages", BaseUri, _platform.User.id),
			   new SendChatRequest(roomId, message)
			).Map(response => response.message);
		}

		/// <summary>
		/// Get the current player's set of <see cref="RoomInfo"/>.
		/// The player can create a new room using the <see cref="CreateRoom"/> method.
		/// </summary>
		/// <returns>A <see cref="Promise"/> containing the player's <see cref="RoomInfo"/></returns>
		public Promise<List<RoomInfo>> GetMyRooms()
		{
			return _requester.Request<GetMyRoomsResponse>(
			   Method.GET,
			   string.Format("{0}/{1}/rooms", BaseUri, _platform.User.id)
			).Map(response => response.rooms);
		}

		/// <summary>
		/// Creates a new private chat room for the current player, and a set of other players.
		/// </summary>
		/// <param name="roomName">A name for the room</param>
		/// <param name="keepSubscribed">When true, the current player will receive messages for the room.</param>
		/// <param name="players">A list of gamertags of other players who will be included in the chat room.</param>
		/// <returns>A <see cref="Promise"/> containing the newly created <see cref="RoomInfo"/></returns>
		public Promise<RoomInfo> CreateRoom(string roomName, bool keepSubscribed, List<long> players)
		{
			return _requester.Request<CreateRoomResponse>(
			   Method.POST,
			   string.Format("{0}/{1}/rooms", BaseUri, _platform.User.id),
			   new CreateRoomRequest(roomName, keepSubscribed, players)
			).Map(response => response.room);
		}

		/// <summary>
		/// Remove the current player from a room
		/// </summary>
		/// <param name="roomId">The room id to leave</param>
		/// <returns>A <see cref="Promise"/> representing the network call.</returns>
		public Promise<EmptyResponse> LeaveRoom(string roomId)
		{
			return _requester.Request<EmptyResponse>(
			   Method.DELETE,
			   string.Format("{0}/{1}/rooms?roomId={2}", BaseUri, _platform.User.id, roomId)
			);
		}

		/// <summary>
		/// Check to see if a piece of text would trigger the Beamable profanity filter.
		/// </summary>
		/// <param name="text">some text</param>
		/// <returns>A <see cref="Promise"/> representing the network call.
		/// If the text contains profanity, the promise will fail with a PlatformRequesterException with an error of "ProfanityFilter" and a status of 400.
		/// </returns>
		public Promise<EmptyResponse> ProfanityAssert(string text)
		{
			return _requester.Request<EmptyResponse>(
			   Method.GET,
			   $"/basic/chat/profanityAssert?text={text}"
			);
		}
	}
	[Serializable]
	public class SendChatRequest
	{
		public string roomId;
		public string content;

		public SendChatRequest(string roomId, string content)
		{
			this.roomId = roomId;
			this.content = content;
		}
	}

	[Serializable]
	public class SendChatResponse
	{
		public Message message;
	}

	[Serializable]
	public class GetMyRoomsResponse
	{
		public List<RoomInfo> rooms;
	}

	[Serializable]
	public class CreateRoomRequest
	{
		public string roomName;
		public bool keepSubscribed;
		public List<long> players;

		public CreateRoomRequest(string roomName, bool keepSubscribed, List<long> players)
		{
			this.roomName = roomName;
			this.keepSubscribed = keepSubscribed;
			this.players = players;
		}
	}

	[Serializable]
	public class CreateRoomResponse
	{
		public RoomInfo room;
	}

	[Serializable]
	public class RoomInfo
	{
		/// <summary>
		/// The id of the room
		/// </summary>
		public string id;

		/// <summary>
		/// The name of the room
		/// </summary>
		public string name;

		/// <summary>
		/// When true, the current player will receive messages from the room
		/// </summary>
		public bool keepSubscribed;

		/// <summary>
		/// A list of gamertags who are in the room
		/// </summary>
		public List<long> players;
	}
}
