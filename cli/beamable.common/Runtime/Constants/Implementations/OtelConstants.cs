namespace Beamable.Common
{
    public static partial class Constants
    {
        public static partial class Features
        {
            public static partial class Otel
            {
                public const string ENV_COLLECTOR_HOST = "BEAM_COLLECTOR_DISCOVERY_HOST";
                public const string ENV_COLLECTOR_PORT = "BEAM_COLLECTOR_DISCOVERY_PORT";
                public const string ENV_COLLECTOR_PORT_DEFAULT_VALUE = "8688"; // some random port number :shrug:
                public const string ENV_COLLECTOR_HOST_DEFAULT_VALUE = "127.0.0.1"; // loopback

                public const string ENV_COLLECTOR_CLICKHOUSE_ENDPOINT = "BEAM_CLICKHOUSE_ENDPOINT";
                public const string ENV_COLLECTOR_CLICKHOUSE_USERNAME = "BEAM_CLICKHOUSE_USERNAME";
                public const string ENV_COLLECTOR_CLICKHOUSE_PASSWORD = "BEAM_CLICKHOUSE_PASSWORD";
                
                
                public const string DATADOG_PARENT_TRACE_ID_HEADER = "x-datadog-parent-id";
                public const string DATADOG_TRACE_ID_HEADER = "x-datadog-trace-id";
                
                public const string ATTR_REQ_BEAM_PARENT_TRACE_ID = "beam.connection.request.root_trace_id";
                public const string ATTR_REQ_BEAM_PARENT_SPAN_ID = "beam.connection.request.parent_trace_id";

                /// <summary>
                /// When a callable-request is in flight, the path of the callable
                /// </summary>
                public const string ATTR_REQ_PATH = "beam.connection.request.path";
                public const string ATTR_REQ_CLIENT_TYPE = "beam.connection.request.client.type";
                public const string ATTR_REQ_CLIENT_SDK_VERSION = "beam.connection.request.client.sdk_version";
                public const string ATTR_REQ_CLIENT_VERSION = "beam.connection.request.client.version";
                public const string ATTR_REQ_CLIENT_GAME_VERSION = "beam.connection.request.client.game_version";
                
                /// <summary>
                /// The player id that initiated the request
                /// </summary>
                public const string ATTR_REQ_PLAYER_ID = "beam.connection.request.player_id";
                
                public const string ATTR_REQ_CONTEXT_ID = "beam.connection.request.request_id";

                public const string ATTR_REQ_CONN_CONTEXT_ID = "beam.connection.request.connection_request_id";
                
                /// <summary>
                /// A tag used to identify the type of source providing data,
                ///  for now, always "microservice".
                /// But in the future, could be "UnityEditor" or "UnityRuntime" for example.
                /// </summary>
                public const string ATTR_SOURCE = "beam.source";
                
                /// <summary>
                /// A UUID to identify the individual connection within an application.
                /// </summary>
                public const string ATTR_CONNECTION_ID = "beam.connection.id";
                
                public const string ATTR_TRACE_LEVEL = "beam.trace.level";
                
                /// <summary>
                /// The customer id
                /// </summary>
                public const string ATTR_CID = "beam.cid";
                
                /// <summary>
                /// The project id
                /// </summary>
                public const string ATTR_PID = "beam.pid";
                
                /// <summary>
                /// The player id of the user that started the service, or 0 if remote. 
                /// </summary>
                public const string ATTR_AUTHOR = "beam.owner_id";
                
                /// <summary>
                /// The routing key the Microservice used to register itself
                /// </summary>
                public const string ATTR_ROUTING_KEY = "beam.routing_key";
                
                /// <summary>
                /// The SDK version publishing the data
                /// </summary>
                public const string ATTR_SDK_VERSION = "beam.sdk_version";
                
                
                
                
                public const string METER_NAME = "Beamable.Service.Core";
                
                public const string TRACE_REQUEST = "beam.request.outbound";
                public const string TRACE_REQUEST_SEND = "beam.request.outbound.send";
                
                public const string TRACE_WS = "beam.request.inbound";
                public const string TRACE_WS_BEAM = "beam.request.inbound.beam";
                public const string TRACE_WS_CLIENT = "beam.request.inbound.client";
                
                public const string TRACE_CONSTRUCT_CTX = "beam.request.context";

            }
        }
    }
}