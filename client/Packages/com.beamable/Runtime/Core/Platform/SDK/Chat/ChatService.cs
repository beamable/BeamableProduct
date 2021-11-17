using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Chat
{
	public class ChatSubscription : PlatformSubscribable<GetMyRoomsResponse, ChatView>
	{
		private const string SERVICE = "chatV2";
		private readonly ChatView view = new ChatView();

		private const string GroupMembershipEvent = "GROUP.MEMBERSHIP";
		private bool _group_subscribed = false;

		public ChatSubscription(IPlatformService platform, IBeamableRequester requester) : base(
			platform, requester, SERVICE)
		{
		}

		protected override void OnRefresh(GetMyRoomsResponse data)
		{
			// Subscribe for re-syncs
			if (!_group_subscribed)
			{
				_group_subscribed = true;
				platform.Notification.Subscribe(GroupMembershipEvent, _ =>
				{
					Refresh();
				});
			}

			view.Update(data.rooms);
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
		private readonly PlatformService _platform;
		private IBeamableRequester _requester;
		private const string BaseUri = "/object/chatV2";

		public ChatSubscription Subscribable
		{
			get;
		}

		public ChatService(PlatformService platform, IBeamableRequester requester)
		{
			Subscribable = new ChatSubscription(platform, requester);
			_platform = platform;
			_requester = requester;
		}

		public Promise<Message> SendMessage(string roomId, string message)
		{
			return _requester.Request<SendChatResponse>(
				Method.POST,
				string.Format("{0}/{1}/messages", BaseUri, _platform.User.id),
				new SendChatRequest(roomId, message)
			).Map(response => response.message);
		}

		public Promise<List<RoomInfo>> GetMyRooms()
		{
			return _requester.Request<GetMyRoomsResponse>(
				Method.GET,
				string.Format("{0}/{1}/rooms", BaseUri, _platform.User.id)
			).Map(response => response.rooms);
		}

		public Promise<RoomInfo> CreateRoom(string roomName, bool keepSubscribed, List<long> players)
		{
			return _requester.Request<CreateRoomResponse>(
				Method.POST,
				string.Format("{0}/{1}/rooms", BaseUri, _platform.User.id),
				new CreateRoomRequest(roomName, keepSubscribed, players)
			).Map(response => response.room);
		}

		public Promise<EmptyResponse> LeaveRoom(string roomId)
		{
			return _requester.Request<EmptyResponse>(
				Method.DELETE,
				string.Format("{0}/{1}/rooms?roomId={2}", BaseUri, _platform.User.id, roomId)
			);
		}

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
		public string id;
		public string name;
		public bool keepSubscribed;
		public List<long> players;
	}
}
