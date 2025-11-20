// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

namespace Beamable.Common
{
    public static partial class Constants
    {
        public static partial class Features
        {
            public static partial class Otel
            {
                public const string ENV_CLI_DISABLE_TELEMETRY = "BEAM_NO_TELEMETRY";
                public const string ENV_CLI_AUTO_SETUP_TELEMETRY = "BEAM_AUTO_SETUP_TELEMETRY";
                public const string ENV_CLI_RUNNING_ON_DOCKER = "DOTNET_RUNNING_IN_CONTAINER";

                public static bool CliTracesEnabled() =>
                    // if the disable env var is set, we don't setup otel
                    string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(ENV_CLI_DISABLE_TELEMETRY));
                
                public static bool CliAutoSetupTelemetryEnabled() =>
	                // if the env var is set it auto setup telemetry
	                !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(ENV_CLI_AUTO_SETUP_TELEMETRY));

                public static bool CliAutoSetupTelemetryAccept()
                {
	                // if the env var is set it auto setup telemetry
	                // true will accept
	                // false will reject
	                string autoSetupTelemetry =  System.Environment.GetEnvironmentVariable(ENV_CLI_AUTO_SETUP_TELEMETRY);
	                if (bool.TryParse(autoSetupTelemetry, out bool result))
	                {
		                return result;
	                }

	                return false;
                }
	              
                
                public static bool CliRunningOnDockerContainer() =>
	                // if the env var is set it is running on a docker container.
	                !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(ENV_CLI_RUNNING_ON_DOCKER));
                

                public const long MAX_OTEL_TEMP_DIR_SIZE = 1024 * 1024 * 500; // Equivalent to 500mb worth of space

                public const string ENV_COLLECTOR_PORT = "BEAM_COLLECTOR_DISCOVERY_PORT";
                public const string ENV_COLLECTOR_PORT_DEFAULT_VALUE = "8688"; // some random port number :shrug:

                public const string ENV_COLLECTOR_CLICKHOUSE_ENDPOINT = "BEAM_CLICKHOUSE_ENDPOINT";
                public const string ENV_COLLECTOR_CLICKHOUSE_USERNAME = "BEAM_CLICKHOUSE_USERNAME";
                public const string ENV_COLLECTOR_CLICKHOUSE_PASSWORD = "BEAM_CLICKHOUSE_PASSWORD";

                public const string ENV_BEAM_CLICKHOUSE_PROCESSOR_TIMEOUT = "BEAM_CLICKHOUSE_PROCESSOR_TIMEOUT";
                public const string BEAM_CLICKHOUSE_PROCESSOR_TIMEOUT = "5s";

                public const string ENV_BEAM_CLICKHOUSE_PROCESSOR_BATCH_SIZE = "BEAM_CLICKHOUSE_PROCESSOR_BATCH_SIZE";
                public const string BEAM_CLICKHOUSE_PROCESSOR_BATCH_SIZE = "5000";

                public const string ENV_BEAM_CLICKHOUSE_EXPORTER_TIMEOUT = "BEAM_CLICKHOUSE_EXPORTER_TIMEOUT";
                public const string BEAM_CLICKHOUSE_EXPORTER_TIMEOUT = "5s";

                public const string ENV_BEAM_CLICKHOUSE_EXPORTER_QUEUE_SIZE = "BEAM_CLICKHOUSE_EXPORTER_QUEUE_SIZE";
                public const string BEAM_CLICKHOUSE_EXPORTER_QUEUE_SIZE = "1000";

                public const string ENV_BEAM_CLICKHOUSE_EXPORTER_RETRY_ENABLED = "BEAM_CLICKHOUSE_EXPORTER_RETRY_ENABLED";
                public const string BEAM_CLICKHOUSE_EXPORTER_RETRY_ENABLED = "true";

                public const string ENV_BEAM_CLICKHOUSE_EXPORTER_RETRY_INITIAL_INTERVAL = "BEAM_CLICKHOUSE_EXPORTER_RETRY_INITIAL_INTERVAL";
                public const string BEAM_CLICKHOUSE_EXPORTER_RETRY_INITIAL_INTERVAL = "5s";

                public const string ENV_BEAM_CLICKHOUSE_EXPORTER_RETRY_MAX_INTERVAL = "BEAM_CLICKHOUSE_EXPORTER_RETRY_MAX_INTERVAL";
                public const string BEAM_CLICKHOUSE_EXPORTER_RETRY_MAX_INTERVAL = "30s";

                public const string ENV_BEAM_CLICKHOUSE_EXPORTER_RETRY_MAX_ELAPSED_TIME = "BEAM_CLICKHOUSE_EXPORTER_RETRY_MAX_ELAPSED_TIME";
                public const string BEAM_CLICKHOUSE_EXPORTER_RETRY_MAX_ELAPSED_TIME = "300s";

                
                
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
                ///  for now, it's either "microservice" or "cli".
                /// </summary>
                public const string ATTR_SOURCE = "beam.source";

                /// <summary>
                /// A tag used to identify which engine is using the CLI, either "unity" or "unreal"
                /// </summary>
                public const string ATTR_ENGINE_SOURCE = "beam.engine.source";

                /// <summary>
                /// A tag used tp identify the version of Beamable SDK calling the CLI
                /// </summary>
                public const string ATTR_ENGINE_SDK_VERSION = "beam.engine.source.sdk_version";

                /// <summary>
                /// A tag used to identify the engine version that is running and calling the CLI
                /// </summary>
                public const string ATTR_ENGINE_VERSION = "beam.engine.source.engine_version";

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
                /// The player email of the user that started the service, or blank if remote. 
                /// </summary>
                public const string ATTR_AUTHOR_EMAIL = "beam.owner_email";
                
                /// <summary>
                /// The routing key the Microservice used to register itself
                /// </summary>
                public const string ATTR_ROUTING_KEY = "beam.routing_key";
                
                /// <summary>
                /// The SDK version publishing the data
                /// </summary>
                public const string ATTR_SDK_VERSION = "beam.sdk_version";
                
                
                public const string METER_SERVICE_NAME = "Beamable.Service.Core";
                public const string METER_CLI_NAME = "Beamable.Cli";
                
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
