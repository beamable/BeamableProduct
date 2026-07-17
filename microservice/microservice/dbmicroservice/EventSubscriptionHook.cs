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
    private IMicroserviceArgs _args;
    public HashSet<string> EventNames { get; set; } = new HashSet<string>();
    public HashSet<string> UniqueBindings { get; set; } = new HashSet<string>();

    public DefaultEventSubscriptionConfiguration(IMicroserviceArgs args)
    {
        _args = args;
    }
    
    public void AddSubscription(string evtName, bool toAll)
    {
        EventNames.Add(evtName);
        if (!toAll)
        {
            UniqueBindings.Add(evtName);
        }

        if (_args.MaxUniqueEventBindingCount != 0 && _args.MaxUniqueEventBindingCount < UniqueBindings.Count)
        {
            throw new InvalidOperationException(
                "Exceeded soft unique event binding count. Reach out to Beamable, or increase env var BEAMABLE_MAX_UNIQUE_EVENT_BINDING_COUNT ");
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


public static class EventExtensions
{
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, string evt, bool toAll,
        Action<IUserScope, T> handler)
    {
        return HandleEvent<T>(builder, evt, toAll, (scope, payload) =>
        {
            handler(scope, payload);
            return Task.CompletedTask;
        });
    }
    
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, string evt, bool toAll,
        Action<T> handler)
    {
        return HandleEvent<T>(builder, evt, toAll, (_, payload) =>
        {
            handler(payload);
            return Task.CompletedTask;
        });
    }
    
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, string evt, bool toAll,
        Func<T, Task> handler)
    {
        return HandleEvent<T>(builder, evt, toAll, (_, payload) => handler(payload));
    }
    
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, string evt, bool toAll, Func<IUserScope, T, Task> handler)
    {
        Task SetupCallback(IDependencyProviderScope p)
        {
            var ctx = p.GetService<SocketRequesterContext>();
            var conf = p.GetService<IEventSubscriptionConfiguration>();
            conf.AddSubscription(evt, toAll);
            ctx.Subscribe<T>(evt, handler);
            return Task.CompletedTask;
        }
        
        if (toAll)
        {
            // if the event is going to everyone, then we only want one receiver, otherwise we cannot de-dupe the event. 
            builder.InitializeServices(SetupCallback);
        }
        else
        {
            // if the event is only going to a single consumer, then we can receive it across any of our open connections. 
            IBeamServiceConfig c = builder.Config;
            c.PerServiceInitializers.Add(SetupCallback);
        }
        
        return builder;
    }

    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, CustomEvent<T> evt,
        Action<IUserScope, T> handler)
    {
        return builder.HandleEvent(evt.EventName, evt.ToAll, handler);
    }
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, CustomEvent<T> evt,
        Action<T> handler)
    {
        return builder.HandleEvent(evt.EventName, evt.ToAll, handler);
    }
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, CustomEvent<T> evt,
        Func<T, Task> handler)
    {
        return builder.HandleEvent(evt.EventName, evt.ToAll, handler);
    }
    public static BeamServiceConfigBuilder HandleEvent<T>(this BeamServiceConfigBuilder builder, CustomEvent<T> evt, Func<IUserScope, T, Task> handler)
    {
        return builder.HandleEvent(evt.EventName, evt.ToAll, handler);
    }
}