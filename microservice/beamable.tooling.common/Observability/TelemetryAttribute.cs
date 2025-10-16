using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Common.Util;
using Beamable.Tooling.Common.OpenAPI;
using Otel = Beamable.Common.Constants.Features.Otel;

namespace Beamable.Server
{
    public enum TelemetryImportance
    {
        /// <summary>
        /// This type of information must always be included.
        /// Without essential information, downstream systems can break.
        /// </summary>
        ESSENTIAL = 30,
        
        /// <summary>
        /// Errors, messages, logic.
        /// Most telemetry is informational.
        /// </summary>
        INFO = 20,
        
        /// <summary>
        /// Used for advanced debugging. Not meant for everyday use. 
        /// </summary>
        VERBOSE = 10
    }

    public enum TelemetryAttributeType
    {
        STRING, 
        LONG
    }

    [Flags]
    public enum TelemetryAttributeSource
    {
        NONE = 0,
        RESOURCE = 1, 
        CONNECTION = 2,
        REQUEST = 4
    }
    
    public struct TelemetryAttribute
    {
        public string name;
        // TODO: place the description in the open-api-document. 
        public string description;
        public string stringValue;
        public long longValue;
        public TelemetryImportance level;
        public TelemetryAttributeType type;

        public static TelemetryAttribute String(string name, string description, string value,
            TelemetryImportance level = TelemetryImportance.INFO)
        {
            return new TelemetryAttribute
            {
                name = name, 
                description = description,
                stringValue = value, 
                level = level,
                type = TelemetryAttributeType.STRING
            };
        }
        
        
        public static TelemetryAttribute Long(string name, string description, long value,
            TelemetryImportance level = TelemetryImportance.INFO)
        {
            return new TelemetryAttribute
            {
                name = name, 
                description = description,
                longValue = value, 
                level = level,
                type = TelemetryAttributeType.LONG
            };
        }

    }

    public static class TelemetryAttributes
    {
        public static TelemetryAttribute Cid(string cid) => TelemetryAttribute.String(
            name: Otel.ATTR_CID, 
            description: "The customer id",
            cid, TelemetryImportance.ESSENTIAL);
        
        public static TelemetryAttribute Pid(string pid) => TelemetryAttribute.String(
            name: Otel.ATTR_PID, 
            description: "The project id",
            pid, TelemetryImportance.ESSENTIAL);
        
        public static TelemetryAttribute Source(string src) => TelemetryAttribute.String(
            name: Otel.ATTR_SOURCE, 
            description: "Used to identify what type of process generated the telemetry",
            src, TelemetryImportance.ESSENTIAL);
        
        public static TelemetryAttribute OwnerPlayerId(long playerId) => TelemetryAttribute.Long(
            name: Otel.ATTR_AUTHOR, 
            description: "The player id of the user that started the service, or 0 if remote. ",
            playerId);
        
        public static TelemetryAttribute OwnerPlayerEmail(string email) => TelemetryAttribute.String(
            name: Otel.ATTR_AUTHOR_EMAIL, 
            description: "The player email of the user that started the service, or blank if remote. ",
            email);
        
        public static TelemetryAttribute RequestPlayerId(long playerId) => TelemetryAttribute.Long(
            name: Otel.ATTR_REQ_PLAYER_ID, 
            description: "The player id of the user that started the request, or 0 if no user started the request. ",
            playerId);

        
        public static TelemetryAttribute RoutingKey(string routingKey) => TelemetryAttribute.String(
            name: Otel.ATTR_ROUTING_KEY, 
            description: "The routing key the Microservice used to register itself. ",
            routingKey);
        
        public static TelemetryAttribute SdkVersion() => TelemetryAttribute.String(
            name: Otel.ATTR_SDK_VERSION, 
            description: "The SDK version publishing the data. ",
            BeamAssemblyVersionUtil.GetVersion<Promise>());
        
        
        public static TelemetryAttribute ConnectionId(string connectionId) => TelemetryAttribute.String(
            name: Otel.ATTR_CONNECTION_ID, 
            description: "A UUID to identify the individual connection within an application.",
            connectionId);
        
        public static TelemetryAttribute RequestId(string requestId) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_CONTEXT_ID, 
            description: "A UUID to identify the request id. ",
            requestId);
        
        public static TelemetryAttribute ConnectionRequestId(long requestId) => TelemetryAttribute.Long(
            name: Otel.ATTR_REQ_CONN_CONTEXT_ID, 
            description: "The ordered numeric request id of a specific connection to Beamable. ",
            requestId);
        
        public static TelemetryAttribute RequestPath(string path) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_PATH, 
            description: "The relative path of the `[Callable]` being invoked through a request. ",
            path);
        
        public static TelemetryAttribute RequestBeamRootTrace(string trace) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_BEAM_PARENT_TRACE_ID, 
            description: "The top level trace id from Beamable's internal observability stack. ",
            trace);
        
        public static TelemetryAttribute RequestBeamParentTrace(string trace) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_BEAM_PARENT_SPAN_ID, 
            description: "The most recent parent trace id from Beamable's internal observability stack. ",
            trace);
        
        public static TelemetryAttribute RequestClientType(string clientType) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_CLIENT_TYPE, 
            description: "The type of client that issued the request. ",
            clientType);
        
        public static TelemetryAttribute RequestClientSdkVersion(string sdkVersion) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_CLIENT_SDK_VERSION, 
            description: "The Beamable SDK version used by the client that issued the request. ",
            sdkVersion);
        
        public static TelemetryAttribute RequestClientVersion(string clientVersion) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_CLIENT_VERSION, 
            description: "The version of the client that issued the request. ",
            clientVersion);
        
        public static TelemetryAttribute RequestClientGameVersion(string gameVersion) => TelemetryAttribute.String(
            name: Otel.ATTR_REQ_CLIENT_GAME_VERSION, 
            description: "The application version of the client that issued the request. ",
            gameVersion);
        
        public static TelemetryAttribute TraceLevel(TelemetryImportance importance) => TelemetryAttribute.String(
            name: Otel.ATTR_TRACE_LEVEL, 
            description: "A level of importance for a trace",
            importance.ToString());

    }

    public class TelemetryAttributeCollection
    {
        public List<TelemetryAttribute> attributes = new List<TelemetryAttribute>();

        public void Add(TelemetryAttribute attr)
        {
            if (attr.type == TelemetryAttributeType.STRING && attr.stringValue == null) return;
            attributes.Add(attr);
        }

        public TelemetryAttributeCollection With(TelemetryAttribute attr)
        {
            attributes.Add(attr);
            return this;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var results = new Dictionary<string, object>();
            return ToDictionary(results);
        }
        public Dictionary<string, object> ToDictionary(Dictionary<string, object> results)
        {
            results ??= new Dictionary<string, object>();
            foreach (var attr in attributes)
            {
                if (string.IsNullOrEmpty(attr.name)) continue;
                switch (attr.type)
                {
                    case TelemetryAttributeType.LONG:
                        results[attr.name] = attr.longValue;
                        break;
                    default:
                        results[attr.name] = attr.stringValue;
                        break;
                }
            }
            return results;
        }
    }


    public interface IDefaultAttributeContext
    {
        public IMicroserviceArgs Args { get; }
        public TelemetryAttributeCollection Attributes { get; }
    }
    
    public class DefaultAttributeContext : IDefaultAttributeContext
    {
        public IMicroserviceArgs Args { get; set; }
        public TelemetryAttributeCollection Attributes { get; set; }
    }

    public interface IConnectionAttributeContext : IDefaultAttributeContext
    {
        public string ConnectionId { get; }
    }
    
    public class ConnectionAttributeContext : IConnectionAttributeContext
    {
        public IMicroserviceArgs Args { get; set; }
        public TelemetryAttributeCollection Attributes { get; set; }
        public string ConnectionId { get; set; }
    }

    public interface IRequestAttributeContext : IConnectionAttributeContext
    {
        public RequestContext Request { get; }
    }

    public class RequestAttributeContext : IRequestAttributeContext
    {
        public IMicroserviceArgs Args { get; set; }
        public TelemetryAttributeCollection Attributes { get; set; }
        public string ConnectionId { get; set; }
        public RequestContext Request { get; set; }
    }
    
    public interface ITelemetryAttributeProvider
    {
        List<TelemetryAttributeDescriptor> GetDescriptors();
        
        void CreateDefaultAttributes(IDefaultAttributeContext ctx);
        void CreateConnectionAttributes(IConnectionAttributeContext ctx);
        void CreateRequestAttributes(IRequestAttributeContext ctx);
    }

    public static class TelemetryProviderExtensions
    {
        public static Dictionary<string, object> CreateConnectionAttributes(this SingletonDependencyList<ITelemetryAttributeProvider> providers, IMicroserviceArgs args, string connectionId)
        {
            var telemetryCtx = new ConnectionAttributeContext
            {
                Attributes = new TelemetryAttributeCollection(),
                Args = args,
                ConnectionId = connectionId
            };
            foreach (var provider in providers.Elements)
            {
                provider.CreateConnectionAttributes(telemetryCtx);
            }

            return telemetryCtx.Attributes.ToDictionary();
        }
        
        public static TelemetryAttributeCollection CreateRequestAttributes(this SingletonDependencyList<ITelemetryAttributeProvider> providers, IMicroserviceArgs args, RequestContext ctx, string connectionId)
        {
            var requestTelemetryContext = new RequestAttributeContext
            {
                Attributes = new TelemetryAttributeCollection(),
                Args = args,
                Request = ctx,
                ConnectionId = connectionId
            };
            foreach (var provider in providers.Elements)
            {
                try
                {
                    provider.CreateRequestAttributes(requestTelemetryContext);
                }
                catch (Exception ex)
                {
                    BeamableZLoggerProvider.Instance.Error("Failed to add request attributes");
                    BeamableZLoggerProvider.Instance.Error(ex);
                }
            }

            return requestTelemetryContext.Attributes;
        }
    }

    public class BeamStandardTelemetryAttributeProvider : ITelemetryAttributeProvider
    {
        public List<TelemetryAttributeDescriptor> GetDescriptors()
        {
            var resourceBased = new List<TelemetryAttribute>()
            {
                TelemetryAttributes.OwnerPlayerId(0),
                TelemetryAttributes.OwnerPlayerEmail(""),
                TelemetryAttributes.Pid(null),
                TelemetryAttributes.SdkVersion(),
                TelemetryAttributes.RoutingKey(null),
                TelemetryAttributes.Source(null)
            }.Select(x => x.FromAttribute(TelemetryAttributeSource.RESOURCE));

            
            var connectionBased = new List<TelemetryAttribute>()
            {
                TelemetryAttributes.ConnectionId(null)
            }.Select(x => x.FromAttribute(TelemetryAttributeSource.CONNECTION));

            
            var requestBased = new List<TelemetryAttribute>()
            {
                TelemetryAttributes.ConnectionId(null),
                TelemetryAttributes.RequestPlayerId(0),
                TelemetryAttributes.RequestId(null),
                TelemetryAttributes.ConnectionRequestId(0),
                TelemetryAttributes.RequestPath(null),
                TelemetryAttributes.RequestClientType(null),
                TelemetryAttributes.RequestClientSdkVersion(null),
                TelemetryAttributes.RequestClientVersion(null),
                TelemetryAttributes.RequestClientGameVersion(null),
                TelemetryAttributes.RequestBeamRootTrace(null),
                TelemetryAttributes.RequestBeamParentTrace(null),
            }.Select(x => x.FromAttribute(TelemetryAttributeSource.REQUEST));

            
            var final = resourceBased.ToList();
            final.AddRange(connectionBased.ToList());
            final.AddRange(requestBased.ToList());
            return final;
        }

        public void CreateDefaultAttributes(IDefaultAttributeContext ctx)
        {
            ctx.Attributes.Add(TelemetryAttributes.OwnerPlayerId(ctx.Args.AccountId));
            ctx.Attributes.Add(TelemetryAttributes.OwnerPlayerEmail(ctx.Args.AccountEmail));
            
            ctx.Attributes.Add(TelemetryAttributes.Cid(ctx.Args.CustomerID));
            ctx.Attributes.Add(TelemetryAttributes.Pid(ctx.Args.ProjectName));
            ctx.Attributes.Add(TelemetryAttributes.SdkVersion());
            ctx.Attributes.Add(TelemetryAttributes.RoutingKey(ctx.Args.NamePrefix));
            ctx.Attributes.Add(TelemetryAttributes.Source("microservice"));
        }

        public void CreateConnectionAttributes(IConnectionAttributeContext ctx)
        {
            ctx.Attributes.Add(TelemetryAttributes.ConnectionId(ctx.ConnectionId));
        }

        public void CreateRequestAttributes(IRequestAttributeContext ctx)
        {
            ctx.Attributes.Add(TelemetryAttributes.RequestPlayerId(ctx.Request.UnsafeUserId));
            ctx.Attributes.Add(TelemetryAttributes.RequestId(Guid.NewGuid().ToString()));
            ctx.Attributes.Add(TelemetryAttributes.ConnectionRequestId(ctx.Request.Id));

            var path = ctx.Request.Path;
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("micro_"))
                {
                    path = path.Substring("micro_".Length);
                }

                ctx.Attributes.Add(TelemetryAttributes.RequestPath(path));
            }

            if (!ctx.Request.Headers.TryGetClientType(out var clientType))
            {
                if (ctx.Request.Headers.TryGetValue("Origin", out var origin) && origin.Contains("portal"))
                {
                    ctx.Attributes.Add(TelemetryAttributes.RequestClientType("portal"));
                }
            }
            else
            {
                ctx.Attributes.Add(TelemetryAttributes.RequestClientType(clientType));
            }

            if (ctx.Request.Headers.TryGetBeamableSdkVersion(out var clientSdkVersion))
            {
                ctx.Attributes.Add(TelemetryAttributes.RequestClientSdkVersion(clientSdkVersion));
            }
            
            if (ctx.Request.Headers.TryGetClientEngineVersion(out var clientVersion))
            {
                ctx.Attributes.Add(TelemetryAttributes.RequestClientVersion(clientVersion));
            }

            if (ctx.Request.Headers.TryGetClientGameVersion(out var gameVersion))
            {
                ctx.Attributes.Add(TelemetryAttributes.RequestClientGameVersion(gameVersion));
            }
            
            { // pull the beamable's datadog trace ids off the headers and add them to the otel data. 
                //  this may be useful to correlate customer logs back to Beamable's internal observability stack.
                if (ctx.Request.Headers.TryGetValue(Otel.DATADOG_PARENT_TRACE_ID_HEADER, out var existingTraceId))
                {
                    ctx.Attributes.Add(TelemetryAttributes.RequestBeamRootTrace(existingTraceId));
                }

                if (ctx.Request.Headers.TryGetValue(Otel.DATADOG_TRACE_ID_HEADER, out var existingSpanId))
                {
                    ctx.Attributes.Add(TelemetryAttributes.RequestBeamParentTrace(existingSpanId));
                }
            }
        }
    }
}