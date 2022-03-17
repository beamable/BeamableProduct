namespace Beamable.Common
{
	public static partial class Constants
	{
		public static partial class Features
		{
			public static partial class Services
			{
				public const string COMPONENTS_PATH = Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI + "/Components";

				public const string DEPENDENT_SERVICES_WINDOW_TITLE = "Dependent services";

				public const string PUBLISH = "Publish";
				public const string STOP = "Stop";
				public const string BUILD_DEBUG_PREFIX = "[DEBUG]";
				public const string BUILD_ENABLE_DEBUG = "Enable Debug Tools";
				public const string BUILD_DISABLE_DEBUG = "Disable Debug Tools";

				public const string PROMPT_STARTED_FAILURE = "MICROSERVICE HASN'T STARTED...";
				public const string PROMPT_STOPPED_FAILURE = "MICROSERVICE HASN'T STOPPED...";
				
				public const string REMOTE_ONLY = "Remote Only";

				public const int HEALTH_PORT = 6565;

				public const string UPLOAD_CONTAINER_MESSAGE = "Uploaded container service=[{0}]";
				public const string CONTAINER_ALREADY_UPLOADED_MESSAGE = "Service [{0}] is already deployed at imageId";
				public const string CANT_UPLOAD_CONTAINER_MESSAGE = "Can't upload container service=[{0}]";
				public const string USING_REMOTE_SERVICE_MESSAGE = "Using remote service";

				public static string GetBuildButtonString(bool includeDebugTools, string text) => includeDebugTools
					? $"{BUILD_DEBUG_PREFIX} {text}"
					: text;

				public static class Logs
				{
					public const string READY_FOR_TRAFFIC_PREFIX = "Service ready for traffic.";
					public const string STARTING_PREFIX = "Starting..";
					public const string SCANNING_CLIENT_PREFIX = "Scanning client methods for ";
					public const string REGISTERING_STANDARD_SERVICES = "Registering standard services";
					public const string REGISTERING_CUSTOM_SERVICES = "Registering custom services";
					public const string SERVICE_PROVIDER_INITIALIZED = "Service provider initialized";
					public const string EVENT_PROVIDER_INITIALIZED = "Event provider initialized";
					public const string STORAGE_READY = "Waiting for connections";
				}
			}
		}
	}
}
