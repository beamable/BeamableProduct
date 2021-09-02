using System;
using System.Collections.Generic;
using UnityEngine;
using Beamable.Common;
using Beamable.Service;
using Beamable.Serialization;
using Beamable.Api;
using Beamable.Api.Notification;

namespace Beamable.Experimental.Api.Chat
{
    [Serializable]
    public class ChatView
    {
        public readonly List<RoomHandle> roomHandles;

        public RoomHandle GeneralRoom
        {
            get;
            private set;
        }

        public List<RoomHandle> GuildRooms
        {
            get;
            private set;
        }

        public List<RoomHandle> DirectMessageRooms
        {
            get;
            private set;
        }

        public ChatView()
        {
            this.roomHandles = new List<RoomHandle>();
        }

        public void Update(List<RoomInfo> roomInfo)
        {
            HashSet<string> remove = new HashSet<string>();
            foreach (var handle in roomHandles)
            {
                var room = roomInfo.Find(info => info.id == handle.Id);
                if (room == null)
                {
                    remove.Add(handle.Id);
                    handle.Terminate();
                }
            }
            roomHandles.RemoveAll(handle => remove.Contains(handle.Id));

            foreach (var info in roomInfo)
            {
                var room = roomHandles.Find(handle => handle.Id == info.id);
                if (room == null)
                {
                    roomHandles.Add(new RoomHandle(info));
                }
            }

            GeneralRoom = roomHandles.Find(room => room.Name.StartsWith("general"));
            GuildRooms = roomHandles.FindAll(room => room.Name.StartsWith("group"));
            DirectMessageRooms = roomHandles.FindAll(room => room.Name.StartsWith("direct"));
        }
    }

    [Serializable]
    public class RoomHandle
    {
        private const string ChatEvent = "CHAT.RECEIVED";

        public readonly string Id;
        public readonly string Name;
        public readonly bool KeepSubscribed;
        public readonly List<long> Players;
        public readonly List<Message> Messages;

        public bool ShowPlayerList => Players == null;
        public bool IsSubscribed
        {
            get
            {
                if (_subscribe == null)
                    return false;
                else
                    return _subscribe.IsCompleted;
            }
        }

        public Action OnRemoved;
        public Action<Message> OnMessageReceived;

        private Promise<Unit> _subscribe;

        public RoomHandle(RoomInfo room)
        {
            this.Id = room.id;
            this.Name = room.name;
            this.KeepSubscribed = room.keepSubscribed;
            this.Players = room.players;
            this.Messages = new List<Message>();

            if (KeepSubscribed)
            {
                Subscribe();
            }
        }

        public Promise<Unit> Subscribe()
        {
            if (_subscribe != null)
            {
                return _subscribe;
            }

            var promise = new Promise<Unit>();
            _subscribe = promise.FlatMap(_ => {
                return LoadHistory();
            });

            var pubnub = ServiceManager.Resolve<PlatformService>().PubnubSubscriptionManager;
            pubnub.EnqueueOperation(new PubNubOp(PubNubOp.PNO.OpSubscribe, Id, () =>
            {
                ServiceManager.Resolve<PlatformService>().Notification.Subscribe(ChatEvent, OnChatEvent);
                promise.CompleteSuccess(PromiseBase.Unit);
            }), shouldRunNextOp: true);

            return _subscribe;
        }

        public Promise<Unit> Unsubscribe()
        {
            var promise = new Promise<Unit>();
            var pubnub = ServiceManager.Resolve<PlatformService>().PubnubSubscriptionManager;
            pubnub.EnqueueOperation(new PubNubOp(PubNubOp.PNO.OpUnsubscribe, Id, () =>
            {
                _subscribe = null;
                ServiceManager.Resolve<PlatformService>().Notification.Unsubscribe(ChatEvent, OnChatEvent);
                promise.CompleteSuccess(PromiseBase.Unit);
            }), shouldRunNextOp: true);

            return promise;
        }

        public Promise<Unit> LeaveRoom()
        {
            return ServiceManager.Resolve<PlatformService>().Chat.LeaveRoom(Id).Map(_ => PromiseBase.Unit);
        }

        public Promise<Unit> SendMessage(string message)
        {
            return ServiceManager.Resolve<PlatformService>().Chat.SendMessage(Id, message).Map(_ => PromiseBase.Unit);
        }

        public void Terminate()
        {
            Unsubscribe().Then(_ =>
            {
                OnRemoved?.Invoke();
            });
        }

        private Promise<Unit> LoadHistory()
        {
            //TODO: Fetch this another way
            var pubnub = ServiceManager.Resolve<PlatformService>().PubnubSubscriptionManager;

            var promise = new Promise<Unit>();
            pubnub.LoadChannelHistory(Id, 50,
               pubnubMessages =>
               {
                   Messages.Clear();
                   foreach (var message in pubnubMessages)
                   {
                       Messages.Add(ToMessage(message));
                   }

                   promise.CompleteSuccess(PromiseBase.Unit);
               },
               error =>
               {
                   Debug.LogError(error.Message);
                   promise.CompleteError(new ErrorCode(error.StatusCode));
               }
            );

            return promise;
        }

        private void OnChatEvent(object payload)
        {
            var message = ToMessage(payload);
            if (message.roomId == Id)
            {
                bool foundMessage = Messages.Exists(m => m.messageId == message.messageId);
                if (!foundMessage)
                {
                    Messages.Add(message);
                    OnMessageReceived?.Invoke(message);
                }
            }
        }

        private Message ToMessage(object payload)
        {
            var result = new Message();
            var data = payload as IDictionary<string, object>;
            JsonSerializable.Deserialize(result, data, JsonSerializable.ListMode.kReplace);

            return result;
        }
    }
}
