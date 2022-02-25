using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Serialization.SmallerJSON;
using Newtonsoft.Json;

namespace Beamable.Server.Api.Notifications;


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
        var notifyRequest = Requester.Request<EmptyResponse>(Method.POST, "/basic/notification/player",
            new PlayerNotification { dbid = dbid, payload = new PlayerNotificationPayload { context = subscriptionId, messageFull = messagePayload } });

        return notifyRequest;
    }

    public Promise<EmptyResponse> NotifyPlayer(List<long> dbids, string subscriptionId, string messagePayload)
    {
        var notifyRequest = Requester.Request<EmptyResponse>(Method.POST, "/basic/notification/player",
            new PlayerBatchNotification() { dbids = dbids, payload = new PlayerNotificationPayload { context = subscriptionId, messageFull = messagePayload } });

        return notifyRequest;
    }

    public Promise<EmptyResponse> NotifyPlayer<T>(long dbid, string subscriptionId, T messagePayload)
    {
        var jsonSerializedPayload = JsonConvert.SerializeObject(messagePayload, Formatting.None);
        return NotifyPlayer(dbid, subscriptionId, jsonSerializedPayload);
    }

    public Promise<EmptyResponse> NotifyPlayer<T>(List<long> dbids, string subscriptionId, T messagePayload)
    {
        var jsonSerializedPayload = JsonConvert.SerializeObject(messagePayload, Formatting.None);
        return NotifyPlayer(dbids, subscriptionId, jsonSerializedPayload);
    }
}