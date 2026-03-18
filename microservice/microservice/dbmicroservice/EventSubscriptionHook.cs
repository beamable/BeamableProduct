using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Content;

namespace Beamable.Server;

public interface IEventSubscriptionHook
{
    Task CreateSubscription(CreateSubscriptionArgs args);
    
}

public interface IEventSubscriptionConfiguration
{
    void AddSubscription(string evtName, bool toAll);
    (string[] EventNames, string[] UniqueBindings) GetSubscriptions();
}

public class DefaultEventSubscriptionConfiguration : IEventSubscriptionConfiguration
{
    public HashSet<string> EventNames { get; set; } = new HashSet<string>();
    public HashSet<string> UniqueBindings { get; set; } = new HashSet<string>();
    
    public void AddSubscription(string evtName, bool toAll)
    {
        EventNames.Add(evtName);
        if (!toAll)
        {
            UniqueBindings.Add(evtName);
        }
    }

    public (string[] EventNames, string[] UniqueBindings) GetSubscriptions()
    {
        return (EventNames.ToArray(), UniqueBindings.ToArray());
    }
}


public class CreateSubscriptionArgs
{
    public IDependencyProvider Provider { get; init; }
    public IMicroserviceAttributes Attributes { get; init; }
    public MicroserviceRequester Requester { get; init; }
    public IEventSubscriptionConfiguration Configuration { get; init; }
}

public class DefaultEventSubscription : IEventSubscriptionHook
{
    public Func<CreateSubscriptionArgs, Task> handler;

    public DefaultEventSubscription()
    {
        handler = DefaultHandler;
    }

    public async Task CreateSubscription(CreateSubscriptionArgs args)
    {
        var task = handler?.Invoke(args) ?? Task.CompletedTask;
        await task;
    }

    public async Task DefaultHandler(CreateSubscriptionArgs args)
    {
        // the user is left to handle it themselves. 
        if (args.Attributes.DisableAllBeamableEvents)
        {
            return;
        }

        // subscribe to all the known events. 
        var data = args.Configuration.GetSubscriptions();
        await args.Requester.InitializeSubscription(data.EventNames, data.UniqueBindings);
    }
}

public class CustomEvent<TPayload>
{
    public string EventName;
    public bool ToAll;

    public CustomEvent(string eventName, bool toAll)
    {
        EventName = eventName;
        ToAll = toAll;
    }
}

public static class StandardBeamableEvents
{
    public static readonly ContentRefreshEvent ContentRefreshEvent = ContentRefreshEvent.Instance;
    public static readonly RealmConfigUpdateEvent RealmConfigUpdateEvent = RealmConfigUpdateEvent.Instance;
}

public class ContentRefreshEvent : CustomEvent<ContentManifestEvent>
{
    public static ContentRefreshEvent Instance { get; } = new ContentRefreshEvent();
    private ContentRefreshEvent() : base(Constants.Features.Services.CONTENT_UPDATE_EVENT, true)
    {
    }
}


public class RealmConfigUpdateEvent : CustomEvent<GetRealmConfigResponse>
{
    public static RealmConfigUpdateEvent Instance { get; } = new RealmConfigUpdateEvent();
    private RealmConfigUpdateEvent() : base(Constants.Features.Services.REALM_CONFIG_UPDATE_EVENT, true)
    {
    }
}


public static class EventExtensions
{
    public delegate void EventHandler<T>(T payload);
    
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, string evt, bool toAll, EventHandler<T> handler)
    {
        void SetupCallback(IDependencyProviderScope p)
        {
            var ctx = p.GetService<SocketRequesterContext>();
            var conf = p.GetService<IEventSubscriptionConfiguration>();
            conf.AddSubscription(evt, toAll);
            ctx.Subscribe<T>(evt, data =>
            {
                handler?.Invoke(data);
            });
        }
        
        if (toAll)
        {
            // if the event is going to everyone, then we only want one receiver, otherwise we cannot de-dupe the event. 
            builder.InitializeServices(SetupCallback);
        }
        else
        {
            // if the event is only going to a single consumer, then we can receive it across any of our open connections. 
            builder.SetupConnection(SetupCallback);
        }
        
        return builder;
    }

    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, CustomEvent<T> evt, EventHandler<T> handler)
    {
        return builder.HandleEvent(evt.EventName, evt.ToAll, handler);
    }
}