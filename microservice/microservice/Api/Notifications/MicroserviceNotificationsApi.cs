using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Notifications;
using Beamable.Server.Common;
using Newtonsoft.Json;

namespace Beamable.Server.Api.Notifications
{
    public class MicroserviceNotificationApi : IMicroserviceNotificationsApi
    {
        public IBeamableRequester Requester { get; }
        public RequestContext Context { get; }

        private const string URI_NOTIFICATION_BASE = "/basic/notification";
        
        private readonly string URI_NOTIFICATION_PLAYER = $"{URI_NOTIFICATION_BASE}/player";
        private readonly string URI_NOTIFICATION_GLOBAL = $"{URI_NOTIFICATION_BASE}/global";
        private readonly string URI_NOTIFICATION_CUSTOM = $"{URI_NOTIFICATION_BASE}/custom";
        private readonly string URI_NOTIFICATION_GENERIC = $"{URI_NOTIFICATION_BASE}/generic";
        private readonly string URI_NOTIFICATION_GAME = $"{URI_NOTIFICATION_BASE}/game";
        private readonly string URI_NOTIFICATION_SERVER = $"{URI_NOTIFICATION_BASE}/server";

        public MicroserviceNotificationApi(IBeamableRequester requester, RequestContext context)
        {
            Requester = requester;
            Context = context;
        }

        #region Notify Player
        public Promise<EmptyResponse> NotifyPlayer(long dbid, string subscriptionId, string messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_PLAYER);
        public Promise<EmptyResponse> NotifyPlayer(List<long> dbids, string subscriptionId, string messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_PLAYER);
        public Promise<EmptyResponse> NotifyPlayer<T>(long dbid, string subscriptionId, T messagePayload)=>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_PLAYER);
        public Promise<EmptyResponse> NotifyPlayer<T>(List<long> dbids, string subscriptionId, T messagePayload)=>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_PLAYER);
        #endregion

        #region Notify Global
        public Promise<EmptyResponse> NotifyGlobal(long dbid, string subscriptionId, string messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_GLOBAL);
        public Promise<EmptyResponse> NotifyGlobal(List<long> dbids, string subscriptionId, string messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_GLOBAL);
        public Promise<EmptyResponse> NotifyGlobal<T>(long dbid, string subscriptionId, T messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_GLOBAL);
        public Promise<EmptyResponse> NotifyGlobal<T>(List<long> dbids, string subscriptionId, T messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_GLOBAL);
        #endregion

        #region Notify Custom
        public Promise<EmptyResponse> NotifyCustom(long dbid, string subscriptionId, string messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_CUSTOM);
        public Promise<EmptyResponse> NotifyCustom(List<long> dbids, string subscriptionId, string messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_CUSTOM);
        public Promise<EmptyResponse> NotifyCustom<T>(long dbid, string subscriptionId, T messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_CUSTOM);
        public Promise<EmptyResponse> NotifyCustom<T>(List<long> dbids, string subscriptionId, T messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_CUSTOM);
        #endregion

        #region Notify Generic
        public Promise<EmptyResponse> NotifyGeneric(long dbid, string subscriptionId, string messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_GENERIC);
        public Promise<EmptyResponse> NotifyGeneric(List<long> dbids, string subscriptionId, string messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_GENERIC);
        public Promise<EmptyResponse> NotifyGeneric<T>(long dbid, string subscriptionId, T messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_GENERIC);
        public Promise<EmptyResponse> NotifyGeneric<T>(List<long> dbids, string subscriptionId, T messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_GENERIC);
        #endregion

        #region Notify Game
        public Promise<EmptyResponse> NotifyGame(long dbid, string subscriptionId, string messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_GAME);
        public Promise<EmptyResponse> NotifyGame(List<long> dbids, string subscriptionId, string messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_GAME);
        public Promise<EmptyResponse> NotifyGame<T>(long dbid, string subscriptionId, T messagePayload) =>
	        Notify(dbid, subscriptionId, messagePayload, URI_NOTIFICATION_GAME);
        public Promise<EmptyResponse> NotifyGame<T>(List<long> dbids, string subscriptionId, T messagePayload) =>
	        Notify(dbids, subscriptionId, messagePayload, URI_NOTIFICATION_GAME);
        #endregion

        #region Notify Server
        public Promise<EmptyResponse> NotifyServer(bool toAll, string eventName, string messagePayload)
        {
	        var payload = CreateStringPayload(messagePayload);
	        var jsonSerializedPayload = JsonConvert.SerializeObject(payload, Formatting.None, UnitySerializationSettings.Instance);
	        
	        var body = new
	        {
		        @event = eventName, 
		        payload = messagePayload,
		        toAll = toAll
	        };
	        return Requester.Request<EmptyResponse>(Method.POST, URI_NOTIFICATION_SERVER, body);
        }
        #endregion

        private Promise<EmptyResponse> Notify(long dbid, string subscriptionId, string messagePayload, string uri)
        {
	        var payload = CreateStringPayload(messagePayload);
	        var jsonSerializedPayload = JsonConvert.SerializeObject(payload, Formatting.None, UnitySerializationSettings.Instance);
	        return SendNotificationJson(dbid, subscriptionId, jsonSerializedPayload, uri);
        }
        private Promise<EmptyResponse> Notify(List<long> dbids, string subscriptionId, string messagePayload, string uri)
        {
	        var payload = CreateStringPayload(messagePayload);
	        var jsonSerializedPayload = JsonConvert.SerializeObject(payload, Formatting.None, UnitySerializationSettings.Instance);
	        return SendNotificationJson(dbids, subscriptionId, jsonSerializedPayload, uri);
        }
        private Promise<EmptyResponse> Notify<T>(long dbid, string subscriptionId, T messagePayload, string uri)
        {
	        var jsonSerializedPayload = JsonConvert.SerializeObject(messagePayload, Formatting.None, UnitySerializationSettings.Instance);
	        return SendNotificationJson(dbid, subscriptionId, jsonSerializedPayload, uri);
        }
        private Promise<EmptyResponse> Notify<T>(List<long> dbids, string subscriptionId, T messagePayload, string uri)
        {
	        var jsonSerializedPayload = JsonConvert.SerializeObject(messagePayload, Formatting.None, UnitySerializationSettings.Instance);
	        return SendNotificationJson(dbids, subscriptionId, jsonSerializedPayload, uri);
        }
        
        private PrimitiveStringPayload CreateStringPayload(string message)
        {
            return new PrimitiveStringPayload
            {
                stringValue = message
            };
        }
        private Promise<EmptyResponse> SendNotificationJson(long dbid, string subscriptionId, string json, string uri)
        {
	        var body = new NotificationBody
	        {
		        dbid = dbid, 
		        payload = new NotificationPayload
		        {
			        context = subscriptionId, 
			        messageFull = json
		        }
	        };
            return Requester.Request<EmptyResponse>(Method.POST, uri, body);
        }
        private Promise<EmptyResponse> SendNotificationJson(List<long> dbids, string subscriptionId, string json, string uri)
        {
	        var body = new NotificationBatchedBody
	        {
		        dbids = dbids, 
		        payload = new NotificationPayload
		        {
			        context = subscriptionId, 
			        messageFull = json
		        }
	        };
	        return Requester.Request<EmptyResponse>(Method.POST, uri, body);
        }
    }


    /// <summary>
    /// Notification request format.
    /// </summary>
    [Serializable]
    public class NotificationBody
    {
        public long dbid;
        public NotificationPayload payload;
    }

    /// <summary>
    /// Format of the Notification request when notifying multiple players.
    /// </summary>
    [Serializable]
    public class NotificationBatchedBody
    {
        public List<long> dbids;
        public NotificationPayload payload;
    }

    /// <summary>
    /// Structure representing the expected format for the notification we are sending.
    /// </summary>
    [Serializable]
    public class NotificationPayload
    {
        public string messageFull;
        public string context;
    }
}
